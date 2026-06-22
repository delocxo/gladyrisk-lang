using gladyrisk_lang.src.bytecode.runtime;
using System;
using System.Collections.Generic;
using System.Text;

//static Dictionary<string, Value> s_globals = new Dictionary<string, Value>()
//        {
//            {
//                "format",
//                Value.FromNative(new NativeObject("format", 1, ArgMode.Atleast, (args, pos) =>
//                {
//                    var sb = new StringBuilder();
//                    string template = args[0].Expect(ValueKind.Text, pos).Text;
//                    ReadOnlySpan<char> span = template.AsSpan();
//                    int currentArg = 1;
//                    for (int i = 0; i < span.Length; i++)
//                    {
//                        if (i + 1 < span.Length && span[i] == '$' && span[i + 1] == '$')
//                        {
//                            sb.Append('$');
//                            i++;
//                            continue;
//                        }
//                        else if (span[i] == '$')
//                        {
//                            if (currentArg >= args.Count)
//                                throw new Error("Missing argument(s) for format", pos);
//                            sb.Append(args[currentArg++]);
//                            continue;
//                        }
//                        sb.Append(span[i]);
//                    }
//                    if (currentArg < args.Count)
//                        throw new Error($"Too many arguments for format", pos);
//                    return new Value(sb.ToString());
//                }))
//            },
//            {
//                "println",
//                Value.FromNative(new NativeObject("println", -1, ArgMode.Unlimited, (args, pos) =>
//                {
//                    var sb = new StringBuilder();
//                    sb.AppendJoin("", args);
//                    Console.WriteLine(sb.ToString());
//                    return Value.Null;
//                }))
//            },
//            {
//                "prompt",
//                Value.FromNative(new NativeObject("prompt", 1, ArgMode.Expect, (args, pos) =>
//                {
//                    string prompt = args[0].Expect(ValueKind.Text, pos).Text;
//                    Console.Write(prompt);
//                    return new Value(Console.ReadLine() ?? "");
//                }))
//            }
//        };

namespace gladyrisk_lang.src.bytecode.compiler
{
    internal static partial class NativeGlobals
    {
        public static Dictionary<string, Value> ValueGlobals => new Dictionary<string, Value>()
        {
            { "core", Core },
            { "string", String }
        };
    }
}
