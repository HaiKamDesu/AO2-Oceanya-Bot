using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    //class that will be used to handle console output by calling both console.write line and debug.log (so that it appears both in console and output)
    public static class CustomConsole
    {
        public static List<string> lines = new List<string>();

        public static void WriteLine(string message)
        {
            string timestampedMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}";
            Console.WriteLine(timestampedMessage);
            System.Diagnostics.Debug.WriteLine(timestampedMessage);

            lines.Add(timestampedMessage);
        }
    }
}
