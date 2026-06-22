using gladyrisk_lang.src.bytecode.compiler;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace gladyrisk_lang.src.bytecode.runtime
{
    internal sealed class CallFrame
    {
        public FunctionInfo Function { get; }
        public Chunk Program { get; }
        public int Ip { get; set; }
        public Value[] Registers { get; }
        public Value[] Locals { get; }
        public int ReturnRegister { get; }

        public CallFrame(FunctionInfo function, int returnRegister)
        {
            Function = function;
            Program = function.Chunk;
            Ip = 0;
            Registers = new Value[function.Chunk.MaxRegisters];
            Locals = new Value[function.Chunk.LocalCount];
            ReturnRegister = returnRegister;
        }
    }

    internal class Vm
    {
        public static string Run(Chunk chunk)
        {
            Stack<CallFrame> callFrames = new Stack<CallFrame>();
            callFrames.Push(new CallFrame(chunk.Main!, -1));

            FunctionObject[] functionObjects = new FunctionObject[chunk.FunctionInfos.Count];
            for (int i = 0; i < chunk.FunctionInfos.Count; i++)
            {
                functionObjects[i] = new FunctionObject(chunk.FunctionInfos[i]);
            }

            for (; ; )
            {
                var frame = callFrames.Peek();
                var instruction = frame.Program.Instructions[frame.Ip++];

                switch (instruction.OpCode)
                {
                    case OpCode.LoadConst:
                        {
                            frame.Registers[instruction.A] = frame.Program.Constants[instruction.B];
                            break;
                        }

                    case OpCode.LoadLocal:
                        {
                            frame.Registers[instruction.A] = frame.Locals[instruction.B];
                            break;
                        }

                    case OpCode.StoreLocal:
                        {
                            frame.Locals[instruction.A] = frame.Registers[instruction.B];
                            break;
                        }

                    case OpCode.LoadFunction:
                        {
                            frame.Registers[instruction.A] = Value.FromFunction(functionObjects[instruction.B]);
                            break;
                        }

                    case OpCode.GetMember:
                        {
                            int result = instruction.A;
                            ref Value target = ref frame.Registers[instruction.B];
                            ref Value memberName = ref frame.Program.Constants[instruction.C];
                            string name = memberName.Expect(ValueKind.Text, GetPosition(instruction.PositionIndex)).Text;
                            if (target.Check(ValueKind.Namespace))
                            {
                                var namespaceObj = target.ObjectAs<NamespaceObject>();
                                if (namespaceObj.Values.TryGetValue(name, out var value))
                                {
                                    frame.Registers[result] = value;
                                    break;
                                }
                                throw new Error($"Namespace '{namespaceObj.Name}' does not have '{name}'");
                            }
                            throw new Error($"Type: {target.ValueKind} has no members", GetPosition(instruction.PositionIndex));
                        }

                    case OpCode.GetIndex:
                        {
                            ref Position pos = ref GetPosition(instruction.PositionIndex);
                            int result = instruction.A;
                            ref Value target = ref frame.Registers[instruction.B];
                            target.ExpectTypes(pos, ValueKind.Array, ValueKind.Text);
                            int index = frame.Registers[instruction.C].GetInt(pos);
                            if (target.Check(ValueKind.Array))
                            {
                                var array = target.ObjectAs<ArrayObject>();
                                if (index < 0 || index >= array.Count)
                                    throw new Error("Index out of bounds", pos);
                                frame.Registers[result] = array.Elements[index];
                            }
                            else
                            {
                                string text = target.Text;
                                if (index < 0 || index >= text.Length)
                                    throw new Error("Index out of bounds", pos);
                                frame.Registers[result] = new Value(text[index].ToString());
                            }
                            break;
                        }

                    case OpCode.SetIndex:
                        {
                            ref Position pos = ref GetPosition(instruction.PositionIndex);
                            int result = instruction.A;
                            ref Value target = ref frame.Registers[instruction.B];
                            target.Expect(ValueKind.Array, pos);
                            int index = frame.Registers[instruction.C].GetInt(pos);
                            ref Value value = ref frame.Registers[instruction.D];
                            var array = target.ObjectAs<ArrayObject>();
                            if (index < 0 || index >= array.Count)
                                throw new Error("Index out of bounds", pos);
                            array.Elements[index] = value;
                            break;
                        }

                    case OpCode.MakeArray:
                        {
                            int result = instruction.A;
                            List<Value> elements = new List<Value>();
                            for (int i = 0; i < instruction.Args.Length; i++)
                                elements.Add(frame.Registers[instruction.Args[i]]);
                            frame.Registers[result] = Value.FromArray(new ArrayObject(elements));
                            break;
                        }

                    case OpCode.Call:
                        {
                            var result = instruction.A;
                            ref var callee = ref frame.Registers[instruction.B];
                            if (callee.Check(ValueKind.Function))
                            {
                                FunctionObject functionObject = callee.ObjectAs<FunctionObject>();
                                if (instruction.Args.Length != functionObject.Arity)
                                    throw new Error($"Function: {functionObject.Name} expects {functionObject.Arity} argument(s), got only {instruction.Args.Length}", GetPosition(instruction.PositionIndex));
                                var calleeFrame = new CallFrame(functionObject.FunctionInfo, instruction.A);
                                for (int i = 0; i < instruction.Args.Length; i++)
                                    calleeFrame.Locals[i] = frame.Registers[instruction.Args[i]];
                                callFrames.Push(calleeFrame);
                            }
                            if (callee.Check(ValueKind.Native))
                            {
                                var pos = GetPosition(instruction.PositionIndex);
                                NativeObject nativeObject = callee.ObjectAs<NativeObject>();
                                ValidateNativeArgs(instruction.Args.Length, nativeObject.Arity, nativeObject.MaxArity, nativeObject.ArgMode, nativeObject.Name, pos);
                                List<Value> args = new List<Value>();
                                for (int i = 0; i < instruction.Args.Length; i++)
                                    args.Add(frame.Registers[instruction.Args[i]]);
                                frame.Registers[result] = nativeObject.Function(args, pos);
                            }
                            break;
                        }

                    case OpCode.Jmp:
                        {
                            frame.Ip = instruction.A;
                            break;

                        }

                    case OpCode.JmpFalse:
                        {
                            if (!frame.Registers[instruction.B].Bool)
                                frame.Ip = instruction.A;
                            break;
                        }

                    case OpCode.JmpTrue:
                        {
                            if (frame.Registers[instruction.B].Bool)
                                frame.Ip = instruction.A;
                            break;
                        }

                    case OpCode.Flip:
                        {
                            ref var value = ref frame.Registers[instruction.B];
                            if (value.Check(ValueKind.Bool))
                            {
                                frame.Registers[instruction.A] = new Value(!value.Bool);
                                break;
                            }
                            throw new Error($"Type: {value.ValueKind} can't be flipped", GetPosition(instruction.PositionIndex));
                        }

                    case OpCode.Neg:
                        {
                            ref var value = ref frame.Registers[instruction.B];
                            if (value.Check(ValueKind.Number))
                            {
                                frame.Registers[instruction.A] = new Value(-value.Number);
                                break;
                            }
                            throw new Error($"Type: {value.ValueKind} can't be negated", GetPosition(instruction.PositionIndex));
                        }

                    case OpCode.Add:
                        {
                            int result = instruction.A;
                            ref var  left = ref frame.Registers[instruction.B];
                            ref var right = ref frame.Registers[instruction.C];
                            if (left.Check(ValueKind.Number) && right.Check(ValueKind.Number))
                            {
                                frame.Registers[result] = new Value(left.Number + right.Number);
                                break;
                            }
                            else if (left.Check(ValueKind.Text) && right.Check(ValueKind.Text))
                            {
                                frame.Registers[result] = new Value(left.Text + right.Text);
                                break;
                            }
                            throw new Error($"Types: {left.ValueKind} and {right.ValueKind} cannot be added", GetPosition(instruction.PositionIndex));
                        }

                    case OpCode.Sub:
                        {
                            int result = instruction.A;
                            ref var left = ref frame.Registers[instruction.B];
                            ref var right = ref frame.Registers[instruction.C];
                            if (left.Check(ValueKind.Number) && right.Check(ValueKind.Number))
                            {
                                frame.Registers[result] = new Value(left.Number - right.Number);
                                break;
                            }
                            throw new Error($"Types: {left.ValueKind} and {right.ValueKind} cannot be subtracted", GetPosition(instruction.PositionIndex));
                        }

                    case OpCode.Mul:
                        {
                            int result = instruction.A;
                            ref var left = ref frame.Registers[instruction.B];
                            ref var right = ref frame.Registers[instruction.C];
                            if (left.Check(ValueKind.Number) && right.Check(ValueKind.Number))
                            {
                                frame.Registers[result] = new Value(left.Number * right.Number);
                                break;
                            }
                            throw new Error($"Types: {left.ValueKind} and {right.ValueKind} cannot be multiplied", GetPosition(instruction.PositionIndex));
                        }

                    case OpCode.Div:
                        {
                            int result = instruction.A;
                            ref var left = ref frame.Registers[instruction.B];
                            ref var right = ref frame.Registers[instruction.C];
                            if (left.Check(ValueKind.Number) && right.Check(ValueKind.Number))
                            {
                                if (right.Number == 0)
                                    throw new Error("Division by zero", GetPosition(instruction.PositionIndex));
                                frame.Registers[result] = new Value(left.Number / right.Number);
                                break;
                            }
                            throw new Error($"Types: {left.ValueKind} and {right.ValueKind} cannot be divided", GetPosition(instruction.PositionIndex));
                        }

                    case OpCode.Mod:
                        {
                            int result = instruction.A;
                            ref var left = ref frame.Registers[instruction.B];
                            ref var right = ref frame.Registers[instruction.C];
                            if (left.Check(ValueKind.Number) && right.Check(ValueKind.Number))
                            {
                                if (right.Number == 0)
                                    throw new Error("Modulo by zero", GetPosition(instruction.PositionIndex));
                                frame.Registers[result] = new Value(left.Number % right.Number);
                                break;
                            }
                            throw new Error($"Types: {left.ValueKind} and {right.ValueKind} cannot be modded", GetPosition(instruction.PositionIndex));
                        }

                    case OpCode.Less:
                        {
                            int result = instruction.A;
                            ref var left = ref frame.Registers[instruction.B];
                            ref var right = ref frame.Registers[instruction.C];
                            if (left.Check(ValueKind.Number) && right.Check(ValueKind.Number))
                            {
                                frame.Registers[result] = new Value(left.Number < right.Number);
                                break;
                            }
                            throw new Error($"Types: {left.ValueKind} and {right.ValueKind} cannot be compared", GetPosition(instruction.PositionIndex));
                        }

                    case OpCode.Greater:
                        {
                            int result = instruction.A;
                            ref var left = ref frame.Registers[instruction.B];
                            ref var right = ref frame.Registers[instruction.C];
                            if (left.Check(ValueKind.Number) && right.Check(ValueKind.Number))
                            {
                                frame.Registers[result] = new Value(left.Number > right.Number);
                                break;
                            }
                            throw new Error($"Types: {left.ValueKind} and {right.ValueKind} cannot be compared", GetPosition(instruction.PositionIndex));
                        }

                    case OpCode.LessEqual:
                        {
                            int result = instruction.A;
                            ref var left = ref frame.Registers[instruction.B];
                            ref var right = ref frame.Registers[instruction.C];
                            if (left.Check(ValueKind.Number) && right.Check(ValueKind.Number))
                            {
                                frame.Registers[result] = new Value(left.Number <= right.Number);
                                break;
                            }
                            throw new Error($"Types: {left.ValueKind} and {right.ValueKind} cannot be compared", GetPosition(instruction.PositionIndex));
                        }

                    case OpCode.GreaterEqual:
                        {
                            int result = instruction.A;
                            ref var left = ref frame.Registers[instruction.B];
                            ref var right = ref frame.Registers[instruction.C];
                            if (left.Check(ValueKind.Number) && right.Check(ValueKind.Number))
                            {
                                frame.Registers[result] = new Value(left.Number >= right.Number);
                                break;
                            }
                            throw new Error($"Types: {left.ValueKind} and {right.ValueKind} cannot be compared", GetPosition(instruction.PositionIndex));
                        }

                    case OpCode.EqualEqual:
                        {
                            int result = instruction.A;
                            ref var left = ref frame.Registers[instruction.B];
                            ref var right = ref frame.Registers[instruction.C];
                            frame.Registers[result] = new Value(left.CheckEquality(ref right));
                            break;
                        }

                    case OpCode.NotEqual:
                        {
                            int result = instruction.A;
                            ref var left = ref frame.Registers[instruction.B];
                            ref var right = ref frame.Registers[instruction.C];
                            frame.Registers[result] = new Value(!left.CheckEquality(ref right));
                            break;
                        }

                    case OpCode.Ret:
                        {
                            ref Value result = ref frame.Registers[instruction.A];

                            callFrames.Pop();

                            if (callFrames.Count == 0)
                                return result.ToString();

                            var caller = callFrames.Peek();
                            caller.Registers[frame.ReturnRegister] = result;
                            break;
                        }
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            ref Position GetPosition(int index) => ref callFrames.Peek().Program.Positions[index];

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            void ValidateNativeArgs(int got, int arity, int maxArity, ArgMode argMode, string name, Position position)
            {
                if (argMode == ArgMode.Unlimited)
                    return;
                else if (argMode == ArgMode.Atleast)
                    if (got < arity)
                        throw new Error($"Native function: '{name}' requires atleast {arity} argument(s)", position);
                else if (argMode == ArgMode.Optional)
                    if (got <= arity)
                        throw new Error($"Native function: '{name}' expects at most {arity} argument(s), got {got}", position);
                else if (argMode == ArgMode.Expect)
                    if (got != arity)
                        throw new Error($"Native function: '{name}' expects {arity} argument(s), got {got}", position);
                else if (argMode == ArgMode.Clamp)
                    if (got < arity || got > maxArity)
                        throw new Error($"Native function: '{name}' expects between {arity} and {maxArity} argument(s), got {got}", position);

            }
        }
    }
}
