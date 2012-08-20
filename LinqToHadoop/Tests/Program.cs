using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LinqToHadoop;
using LinqToHadoop.Compiler;
using Tests.Compiler;
using Tests.IO;

namespace Tests
{
    class Program
    {
        static void Main(string[] args)
        {
            var tests = new Dictionary<string, Action> {
                { "EncodingTest", () => EncodingTests.RunAll() },
                { "QueryCompilerTest", () => QueryCompilerTest.RunAll() },
            };

            Console.WriteLine("Running all tests");
            var fullStart = DateTime.Now;
            foreach (var test in tests)
            {
                var start = DateTime.Now;
                try
                {
                    Console.WriteLine("Starting " + test.Key);
                    test.Value();
                    Console.WriteLine("Completed in " + (DateTime.Now - start).TotalSeconds + " seconds");
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Failure! " + ex);
                }
                Console.WriteLine();
            }

            Console.ReadKey();
        }
    }
}
