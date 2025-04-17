using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CrossPlatformTests
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
        }
    }
}