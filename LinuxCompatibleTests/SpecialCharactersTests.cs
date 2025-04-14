using NUnit.Framework;
using Common;

namespace LinuxCompatibleTests
{
    [TestFixture]
    public class SpecialCharactersTests
    {
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
}