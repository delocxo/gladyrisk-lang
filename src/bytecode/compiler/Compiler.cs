using gladyrisk_lang.src.ast;
using gladyrisk_lang.src.bytecode.runtime;
using System;
using System.Text;
using System.Xml.Linq;

namespace gladyrisk_lang.src.bytecode.compiler
{
    internal sealed class Chunk
    {
        public Chunk(List<Instruction> instructions, List<Position> positions, List<Value> constants, List<FunctionInfo> functionInfos, FunctionInfo? main, int localCount, int maxRegisters)
        {
            Instructions = instructions;
            Positions = positions.ToArray();
            Constants = constants.ToArray();
            FunctionInfos = functionInfos;
            LocalCount = localCount;
            Main = main;
            MaxRegisters = maxRegisters;
        }

        public static Chunk Empty() => new Chunk([], [], [], [], null, 0, 0);

        public List<Instruction> Instructions { get; }
        public Position[] Positions { get; }
        public Value[] Constants { get; }
        public List<FunctionInfo> FunctionInfos { get; }
        public FunctionInfo? Main { get; }
        public int LocalCount { get; }
        public int MaxRegisters { get; }
    }

    internal class Compiler
    {
        public Dictionary<string, int> Locals { get; } = new Dictionary<string, int>();

        Dictionary<string, int> _labels = new Dictionary<string, int>();
        List<FunctionInfo> _functionInfos = new List<FunctionInfo>();
        List<Instruction> _instructions = new List<Instruction>();
        List<Position> _positions = new List<Position>();
        List<Value> _constants = new List<Value>();
        Dictionary<int, string> _jumps = new Dictionary<int, string>();

        public FunctionInfo? Main = null;

        bool _atGlobal;
        int _currentRegister = 0;
        int _maxRegister = 0;
        public int NextSlot { get; set; } = 0;

        int NewRegister()
        {
            int reg = _currentRegister;
            _currentRegister++;
            if (_currentRegister > _maxRegister)
                _maxRegister = _currentRegister;
            return reg;
        }

        public Compiler(List<FunctionInfo> functionInfos, bool atGlobal = true)
        {
            _atGlobal = atGlobal;
            _functionInfos = functionInfos;
        }

        public Chunk CompileStatements(List<Statement> statements)
        {
            statements.ForEach(x =>
            {
                if (x is FnStatement fnStatement)
                {
                    if (!_atGlobal)
                        throw new Error("Functions can only be declared at the top level", fnStatement.Position);
                    int index = _functionInfos.FindIndex(x => x.Name == fnStatement.Name);
                    if (index != -1)
                        throw new Error($"Function '{fnStatement.Name}' already exist", fnStatement.Position);
                    if (fnStatement.Body.Count == 0 || fnStatement.Body[fnStatement.Body.Count - 1] is not RetStatement)
                        throw new Error($"Function '{fnStatement.Name}' must explicitly return a value", fnStatement.Position);
                    _functionInfos.Add(new FunctionInfo(Chunk.Empty(), fnStatement.Name, fnStatement.Parameters.Count));
                }
                else if (x is LabelStatement labelStatement && !_atGlobal)
                {
                    if (_labels.ContainsKey(labelStatement.Label))
                        throw new Error($"Label '{labelStatement.Label}' already exist", labelStatement.Position);
                    _labels.Add(labelStatement.Label, 0); // Placeholder, will be patched later
                }
            });
            statements.ForEach(x =>
            {
                if (x is FnStatement || x is LabelStatement)
                    return;
                if ((x is JmpStatement jmpStatement) && !_atGlobal)
                {
                    if (!_labels.ContainsKey(jmpStatement.Label))
                        throw new Error($"'{jmpStatement.Label}' is not an existing label", jmpStatement.Position);
                }
                else if ((x is JmpFalseStatement jmpFalseStatement) && !_atGlobal)
                {
                    if (!_labels.ContainsKey(jmpFalseStatement.Label))
                        throw new Error($"'{jmpFalseStatement.Label}' is not an existing label", jmpFalseStatement.Position);
                }
                else if ((x is JmpTrueStatement jmpTrueStatement) && !_atGlobal)
                {
                    if (!_labels.ContainsKey(jmpTrueStatement.Label))
                        throw new Error($"'{jmpTrueStatement.Label}' is not an existing label", jmpTrueStatement.Position);
                }
                else
                    if (_atGlobal)
                        throw new Error($"'{x.ActualName}' cannot be used at the top level", x.Position);
            });
            if (_atGlobal)
            {
                int mainIndex = _functionInfos.FindIndex(x => x.Name == "main");
                if (mainIndex == -1)
                    throw new Error("No main function was found");
                Main = _functionInfos[mainIndex];
                _functionInfos.RemoveAt(mainIndex);
            }
            statements.ForEach(CompileStatement);
            PatchAllJumps();
            return new Chunk(_instructions, _positions, _constants, _functionInfos, Main, NextSlot, _maxRegister);
        }

        void CompileStatement(Statement statement)
        {
            if (statement is FnStatement fnStatement)
            {
                Compiler compiler = new Compiler(_functionInfos, false);
                for (int i = 0; i < fnStatement.Parameters.Count; i++)
                    compiler.Locals[fnStatement.Parameters[i]] = compiler.NextSlot++;
                Chunk program = compiler.CompileStatements(fnStatement.Body);
                if (fnStatement.Name == "main")
                {
                    Main = new FunctionInfo(program, fnStatement.Name, fnStatement.Parameters.Count);
                    return;
                }
                int fnIndex = _functionInfos.FindIndex(x => x.Name == fnStatement.Name);
                _functionInfos[fnIndex] = new FunctionInfo(program, fnStatement.Name, fnStatement.Parameters.Count);
                
            }
            else if (statement is VarStatement varStatement)
            {
                ValidateName(varStatement.Name, varStatement.Position);
                int result = CompileExpression(varStatement.Expression!);
                Locals[varStatement.Name] = NextSlot++;
                Emit(new Instruction(AddPosition(varStatement.Position), OpCode.StoreLocal, Locals[varStatement.Name], result));
            }
            else if (statement is CallStatement callStatement)
            {
                CompileExpression(callStatement.CallExpression);
            }
            else if (statement is VarMovStatement varMovStatement)
            {
                if (!Locals.TryGetValue(varMovStatement.name, out int slot))
                    throw new Error($"'{varMovStatement.name}' does not exist", varMovStatement.Position);
                int result = CompileExpression(varMovStatement.Expression);
                Emit(new Instruction(AddPosition(varMovStatement.Position), OpCode.StoreLocal, slot, result));
            }
            else if (statement is LabelStatement labelStatement)
            {
                PatchLabel(labelStatement.Label);
            }
            else if (statement is JmpStatement jmpStatement)
            {
                Emit(new Instruction(AddPosition(jmpStatement.Position), OpCode.Jmp));
                _jumps[_instructions.Count - 1] = jmpStatement.Label;
            }
            else if (statement is JmpFalseStatement jmpFalseStatement)
            {
                int register = CompileExpression(jmpFalseStatement.Expression);
                Emit(new Instruction(AddPosition(jmpFalseStatement.Position), OpCode.JmpFalse, 0, register));
                _jumps[_instructions.Count - 1] = jmpFalseStatement.Label;
            }
            else if (statement is JmpTrueStatement jmpTrueStatement)
            {
                int register = CompileExpression(jmpTrueStatement.Expression);
                Emit(new Instruction(AddPosition(jmpTrueStatement.Position), OpCode.JmpTrue, 0, register));
                _jumps[_instructions.Count - 1] = jmpTrueStatement.Label;
            }
            else if (statement is RetStatement retStatement)
            {
                int register = CompileExpression(retStatement.Expression);
                Emit(new Instruction(AddPosition(retStatement.Position), OpCode.Ret, register));
            }
            else if (statement is IndexMovStatement indexMovStatement)
            {
                IndexExpression indexExpression = indexMovStatement.IndexExpression;
                int target = CompileExpression(indexExpression.Target);
                int index = CompileExpression(indexExpression.Index);
                int value = CompileExpression(indexMovStatement.Expression);
                int result = NewRegister();
                Emit(new Instruction(AddPosition(indexExpression.Position), OpCode.SetIndex, result, target, index, value));
            }
        
            _currentRegister = 0;
        }

        int CompileExpression(Expression expression)
        {
            if (expression is NumberExpression numberExpression)
            {
                int register = NewRegister();
                Emit(new Instruction(AddPosition(expression.Position), OpCode.LoadConst, register, AddConstant(new Value(numberExpression.Value))));
                return register;
            }
            else if (expression is StringExpression stringExpression)
            {
                int register = NewRegister();
                Emit(new Instruction(AddPosition(expression.Position), OpCode.LoadConst, register, AddConstant(new Value(stringExpression.Value))));
                return register;
            }
            else if (expression is BoolExpression boolExpression)
            {
                int register = NewRegister();
                Emit(new Instruction(AddPosition(expression.Position), OpCode.LoadConst, register, AddConstant(new Value(boolExpression.Value))));
                return register;
            }
            else if (expression is NullExpression nullExpression)
            {
                int register = NewRegister();
                Emit(new Instruction(AddPosition(expression.Position), OpCode.LoadConst, register, AddConstant(Value.Null)));
                return register;
            }
            else if (expression is NameExpression nameExpression)
            {
                int index = _functionInfos.FindIndex(x => x.Name == nameExpression.Name);
                if (index != -1)
                {
                    int register = NewRegister();
                    Emit(new Instruction(AddPosition(expression.Position), OpCode.LoadFunction, register, index));
                    return register;
                }
                else if (Locals.TryGetValue(nameExpression.Name, out int slot))
                {
                    int register = NewRegister();
                    Emit(new Instruction(AddPosition(expression.Position), OpCode.LoadLocal, register, slot));
                    return register;
                }
                else if (NativeGlobals.ValueGlobals.TryGetValue(nameExpression.Name, out var global))
                {
                    int register = NewRegister();
                    Emit(new Instruction(AddPosition(expression.Position), OpCode.LoadConst, register, AddConstant(global)));
                    return register;
                }
                throw new Error($"'{nameExpression.Name}' does not exist", nameExpression.Position);
            }
            else if (expression is ArrayExpression arrayExpression)
            {
                List<int> elements = new List<int>();
                foreach (Expression element in arrayExpression.Items)
                {
                    elements.Add(CompileExpression(element));
                }

                int result = NewRegister();
                Emit(Instruction.Array(result, elements.ToArray(), AddPosition(arrayExpression.Position)));
                return result;
            }
            else if (expression is MemberExpression memberExpression)
            {
                int target = CompileExpression(memberExpression.Target);
                int result = NewRegister();
                Emit(new Instruction(AddPosition(memberExpression.Position), OpCode.GetMember, result, target, AddConstant(new Value(memberExpression.Member))));
                return result;
            }
            else if (expression is IndexExpression indexExpression)
            {
                int target = CompileExpression(indexExpression.Target);
                int index = CompileExpression(indexExpression.Index);
                int result = NewRegister();
                Emit(new Instruction(AddPosition(indexExpression.Position), OpCode.GetIndex, result, target, index));
                return result;
            }
            else if (expression is UnaryExpression unaryExpression)
            {
                int valueReg = CompileExpression(unaryExpression.Right);
                int register = NewRegister();
                if (unaryExpression.Op == TokenKind.Sub)
                {
                    Emit(new Instruction(AddPosition(expression.Position), OpCode.Neg, register, valueReg));
                    return register;
                }
                else
                {
                    Emit(new Instruction(AddPosition(expression.Position), OpCode.Flip, register, valueReg));
                    return register;
                }
            }
            else if (expression is CallExpression callExpression)
            {
                int target = CompileExpression(callExpression.Right);

                List<int> args = new List<int>();
                foreach (Expression arg in callExpression.Arguments)
                {
                    args.Add(CompileExpression(arg));
                }

                int result = NewRegister();
                Emit(Instruction.Call(result, target, args.ToArray(), AddPosition(callExpression.Position)));
                return result;
            }
            else if (expression is BinaryExpression binaryExpression)
            {
                int left = CompileExpression(binaryExpression.Left);
                int right = CompileExpression(binaryExpression.Right);
                int result = NewRegister();

                int posIndex = AddPosition(binaryExpression.Position);

                OpCode op = binaryExpression.Op switch
                {
                    TokenKind.Add => OpCode.Add,
                    TokenKind.Sub => OpCode.Sub,
                    TokenKind.Mul => OpCode.Mul,
                    TokenKind.Div => OpCode.Div,
                    TokenKind.Mod => OpCode.Mod,

                    TokenKind.Less => OpCode.Less,
                    TokenKind.Greater => OpCode.Greater,
                    TokenKind.LessEqual => OpCode.LessEqual,
                    TokenKind.GreaterEqual => OpCode.GreaterEqual,
                    TokenKind.EqualEqual => OpCode.EqualEqual,
                    TokenKind.NotEqual => OpCode.NotEqual,

                    _ => throw new Error("Invalid binary operator", binaryExpression.Position)
                };

                Emit(Instruction.Operator(result, left, right, op, posIndex));
                return result;
            }
            throw new Error("Invalid Expression", expression.Position);
        }

        int AddConstant(Value value)
        {
            int index = _constants.IndexOf(value);
            if (index != -1)
                return index;
            _constants.Add(value);
            return _constants.Count - 1;
        }

        int AddPosition(Position position)
        {
            _positions.Add(position);
            return _positions.Count - 1;
        }

        void Emit(Instruction instruction) => _instructions.Add(instruction);

        void PatchLabel(string name) => _labels[name] = _instructions.Count;

        void PatchAllJumps()
        {
            foreach (var jump in _jumps)
            {
                var instruction = _instructions[jump.Key];
                instruction.A = _labels[jump.Value];
                _instructions[jump.Key] = instruction;
            }
        }

        void ValidateName(string name, Position position)
        {
            if (Locals.ContainsKey(name))
                throw new Error($"'{name}' is already a variable", position);
            if (_functionInfos.FindIndex(x => x.Name == name) != -1)
                throw new Error($"'{name}' is already a function");
            if (NativeGlobals.ValueGlobals.ContainsKey(name))
                throw new Error($"'{name}' is already a global", position);
        }
    }
}
