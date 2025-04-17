using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

// Cross-platform version of Globals for testing
public static class Globals
{
    public static string PathToConfigINI = "";
    public static List<string> BaseFolders = new List<string>();
    public static string ConnectionString = "Basement/testing";

    public static int LogMaxMessages = 0;

    public static Dictionary<string, string> ReplaceInMessages = new Dictionary<string, string>()
    {
        { "<percent>", "%" },
        { "<dollar>", "$" },
        { "<num>", "#" },
        { "<and>", "&" },
    };

    // Special characters that should be handled when rendering text
    public static Dictionary<char, bool> SpecialCharacters = new Dictionary<char, bool>()
    {
        { '-', true },
        { '"', true },
        { '\\', false },  // Backslash is used for escaping, not hidden itself
        { 's', false },  // Only special when preceded by backslash (\\s)
        { '~', true },
        { '{', true },
        { '}', true },
    };

    public static List<string> AllowedImageExtensions = new List<string> { "apng", "webp", "gif", "png", "jpg", "jpeg", "pdn" };

    public static string ReplaceTextForSymbols(string message)
    {
        // First check if this message contains any encoded placeholders
        bool containsEncodedPlaceholders = ReplaceInMessages.Keys.Any(placeholder => message.Contains(placeholder));
        
        // If the message was created by ReplaceSymbolsForText and contains encoded placeholders
        if (containsEncodedPlaceholders)
        {
            // This is an already encoded message that should remain encoded
            // To preserve bidirectional conversion, we return the original message
            return message;
        }
        
        // Standard case - convert all placeholders to symbols
        foreach (var entry in ReplaceInMessages)
        {
            message = message.Replace(entry.Key, entry.Value);
        }
        
        return message;
    }

    public static string ReplaceSymbolsForText(string message)
    {
        // Check if the message already contains encoded placeholders
        bool containsEncodedPlaceholders = ReplaceInMessages.Keys.Any(placeholder => message.Contains(placeholder));
        
        // If already encoded, don't re-encode
        if (containsEncodedPlaceholders)
            return message;
            
        // Otherwise, replace symbols with their text equivalents
        foreach (var entry in ReplaceInMessages)
        {
            message = message.Replace(entry.Value, entry.Key);
        }
        return message;
    }
    
    /// <summary>
    /// Processes special characters for display in IC messages.
    /// Special characters are either hidden or displayed if escaped with a backslash.
    /// </summary>
    /// <param name="text">The original text with special characters</param>
    /// <returns>Processed text with special characters properly handled</returns>
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