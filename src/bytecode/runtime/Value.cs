using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace gladyrisk_lang.src.bytecode.runtime
{
    internal abstract class RuntimeObject {}

    internal enum ArgMode
    {
        Expect,
        Unlimited,
        Atleast,
        Optional,
        Clamp,
    }

    internal class FunctionObject : RuntimeObject
    {
        public FunctionObject(FunctionInfo functionInfo)
        {
            FunctionInfo = functionInfo;
        }

        public FunctionInfo FunctionInfo { get; }
        public string Name => FunctionInfo.Name;
        public int Arity => FunctionInfo.Arity;
    }

    internal class NativeObject : RuntimeObject
    {
        public NativeObject(string name, int arity, ArgMode argMode, Func<List<Value>, Position, Value> function)
        {
            Name = name;
            Arity = arity;
            Function = function;
            ArgMode = argMode;
        }

        public NativeObject(string name, int arity, int maxArity, ArgMode argMode, Func<List<Value>, Position, Value> function)
        {
            Name = name;
            Arity = arity;
            MaxArity = maxArity;
            Function = function;
            ArgMode = argMode;
        }

        public string Name { get; }
        public int Arity { get; }
        public int MaxArity { get; }
        public Func<List<Value>, Position, Value> Function { get; }
        public ArgMode ArgMode { get; }
    }

    internal class NamespaceObject : RuntimeObject
    {
        public NamespaceObject(string name, Dictionary<string, Value> values)
        {
            Name = name;
            Values = values;
        }

        public string Name { get; }
        public Dictionary<string, Value> Values { get; }
    }

    internal class ArrayObject : RuntimeObject
    {
        public ArrayObject(List<Value> elements)
        {
            Elements = elements;
        }

        public List<Value> Elements { get; }
        public int Count => Elements.Count;
    }

    internal class EnumObject : RuntimeObject
    {
        public EnumObject(string enumName, string memberName, int raw)
        {
            EnumName = enumName;
            MemberName = memberName;
            Raw = raw;
        }

        public string EnumName { get; }
        public string MemberName { get; }
        public int Raw { get; }
    }

    internal enum ValueKind
    {
        Null,
        Number,
        Bool,
        Text,
        Function,
        Native,
        Namespace,
        Array,
        Enum
    }

    internal struct Value
    {
        public Value(double number) : this()
        {
            ValueKind = ValueKind.Number;
            Number = number;
        }

        public Value(bool @bool) : this()
        {
            ValueKind = ValueKind.Bool;
            Bool = @bool;
        }

        public Value(string text) : this()
        {
            ValueKind = ValueKind.Text;
            Text = text;
        }

        public Value(ValueKind kind)
        {
            ValueKind = kind;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Value Expect(ValueKind kind, Position position)
        {
            if (!Check(kind))
                throw new Error($"Expected type: {kind}", position);
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Value ExpectTypes(Position position, params ValueKind[] kinds)
        {
            for (int i = 0; i < kinds.Length; i++)
            {
                if (Check(kinds[i]))
                    return this;
            }
            throw new Error($"Expected types: [{string.Join(", ", kinds)}]", position);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetInt(Position position)
        {
            if (ValueKind == ValueKind.Number && double.IsInteger(Number))
                return (int)Number;
            throw new Error("Expected a whole number", position);
        }

        public static Value FromFunction(FunctionObject functionObject)
        {
            Value value = new Value(ValueKind.Function)
            {
                Object = functionObject
            };
            return value;
        }

        public static Value FromNative(NativeObject nativeFunctionObject)
        {
            Value value = new Value(ValueKind.Native)
            {
                Object = nativeFunctionObject
            };
            return value;
        }

        public static Value FromNamespace(NamespaceObject namespaceObject)
        {
            Value value = new Value(ValueKind.Namespace)
            {
                Object = namespaceObject
            };
            return value;
        }

        public static Value FromArray(ArrayObject arrayObject)
        {
            Value value = new Value(ValueKind.Array)
            {
                Object = arrayObject
            };
            return value;
        }

        public static Value FromEnum(EnumObject enumObject)
        {
            Value value = new Value(ValueKind.Enum)
            {
                Object = enumObject
            };
            return value;
        }

        public static Value Null => new Value(ValueKind.Null);

        public ValueKind ValueKind { get; set; }
        public double Number { get; set; }
        public bool Bool { get; set; }
        public string Text { get; set; } = string.Empty;
        public RuntimeObject? Object { get; set; } = null;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T ObjectAs<T>() where T : RuntimeObject
        {
            return (Object as T)!;
        }

        public override string ToString()
        {
            return ValueKind switch
            {
                ValueKind.Number => Number.ToString(),
                ValueKind.Text => Text,
                ValueKind.Bool => Bool.ToString(),
                ValueKind.Null => "Null",
                ValueKind.Function => $"<fn {ObjectAs<FunctionObject>().Name}(...)>",
                ValueKind.Native => $"<native {ObjectAs<NativeObject>().Name}(...)>",
                ValueKind.Array => $"[{string.Join(", ", ObjectAs<ArrayObject>().Elements)}]",
                ValueKind.Enum => EnumToString(),
                ValueKind.Namespace => $"<namespace {ObjectAs<NamespaceObject>().Name}>",
                _ => "Void"
            };
        }

        string EnumToString()
        {
            var enumObject = ObjectAs<EnumObject>();
            return $"{enumObject.EnumName}{Lexer.GetSymbolFromKind(TokenKind.Member) ?? "."}{enumObject.MemberName}";
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Check(ValueKind kind) => ValueKind == kind;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool CheckEquality(ref Value other)
        {
            return (ValueKind, other.ValueKind) switch
            {
                (ValueKind.Number, ValueKind.Number) => Number == other.Number,
                (ValueKind.Text, ValueKind.Text) => Text == other.Text,
                (ValueKind.Bool, ValueKind.Bool) => Bool == other.Bool,
                (ValueKind.Null, ValueKind.Null) => true,
                (ValueKind.Function, ValueKind.Function) => ObjectAs<FunctionObject>() == other.ObjectAs<FunctionObject>(),
                (ValueKind.Native, ValueKind.Native) => ObjectAs<NativeObject>() == other.ObjectAs<NativeObject>(),
                (ValueKind.Array, ValueKind.Array) => ObjectAs<ArrayObject>() == other.ObjectAs<ArrayObject>(),
                (ValueKind.Enum, ValueKind.Enum) => CompareEnums(other.ObjectAs<EnumObject>()),
                _ => false
            };
        }

        bool CompareEnums(EnumObject other)
        {
            var thisEnumObject = ObjectAs<EnumObject>();
            return thisEnumObject.EnumName == other.EnumName && thisEnumObject.MemberName == other.MemberName;
        }
    }
}
