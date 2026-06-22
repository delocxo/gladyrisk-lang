using gladyrisk_lang.src.bytecode.runtime;
using System;
using System.Collections.Generic;
using System.Text;

namespace gladyrisk_lang.src.bytecode.compiler
{
    internal static partial class NativeGlobals
    {
        static Value Core = Value.FromNamespace(new NamespaceObject("core", new Dictionary<string, Value>()
        {
            {
                "format",
                Value.FromNative(new NativeObject("format", 1, ArgMode.Atleast, (args, pos) =>
                {
                    var sb = new StringBuilder();
                    string template = args[0].Expect(ValueKind.Text, pos).Text;
                    ReadOnlySpan<char> span = template.AsSpan();
                    int currentArg = 1;
                    for (int i = 0; i < span.Length; i++)
                    {
                        if (i + 1 < span.Length && span[i] == '$' && span[i + 1] == '$')
                        {
                            sb.Append('$');
                            i++;
                            continue;
                        }
                        else if (span[i] == '$')
                        {
                            if (currentArg >= args.Count)
                                throw new Error("Missing argument(s) for format", pos);
                            sb.Append(args[currentArg++]);
                            continue;
                        }
                        sb.Append(span[i]);
                    }
                    if (currentArg < args.Count)
                        throw new Error($"Too many arguments for format", pos);
                    return new Value(sb.ToString());
                }))
            },
            {
                "println",
                Value.FromNative(new NativeObject("println", -1, ArgMode.Unlimited, (args, pos) =>
                {
                    var sb = new StringBuilder();
                    sb.AppendJoin("", args);
                    Console.WriteLine(sb.ToString());
                    return Value.Null;
                }))
            },
            {
                "print",
                Value.FromNative(new NativeObject("print", -1, ArgMode.Unlimited, (args, pos) =>
                {
                    var sb = new StringBuilder();
                    sb.AppendJoin("", args);
                    Console.Write(sb.ToString());
                    return Value.Null;
                }))
            },
            {
                "prompt",
                Value.FromNative(new NativeObject("prompt", 1, ArgMode.Expect, (args, pos) =>
                {
                    string prompt = args[0].Expect(ValueKind.Text, pos).Text;
                    Console.Write(prompt);
                    return new Value(Console.ReadLine() ?? "");
                }))
            },
            {
                "clock",
                Value.FromNative(new NativeObject("clock", 0, ArgMode.Expect, (args, pos) =>
                {
                    return new Value(Environment.TickCount64);
                }))
            },
            {
                "typeof",
                Value.FromNative(new NativeObject("typeof", 1, ArgMode.Expect, (args, pos) =>
                {
                    return new Value(args[0].ValueKind.ToString());
                }))
            },
            {
                "assert",
                Value.FromNative(new NativeObject("assert", 1, 2, ArgMode.Clamp, (args, pos) =>
                {
                    bool condition = args[0].Expect(ValueKind.Bool, pos).Bool;
                    if (!condition)
                    {
                        string msg = args.Count > 1 ? args[1].Expect(ValueKind.Text, pos).Text : "Assertion Failed";
                        throw new Error(msg, pos);
                    }
                    return Value.Null;
                }))
            },
            {
                "toText",
                Value.FromNative(new NativeObject("typeof", 1, ArgMode.Expect, (args, pos) =>
                {
                    return new Value(args[0].ToString());
                }))
            },
            {
                "panic",
                Value.FromNative(new NativeObject("panic", -1, ArgMode.Unlimited, (args, pos) =>
                {
                    var sb = new StringBuilder();
                    sb.AppendJoin("", args);
                    throw new Error(sb.ToString(), pos);
                }))
            },
            {
                "len",
                Value.FromNative(new NativeObject("len", 1, ArgMode.Expect, (args, pos) =>
                {
                    var value = args[0].ExpectTypes(pos, ValueKind.Text, ValueKind.Array);
                    if (value.Check(ValueKind.Text))
                        return new Value(value.Text.Length);
                    else
                        return new Value(value.ObjectAs<ArrayObject>().Count);
                }))
            }
        }));
    }
}