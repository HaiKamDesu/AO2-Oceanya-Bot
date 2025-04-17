using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

// Cross-platform version of CustomConsole for testing
public static class CustomConsole
{
    public enum LogLevel { Debug, Info, Warning, Error }
    
    public static Action<string> OnWriteLine = Console.WriteLine;
    
    public static void Info(string message)
    {
        Log(message, LogLevel.Info);
    }
    
    public static void Error(string message, Exception? ex = null)
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine($"❌ {message}");
        
        if (ex != null)
        {
            sb.AppendLine($"Exception: {ex.GetType()}");
            sb.AppendLine($"Message: {ex.Message}");
            sb.AppendLine($"StackTrace: {ex.StackTrace}");
        }
        
        OnWriteLine(sb.ToString());
    }
    
    public static void Warning(string message)
    {
        Log(message, LogLevel.Warning);
    }
    
    public static void Debug(string message)
    {
#if DEBUG
        Log(message, LogLevel.Debug);
#endif
    }
    
    public static void Log(string message, LogLevel level)
    {
        string prefix = level switch
        {
            LogLevel.Debug => "🔍",
            LogLevel.Info => "ℹ️",
            LogLevel.Warning => "⚠️",
            LogLevel.Error => "❌",
            _ => "ℹ️"
        };
        
        OnWriteLine($"{prefix} {message}");
    }
}