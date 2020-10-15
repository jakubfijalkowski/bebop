using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Compiler.Exceptions;
using Compiler.Meta;
using Compiler.Meta.Interfaces;
using Superpower;
using Superpower.Display;
using Superpower.Model;
using Superpower.Parsers;
using Superpower.Tokenizers;

namespace Compiler.Parser
{
    // Following:
    // https://github.com/datalust/superpower/blob/937cacff2a082fd19783fde849e554efcd199319/sample/JsonParser/Program.cs
    enum PierogiToken
    {
        [Token(Example = "{")]
        LeftCurlyBracket,
        [Token(Example = "}")]
        RightCurlyBracket,
        [Token(Example = "[")]
        LeftSquareBracket,
        [Token(Example = "]")]
        RightSquareBracket,
        [Token(Example = "(")]
        LeftParenthesis,
        [Token(Example = ")")]
        RightParenthesis,
        [Token(Example = ";")]
        Semicolon,
        [Token(Example = "=")]
        Equals,

        LowerName,
        UpperName,
        Number,
        String,
    }

    static class PierogiTokenizer
    {
        static TextParser<Unit> LowerNameToken { get; } =
            from c in Character.Lower
            from cs in Character.LetterOrDigit.Or(Character.EqualTo('_')).IgnoreMany()
            select Unit.Value;

        static TextParser<Unit> UpperNameToken { get; } =
            from c in Character.Upper
            from cs in Character.LetterOrDigit.Or(Character.EqualTo('_')).IgnoreMany()
            select Unit.Value;

        static TextParser<Unit> StringToken { get; } =
            from open in Character.EqualTo('"')
            from content in Span.EqualTo("\\\"").Value(Unit.Value).Try()
                .Or(Span.EqualTo("\\\\").Value(Unit.Value).Try())
                .Or(Character.Except('"').Value(Unit.Value))
                .IgnoreMany()
            from close in Character.EqualTo('"')
            select Unit.Value;

        static TextParser<Unit> NumberToken { get; } =
            from digits in Character.Digit.AtLeastOnce()
            select Unit.Value;

        public static Tokenizer<PierogiToken> Instance { get; } =
            new TokenizerBuilder<PierogiToken>()
                .Ignore(Span.WhiteSpace)
                .Ignore(Comment.CPlusPlusStyle.Optional())
                .Match(Character.EqualTo('{'), PierogiToken.LeftCurlyBracket)
                .Match(Character.EqualTo('}'), PierogiToken.RightCurlyBracket)
                .Match(Character.EqualTo('['), PierogiToken.LeftSquareBracket)
                .Match(Character.EqualTo(']'), PierogiToken.RightSquareBracket)
                .Match(Character.EqualTo('('), PierogiToken.LeftParenthesis)
                .Match(Character.EqualTo(')'), PierogiToken.RightParenthesis)
                .Match(Character.EqualTo(';'), PierogiToken.Semicolon)
                .Match(Character.EqualTo('='), PierogiToken.Equals)
                .Match(LowerNameToken, PierogiToken.LowerName, requireDelimiters: true)
                .Match(UpperNameToken, PierogiToken.UpperName, requireDelimiters: true)
                .Match(NumberToken, PierogiToken.Number)
                .Match(StringToken, PierogiToken.String)
                .Build();
    }

    static class PierogiTextParsers
    {
        public static TextParser<string> String { get; } =
            from open in Character.EqualTo('"')
            from chars in Character.ExceptIn('"', '\\')
                .Or(Character.EqualTo('\\')
                    .IgnoreThen(
                        Character.EqualTo('\\')
                        .Or(Character.EqualTo('"'))))
                .Many()
            from close in Character.EqualTo('"')
            select new string(chars);

        public static TextParser<int> Number { get; } =
            from digits in Character.Digit.AtLeastOnce()
            select int.Parse(new string(digits));
    }

    public static class SchemaParser
    {
        static TokenListParser<PierogiToken, string> PierogiString { get; } =
            Token.EqualTo(PierogiToken.String)
                .Apply(PierogiTextParsers.String);

        static TokenListParser<PierogiToken, int> PierogiNumber { get; } =
            Token.EqualTo(PierogiToken.Number)
                .Apply(PierogiTextParsers.Number);
    }
}