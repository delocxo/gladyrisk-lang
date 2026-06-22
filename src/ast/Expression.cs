using System;
using System.Collections.Generic;
using System.Text;

namespace gladyrisk_lang.src.ast
{
    internal abstract record Expression(Position Position);
    internal record NumberExpression(double Value, Position Position) : Expression(Position);
    internal record StringExpression(string Value, Position Position) : Expression(Position);
    //internal record FStringExpression(object? Contents, Position Position) : Expression(Position);
    internal record BoolExpression(bool Value, Position Position) : Expression(Position);
    internal record NullExpression(Position Position) : Expression(Position);
    internal record NameExpression(string Name, Position Position) : Expression(Position);
    internal record ArrayExpression(List<Expression> Items, Position Position) : Expression(Position);
    internal record IndexExpression(Expression Target, Expression Index, Position Position) : Expression(Position);
    internal record MemberExpression(Expression Target, string Member, Position Position) : Expression(Position);
    internal record UnaryExpression(Expression Right, TokenKind Op, Position Position) : Expression(Position);
    internal record CallExpression(Expression Right, List<Expression> Arguments, Position Position) : Expression(Position);
    internal record BinaryExpression(Expression Left, Expression Right, TokenKind Op, Position Position) : Expression(Position);
}
