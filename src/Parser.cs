using gladyrisk_lang.src.ast;
using System;
using System.Collections.Generic;
using System.Text;

namespace gladyrisk_lang.src
{
    internal class Parser
    {
        List<Token> _tokens;
        int _i = 0;

        public Parser(List<Token> tokens)
        {
            _tokens = tokens;
        }

        public List<Statement> Parse()
        {
            var statements = new List<Statement>();

            while (NotAtEnd())
            {
                statements.Add(ParseStatement());
            }

            return statements;
        }

        Statement ParseStatement()
        {
            if (Check(TokenKind.Fn))
                return ParseFn();
            if (Check(TokenKind.Var))
                return ParseVar();
            if (Check(TokenKind.Mov))
                return ParseMov();
            if (Check(TokenKind.Label))
                return ParseLabel();
            if (Check(TokenKind.Jmp))
                return ParseJmp();
            if (Check(TokenKind.JmpFalse))
                return ParseJmpFalse();
            if (Check(TokenKind.JmpTrue))
                return ParseJmpTrue();
            if (Check(TokenKind.Call))
                return ParseCall();
            if (Check(TokenKind.Ret))
                return ParseRet();
            if (Check(TokenKind.End))
                throw new Error("Unexpected end", Current().Position);
            throw new Error("Invalid token", Current().Position);
        }

        FnStatement ParseFn()
        {
            Position position = Current().Position;

            Next();

            string name = ParseName();

            List<string> parameters = new List<string>();

            if (Check(TokenKind.Identifier))
                parameters = ParseParameters();

            List<Statement> body = ParseBody();

            return new FnStatement(body, name, parameters, position);
        }

        VarStatement ParseVar()
        {
            Position position = Current().Position;

            Next();

            Expect(TokenKind.Comma);

            string name = ParseName();

            if (Match(TokenKind.Semicolon))
                return new VarStatement(name, null, position);

            Expect(TokenKind.Comma);

            Expression expression = ParseExpression();

            Expect(TokenKind.Semicolon);

            return new VarStatement(name, expression, position);
        }

        Statement ParseMov()
        {
            Position position = Current().Position;

            Next();

            Expect(TokenKind.Comma);

            Expression assignee = ParsePostfix();

            Expect(TokenKind.Comma);

            Expression expression = ParseExpression();

            Expect(TokenKind.Semicolon);

            if (assignee is NameExpression nameExpression)
                return new VarMovStatement(nameExpression.Name, expression, position);
            else if (assignee is IndexExpression indexExpression)
                return new IndexMovStatement(indexExpression, expression, position);

            throw new Error("Invalid mov target", position);
        }

        LabelStatement ParseLabel()
        {
            Position position = Current().Position;

            Next();

            Expect(TokenKind.Comma);

            string name = ParseName();

            Expect(TokenKind.Semicolon);

            return new LabelStatement(name, position);
        }

        JmpStatement ParseJmp()
        {
            Position position = Current().Position;

            Next();

            Expect(TokenKind.Comma);

            string name = ParseName();

            Expect(TokenKind.Semicolon);

            return new JmpStatement(name, position);
        }

        JmpFalseStatement ParseJmpFalse()
        {
            Position position = Current().Position;

            Next();

            Expect(TokenKind.Comma);

            string name = ParseName();

            Expect(TokenKind.Comma);

            Expression expression = ParseExpression();

            Expect(TokenKind.Semicolon);

            return new JmpFalseStatement(name, expression, position);
        }

        JmpTrueStatement ParseJmpTrue()
        {
            Position position = Current().Position;

            Next();

            Expect(TokenKind.Comma);

            string name = ParseName();

            Expect(TokenKind.Comma);

            Expression expression = ParseExpression();

            Expect(TokenKind.Semicolon);

            return new JmpTrueStatement(name, expression, position);
        }

        CallStatement ParseCall()
        {
            Position position = Current().Position;

            CallExpression callExpression = (CallExpression)ParsePrimary();

            Expect(TokenKind.Semicolon);

            return new CallStatement(callExpression, position);
        }

        RetStatement ParseRet()
        {
            Position position = Current().Position;

            Next();

            Expect(TokenKind.Comma);

            Expression expression = ParseExpression();

            Expect(TokenKind.Semicolon);

            return new RetStatement(expression, position);
        }

        List<Statement> ParseBody()
        {
            List<Statement> statements = new List<Statement>();

            while (NotAtEnd() && !Check(TokenKind.End))
            {
                Statement? statement = ParseStatement();
                if (statement != null)
                    statements.Add(statement);
            }

            Expect(TokenKind.End);

            return statements;
        }

        List<Expression> ParseExpressions()
        {
            List<Expression> expressions = [ParseExpression()];

            while (Match(TokenKind.Comma))
                expressions.Add(ParseExpression());

            return expressions;
        }

        List<string> ParseParameters()
        {
            List<string> parameters = new List<string>() {
                ParseName()
            };

            while (Match(TokenKind.Comma))
            {
                parameters.Add(ParseName());
            }

            return parameters;
        }

        string ParseName()
        {
            string name = Current().Lexeme;
            Consume("Expected a name", TokenKind.Identifier);
            return name;
        }

        Expression ParsePrimary()
        {
            Token token = Current();

            if (Match(TokenKind.Number))
                return new NumberExpression(double.Parse(token.Lexeme), token.Position);
            else if (Match(TokenKind.String))
                return new StringExpression(token.Lexeme, token.Position);
            else if (Match(TokenKind.True))
                return new BoolExpression(true, token.Position);
            else if (Match(TokenKind.False))
                return new BoolExpression(false, token.Position);
            else if (Match(TokenKind.Null))
                return new NullExpression(token.Position);
            else if (Match(TokenKind.Identifier))
                return new NameExpression(token.Lexeme, token.Position);
            else if (Match(TokenKind.LeftParen))
            {
                Expression group = ParseExpression();
                Expect(TokenKind.RightParen); 
                return group;
            }  
            else if (Match(TokenKind.Call))
            {
                Expect(TokenKind.LeftParen);

                Expression callee = ParsePostfix();

                List<Expression> args = new List<Expression>();

                if (Match(TokenKind.Comma))
                    args = ParseExpressions();

                Expect(TokenKind.RightParen);
                 
                return new CallExpression(callee, args, token.Position);
            }
            else if (Match(TokenKind.LeftBracket))
            {
                if (Match(TokenKind.RightBracket))
                    return new ArrayExpression([], token.Position);
                List<Expression> elements = ParseExpressions();
                Expect(TokenKind.RightBracket);
                return new ArrayExpression(elements, token.Position);
            }

            throw new Error("Invalid expression", token.Position);
        }

        Expression ParsePostfix()
        {
            Expression left = ParsePrimary();
            while (Check(TokenKind.Member, TokenKind.LeftBracket))
            {
                if (Check(TokenKind.Member))
                {
                    Position position = Current().Position;

                    Next();

                    string member = ParseName();

                    left = new MemberExpression(left, member, position);
                    continue;
                }

                if (Check(TokenKind.LeftBracket))
                {
                    Position position = Current().Position;

                    Next();

                    Expression index = ParseExpression();

                    Expect(TokenKind.RightBracket);

                    left = new IndexExpression(left, index, position);
                    continue;
                }

                break;
            }
            return left;
        }

        Expression ParseUnary()
        {
            if (Check(TokenKind.Sub, TokenKind.Not))
            {
                Token op = Current();

                Next();

                return new UnaryExpression(ParseUnary(), op.Kind, op.Position);
            }

            return ParsePostfix();
        }

        Expression ParseFactor()
        {
            Expression left = ParseUnary();

            while (Check(TokenKind.Mul, TokenKind.Div, TokenKind.Mod))
            {
                Token op = Current();

                Next();

                Expression right = ParseUnary();

                left = new BinaryExpression(left, right, op.Kind, op.Position);
            }

            return left;
        }

        Expression ParseTerm()
        {
            Expression left = ParseFactor();

            while (Check(TokenKind.Add, TokenKind.Sub))
            {
                Token op = Current();

                Next();

                Expression right = ParseFactor();

                left = new BinaryExpression(left, right, op.Kind, op.Position);
            }

            return left;
        }

        Expression ParseComparison()
        {
            Expression left = ParseTerm();

            while (Check(TokenKind.Less, TokenKind.Greater, TokenKind.LessEqual, TokenKind.GreaterEqual))
            {
                Token op = Current();

                Next();

                Expression right = ParseTerm();

                left = new BinaryExpression(left, right, op.Kind, op.Position);
            }

            return left;
        }

        Expression ParseEquality()
        {
            Expression left = ParseComparison();

            while (Check(TokenKind.EqualEqual, TokenKind.NotEqual))
            {
                Token op = Current();

                Next();

                Expression right = ParseComparison();

                left = new BinaryExpression(left, right, op.Kind, op.Position);
            }

            return left;
        }

        Expression ParseExpression() => ParseEquality();

        Token Current() => _tokens[_i];
        bool Check(params TokenKind[] kinds) =>
            kinds.Contains(Current().Kind);
        bool NotAtEnd() => !Check(TokenKind.EndOfFile);

        void Consume(string msg, params TokenKind[] kinds)
        {
            if (Check(kinds))
            {
                Next();
                return;
            }
            throw new Error(msg, Current().Position);
        }

        bool Match(params TokenKind[] kinds) 
        {
            if (Check(kinds))
            {
                Next();
                return true;
            }
            return false;
        }

        void Expect(TokenKind kind)
        {
            if (Check(kind))
            {
                Next();
                return;
            }
            if (kind == TokenKind.Empty)
                return;
            string? keywordValue = Lexer.GetKeywordFromKind(kind);
            if (keywordValue != null)
                throw new Error($"Expected keyword '{keywordValue}'", Current().Position);

            string? symbolValue = Lexer.GetSymbolFromKind(kind);
            if (symbolValue != null)
                throw new Error($"Expected symbol '{symbolValue}'", Current().Position);

            throw new Error($"Syntax error", Current().Position);
        }

        void Next() => _i++;
    }
}
