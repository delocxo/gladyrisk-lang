using gladyrisk_lang.src.bytecode.runtime;
using System;
using System.Collections.Generic;
using System.Text;

namespace gladyrisk_lang.src.bytecode.compiler
{
    internal static partial class NativeGlobals
    {
        static Value String = Value.FromNamespace(new NamespaceObject("string", new Dictionary<string, Value>()
        {
            {
                "sub",
                Value.FromNative(new NativeObject("sub", 3, ArgMode.Expect, (args, pos) =>
                {
                    string text = args[0].Expect(ValueKind.Text, pos).Text;
                    int startIndex = args[1].GetInt(pos);
                    int length = args[2].GetInt(pos);
                    if (startIndex < 0)
                        throw new Error("Start index cannot be negative", pos);
                    if (length < 0)
                        throw new Error("Length cannot be negative", pos);
                    if (startIndex > text.Length - length)
                        throw new Error("Index and length are not within the target length", pos);
                    if (length == 0)
                        return new Value(string.Empty);
                    return new Value(text.Substring(startIndex, length));
                    
                }))
            },
            {
                "contains",
                Value.FromNative(new NativeObject("contains", 2, ArgMode.Expect, (args, pos) =>
                {
                    string text = args[0].Expect(ValueKind.Text, pos).Text;
                    string needle = args[1].Expect(ValueKind.Text, pos).Text;
                    return new Value(text.Contains(needle));
                }))
            },
            {
                "indexOf",
                Value.FromNative(new NativeObject("indexOf", 2, ArgMode.Expect, (args, pos) =>
                {
                    string text = args[0].Expect(ValueKind.Text, pos).Text;
                    string needle = args[1].Expect(ValueKind.Text, pos).Text;
                    return new Value(text.IndexOf(needle));
                }))
            },
            {
                "toUpper",
                Value.FromNative(new NativeObject("toUpper", 1, ArgMode.Expect, (args, pos) =>
                {
                    string text = args[0].Expect(ValueKind.Text, pos).Text;
                    return new Value(text.ToUpperInvariant());
                }))
            },
            {
                "toLower",
                Value.FromNative(new NativeObject("toLower", 1, ArgMode.Expect, (args, pos) =>
                {
                    string text = args[0].Expect(ValueKind.Text, pos).Text;
                    return new Value(text.ToLowerInvariant());
                }))
            },
            {
                "startsWith",
                Value.FromNative(new NativeObject("startsWith", 2, ArgMode.Expect, (args, pos) =>
                {
                    string text = args[0].Expect(ValueKind.Text, pos).Text;
                    string needle = args[1].Expect(ValueKind.Text, pos).Text;
                    return new Value(text.StartsWith(needle));
                }))
            },
            {
                "endsWith",
                Value.FromNative(new NativeObject("endsWith", 2, ArgMode.Expect, (args, pos) =>
                {
                    string text = args[0].Expect(ValueKind.Text, pos).Text;
                    string needle = args[1].Expect(ValueKind.Text, pos).Text;
                    return new Value(text.EndsWith(needle));
                }))
            },
            {
                "toCharArray",
                Value.FromNative(new NativeObject("toCharArray", 1, ArgMode.Expect, (args, pos) =>
                {
                    string text = args[0].Expect(ValueKind.Text, pos).Text;
                    char[] array = text.ToCharArray();
                    List<Value> charArray = new List<Value>(array.Length);
                    for (int i = 0; i < array.Length; i++)
                        charArray.Add(new Value(array[i].ToString()));
                    return Value.FromArray(new ArrayObject(charArray));
                }))
            },
            {
                "isDigit",
                Value.FromNative(new NativeObject("isDigit", 1, ArgMode.Expect, (args, pos) =>
                {
                    string text = args[0].Expect(ValueKind.Text, pos).Text;
                    return new Value(text.All(x => x >= '0' && x <= '9'));
                }))
            },
            {
                "isAlpha",
                Value.FromNative(new NativeObject("isAlpha", 1, ArgMode.Expect, (args, pos) =>
                {
                    string text = args[0].Expect(ValueKind.Text, pos).Text;
                    return new Value(text.All(x => (x >= 'a' && x <= 'z') || (x >= 'A' && x <= 'Z')));
                }))
            },
            {
                "isAlphaDigit",
                Value.FromNative(new NativeObject("isAlphaDigit", 1, ArgMode.Expect, (args, pos) =>
                {
                    string text = args[0].Expect(ValueKind.Text, pos).Text;
                    return new Value(text.All(x => (x >= 'a' && x <= 'z') || (x >= 'A' && x <= 'Z') || (x >= '0' && x <= '9')));
                }))
            },
            {
                "trim",
                Value.FromNative(new NativeObject("trim", 1, ArgMode.Expect, (args, pos) =>
                {
                    string text = args[0].Expect(ValueKind.Text, pos).Text;
                    return new Value(text.Trim());
                }))
            },
            {
                "trimStart",
                Value.FromNative(new NativeObject("trimStart", 1, ArgMode.Expect, (args, pos) =>
                {
                    string text = args[0].Expect(ValueKind.Text, pos).Text;
                    return new Value(text.TrimStart());
                }))
            },
            {
                "trimEnd",
                Value.FromNative(new NativeObject("trimEnd", 1, ArgMode.Expect, (args, pos) =>
                {
                    string text = args[0].Expect(ValueKind.Text, pos).Text;
                    return new Value(text.TrimEnd());
                }))
            },
        }));
    }
}
