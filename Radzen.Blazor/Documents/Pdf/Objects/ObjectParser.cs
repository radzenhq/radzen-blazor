using System.Collections.Generic;
using System.Text;

namespace Radzen.Documents.Pdf.Objects;

internal sealed class ObjectParser
{
    private readonly Lexer lexer;
    private readonly List<Token> lookahead = new();

    internal ObjectParser(Lexer lexer)
    {
        this.lexer = lexer;
    }

    internal Lexer Lexer => lexer;

    internal static DocumentObject Parse(byte[] data, int position)
        => new ObjectParser(new Lexer(data, position)).ParseValue();

    internal Token NextToken()
    {
        if (lookahead.Count > 0)
        {
            var token = lookahead[0];
            lookahead.RemoveAt(0);
            return token;
        }

        return lexer.Next();
    }

    internal DocumentObject ParseValue()
    {
        var token = NextToken();
        switch (token.Kind)
        {
            case TokenKind.Integer:
                return ParseIntegerOrReference(token);
            case TokenKind.Real:
                return new NumberObject(token.RealValue);
            case TokenKind.Name:
                return new NameObject(token.Text);
            case TokenKind.StringLiteral:
            case TokenKind.HexString:
                return new StringObject(Encoding.Latin1.GetString(token.Bytes));
            case TokenKind.ArrayOpen:
                return ParseArray();
            case TokenKind.DictOpen:
                return ParseDictionary();
            case TokenKind.Keyword:
                return ParseKeyword(token);
            default:
                throw new DocumentParseException("Unexpected token.", lexer.Position);
        }
    }

    private Token Peek(int ahead)
    {
        while (lookahead.Count <= ahead)
        {
            lookahead.Add(lexer.Next());
        }

        return lookahead[ahead];
    }

    private DocumentObject ParseIntegerOrReference(Token token)
    {
        if (Peek(0).Kind == TokenKind.Integer
            && Peek(1).Kind == TokenKind.Keyword && Peek(1).Text == "R")
        {
            var generation = NextToken();
            NextToken();
            return new ReferenceObject((int)token.IntValue, (int)generation.IntValue);
        }

        return new NumberObject((int)token.IntValue);
    }

    private DocumentObject ParseArray()
    {
        var array = new ArrayObject();
        while (true)
        {
            var token = Peek(0);
            if (token.Kind == TokenKind.ArrayClose)
            {
                NextToken();
                return array;
            }

            if (token.Kind == TokenKind.EndOfData)
            {
                throw new DocumentParseException("Unterminated array.", lexer.Position);
            }

            array.Add(ParseValue());
        }
    }

    private DocumentObject ParseDictionary()
    {
        var dictionary = new DictionaryObject();
        while (true)
        {
            var token = NextToken();
            if (token.Kind == TokenKind.DictClose)
            {
                return dictionary;
            }

            if (token.Kind != TokenKind.Name)
            {
                throw new DocumentParseException("Expected dictionary key.", lexer.Position);
            }

            if (Peek(0).Kind == TokenKind.EndOfData)
            {
                throw new DocumentParseException("Missing dictionary value.", lexer.Position);
            }

            dictionary[token.Text] = ParseValue();
        }
    }

    private DocumentObject ParseKeyword(Token token)
    {
        switch (token.Text)
        {
            case "true":
                return new BooleanObject(true);
            case "false":
                return new BooleanObject(false);
            case "null":
                return new NullObject();
            default:
                throw new DocumentParseException("Unexpected keyword.", lexer.Position);
        }
    }
}
