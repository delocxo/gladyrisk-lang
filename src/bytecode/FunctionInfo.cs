using gladyrisk_lang.src.bytecode.compiler;
using System;
using System.Collections.Generic;
using System.Text;

namespace gladyrisk_lang.src.bytecode
{
    internal class FunctionInfo
    {
        public FunctionInfo(Chunk chunk, string name, int arity)
        {
            Chunk = chunk;
            Name = name;
            Arity = arity;
        }

        public Chunk Chunk { get; set; }
        public string Name { get; }
        public int Arity { get; }
    }
}
