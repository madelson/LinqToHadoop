using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LinqToHadoop;
using LinqToHadoop.Compiler;
using Tests.Compiler;
using Tests.IO;
using Tests.Reflection;

namespace Tests
{
    class Program
    {
        static void Main(string[] args)
        {
            var tests = new Dictionary<string, Action> {
                    { "EncodingTest", () => EncodingTests.RunAll() },
                    { "CompilerTest", () => CompilerTests.RunAll() },
                    { "ReflectionTest", () => ReflectionTests.RunAll() },
                    { "SerializationTest", () => SerializationTests.RunAll() },
                }
                .OrderBy(kvp => kvp.Key)
                .ToList();

            Console.WriteLine("Running all tests");
            var fullStart = DateTime.Now;
            var failedTests = new List<string>();
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
                    failedTests.Add(test.Key);
                    Console.WriteLine("Failure! " + ex);
                }
                Console.WriteLine();
            }

            if (failedTests.Any()) 
            {
                Console.WriteLine("{0} test(s) failed!", failedTests.Count);
                failedTests.ForEach(Console.WriteLine);
            }
            else 
            {
                Console.WriteLine("All tests succeeded!");
            }
                
            Console.ReadKey();
        }
    }
}
