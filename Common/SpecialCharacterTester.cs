using System;
using System.Collections.Generic;
using System.Text;

namespace Common
{
    /// <summary>
    /// This class provides a standalone way to test special character handling
    /// without needing the full test framework. It can be incorporated into any application
    /// to verify the special character handling logic works correctly.
    /// </summary>
    public static class SpecialCharacterTester
    {
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

        /// <summary>
        /// Runs all test cases for special character handling and returns a report of the results
        /// </summary>
        public static string RunAllTests()
        {
            var testResults = new StringBuilder();
            testResults.AppendLine("Special Character Handling Test Results:");
            testResults.AppendLine("=======================================");
            testResults.AppendLine();

            RunBasicTests(testResults);
            RunSpecialCharTests(testResults);
            RunEscapeTests(testResults);
            RunComplexCases(testResults);
            RunBackslashTests(testResults);

            testResults.AppendLine();
            testResults.AppendLine("Test Summary:");
            testResults.AppendLine($"Total Tests: {_totalTests}");
            testResults.AppendLine($"Passed: {_testsPassed}");
            testResults.AppendLine($"Failed: {_totalTests - _testsPassed}");
            testResults.AppendLine();
            testResults.AppendLine(_testsPassed == _totalTests ? "All tests passed!" : "Some tests failed!");

            return testResults.ToString();
        }

        private static int _totalTests = 0;
        private static int _testsPassed = 0;

        private static void RunTest(StringBuilder output, string testName, string input, string expectedOutput)
        {
            _totalTests++;
            string actualOutput = ProcessSpecialCharacters(input);
            bool passed = actualOutput == expectedOutput;
            
            if (passed)
                _testsPassed++;

            output.AppendLine($"Test: {testName}");
            output.AppendLine($"Input: \"{input}\"");
            output.AppendLine($"Expected: \"{expectedOutput}\"");
            output.AppendLine($"Actual: \"{actualOutput}\"");
            output.AppendLine($"Result: {(passed ? "PASSED" : "FAILED")}");
            output.AppendLine();
        }

        private static void RunBasicTests(StringBuilder output)
        {
            output.AppendLine("Basic Tests:");
            output.AppendLine("-----------");
            
            RunTest(output, "Empty String", "", "");
            RunTest(output, "Null String", null, null);
            RunTest(output, "Normal Text", "Hello world!", "Hello world!");
        }

        private static void RunSpecialCharTests(StringBuilder output)
        {
            output.AppendLine("Special Character Tests:");
            output.AppendLine("-----------------------");
            
            RunTest(output, "With Dash", "Text with - dash", "Text with  dash");
            RunTest(output, "With Quote", "Text with \" quote", "Text with  quote");
            RunTest(output, "With Tilde", "Text with ~ tilde", "Text with  tilde");
            RunTest(output, "With Opening Brace", "Text with { brace", "Text with  brace");
            RunTest(output, "With Closing Brace", "Text with } brace", "Text with  brace");
            RunTest(output, "With \\s Sequence", "Text with \\s sequence", "Text with  sequence");
        }

        private static void RunEscapeTests(StringBuilder output)
        {
            output.AppendLine("Escape Tests:");
            output.AppendLine("-------------");
            
            RunTest(output, "Escaped Dash", "Text with \\- escaped dash", "Text with - escaped dash");
            RunTest(output, "Escaped Quote", "Text with \\\" escaped quote", "Text with \" escaped quote");
            RunTest(output, "Escaped Tilde", "Text with \\~ escaped tilde", "Text with ~ escaped tilde");
            RunTest(output, "Escaped Opening Brace", "Text with \\{ escaped brace", "Text with { escaped brace");
            RunTest(output, "Escaped Closing Brace", "Text with \\} escaped brace", "Text with } escaped brace");
        }

        private static void RunComplexCases(StringBuilder output)
        {
            output.AppendLine("Complex Cases:");
            output.AppendLine("--------------");
            
            RunTest(output, "Multiple Special Chars", 
                "Text with {multiple} -special- \"characters\" and ~tildes~",
                "Text with multiple special characters and tildes");
                
            RunTest(output, "Mixed Escaped and Normal", 
                "Normal {hidden} and \\{visible\\} special chars",
                "Normal  and {visible} special chars");
                
            RunTest(output, "Consecutive Special Chars", 
                "Text with ---consecutive--- dashes",
                "Text with consecutive dashes");
                
            RunTest(output, "Mixed Escaped Consecutive", 
                "Text with -\\-\\- mixed dashes",
                "Text with -- mixed dashes");
                
            RunTest(output, "Multiple \\s Sequences", 
                "Multiple \\s\\s sequences",
                "Multiple  sequences");
        }

        private static void RunBackslashTests(StringBuilder output)
        {
            output.AppendLine("Backslash Tests:");
            output.AppendLine("----------------");
            
            RunTest(output, "Ending With Backslash", 
                "Text ending with \\",
                "Text ending with \\");
                
            RunTest(output, "Double Backslash", 
                "Double backslash: \\\\",
                "Double backslash: \\");
                
            RunTest(output, "Backslash + Special Char", 
                "Escaped backslash before special: \\\\-",
                "Escaped backslash before special: \\");
                
            RunTest(output, "Multiple Escaped Backslashes", 
                "Multiple escaped backslashes: \\\\\\\\",
                "Multiple escaped backslashes: \\\\");
                
            RunTest(output, "Escaped Backslash In Text", 
                "Escaped \\\\ backslash in text",
                "Escaped \\ backslash in text");
                
            RunTest(output, "Backslash + Escaped Special", 
                "Escaped backslash + escaped dash: \\\\\\-",
                "Escaped backslash + escaped dash: \\-");
        }

        /// <summary>
        /// Processes special characters for display in IC messages.
        /// Special characters are either hidden or displayed if escaped with a backslash.
        /// This is a copy of the method from Globals.cs to ensure standalone testing works.
        /// </summary>
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
    }
}