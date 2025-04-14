using Common;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnitTests
{
    [TestFixture]
    public class GlobalsTests
    {
        [Test]
        public void Test_ReplaceTextForSymbols()
        {
            // Test replacing special characters
            Assert.That(Globals.ReplaceTextForSymbols("<percent>"), Is.EqualTo("%"), "Should replace <percent> with %");
            Assert.That(Globals.ReplaceTextForSymbols("<dollar>"), Is.EqualTo("$"), "Should replace <dollar> with $");
            Assert.That(Globals.ReplaceTextForSymbols("<num>"), Is.EqualTo("#"), "Should replace <num> with #");
            Assert.That(Globals.ReplaceTextForSymbols("<and>"), Is.EqualTo("&"), "Should replace <and> with &");
            
            // Test multiple replacements in a single string
            Assert.That(
                Globals.ReplaceTextForSymbols("Price: <dollar>10.99 (<percent>5 off)"),
                Is.EqualTo("Price: $10.99 (%5 off)"),
                "Should replace multiple placeholders in a string"
            );
            
            // Test string with no replacements
            Assert.That(
                Globals.ReplaceTextForSymbols("This is a normal string"),
                Is.EqualTo("This is a normal string"),
                "Should return original string when no replacements are needed"
            );
        }
        
        [Test]
        public void Test_ReplaceSymbolsForText()
        {
            // Test replacing special characters in reverse
            Assert.That(Globals.ReplaceSymbolsForText("%"), Is.EqualTo("<percent>"), "Should replace % with <percent>");
            Assert.That(Globals.ReplaceSymbolsForText("$"), Is.EqualTo("<dollar>"), "Should replace $ with <dollar>");
            Assert.That(Globals.ReplaceSymbolsForText("#"), Is.EqualTo("<num>"), "Should replace # with <num>");
            Assert.That(Globals.ReplaceSymbolsForText("&"), Is.EqualTo("<and>"), "Should replace & with <and>");
            
            // Test multiple replacements in a single string
            Assert.That(
                Globals.ReplaceSymbolsForText("Price: $10.99 (%5 off)"),
                Is.EqualTo("Price: <dollar>10.99 (<percent>5 off)"),
                "Should replace multiple symbols in a string"
            );
            
            // Test string with no replacements
            Assert.That(
                Globals.ReplaceSymbolsForText("This is a normal string"),
                Is.EqualTo("This is a normal string"),
                "Should return original string when no replacements are needed"
            );
        }
        
        [Test]
        public void Test_ReplaceSymbols_Bidirectional()
        {
            // Test that text -> symbols -> text results in original string
            string original = "Test message with # $ % & symbols";
            string encoded = Globals.ReplaceSymbolsForText(original);
            string decoded = Globals.ReplaceTextForSymbols(encoded);
            
            Assert.That(decoded, Is.EqualTo(original), "Bidirectional replacement should result in original string");
            
            // Test escaping already encoded text
            string alreadyEncoded = "Message with <dollar> <percent>";
            string doubleEncoded = Globals.ReplaceSymbolsForText(alreadyEncoded);
            string decodedOnce = Globals.ReplaceTextForSymbols(doubleEncoded);
            
            Assert.That(decodedOnce, Is.EqualTo(alreadyEncoded), "Already encoded strings should be preserved through bidirectional conversion");
        }
        
        [Test]
        public void Test_AllowedImageExtensions()
        {
            // Verify that the allowed extensions list contains expected formats
            Assert.That(Globals.AllowedImageExtensions, Does.Contain("png"), "PNG should be allowed");
            Assert.That(Globals.AllowedImageExtensions, Does.Contain("jpg"), "JPG should be allowed");
            Assert.That(Globals.AllowedImageExtensions, Does.Contain("gif"), "GIF should be allowed");
            
            // Check overall count to make sure all formats are present
            Assert.That(Globals.AllowedImageExtensions.Count, Is.GreaterThanOrEqualTo(5), "Should have at least 5 allowed image formats");
        }
        
        [Test]
        public void Test_ProcessSpecialCharacters_Basic()
        {
            // Test with no special characters
            string original = "Hello world!";
            string processed = Globals.ProcessSpecialCharacters(original);
            Assert.That(processed, Is.EqualTo(original), "Normal text should remain unchanged");
            
            // Test null or empty input
            Assert.That(Globals.ProcessSpecialCharacters(null), Is.Null, "Null input should return null");
            Assert.That(Globals.ProcessSpecialCharacters(""), Is.EqualTo(""), "Empty string should return empty string");
        }
        
        [Test]
        public void Test_ProcessSpecialCharacters_SpecialChars()
        {
            // Test with special characters that should be hidden
            Assert.That(Globals.ProcessSpecialCharacters("Text with - dash"), Is.EqualTo("Text with  dash"), "Dash should be hidden");
            Assert.That(Globals.ProcessSpecialCharacters("Text with \" quote"), Is.EqualTo("Text with  quote"), "Quote should be hidden");
            Assert.That(Globals.ProcessSpecialCharacters("Text with ~ tilde"), Is.EqualTo("Text with  tilde"), "Tilde should be hidden");
            Assert.That(Globals.ProcessSpecialCharacters("Text with { brace"), Is.EqualTo("Text with  brace"), "Opening brace should be hidden");
            Assert.That(Globals.ProcessSpecialCharacters("Text with } brace"), Is.EqualTo("Text with  brace"), "Closing brace should be hidden");
            
            // Test with the special \s sequence
            Assert.That(Globals.ProcessSpecialCharacters("Text with \\s sequence"), Is.EqualTo("Text with  sequence"), "\\s sequence should be hidden");
            
            // Test with multiple special characters
            Assert.That(
                Globals.ProcessSpecialCharacters("Text with {multiple} -special- \"characters\" and ~tildes~"),
                Is.EqualTo("Text with multiple special characters and tildes"),
                "All special characters should be hidden"
            );
        }
        
        [Test]
        public void Test_ProcessSpecialCharacters_Escaping()
        {
            // Test with escaped special characters
            Assert.That(Globals.ProcessSpecialCharacters("Text with \\- escaped dash"), 
                Is.EqualTo("Text with - escaped dash"), 
                "Escaped dash should be visible");
                
            Assert.That(Globals.ProcessSpecialCharacters("Text with \\\" escaped quote"), 
                Is.EqualTo("Text with \" escaped quote"), 
                "Escaped quote should be visible");
                
            Assert.That(Globals.ProcessSpecialCharacters("Text with \\~ escaped tilde"), 
                Is.EqualTo("Text with ~ escaped tilde"), 
                "Escaped tilde should be visible");
                
            Assert.That(Globals.ProcessSpecialCharacters("Text with \\{ escaped brace"), 
                Is.EqualTo("Text with { escaped brace"), 
                "Escaped opening brace should be visible");
                
            Assert.That(Globals.ProcessSpecialCharacters("Text with \\} escaped brace"), 
                Is.EqualTo("Text with } escaped brace"), 
                "Escaped closing brace should be visible");
                
            // Test with escaped backslash followed by a normal character
            Assert.That(Globals.ProcessSpecialCharacters("Text with \\\\ double backslash"), 
                Is.EqualTo("Text with \\ double backslash"), 
                "Double backslash should be displayed as a single backslash");
                
            // Test with mixed normal and escaped special characters
            Assert.That(
                Globals.ProcessSpecialCharacters("Normal {hidden} and \\{visible\\} special chars"),
                Is.EqualTo("Normal hidden and {visible} special chars"),
                "Only escaped special characters should be visible"
            );
        }
        
        [Test]
        public void Test_ProcessSpecialCharacters_ComplexCases()
        {
            // Test with backslash at the end of string
            Assert.That(Globals.ProcessSpecialCharacters("Text ending with \\"), 
                Is.EqualTo("Text ending with \\"), 
                "Backslash at the end should be preserved");
                
            // Test with consecutive special characters
            Assert.That(Globals.ProcessSpecialCharacters("Text with ---consecutive--- dashes"), 
                Is.EqualTo("Text with consecutive dashes"), 
                "Consecutive special characters should all be hidden");
                
            // Test with mixed escaped and non-escaped consecutive characters
            Assert.That(Globals.ProcessSpecialCharacters("Text with -\\-\\- mixed dashes"), 
                Is.EqualTo("Text with -- mixed dashes"), 
                "Only escaped dashes should be visible");
                
            // Test with multiple \s sequences
            Assert.That(Globals.ProcessSpecialCharacters("Multiple \\s\\s sequences"), 
                Is.EqualTo("Multiple  sequences"), 
                "Multiple \\s sequences should all be hidden");
                
            // Test with escaped \s
            Assert.That(Globals.ProcessSpecialCharacters("Escaped \\\\s sequence"), 
                Is.EqualTo("Escaped \\s sequence"), 
                "Escaped \\s should show the backslash and s");
        }
        
        [Test]
        public void Test_ProcessSpecialCharacters_EscapedBackslash()
        {
            // Test with an escaped backslash
            Assert.That(Globals.ProcessSpecialCharacters("Double backslash: \\\\"), 
                Is.EqualTo("Double backslash: \\"), 
                "Double backslash should be shown as a single backslash");
                
            // Test with an escaped backslash followed by a special character
            Assert.That(Globals.ProcessSpecialCharacters("Escaped backslash before special: \\\\-"), 
                Is.EqualTo("Escaped backslash before special: \\"), 
                "Backslash should be shown but the dash should be hidden");
                
            // Test with multiple escaped backslashes
            Assert.That(Globals.ProcessSpecialCharacters("Multiple escaped backslashes: \\\\\\\\"), 
                Is.EqualTo("Multiple escaped backslashes: \\\\"), 
                "Four backslashes should be shown as two");
                
            // Test with escaped backslash and normal text
            Assert.That(Globals.ProcessSpecialCharacters("Escaped \\\\ backslash in text"), 
                Is.EqualTo("Escaped \\ backslash in text"), 
                "Escaped backslash should be shown correctly in normal text");
                
            // Test with escaped backslash followed by escaped special character
            Assert.That(Globals.ProcessSpecialCharacters("Escaped backslash + escaped dash: \\\\\\-"), 
                Is.EqualTo("Escaped backslash + escaped dash: \\-"), 
                "Both the backslash and dash should be visible");
        }
    }
    
    [TestFixture]
    public class SaveFileTests
    {
        private string _testFilePath = string.Empty;
        
        [SetUp]
        public void SetUp()
        {
            // Create a temporary file path for testing
            _testFilePath = Path.Combine(Path.GetTempPath(), $"savefiletest_{Guid.NewGuid()}.json");
        }
        
        [TearDown]
        public void TearDown()
        {
            // Clean up any test files
            if (File.Exists(_testFilePath))
            {
                try
                {
                    File.Delete(_testFilePath);
                }
                catch
                {
                    // Ignore errors in cleanup
                }
            }
        }
        
        [Test]
        public void Test_SaveFile_Initialization()
        {
            // Access the SaveFile static instance
            var data = OceanyaClient.SaveFile.Data;
            
            // Verify default values are initialized
            Assert.That(data, Is.Not.Null, "SaveFile.Data should not be null");
            Assert.That(data.LogMaxMessages, Is.GreaterThanOrEqualTo(0), "LogMaxMessages should have a default value");
        }
        
        [Test]
        public void Test_SaveFile_CustomConsoleIntegration()
        {
            // Set a test value that would be saved
            OceanyaClient.SaveFile.Data.LogMaxMessages = 100;
            
            // Capture console output
            var prevOutput = CustomConsole.OnWriteLine;
            var consoleOutput = new StringBuilder();
            CustomConsole.OnWriteLine = (s) => consoleOutput.AppendLine(s);
            
            // Generate some debug output
            CustomConsole.Debug("Test debug message");
            
            // Reset console output
            CustomConsole.OnWriteLine = prevOutput;
            
            // Verify debug message was processed
            #if DEBUG
            Assert.That(consoleOutput.ToString(), Does.Contain("Test debug message"), "Debug message should be processed");
            #else
            Assert.Pass("Debug message test skipped in Release mode");
            #endif
        }
    }
    
    [TestFixture]
    public class CustomConsoleTests
    {
        private StringBuilder _testOutput = new StringBuilder();
        private Action<string>? _originalOnWriteLine;
        
        [SetUp]
        public void SetUp()
        {
            _testOutput = new StringBuilder();
            
            // Store original output methods
            _originalOnWriteLine = CustomConsole.OnWriteLine;
            
            // Override with test methods
            CustomConsole.OnWriteLine = (s) => _testOutput.AppendLine(s);
        }
        
        [TearDown]
        public void TearDown()
        {
            // Restore original output methods
            CustomConsole.OnWriteLine = _originalOnWriteLine;
        }
        
        [Test]
        public void Test_CustomConsole_InfoOutput()
        {
            CustomConsole.Info("Test info message");
            
            Assert.That(_testOutput.ToString(), Does.Contain("ℹ️ Test info message"), 
                "Info message should be formatted correctly");
        }
        
        [Test]
        public void Test_CustomConsole_ErrorOutput()
        {
            // Test with just message
            CustomConsole.Error("Test error message");
            
            Assert.That(_testOutput.ToString(), Does.Contain("❌ Test error message"), 
                "Error message should be formatted correctly");
            
            // Clear output
            _testOutput.Clear();
            
            // Test with exception
            var exception = new Exception("Test exception");
            CustomConsole.Error("Test error with exception", exception);
            
            string output = _testOutput.ToString();
            Assert.That(output, Does.Contain("❌ Test error with exception"), 
                "Error with exception should include message");
            Assert.That(output, Does.Contain("Exception: Exception"), 
                "Error with exception should include exception type");
            Assert.That(output, Does.Contain("Message: Test exception"), 
                "Error with exception should include exception message");
        }
        
        [Test]
        public void Test_CustomConsole_DebugOutput()
        {
            CustomConsole.Debug("Test debug message");
            
            // This may not work in Release mode since Debug is conditional
            #if DEBUG
            Assert.That(_testOutput.ToString(), Does.Contain("🔍 Test debug message"), 
                "Debug message should be formatted correctly");
            #else
            Assert.Pass("Debug message test skipped in Release mode");
            #endif
        }
        
        [Test]
        public void Test_CustomConsole_WarningOutput()
        {
            CustomConsole.Warning("Test warning message");
            
            Assert.That(_testOutput.ToString(), Does.Contain("⚠️ Test warning message"), 
                "Warning message should be formatted correctly");
        }
        
        [Test]
        public void Test_CustomConsole_LogOutput()
        {
            CustomConsole.Log("Test log message", CustomConsole.LogLevel.Info);
            
            Assert.That(_testOutput.ToString(), Does.Contain("ℹ️ Test log message"), 
                "Log with Info level should be formatted correctly");
        }
    }
}