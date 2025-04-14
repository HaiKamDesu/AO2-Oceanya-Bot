using System;
using Common;

namespace SpecialCharacterTest
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Special Character Handler Test Application");
            Console.WriteLine("===========================================");
            Console.WriteLine();
            
            string results = SpecialCharacterTester.RunAllTests();
            Console.WriteLine(results);
            
            RunInteractiveTest();
            
            Console.WriteLine();
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
        
        static void RunInteractiveTest()
        {
            Console.WriteLine();
            Console.WriteLine("Interactive Testing Mode");
            Console.WriteLine("=======================");
            Console.WriteLine("Type text to see how special characters are processed.");
            Console.WriteLine("Type 'exit' to quit interactive mode.");
            Console.WriteLine();
            
            while (true)
            {
                Console.Write("Enter text to process: ");
                string input = Console.ReadLine();
                
                if (string.IsNullOrEmpty(input) || input.ToLower() == "exit")
                    break;
                
                string processed = SpecialCharacterTester.ProcessSpecialCharacters(input);
                Console.WriteLine();
                Console.WriteLine($"Original: \"{input}\"");
                Console.WriteLine($"Processed: \"{processed}\"");
                Console.WriteLine();
            }
        }
    }
}