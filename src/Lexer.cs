using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Text;

namespace gladyrisk_lang.src
{
    internal class Lexer
    {
        static FrozenDictionary<string, TokenKind> s_symbols = new Dictionary<string, TokenKind>()
        {
            { "+", TokenKind.Add },
            { "-", TokenKind.Sub },
            { "*", TokenKind.Mul },
            { "/", TokenKind.Div },
            { "%", TokenKind.Mod },
            { "<", TokenKind.Less },
            { ">", TokenKind.Greater },
            { "<=", TokenKind.LessEqual },
            { ">=", TokenKind.GreaterEqual },
            { "==", TokenKind.EqualEqual },
            { "!=", TokenKind.NotEqual },
            { "!", TokenKind.Not },
            { "(", TokenKind.LeftParen },
            { ")", TokenKind.RightParen },
            { ",", TokenKind.Comma },
            { ";", TokenKind.Semicolon },
            { "::", TokenKind.Member },
            { "[", TokenKind.LeftBracket },
            { "]", TokenKind.RightBracket }
        }.ToFrozenDictionary();

        static FrozenDictionary<string, TokenKind> s_keywords = new Dictionary<string, TokenKind>()
        {
            { "fn", TokenKind.Fn },
            { "end", TokenKind.End },
            { "true", TokenKind.True },
            { "false", TokenKind.False },
            { "null", TokenKind.Null },
            { "var", TokenKind.Var },
            { "mov", TokenKind.Mov },
            { "label", TokenKind.Label },
            { "jmp", TokenKind.Jmp },
            { "jmpfalse", TokenKind.JmpFalse },
            { "jmptrue", TokenKind.JmpTrue },
            { "ret", TokenKind.Ret },
            { "call", TokenKind.Call },
            { "use", TokenKind.Use },
            { "enum", TokenKind.Enum }
        }.ToFrozenDictionary();

        public static string? GetKeywordFromKind(TokenKind kind)
        {
            return s_keywords.FirstOrDefault(x => x.Value == kind).Key;
        }

        public static string? GetSymbolFromKind(TokenKind kind)
        {
            return s_symbols.FirstOrDefault(x => x.Value == kind).Key;
        }

        Position _position;
        int _i = 0;
        string _source;

        public Lexer(SourceFile sourceFile)
        {
            _source = sourceFile.Text;
            _position = new Position(Error.AddSourceFile(sourceFile), 1, 1);
        }

        public List<Token> Tokenize()
        {
            var tokens = new List<Token>();

            while (NotAtEnd())
            {
                if (Current() is '"' or '\'')
                {
                    tokens.Add(LexString());
                    continue;
                }

                //if (Current() == 'f')
                //{
                //    if (!NotAtEnd(1) || Current(1) is not ('"' or '\''))
                //        throw new Error("Expected string after format specifier", _position);
                //    Next();
                //    Token token = LexString() with { Type = TokenType.FString };
                //    tokens.Add(token);
                //    continue;
                //}

                if (NotAtEnd(1))
                {
                    string value = $"{Current()}{Current(1)}";
                    if (s_symbols.TryGetValue(value, out var kind))
                    {
                        tokens.Add(new Token(value, kind, _position));
                        Next();
                        Next();
                        continue;
                    }
                }

                if (s_symbols.TryGetValue(Current().ToString(), out var skind))
                {
                    tokens.Add(new Token(Current().ToString(), skind, _position));
                    Next();
                    continue;
                }

                if (char.IsDigit(Current()))
                {
                    tokens.Add(LexNumber());
                    continue;
                }

                //if (Current() == '\n')
                //{
                //    tokens.Add(new Token(TokenType.NewLine, "New Line", _position));
                //    Next();
                //    continue;
                //}

                if (char.IsLetter(Current()) || Current() == '_')
                {
                    tokens.Add(LexAlpha());
                    continue;
                }

                Next();
            }

            tokens.Add(new Token("End of File", TokenKind.EndOfFile, _position));

            return tokens;
        }

        Token LexString()
        {
            char target = Current();

            Position position = _position;

            Next();

            StringBuilder sb = new StringBuilder();

            while (NotAtEnd() && Current() != target)
            {
                if (Current() == '\n')
                    throw new Error("Unterminated string", _position);

                if (Current() == '\\')
                {
                    sb.Append(LexEscape());
                    continue;
                }

                sb.Append(Current());
                Next();
            }

            if (!NotAtEnd())
                throw new Error("Unterminated string", _position);

            Next();

            return new Token(sb.ToString(), TokenKind.String, position);
        }

        char LexEscape()
        {
            Position position = _position;

            Next();

            char escape = Current() switch
            {
                'a' => '\a',
                'b' => '\b',
                't' => '\t',
                'n' => '\n',
                'v' => '\v',
                'f' => '\f',
                'r' => '\r',
                'e' => '\e',
                '"' => '\"',
                '\'' => '\'',
                '\\' => '\\',
                _ => throw new Error($"'\\{Current()}' is an invalid escape code", position)
            };

            Next();

            return escape;
        }

        Token LexNumber()
        {
            Position position = _position;

            StringBuilder sb = new StringBuilder();
            bool hasDecimal = false;

            while (NotAtEnd() && (char.IsDigit(Current()) || Current() == '.' || Current() == '_'))
            {
                if (Current() == '_')
                {
                    if (!NotAtEnd(1) || !char.IsDigit(Current(1)))
                        throw new Error("Expected number after underscore", _position);
                    Next();
                    continue;
                }

                if (Current() == '.')
                {
                    if (hasDecimal)
                        throw new Error("Found more than 1 decimal", _position);

                    if (!NotAtEnd(1) || !char.IsDigit(Current(1)))
                        throw new Error("Expected number after decimal", _position);

                    hasDecimal = true;
                    sb.Append('.');
                    Next();
                    continue;
                }

                sb.Append(Current());
                Next();
            }

            return new Token(sb.ToString(), TokenKind.Number, position);
        }

        Token LexAlpha()
        {
            Position position = _position;

            StringBuilder sb = new StringBuilder();

            while (NotAtEnd() && (char.IsLetterOrDigit(Current()) || Current() == '_'))
            {
                sb.Append(Current());
                Next();
            }

            string lexeme = sb.ToString();

            if (s_keywords.TryGetValue(lexeme, out var kind))
            {
                return new Token(lexeme, kind, position);
            }

            return new Token(lexeme, TokenKind.Identifier, position);
        }

        char Current(int dist = 0) => _source[_i + dist];
        bool NotAtEnd(int dist = 0) => _i + dist < _source.Length;

        void Next()
        {
            if (Current() == '\n')
            {
                _position.Line++;
                _position.Column = 1;
            }
            else
                _position.Column++;
            _i++;
        }
    }
}
