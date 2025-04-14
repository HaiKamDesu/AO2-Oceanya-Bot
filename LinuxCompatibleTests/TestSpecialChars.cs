using System;
using System.Collections.Generic;
using System.Text;

// This is a standalone test program that doesn't require NUnit
// The key point is to have the algorithm and test cases in one file
class TestSpecialChars
{
    // Copy of the special character handling code from Globals.cs
    private static Dictionary<char, bool> SpecialCharacters = new Dictionary<char, bool>()
    {
        { '-', true },
        { '"', true },
        { '\\', false },  // Backslash is used for escaping, not hidden itself
        { 's', false },  // Only special when preceded by backslash (\\s)
        { '~', true },
        { '{', true },
        { '}', true },
    };

    public static string ProcessSpecialCharacters(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;
            
        StringBuilder result = new StringBuilder(text.Length);
        bool escaped = false;
        
        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            
            // Handle backslash for escaping
            if (c == '\\' && !escaped)
            {
                // Case 1: End of string - append the backslash
                if (i == text.Length - 1)
                {
                    result.Append(c);
                    continue;
                }
                
                char nextChar = text[i + 1];
                
                // Case 2: Check if this is the special \s case
                if (nextChar == 's')
                {
                    // Skip both the backslash and 's' - this is the special \s sequence
                    i++; // Skip the 's' on the next iteration
                    continue;
                }
                
                // Case 3: Escaped backslash (\\)
                if (nextChar == '\\')
                {
                    result.Append(c); // Add the first backslash
                    i++; // Skip the next backslash (it will be processed in the next iteration)
                    escaped = true;
                    continue;
                }
                
                // Case 4: Next character is a special character that needs escaping
                if (SpecialCharacters.TryGetValue(nextChar, out bool isNextSpecial) && isNextSpecial)
                {
                    escaped = true;
                    continue; // Skip the backslash but remember we're in escape mode
                }
                
                // Case 5: Regular backslash followed by a normal character - keep both
                result.Append(c);
                continue;
            }
            
            // If the character is special and not escaped, skip it (hide it in output)
            if (SpecialCharacters.TryGetValue(c, out bool isSpecial) && isSpecial && !escaped)
            {
                // Skip this character (hide it)
                continue;
            }
            
            // Add the character to the result
            result.Append(c);
            escaped = false;
        }
        
        return result.ToString();
    }

    // Simple test runner
    static void Main()
    {
        Console.WriteLine("Special Character Handling Tests");
        Console.WriteLine("===============================");
        Console.WriteLine();

        // Basic tests
        RunTest("Basic text", "Hello world!", "Hello world!");
        RunTest("Null string", null, null);
        RunTest("Empty string", "", "");

        // Special character tests
        RunTest("With dash", "Text with - dash", "Text with  dash");
        RunTest("With quote", "Text with \" quote", "Text with  quote");
        RunTest("With tilde", "Text with ~ tilde", "Text with  tilde");
        RunTest("With brace", "Text with { brace", "Text with  brace");
        RunTest("With closing brace", "Text with } brace", "Text with  brace");
        RunTest("With \\s", "Text with \\s sequence", "Text with  sequence");

        // Escaped character tests
        RunTest("Escaped dash", "Text with \\- escaped dash", "Text with - escaped dash");
        RunTest("Escaped quote", "Text with \\\" escaped quote", "Text with \" escaped quote");
        RunTest("Escaped tilde", "Text with \\~ escaped tilde", "Text with ~ escaped tilde");
        RunTest("Escaped brace", "Text with \\{ escaped brace", "Text with { escaped brace");
        RunTest("Escaped backslash", "Double backslash: \\\\", "Double backslash: \\");
 
        // Complex cases
        RunTest("Complex case", "Text with {multiple} -special- \"characters\" and ~tildes~",
                "Text with multiple special characters and tildes");
        RunTest("Mixed case", "Normal {hidden} and \\{visible\\} special chars",
                "Normal hidden and {visible} special chars");

        Console.WriteLine();
        Console.WriteLine($"Total Tests: {_totalTests}");
        Console.WriteLine($"Passed: {_testsPassed}");
        Console.WriteLine($"Failed: {_totalTests - _testsPassed}");
        Console.WriteLine();
        Console.WriteLine(_testsPassed == _totalTests ? "All tests passed!" : "Some tests failed!");
    }

    private static int _totalTests = 0;
    private static int _testsPassed = 0;

    private static void RunTest(string name, string input, string expected)
    {
        _totalTests++;
        string result = input != null ? ProcessSpecialCharacters(input) : null;
        bool passed = result == expected;
        
        if (passed)
            _testsPassed++;

        Console.WriteLine($"Test: {name}");
        Console.WriteLine($"Input: {input}");
        Console.WriteLine($"Expected: {expected}");
        Console.WriteLine($"Result: {result}");
        Console.WriteLine($"Status: {(passed ? "PASSED" : "FAILED")}");
        Console.WriteLine();
    }
}