using System;
using System.Collections.Generic;
using System.Text;

namespace gladyrisk_lang.src
{
    internal enum TokenKind 
    {
        Identifier, String, Number,
        
        Fn, End, True, False, Null, Var, 
        Mov, Label, Jmp, JmpFalse, JmpTrue, Ret,
        Call, Use,

        Add, Sub, Mul, Div, Mod,
        Less, Greater, LessEqual, GreaterEqual,
        EqualEqual, NotEqual, Not, LeftParen,
        RightParen,

        Comma, Semicolon, Member, LeftBracket,
        RightBracket,

        EndOfFile, Empty
    }

    internal record Token(string Lexeme, TokenKind Kind, Position Position);
}
