using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SemanticDupScan;

internal sealed record ScanOptions(
    string TargetDirectory,
    string OutputDirectory,
    int MinTokens,
    double Threshold,
    int ShingleK,
    int Top,
    bool NormalizeLiterals);

internal sealed record MemberEntry(
    string File,
    string Type,
    string Member,
    string Signature,
    int StartLine,
    int EndLine,
    int TokenCount,
    string NormalizedHash,
    string NormalizedText,
    string RawText)
{
    [System.Text.Json.Serialization.JsonIgnore]
    public string[] Tokens { get; init; } = [];
}

internal sealed record PairLocation(string File, int StartLine, string Type, string Member, string Signature);

internal sealed record ReportPair(int Rank, double Score, bool ExactHash, PairLocation A, PairLocation B);

internal sealed class CanonicalNameMap
{
    private readonly Dictionary<ISymbol, string> bySymbol = new(SymbolEqualityComparer.Default);
    private readonly Dictionary<string, string> byName = new(StringComparer.Ordinal);
    private int next;

    public string ForSymbol(ISymbol symbol) =>
        bySymbol.TryGetValue(symbol, out var name) ? name : bySymbol[symbol] = "v" + next++;

    public string ForName(string identifier) =>
        byName.TryGetValue(identifier, out var name) ? name : byName[identifier] = "v" + next++;
}

internal static class Program
{
    private const int MinHashCount = 64;
    private const int Bands = 16;
    private const int Rows = 4;
    private const ulong FnvOffsetBasis = 14695981039346656037UL;
    private const ulong FnvPrime = 1099511628211UL;

    private static readonly ulong[] Seeds = CreateSeeds();

    private static readonly JsonSerializerOptions CorpusJson = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private static readonly JsonSerializerOptions ReportJson = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
    };

    private static int Main(string[] args)
    {
        var options = ParseArgs(args);

        if (options is null)
        {
            Console.Error.WriteLine("usage: SemanticDupScan <target-dir> [--out <dir>] [--min-tokens N] [--threshold X] [--shingle-k N] [--top N] [--normalize-literals]");
            return 1;
        }

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var files = Directory.EnumerateFiles(options.TargetDirectory, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}") &&
                        !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
            .OrderBy(f => f, StringComparer.Ordinal)
            .ToList();

        if (files.Count == 0)
        {
            Console.Error.WriteLine($"No .cs files found under {options.TargetDirectory}");
            return 1;
        }

        var repoRoot = FindRepositoryRoot(options.TargetDirectory);
        var parseOptions = new CSharpParseOptions(LanguageVersion.Preview);
        var trees = files
            .Select(f => CSharpSyntaxTree.ParseText(File.ReadAllText(f), parseOptions, path: f))
            .ToList();

        var compilation = CSharpCompilation.Create(
            "SemanticDupScan.Target",
            trees,
            FrameworkReferences(),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: true));

        var entries = new List<MemberEntry>();

        foreach (var tree in trees)
        {
            var model = compilation.GetSemanticModel(tree);
            var relativePath = Path.GetRelativePath(repoRoot, tree.FilePath);

            foreach (var node in tree.GetRoot().DescendantNodes())
            {
                var described = Describe(node);

                if (described is null)
                {
                    continue;
                }

                var (body, member, signature) = described.Value;
                var (normalizedText, tokens) = Normalize(body, node, model, options.NormalizeLiterals);
                var lines = node.GetLocation().GetLineSpan();

                entries.Add(new MemberEntry(
                    relativePath,
                    ContainingTypeName(node),
                    member,
                    signature,
                    lines.StartLinePosition.Line + 1,
                    lines.EndLinePosition.Line + 1,
                    tokens.Length,
                    Sha256Hex(normalizedText),
                    normalizedText,
                    body.ToString())
                { Tokens = tokens });
            }
        }

        Directory.CreateDirectory(options.OutputDirectory);
        var corpusPath = Path.Combine(options.OutputDirectory, "corpus.jsonl");
        File.WriteAllLines(corpusPath, entries.Select(e => JsonSerializer.Serialize(e, CorpusJson)));

        var pairs = FindPairs(entries, options);
        var report = pairs
            .Select((p, i) => new ReportPair(i + 1, Math.Round(p.Score, 4), p.Exact, Location(entries[p.A]), Location(entries[p.B])))
            .ToList();

        var reportJsonPath = Path.Combine(options.OutputDirectory, "report.json");
        var reportMdPath = Path.Combine(options.OutputDirectory, "report.md");
        File.WriteAllText(reportJsonPath, JsonSerializer.Serialize(report, ReportJson));
        File.WriteAllText(reportMdPath, RenderMarkdown(report, options));

        stopwatch.Stop();
        Console.WriteLine($"Scanned {files.Count} files, {entries.Count} members ({entries.Count(e => e.TokenCount >= options.MinTokens)} with >= {options.MinTokens} tokens) in {stopwatch.Elapsed.TotalSeconds:F1}s");
        Console.WriteLine($"Corpus: {corpusPath}");
        Console.WriteLine($"Report: {reportJsonPath}, {reportMdPath}");
        Console.WriteLine($"Candidate pairs: {report.Count} ({report.Count(p => p.ExactHash)} exact-hash, {report.Count(p => !p.ExactHash)} near with Jaccard >= {options.Threshold})");
        Console.WriteLine();

        foreach (var pair in report.Take(options.Top))
        {
            var kind = pair.ExactHash ? "EXACT" : "near ";
            Console.WriteLine($"{pair.Rank,3}. {pair.Score:F3} {kind} {pair.A.File}:{pair.A.StartLine}  {pair.A.Type}.{pair.A.Member}");
            Console.WriteLine($"                {pair.A.Signature}");
            Console.WriteLine($"                {pair.B.File}:{pair.B.StartLine}  {pair.B.Type}.{pair.B.Member}");
            Console.WriteLine($"                {pair.B.Signature}");
        }

        if (report.Count > options.Top)
        {
            Console.WriteLine($"... {report.Count - options.Top} more pairs in {reportJsonPath}");
        }

        return 0;
    }

    private static ScanOptions? ParseArgs(string[] args)
    {
        string? target = null;
        string? output = null;
        var minTokens = 40;
        var threshold = 0.80;
        var shingleK = 5;
        var top = 60;
        var normalizeLiterals = false;

        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--out" when i + 1 < args.Length:
                    output = args[++i];
                    break;
                case "--min-tokens" when i + 1 < args.Length && int.TryParse(args[i + 1], out var mt):
                    minTokens = mt;
                    i++;
                    break;
                case "--threshold" when i + 1 < args.Length && double.TryParse(args[i + 1], System.Globalization.CultureInfo.InvariantCulture, out var th):
                    threshold = th;
                    i++;
                    break;
                case "--shingle-k" when i + 1 < args.Length && int.TryParse(args[i + 1], out var sk):
                    shingleK = sk;
                    i++;
                    break;
                case "--top" when i + 1 < args.Length && int.TryParse(args[i + 1], out var tp):
                    top = tp;
                    i++;
                    break;
                case "--normalize-literals":
                    normalizeLiterals = true;
                    break;
                default:
                    if (target is not null || args[i].StartsWith("--", StringComparison.Ordinal))
                    {
                        return null;
                    }

                    target = args[i];
                    break;
            }
        }

        if (target is null || !Directory.Exists(target))
        {
            return null;
        }

        return new ScanOptions(
            Path.GetFullPath(target),
            Path.GetFullPath(output ?? "dupscan-out"),
            minTokens,
            threshold,
            shingleK,
            top,
            normalizeLiterals);
    }

    private static (SyntaxNode Body, string Member, string Signature)? Describe(SyntaxNode node)
    {
        switch (node)
        {
            case MethodDeclarationSyntax m when BodyOf(m.Body, m.ExpressionBody) is { } body:
                return (body, m.Identifier.Text, Sig($"{Mods(m.Modifiers)}{m.ReturnType} {m.Identifier}{m.TypeParameterList}{m.ParameterList}"));
            case ConstructorDeclarationSyntax c when BodyOf(c.Body, c.ExpressionBody) is { } body:
                return (body, $"{c.Identifier}.ctor", Sig($"{Mods(c.Modifiers)}{c.Identifier}{c.ParameterList}"));
            case OperatorDeclarationSyntax o when BodyOf(o.Body, o.ExpressionBody) is { } body:
                return (body, $"operator {o.OperatorToken.Text}", Sig($"{Mods(o.Modifiers)}{o.ReturnType} operator {o.OperatorToken.Text}{o.ParameterList}"));
            case ConversionOperatorDeclarationSyntax v when BodyOf(v.Body, v.ExpressionBody) is { } body:
                return (body, $"{v.ImplicitOrExplicitKeyword.Text} operator {v.Type}", Sig($"{Mods(v.Modifiers)}{v.ImplicitOrExplicitKeyword.Text} operator {v.Type}{v.ParameterList}"));
            case LocalFunctionStatementSyntax f when BodyOf(f.Body, f.ExpressionBody) is { } body:
                return (body, $"{f.Identifier} (local)", Sig($"{f.ReturnType} {f.Identifier}{f.TypeParameterList}{f.ParameterList} [local function]"));
            case AccessorDeclarationSyntax a when BodyOf(a.Body, a.ExpressionBody) is { } body:
                var owner = OwnerName(a);
                return (body, $"{owner}.{a.Keyword.ValueText}", Sig($"{OwnerSignature(a)} {{ {a.Keyword.ValueText} }}"));
            case PropertyDeclarationSyntax p when p.ExpressionBody is not null:
                return (p.ExpressionBody.Expression, $"{p.Identifier}.get", Sig($"{Mods(p.Modifiers)}{p.Type} {p.Identifier} => ..."));
            case IndexerDeclarationSyntax x when x.ExpressionBody is not null:
                return (x.ExpressionBody.Expression, "this[].get", Sig($"{Mods(x.Modifiers)}{x.Type} this{x.ParameterList} => ..."));
            default:
                return null;
        }
    }

    private static SyntaxNode? BodyOf(BlockSyntax? block, ArrowExpressionClauseSyntax? arrow) =>
        (SyntaxNode?)block ?? arrow?.Expression;

    private static string OwnerName(AccessorDeclarationSyntax accessor) =>
        accessor.Parent?.Parent switch
        {
            PropertyDeclarationSyntax p => p.Identifier.Text,
            IndexerDeclarationSyntax => "this[]",
            EventDeclarationSyntax e => e.Identifier.Text,
            _ => "?",
        };

    private static string OwnerSignature(AccessorDeclarationSyntax accessor) =>
        accessor.Parent?.Parent switch
        {
            PropertyDeclarationSyntax p => $"{Mods(p.Modifiers)}{p.Type} {p.Identifier}",
            IndexerDeclarationSyntax x => $"{Mods(x.Modifiers)}{x.Type} this{x.ParameterList}",
            EventDeclarationSyntax e => $"{Mods(e.Modifiers)}event {e.Type} {e.Identifier}",
            _ => "?",
        };

    private static string Mods(SyntaxTokenList modifiers) =>
        modifiers.Count == 0 ? string.Empty : string.Join(" ", modifiers.Select(t => t.Text)) + " ";

    private static string Sig(string text) => Regex.Replace(text, @"\s+", " ").Trim();

    private static string ContainingTypeName(SyntaxNode node)
    {
        var names = node.Ancestors()
            .OfType<BaseTypeDeclarationSyntax>()
            .Select(t => t.Identifier.Text)
            .Reverse()
            .ToList();

        return names.Count == 0 ? "<global>" : string.Join(".", names);
    }

    private static (string Text, string[] Tokens) Normalize(SyntaxNode body, SyntaxNode member, SemanticModel model, bool normalizeLiterals)
    {
        var map = new CanonicalNameMap();
        var declared = CollectDeclaredNames(member);
        var tokens = new List<string>();

        foreach (var token in body.DescendantTokens())
        {
            tokens.Add(CanonicalTokenText(token, model, map, declared, normalizeLiterals));
        }

        return (string.Join(' ', tokens), tokens.ToArray());
    }

    private static string CanonicalTokenText(SyntaxToken token, SemanticModel model, CanonicalNameMap map, HashSet<string> declared, bool normalizeLiterals)
    {
        if (token.IsKind(SyntaxKind.IdentifierToken))
        {
            var parent = token.Parent;

            if (parent is IdentifierNameSyntax { Parent: NameColonSyntax })
            {
                return token.Text;
            }

            var symbol = ResolveSymbol(token, parent, model);

            if (symbol is ILocalSymbol or IParameterSymbol or IRangeVariableSymbol
                or IMethodSymbol { MethodKind: MethodKind.LocalFunction })
            {
                return map.ForSymbol(symbol);
            }

            if (symbol is null && parent is IdentifierNameSyntax name &&
                declared.Contains(token.Text) && IsStandaloneName(name))
            {
                return map.ForName(token.Text);
            }

            return token.Text;
        }

        if (normalizeLiterals)
        {
            if (token.IsKind(SyntaxKind.NumericLiteralToken))
            {
                return "N";
            }

            if (token.Kind() is SyntaxKind.StringLiteralToken
                or SyntaxKind.SingleLineRawStringLiteralToken
                or SyntaxKind.MultiLineRawStringLiteralToken
                or SyntaxKind.Utf8StringLiteralToken
                or SyntaxKind.Utf8SingleLineRawStringLiteralToken
                or SyntaxKind.Utf8MultiLineRawStringLiteralToken
                or SyntaxKind.CharacterLiteralToken
                or SyntaxKind.InterpolatedStringTextToken)
            {
                return "S";
            }
        }

        return token.Text;
    }

    private static ISymbol? ResolveSymbol(SyntaxToken token, SyntaxNode? parent, SemanticModel model)
    {
        try
        {
            return parent switch
            {
                VariableDeclaratorSyntax d when d.Identifier == token => model.GetDeclaredSymbol(d),
                ParameterSyntax p when p.Identifier == token => model.GetDeclaredSymbol(p),
                SingleVariableDesignationSyntax s when s.Identifier == token => model.GetDeclaredSymbol(s),
                ForEachStatementSyntax fe when fe.Identifier == token => model.GetDeclaredSymbol(fe),
                CatchDeclarationSyntax cd when cd.Identifier == token => model.GetDeclaredSymbol(cd),
                LocalFunctionStatementSyntax lf when lf.Identifier == token => model.GetDeclaredSymbol(lf),
                FromClauseSyntax fc when fc.Identifier == token => model.GetDeclaredSymbol(fc),
                LetClauseSyntax lc when lc.Identifier == token => model.GetDeclaredSymbol(lc),
                JoinClauseSyntax jc when jc.Identifier == token => model.GetDeclaredSymbol(jc),
                JoinIntoClauseSyntax ji when ji.Identifier == token => model.GetDeclaredSymbol(ji),
                QueryContinuationSyntax qc when qc.Identifier == token => model.GetDeclaredSymbol(qc),
                IdentifierNameSyntax id => model.GetSymbolInfo(id).Symbol,
                _ => null,
            };
        }
        catch (ArgumentException)
        {
            return null;
        }
    }

    private static bool IsStandaloneName(IdentifierNameSyntax name) =>
        name.Parent switch
        {
            MemberAccessExpressionSyntax ma => ma.Name != name,
            MemberBindingExpressionSyntax => false,
            QualifiedNameSyntax qn => qn.Right != name,
            AliasQualifiedNameSyntax aq => aq.Name != name,
            NameColonSyntax => false,
            NameEqualsSyntax => false,
            _ => true,
        };

    private static HashSet<string> CollectDeclaredNames(SyntaxNode member)
    {
        var names = new HashSet<string>(StringComparer.Ordinal);

        foreach (var node in member.DescendantNodesAndSelf())
        {
            switch (node)
            {
                case VariableDeclaratorSyntax d:
                    names.Add(d.Identifier.Text);
                    break;
                case ParameterSyntax p:
                    names.Add(p.Identifier.Text);
                    break;
                case SingleVariableDesignationSyntax s:
                    names.Add(s.Identifier.Text);
                    break;
                case ForEachStatementSyntax fe:
                    names.Add(fe.Identifier.Text);
                    break;
                case CatchDeclarationSyntax { Identifier.Text.Length: > 0 } cd:
                    names.Add(cd.Identifier.Text);
                    break;
                case LocalFunctionStatementSyntax lf:
                    names.Add(lf.Identifier.Text);
                    break;
            }
        }

        return names;
    }

    private static List<(int A, int B, double Score, bool Exact)> FindPairs(List<MemberEntry> entries, ScanOptions options)
    {
        var eligible = Enumerable.Range(0, entries.Count)
            .Where(i => entries[i].TokenCount >= options.MinTokens)
            .ToList();

        var pairs = new List<(int A, int B, double Score, bool Exact)>();

        foreach (var group in eligible.GroupBy(i => entries[i].NormalizedHash).Where(g => g.Count() > 1))
        {
            var members = group.OrderBy(i => i).ToList();

            for (var i = 0; i < members.Count; i++)
            {
                for (var j = i + 1; j < members.Count; j++)
                {
                    if (!Overlaps(entries[members[i]], entries[members[j]]))
                    {
                        pairs.Add((members[i], members[j], 1.0, true));
                    }
                }
            }
        }

        var shingled = eligible.Where(i => entries[i].Tokens.Length >= options.ShingleK).ToList();
        var shingles = shingled.ToDictionary(i => i, i => ShingleSet(entries[i].Tokens, options.ShingleK));
        var buckets = new Dictionary<(int Band, ulong Key), List<int>>();

        foreach (var index in shingled)
        {
            var signature = MinHashSignature(shingles[index]);

            for (var band = 0; band < Bands; band++)
            {
                var key = (band, BandKey(signature, band));

                if (!buckets.TryGetValue(key, out var bucket))
                {
                    buckets[key] = bucket = [];
                }

                bucket.Add(index);
            }
        }

        var seen = new HashSet<(int, int)>();

        foreach (var bucket in buckets.Values.Where(b => b.Count > 1))
        {
            for (var i = 0; i < bucket.Count; i++)
            {
                for (var j = i + 1; j < bucket.Count; j++)
                {
                    var (a, b) = bucket[i] < bucket[j] ? (bucket[i], bucket[j]) : (bucket[j], bucket[i]);

                    if (!seen.Add((a, b)) ||
                        entries[a].NormalizedHash == entries[b].NormalizedHash ||
                        Overlaps(entries[a], entries[b]))
                    {
                        continue;
                    }

                    var score = Jaccard(shingles[a], shingles[b]);

                    if (score >= options.Threshold)
                    {
                        pairs.Add((a, b, score, false));
                    }
                }
            }
        }

        return pairs
            .OrderByDescending(p => p.Exact)
            .ThenByDescending(p => p.Score)
            .ThenBy(p => entries[p.A].File, StringComparer.Ordinal)
            .ThenBy(p => entries[p.A].StartLine)
            .ToList();
    }

    private static bool Overlaps(MemberEntry a, MemberEntry b) =>
        a.File == b.File && a.StartLine <= b.EndLine && b.StartLine <= a.EndLine;

    private static HashSet<ulong> ShingleSet(string[] tokens, int k)
    {
        var set = new HashSet<ulong>();

        for (var i = 0; i + k <= tokens.Length; i++)
        {
            var hash = FnvOffsetBasis;

            for (var j = 0; j < k; j++)
            {
                foreach (var c in tokens[i + j])
                {
                    hash = (hash ^ c) * FnvPrime;
                }

                hash = (hash ^ 0x1F) * FnvPrime;
            }

            set.Add(hash);
        }

        return set;
    }

    private static ulong[] MinHashSignature(HashSet<ulong> shingles)
    {
        var signature = new ulong[MinHashCount];
        Array.Fill(signature, ulong.MaxValue);

        foreach (var shingle in shingles)
        {
            for (var i = 0; i < MinHashCount; i++)
            {
                var hash = SplitMix64(shingle + Seeds[i]);

                if (hash < signature[i])
                {
                    signature[i] = hash;
                }
            }
        }

        return signature;
    }

    private static ulong BandKey(ulong[] signature, int band)
    {
        var hash = FnvOffsetBasis;

        for (var row = 0; row < Rows; row++)
        {
            var value = signature[band * Rows + row];

            for (var shift = 0; shift < 64; shift += 8)
            {
                hash = (hash ^ ((value >> shift) & 0xFF)) * FnvPrime;
            }
        }

        return hash;
    }

    private static double Jaccard(HashSet<ulong> a, HashSet<ulong> b)
    {
        var (small, large) = a.Count <= b.Count ? (a, b) : (b, a);
        var intersection = small.Count(large.Contains);

        return (double)intersection / (a.Count + b.Count - intersection);
    }

    private static ulong[] CreateSeeds()
    {
        var seeds = new ulong[MinHashCount];

        for (var i = 0; i < seeds.Length; i++)
        {
            seeds[i] = SplitMix64(0x243F6A8885A308D3UL + (ulong)i);
        }

        return seeds;
    }

    private static ulong SplitMix64(ulong z)
    {
        z += 0x9E3779B97F4A7C15UL;
        z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
        z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;

        return z ^ (z >> 31);
    }

    private static string Sha256Hex(string text) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(text)));

    private static string FindRepositoryRoot(string directory)
    {
        for (var current = new DirectoryInfo(directory); current is not null; current = current.Parent)
        {
            var git = Path.Combine(current.FullName, ".git");

            if (Directory.Exists(git) || File.Exists(git))
            {
                return current.FullName;
            }
        }

        return directory;
    }

    private static IReadOnlyList<MetadataReference> FrameworkReferences()
    {
        if (AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") is not string trusted)
        {
            return [];
        }

        return trusted.Split(Path.PathSeparator)
            .Where(path =>
            {
                var name = Path.GetFileName(path);

                return name.StartsWith("System.", StringComparison.Ordinal) ||
                       name is "System.dll" or "mscorlib.dll" or "netstandard.dll" or "Microsoft.CSharp.dll";
            })
            .Select(path => (MetadataReference)MetadataReference.CreateFromFile(path))
            .ToList();
    }

    private static PairLocation Location(MemberEntry entry) =>
        new(entry.File, entry.StartLine, entry.Type, entry.Member, entry.Signature);

    private static string RenderMarkdown(List<ReportPair> report, ScanOptions options)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Semantic duplication candidates");
        sb.AppendLine();
        sb.AppendLine($"Target: `{options.TargetDirectory}`");
        sb.AppendLine($"Settings: min-tokens={options.MinTokens}, threshold={options.Threshold}, shingle-k={options.ShingleK}, normalize-literals={options.NormalizeLiterals}");
        sb.AppendLine($"Pairs: {report.Count} ({report.Count(p => p.ExactHash)} exact-hash)");
        sb.AppendLine();

        foreach (var pair in report)
        {
            var kind = pair.ExactHash ? "exact-hash" : "near";
            sb.AppendLine($"## {pair.Rank}. score {pair.Score:F4} ({kind})");
            sb.AppendLine();
            sb.AppendLine($"- `{pair.A.File}:{pair.A.StartLine}` `{pair.A.Type}.{pair.A.Member}` - `{pair.A.Signature}`");
            sb.AppendLine($"- `{pair.B.File}:{pair.B.StartLine}` `{pair.B.Type}.{pair.B.Member}` - `{pair.B.Signature}`");
            sb.AppendLine();
        }

        return sb.ToString();
    }
}
