using gladyrisk_lang.src.ast;
using gladyrisk_lang.src.bytecode.compiler;
using gladyrisk_lang.src.bytecode.runtime;
using System.Diagnostics;

namespace gladyrisk_lang.src
{
    internal class Program
    {
        static int Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.Error.WriteLine("Usage: gladrisk <file.grisk> [additional-files...]");
                return 1;
            }

            var readSw = Stopwatch.StartNew();
            List<SourceFile> sourceFiles = new List<SourceFile>();
            for (int i = 0; i < args.Length; i++)
            {
                string path = args[i];
                if (!File.Exists(path))
                {
                    Console.Error.WriteLine($"'{path}' does not exist");
                    return 1;
                }
                SourceFile sourceFile = new SourceFile(path, File.ReadAllText(path));
                sourceFiles.Add(sourceFile);
            }
            readSw.Stop();

            Position position = new Position(Error.AddSourceFile(sourceFiles[0]), 1, 1);

            try
            {
                var tokenizeSw = Stopwatch.StartNew();
                List<List<Token>> tokenSets = new List<List<Token>>();
                sourceFiles.ForEach(x =>
                {
                    Lexer lexer = new Lexer(x);
                    tokenSets.Add(lexer.Tokenize());
                });
                tokenizeSw.Stop();

                var tokenCombineSw = Stopwatch.StartNew();
                List<Token> tokens = new List<Token>();
                foreach (List<Token> tokenSet in tokenSets)
                {
                    tokens.AddRange(tokenSet);
                }
                tokenCombineSw.Stop();

                var tokenEofRemoveSw = Stopwatch.StartNew();
                for (int i = tokens.Count - 1; i >= 0; i--)
                {
                    Token token = tokens[i];
                    if (token.Kind == TokenKind.EndOfFile && i != tokens.Count - 1)
                        tokens.RemoveAt(i);
                }
                tokenEofRemoveSw.Stop();

                //foreach (Token token in tokens)
                //{
                //    Console.WriteLine($"{token.Kind}: {token.Lexeme}");
                //}

                var parserSw = Stopwatch.StartNew();
                Parser parser = new Parser(tokens);
                List<Statement> statements = parser.Parse();
                parserSw.Stop();

                Stopwatch compileSw = Stopwatch.StartNew();
                Compiler compiler = new Compiler([], []);
                Chunk program = compiler.CompileStatements(statements);
                compileSw.Stop();

                //int instructions = 0;
                //int constants = 0;
                //int functions = 0;

                //foreach (var fn in program.FunctionInfos)
                //{
                //    instructions += fn.Chunk.Instructions.Count;
                //    constants += fn.Chunk.Constants.Length;
                //    functions++;
                //}

                //instructions += program.Main.Chunk.Instructions.Count;
                //constants += program.Main.Chunk.Constants.Length;
                //functions++;

                //Console.WriteLine($"""
                //Read Files: {readSw.ElapsedMilliseconds}ms
                //Tokenize Files: {tokenizeSw.ElapsedMilliseconds}ms
                //Combine Tokens: {tokenCombineSw.ElapsedMilliseconds}ms
                //Keep Last Eof Token: {tokenEofRemoveSw.ElapsedMilliseconds}ms
                //Parsing: {parserSw.ElapsedMilliseconds}ms
                //""");

                //Console.WriteLine($"""
                //Compiled successfully
                //Files: {sourceFiles.Count}
                //Functions: {functions}
                //Instructions: {instructions}
                //Constants: {constants}
                //Compile time: {compileSw.ElapsedMilliseconds}ms
                //""");

                foreach (var fn in program.FunctionInfos)
                {
                    Console.WriteLine($"{fn.Name}, {fn.Arity}");
                    foreach (var instruction in fn.Chunk.Instructions)
                        Console.WriteLine($"\t{instruction}");
                }

                Console.WriteLine($"{program.Main!.Name}, {program.Main!.Arity}");
                foreach (var instruction in program.Main!.Chunk.Instructions)
                    Console.WriteLine($"\t{instruction}");
                //Stopwatch vmSw = Stopwatch.StartNew();
                string result = Vm.Run(program);
                //vmSw.Stop();
                //Console.WriteLine($"Speed: {vmSw.ElapsedMilliseconds}ms\nResult:{result}");
            }
            catch (Error e)
            {
                e.Print();
                return 1;
            }

            return 0;
        }
    }
}
