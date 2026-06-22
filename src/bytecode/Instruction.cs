using System;
using System.Collections.Generic;
using System.Text;

namespace gladyrisk_lang.src.bytecode
{
    internal enum OpCode
    {
        LoadConst,
        LoadLocal,
        StoreLocal,
        LoadFunction,

        GetMember,
        MakeArray,

        Call,
        Jmp,
        JmpFalse,
        JmpTrue,

        Flip,
        Neg,
        Add,
        Sub,
        Mul,
        Div,
        Mod,
        Less,
        Greater,
        LessEqual,
        GreaterEqual,
        EqualEqual,
        NotEqual,

        Ret,
    }

    internal struct Instruction
    {
        public Instruction() { }

        public Instruction(int positionIndex, OpCode opCode)
        {
            PositionIndex = positionIndex;
            OpCode = opCode;
        }

        public Instruction(int positionIndex, OpCode opCode, int a)
        {
            PositionIndex = positionIndex;
            OpCode = opCode;
            A = a;
        }

        public Instruction(int positionIndex, OpCode opCode, int a, int b)
        {
            PositionIndex = positionIndex;
            OpCode = opCode;
            A = a;
            B = b;
        }

        public Instruction(int positionIndex, OpCode opCode, int a, int b, int c)
        {
            PositionIndex = positionIndex;
            OpCode = opCode;
            A = a;
            B = b;
            C = c;
        }

        public int PositionIndex { get; set; }
        public OpCode OpCode { get; set; }
        public int A { get; set; }
        public int B { get; set; }
        public int C { get; set; }
        public int[] Args { get; set; } = [];

        public static Instruction Call(int dest, int callee, int[] args, int pos)
        {
            return new Instruction { A = dest, B = callee, Args = args, PositionIndex = pos, OpCode = OpCode.Call };
        }

        public static Instruction Array(int dest, int[] elements, int pos)
        {
            return new Instruction { A = dest, Args = elements, PositionIndex = pos, OpCode = OpCode.MakeArray };
        }

        public static Instruction Operator(int result, int left, int right, OpCode op, int pos)
        {
            return new Instruction { A = result, B = left, C = right, OpCode = op, PositionIndex = pos };
        }

        public override string ToString()
        {
            return $"{OpCode}, {A}, {B}, {C}, [{string.Join(", ", Args)}]";
        }
    }
}
