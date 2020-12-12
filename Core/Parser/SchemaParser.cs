﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.Exceptions;
using Core.Lexer;
using Core.Lexer.Extensions;
using Core.Lexer.Tokenization;
using Core.Lexer.Tokenization.Models;
using Core.Meta;
using Core.Meta.Attributes;
using Core.Meta.Interfaces;
using Core.Parser.Extensions;

namespace Core.Parser
{
    public class SchemaParser
    {
        private readonly SchemaLexer _lexer;
        private readonly Dictionary<string, IDefinition> _definitions = new Dictionary<string, IDefinition>();
        /// <summary>
        /// A set of references to named types found in message/struct definitions:
        /// the left token is the type name, and the right token is the definition it's used in (used to report a helpful error).
        /// </summary>
        private readonly HashSet<(Token, Token)> _typeReferences = new HashSet<(Token, Token)>();
        private uint _index;
        private readonly string _nameSpace;
        private Token[] _tokens = new Token[] { };

        /// <summary>
        /// Creates a new schema parser instance from some schema files on disk.
        /// </summary>
        /// <param name="schemaPaths">The Bebop schema files that will be parsed</param>
        /// <param name="nameSpace"></param>
        public SchemaParser(List<string> schemaPaths, string nameSpace)
        {
            _lexer = SchemaLexer.FromSchemaPaths(schemaPaths);
            _nameSpace = nameSpace;
        }

        /// <summary>
        /// Creates a new schema parser instance and loads the schema into memory.
        /// </summary>
        /// <param name="textualSchema">A string representation of a schema.</param>
        /// <param name="nameSpace"></param>
        public SchemaParser(string textualSchema, string nameSpace)
        {
            _lexer = SchemaLexer.FromTextualSchema(textualSchema);
            _nameSpace = nameSpace;
        }

        /// <summary>
        ///     Gets the <see cref="Token"/> at the current <see cref="_index"/>.
        /// </summary>
        private Token CurrentToken => _tokens[_index];

        /// <summary>
        /// Tokenize the input file, storing the result in <see cref="_tokens"/>.
        /// </summary>
        /// <returns></returns>
        private async Task Tokenize()
        {
            var collection = new List<Token>();
            await foreach (var token in _lexer.TokenStream())
            {
                collection.Add(token);
            }
            _tokens = collection.ToArray();
        }

        /// <summary>
        ///     Peeks a token at the specified <paramref name="index"/>
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        private Token PeekToken(uint index) => _tokens[index];

        /// <summary>
        ///     Sets the current token stream position to the specified <paramref name="index"/>
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        private Token Base(uint index)
        {
            _index = index;
            return _tokens[index];
        }

        /// <summary>
        ///     If the <see cref="CurrentToken"/> matches the specified <paramref name="kind"/>, advance the token stream
        ///     <see cref="_index"/> forward
        /// </summary>
        /// <param name="kind">The <see cref="TokenKind"/> to eat</param>
        /// <returns></returns>
        private bool Eat(TokenKind kind)
        {

            if (CurrentToken.Kind == kind)
            {
                _index++;
                return true;
            }
            return false;
        }

        /// <summary>
        ///     If the <see cref="CurrentToken"/> matches the specified <paramref name="kind"/>, advance the token stream
        ///     <see cref="_index"/> forward.
        ///     Otherwise throw a <see cref="UnexpectedTokenException"/>
        /// </summary>
        /// <param name="kind">The <see cref="TokenKind"/> to eat</param>
        /// <returns></returns>
        private void Expect(TokenKind kind, string? hint = null)
        {

            // don't throw on block comment tokens
            if (CurrentToken.Kind == TokenKind.BlockComment)
            {
                ConsumeBlockComments();
            }
            if (!Eat(kind))
            {
                throw new UnexpectedTokenException(kind, CurrentToken, hint);
            }
        }

        /// <summary>
        /// Consume all sequential block comments.
        /// </summary>
        /// <returns>The content of the last block comment which usually proceeds a definition.</returns>
        private string ConsumeBlockComments()
        {

            var definitionDocumentation = string.Empty;
            if (CurrentToken.Kind == TokenKind.BlockComment)
            {
                while (CurrentToken.Kind == TokenKind.BlockComment)
                {
                    definitionDocumentation = CurrentToken.Lexeme;
                    Eat(TokenKind.BlockComment);
                }
            }
            return definitionDocumentation;
        }


        /// <summary>
        ///     Evaluates a schema and parses it into a <see cref="ISchema"/> object
        /// </summary>
        /// <returns></returns>
        public async Task<ISchema> Evaluate()
        {
            await Tokenize();
            _index = 0;
            _definitions.Clear();
            _typeReferences.Clear();


            while (_index < _tokens.Length && !Eat(TokenKind.EndOfFile))
            {
                var definitionDocumentation = ConsumeBlockComments();
                var attributes = EatAllAttributes();

                var isReadOnly = Eat(TokenKind.ReadOnly);
                var kind = CurrentToken switch
                {
                    _ when Eat(TokenKind.Enum) => AggregateKind.Enum,
                    _ when Eat(TokenKind.Struct) => AggregateKind.Struct,
                    _ when Eat(TokenKind.Message) => AggregateKind.Message,
                    _ => throw new UnexpectedTokenException(TokenKind.Message, CurrentToken)
                };
                DeclareAggregateType(CurrentToken, kind, isReadOnly, definitionDocumentation, attributes);
            }
            foreach (var (typeToken, definitionToken) in _typeReferences)
            {
                if (!_definitions.ContainsKey(typeToken.Lexeme))
                {
                    throw new UnrecognizedTypeException(typeToken, definitionToken.Lexeme);
                }
            }
            return new BebopSchema(_nameSpace, _definitions);
        }

        private List<BaseAttribute> EatAllAttributes()
        {
            var res = new List<BaseAttribute>();
            BaseAttribute? attr;
            while ((attr = EatAttribute()) is not null)
            {
                res.Add(attr);
            }
            return res;

        }

        /// <summary>
        /// Consumes all the tokens belonging to an attribute
        /// </summary>
        /// <returns>An instance of the attribute.</returns>
        private BaseAttribute? EatAttribute()
        {
            if (Eat(TokenKind.OpenBracket))
            {
                var kind = CurrentToken.Lexeme;
                Expect(TokenKind.Identifier);
                var value = string.Empty;
                var isNumber = false;
                if (Eat(TokenKind.OpenParenthesis))
                {
                    value = CurrentToken.Lexeme;
                    if (Eat(TokenKind.StringExpandable) || Eat(TokenKind.StringLiteral) || (kind == "opcode") && Eat(TokenKind.Number))
                    {
                        isNumber = PeekToken(_index - 1).Kind == TokenKind.Number;
                    }
                    else
                    {
                        throw new UnexpectedTokenException(
                            TokenKind.StringExpandable | TokenKind.StringLiteral | TokenKind.Number, CurrentToken);
                    }
                    Expect(TokenKind.CloseParenthesis);
                }
                Expect(TokenKind.CloseBracket);
                return kind switch
                {
                    "deprecated" => new DeprecatedAttribute(value),
                    "opcode" => new OpcodeAttribute(value, isNumber),
                    "query" => new QueryAttribute(value),
                    "command" => new CommandAttribute(),
                    "authorizeWhenHasAnyOf" => new AuthorizeWhenHasAnyOfAttribute(value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)),
                    _ => throw new UnknownAttributeException(kind, CurrentToken)
                };
            }
            return null;
        }


        /// <summary>
        ///     Declares an aggregate data structure and adds it to the <see cref="_definitions"/> collection
        /// </summary>
        /// <param name="definitionToken">The token that names the type to define.</param>
        /// <param name="kind">The <see cref="AggregateKind"/> the type will represents.</param>
        /// <param name="isReadOnly"></param>
        /// <param name="definitionDocumentation"></param>
        /// <param name="opcodeAttribute"></param>
        private void DeclareAggregateType(Token definitionToken,
            AggregateKind kind,
            bool isReadOnly,
            string definitionDocumentation,
            IReadOnlyList<BaseAttribute> customAttributes)
        {
            var fields = new List<IField>();
            var kindName = kind switch { AggregateKind.Enum => "enum", AggregateKind.Struct => "struct", _ => "message" };
            var aKindName = kind switch { AggregateKind.Enum => "an enum", AggregateKind.Struct => "a struct", _ => "a message" };

            Expect(TokenKind.Identifier, hint: $"Did you forget to specify a name for this {kindName}?");
            Expect(TokenKind.OpenBrace);
            var definitionEnd = CurrentToken.Span;
            while (!Eat(TokenKind.CloseBrace))
            {

                var value = 0;

                var fieldDocumentation = ConsumeBlockComments();
                // if we've reached the end of the definition after parsing documentation we need to exit.
                if (Eat(TokenKind.CloseBrace))
                {
                    break;
                }

                var deprecatedAttribute = EatAttribute() as DeprecatedAttribute;

                if (kind == AggregateKind.Message)
                {
                    var indexHint = "Fields in a message must be explicitly indexed: message A { 1 -> string s; 2 -> bool b; }";
                    var indexLexeme = CurrentToken.Lexeme;
                    Expect(TokenKind.Number, hint: indexHint);
                    value = int.Parse(indexLexeme);
                    // Parse an arrow ("->").
                    Expect(TokenKind.Hyphen, hint: indexHint);
                    Expect(TokenKind.CloseCaret, hint: indexHint);
                }

                // Parse a type name, if this isn't an enum:
                TypeBase type = kind == AggregateKind.Enum
                    ? new ScalarType(BaseType.UInt32, definitionToken.Span, definitionToken.Lexeme)
                    : ParseType(definitionToken);

                var fieldName = CurrentToken.Lexeme;
                if (TokenizerExtensions.GetKeywords().Contains(fieldName))
                {
                    throw new ReservedIdentifierException(fieldName, CurrentToken.Span);
                }
                var fieldStart = CurrentToken.Span;

                Expect(TokenKind.Identifier);

                if (kind == AggregateKind.Enum)
                {
                    Expect(TokenKind.Eq, hint: "Every constant in an enum must have an explicit literal value.");
                    var valueLexeme = CurrentToken.Lexeme;
                    Expect(TokenKind.Number, hint: "An enum constant must have a literal integer value.");
                    value = int.Parse(valueLexeme);
                }


                var fieldEnd = CurrentToken.Span;
                Expect(TokenKind.Semicolon, hint: $"Elements in {aKindName} are delimited using semicolons.");
                fields.Add(new Field(fieldName, type, fieldStart.Combine(fieldEnd), deprecatedAttribute, value, fieldDocumentation));
                definitionEnd = CurrentToken.Span;
            }

            var name = definitionToken.Lexeme;
            var definitionSpan = definitionToken.Span.Combine(definitionEnd);
            var definition = new Definition(name, isReadOnly, definitionSpan, kind, fields, definitionDocumentation, customAttributes);
            if (_definitions.ContainsKey(name))
            {
                throw new MultipleDefinitionsException(definition);
            }
            _definitions.Add(name, definition);
        }

        /// <summary>
        ///     Parse a type name.
        /// </summary>
        /// <param name="definitionToken">
        ///     The token acting as a representative for the definition we're in.
        ///     <para/>
        ///     This is used to track how DefinedTypes are referenced in definitions, and give useful error messages.
        /// </param>
        /// <returns>An IType object.</returns>
        private TypeBase ParseType(Token definitionToken)
        {
            TypeBase type;
            Span span = CurrentToken.Span;
            if (Eat(TokenKind.Map))
            {
                // Parse "map[k, v]".
                var mapHint = "The syntax for map types is: map[KeyType, ValueType].";
                Expect(TokenKind.OpenBracket, hint: mapHint);
                var keyType = ParseType(definitionToken);
                if (!IsValidMapKeyType(keyType))
                {
                    throw new InvalidMapKeyTypeException(keyType);
                }
                Expect(TokenKind.Comma, hint: mapHint);
                var valueType = ParseType(definitionToken);
                span = span.Combine(CurrentToken.Span);
                Expect(TokenKind.CloseBracket, hint: mapHint);
                type = new MapType(keyType, valueType, span, $"map[{keyType.AsString}, {valueType.AsString}]");
            }
            else if (Eat(TokenKind.Array))
            {
                // Parse "array[v]".
                var arrayHint = "The syntax for array types is either array[Type] or Type[].";
                Expect(TokenKind.OpenBracket, hint: arrayHint);
                var valueType = ParseType(definitionToken);
                Expect(TokenKind.CloseBracket, hint: arrayHint);
                type = new ArrayType(valueType, span, $"array[{valueType.AsString}]");
            }
            else if (CurrentToken.TryParseBaseType(out var baseType))
            {
                type = new ScalarType(baseType!.Value, span, CurrentToken.Lexeme);
                // Consume the matched basetype name as an identifier:
                Expect(TokenKind.Identifier);
            }
            else
            {
                type = new DefinedType(CurrentToken.Lexeme, span, CurrentToken.Lexeme);
                _typeReferences.Add((CurrentToken, definitionToken));
                // Consume the type name as an identifier:
                Expect(TokenKind.Identifier);
            }

            // Parse any postfix "[]".
            while (Eat(TokenKind.OpenBracket))
            {
                span = span.Combine(CurrentToken.Span);
                Expect(TokenKind.CloseBracket, hint: "The syntax for array types is T[]. You can't specify a fixed size.");
                type = new ArrayType(type, span, $"{type.AsString}[]");
            }

            return type;
        }

        private static bool IsValidMapKeyType(TypeBase keyType)
        {
            return keyType is ScalarType;
        }
    }
}
