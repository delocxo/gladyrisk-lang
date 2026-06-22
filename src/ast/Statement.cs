using System;
using System.Collections.Generic;
using System.Text;

namespace gladyrisk_lang.src.ast
{
    internal abstract record Statement(Position Position, string ActualName);
    internal record FnStatement(List<Statement> Body, string Name, List<string> Parameters, Position Position) : Statement(Position, "fn");
    internal record VarStatement(string Name, Expression? Expression, Position Position) : Statement(Position, "var");
    internal record VarMovStatement(string name, Expression Expression, Position Position) : Statement(Position, "mov");
    internal record LabelStatement(string Label, Position Position) : Statement(Position, "label");
    internal record JmpStatement(string Label, Position Position) : Statement(Position, "jmp");
    internal record JmpFalseStatement(string Label, Expression Expression, Position Position) : Statement(Position, "jmpfalse");
    internal record JmpTrueStatement(string Label, Expression Expression, Position Position) : Statement(Position, "jmptrue");
    internal record CallStatement(CallExpression CallExpression, Position Position) : Statement(Position, "call");
    internal record RetStatement(Expression Expression, Position Position) : Statement(Position, "ret");
}
