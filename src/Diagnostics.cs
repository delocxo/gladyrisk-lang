using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

namespace gladyrisk_lang.src
{
    internal sealed record SourceFile
    {
        public SourceFile(string path, string text)
        {
            Path = path;
            Text = text;
        }

        public string Path { get; }
        public string Text { get; }
    }

    internal struct Position
    {
        public Position(int sourceID, int line, int column)
        {
            SourceID = sourceID;
            Line = line;
            Column = column;
        }

        public int SourceID { get; }
        public int Line { get; set; }
        public int Column { get; set; }
    }

    internal class Error : Exception
    {
        static List<SourceFile> s_sourceFiles = new List<SourceFile>();

        static SourceFile GetSourceFile(int id) => s_sourceFiles[id];
        public static int AddSourceFile(SourceFile sourceFile)
        {
            s_sourceFiles.Add(sourceFile);
            return s_sourceFiles.Count - 1;
        }

        Position? _position;
        string _caller = string.Empty;

        public Error(string message, Position position, [CallerMemberName] string callerName = "") : base(message)
        {
            _position = position;
            _caller = callerName;
        }

        public Error(string message) : base(message)
        {
            _position = null;
        }

        public void Print()
        {
            if (_position != null)
            {
                SourceFile sourceFile = GetSourceFile(_position.Value.SourceID);
                Console.Error.WriteLine($"{sourceFile.Path}:{_position.Value.Line}:{_position.Value.Column} - Error: {Message}");
                Console.Error.WriteLine($"Caller: {_caller}");
            }
            else
            {
                Console.Error.WriteLine($"Error: {Message}");
                Console.Error.WriteLine($"Caller: {_caller}");
            }
        }
    }
}
