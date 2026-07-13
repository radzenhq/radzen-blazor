#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

// Guard-the-guard for the WASM-safety / determinism analyzer fence.
//
// The fence is subtle, load-bearing machinery: BannedApi.globalconfig silences RS0030/RS0031
// everywhere (so Razor-generated code and the rest of the UI library keep compiling), and
// path-scoped .editorconfig sections re-enable them as 'error' - plus CA1305 - only for the PDF
// library trees, fed by BannedSymbols.txt and wired through the .csproj. Any one link breaking
// (a severity flip, a dropped AdditionalFiles entry, a renamed tree) would silently let a banned,
// non-deterministic / non-WASM-safe API into the deterministic PDF output with a still-green build.
//
// These tests assert every link of that wiring exists. The actual "does the build fail?" proof is a
// separate, heavier out-of-process check (verify-analyzer-fence.sh) that plants a banned API in each
// tree and asserts RS0030 - it runs here only when RUN_FENCE_BUILD_CHECK=1 so the normal Pdf suite
// stays fast, but the script is always asserted to exist and to be the real thing.
public class PdfAnalyzerFenceTests
{
    private static readonly string[] FencedTrees =
        ["Documents/Pdf", "Documents/Crypto", "Documents/Codes", "Documents/Markdown"];

    private static string ProjectDir => AnalyzerFenceLocator.ProjectDir;

    private static string ReadConfig(string relativePath)
        => File.ReadAllText(Path.Combine(ProjectDir, relativePath));

    [Fact]
    public void GlobalConfig_SilencesBannedApiEverywhereByDefault()
    {
        var text = ReadConfig("BannedApi.globalconfig");
        Assert.Contains("is_global = true", text, StringComparison.Ordinal);
        Assert.Contains("dotnet_diagnostic.RS0030.severity = none", text, StringComparison.Ordinal);
        Assert.Contains("dotnet_diagnostic.RS0031.severity = none", text, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("Documents/Pdf")]
    [InlineData("Documents/Crypto")]
    [InlineData("Documents/Codes")]
    [InlineData("Documents/Markdown")]
    public void EditorConfig_ReEnablesFenceAsErrorForEachTree(string tree)
    {
        var section = EditorConfigSection($"[{tree}/**.cs]");
        Assert.Contains("dotnet_diagnostic.RS0030.severity = error", section, StringComparison.Ordinal);
        Assert.Contains("dotnet_diagnostic.RS0031.severity = error", section, StringComparison.Ordinal);
        Assert.Contains("dotnet_diagnostic.CA1305.severity = error", section, StringComparison.Ordinal);
    }

    [Theory]
    // Determinism / entropy / crypto (pre-existing fence).
    [InlineData("N:System.Security.Cryptography")]
    [InlineData("P:System.DateTime.Now")]
    [InlineData("P:System.DateTime.UtcNow")]
    [InlineData("M:System.Guid.NewGuid()")]
    [InlineData("P:System.Random.Shared")]
    // Networking / offline-WASM safety (new).
    [InlineData("N:System.Net")]
    [InlineData("N:System.Net.Http")]
    [InlineData("T:System.Net.Http.HttpClient")]
    // High-resolution / wall-clock timers (new).
    [InlineData("T:System.Diagnostics.Stopwatch")]
    [InlineData("P:System.Environment.TickCount")]
    [InlineData("P:System.Environment.TickCount64")]
    // Filesystem entropy / temp files (new).
    [InlineData("M:System.IO.Path.GetRandomFileName()")]
    [InlineData("M:System.IO.Path.GetTempFileName()")]
    public void BannedSymbols_ContainsEntry(string symbol)
    {
        var lines = ReadConfig("BannedSymbols.txt")
            .Split('\n')
            .Select(l => l.Trim())
            .Where(l => l.Length > 0 && !l.StartsWith(";", StringComparison.Ordinal));

        Assert.Contains(lines, l => l.StartsWith(symbol + ";", StringComparison.Ordinal));
    }

    [Fact]
    public void Csproj_WiresBannedSymbolsAndGlobalConfig()
    {
        var csproj = ReadConfig("Radzen.Blazor.csproj");
        Assert.Contains("<AdditionalFiles Include=\"BannedSymbols.txt\" />", csproj, StringComparison.Ordinal);
        Assert.Contains("<GlobalAnalyzerConfigFiles Include=\"BannedApi.globalconfig\" />", csproj, StringComparison.Ordinal);
        Assert.Contains("Microsoft.CodeAnalysis.BannedApiAnalyzers", csproj, StringComparison.Ordinal);
    }

    [Fact]
    public void GuardScript_ExistsAndCoversEveryFencedTree()
    {
        var script = AnalyzerFenceLocator.GuardScriptPath;
        Assert.True(File.Exists(script), $"guard-the-guard script missing: {script}");

        var text = File.ReadAllText(script);
        Assert.Contains("RS0030", text, StringComparison.Ordinal);
        Assert.Contains("System.Guid.NewGuid()", text, StringComparison.Ordinal);
        foreach (var tree in FencedTrees)
        {
            Assert.Contains(tree, text, StringComparison.Ordinal);
        }
    }

    // The real proof: plant a banned API and assert the build fails with RS0030. Opt-in because it
    // runs a full out-of-process `dotnet build`. Set RUN_FENCE_BUILD_CHECK=1 to exercise it (CI can).
    [Fact]
    public void GuardScript_FailsTheBuildWhenABannedApiIsPlanted()
    {
        if (Environment.GetEnvironmentVariable("RUN_FENCE_BUILD_CHECK") != "1")
        {
            return; // Wiring is covered by the always-on tests above; this is the heavy live check.
        }

        var psi = new ProcessStartInfo("bash", $"\"{AnalyzerFenceLocator.GuardScriptPath}\" \"{ProjectDir}\"")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        using var process = Process.Start(psi)!;
        var stdout = process.StandardOutput.ReadToEnd();
        var stderr = process.StandardError.ReadToEnd();
        process.WaitForExit();

        Assert.True(process.ExitCode == 0, $"fence guard failed:\n{stdout}\n{stderr}");
    }

    private static string EditorConfigSection(string header)
    {
        var lines = ReadConfig(".editorconfig").Split('\n');
        var start = Array.FindIndex(lines, l => l.Trim() == header);
        Assert.True(start >= 0, $".editorconfig is missing the fenced section {header}");

        var body = new List<string>();
        for (var i = start + 1; i < lines.Length; i++)
        {
            if (lines[i].TrimStart().StartsWith("[", StringComparison.Ordinal))
            {
                break;
            }
            body.Add(lines[i]);
        }
        return string.Join('\n', body);
    }
}

internal static class AnalyzerFenceLocator
{
    internal static string ProjectDir { get; } = Locate();

    internal static string GuardScriptPath =>
        Path.Combine(Path.GetDirectoryName(ThisFile())!, "verify-analyzer-fence.sh");

    private static string ThisFile([CallerFilePath] string path = "") => path;

    private static string Locate()
    {
        // Preferred: relative to this source file (repo/Radzen.Blazor.Tests/Pdf/ -> repo/Radzen.Blazor).
        var testsDir = Directory.GetParent(Path.GetDirectoryName(ThisFile())!)?.Parent?.FullName;
        if (testsDir is not null)
        {
            var candidate = Path.Combine(testsDir, "Radzen.Blazor");
            if (File.Exists(Path.Combine(candidate, "BannedSymbols.txt")))
            {
                return candidate;
            }
        }

        // Fallback for build environments that rewrite CallerFilePath: walk up from the test binary.
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            var candidate = Path.Combine(dir.FullName, "Radzen.Blazor");
            if (File.Exists(Path.Combine(candidate, "BannedSymbols.txt")))
            {
                return candidate;
            }
        }

        throw new InvalidOperationException("Could not locate the Radzen.Blazor project directory for the analyzer fence tests.");
    }
}
