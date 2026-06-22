using gladyrisk_lang.src.bytecode.runtime;
using System;
using System.Collections.Generic;
using System.Text;

namespace gladyrisk_lang.src.bytecode.compiler
{
    internal static partial class NativeGlobals
    {
        static Value Array = Value.FromNamespace(new NamespaceObject("array", new Dictionary<string, Value>()
        {
            {
                "push",
                Value.FromNative(new NativeObject("push", 2, ArgMode.Expect, (args, pos) =>
                {
                    var array = args[0].Expect(ValueKind.Array, pos).ObjectAs<ArrayObject>();
                    array.Elements.Add(args[1]);
                    return Value.Null;
                }))
            },
            {
                "pop",
                Value.FromNative(new NativeObject("pop", 1, ArgMode.Expect, (args, pos) =>
                {
                    var array = args[0].Expect(ValueKind.Array, pos).ObjectAs<ArrayObject>();
                    if (array.Count == 0)
                        throw new Error("Cannot pop an empty array", pos);
                    var element = array.Elements[array.Count - 1];
                    array.Elements.RemoveAt(array.Count - 1);
                    return element;
                }))
            },
            {
                "clear",
                Value.FromNative(new NativeObject("clear", 1, ArgMode.Expect, (args, pos) =>
                {
                    var array = args[0].Expect(ValueKind.Array, pos).ObjectAs<ArrayObject>();
                    array.Elements.Clear();
                    return Value.Null;
                }))
            },
            {
                "isEmpty",
                Value.FromNative(new NativeObject("isEmpty", 1, ArgMode.Expect, (args, pos) =>
                {
                    var array = args[0].Expect(ValueKind.Array, pos).ObjectAs<ArrayObject>();
                    return new Value(array.Count == 0);
                }))
            },
            {
                "indexOf",
                Value.FromNative(new NativeObject("indexOf", 2, ArgMode.Expect, (args, pos) =>
                {
                    var array = args[0].Expect(ValueKind.Array, pos).ObjectAs<ArrayObject>();
                    Value needle = args[1];
                    for (int i = 0; i < array.Count; i++)
                        if (array.Elements[i].CheckEquality(ref needle))
                            return new Value(i);
                    return new Value(-1);
                }))
            },
            {
                "contains",
                Value.FromNative(new NativeObject("contains", 2, ArgMode.Expect, (args, pos) =>
                {
                    var array = args[0].Expect(ValueKind.Array, pos).ObjectAs<ArrayObject>();
                    Value needle = args[1];
                    for (int i = 0; i < array.Count; i++)
                        if (array.Elements[i].CheckEquality(ref needle))
                            return new Value(true);
                    return new Value(false);
                }))
            },
            {
                "removeAt",
                Value.FromNative(new NativeObject("removeAt", 2, ArgMode.Expect, (args, pos) =>
                {
                    var array = args[0].Expect(ValueKind.Array, pos).ObjectAs<ArrayObject>();
                    int index = args[1].GetInt(pos);
                    if (index < 0 || index >= array.Count)
                        throw new Error("Index out of bounds", pos);
                    array.Elements.RemoveAt(index);
                    return Value.Null; ;
                }))
            },
            {
                "addRange",
                Value.FromNative(new NativeObject("addRange", 2, ArgMode.Expect, (args, pos) =>
                {
                    var array = args[0].Expect(ValueKind.Array, pos).ObjectAs<ArrayObject>();
                    var other = args[1].Expect(ValueKind.Array, pos).ObjectAs<ArrayObject>();
                    array.Elements.AddRange(other.Elements);
                    return Value.Null; ;
                }))
            },
            {
                "last",
                Value.FromNative(new NativeObject("last", 1, ArgMode.Expect, (args, pos) =>
                {
                    var array = args[0].Expect(ValueKind.Array, pos).ObjectAs<ArrayObject>();
                    if (array.Count == 0)
                        throw new Error("Cannot get last from an empty array", pos);
                    return array.Elements[array.Count - 1];
                }))
            },
            {
                "insert",
                Value.FromNative(new NativeObject("insert", 3, ArgMode.Expect, (args, pos) =>
                {
                    var array = args[0].Expect(ValueKind.Array, pos).ObjectAs<ArrayObject>();
                    int index = args[1].GetInt(pos);
                    Value value = args[2];
                    if (index < 0 || index > array.Count)
                        throw new Error("Index out of bounds", pos);
                    array.Elements.Insert(index, value);
                    return Value.Null; ;
                }))
            },
        }));
    }
}