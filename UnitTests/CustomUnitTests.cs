using Common;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnitTests
{
    class CustomUnitTests
    {
        //[Test]
        public void GatherAllScriptsForChatGPT()
        {
            string sourceDirectory = @"D:\Programs\Attorney Online\RPG\AO2-Oceanya-Bot";
            string destinationDirectory = @"D:\Programs\Attorney Online\RPG\Temp";

            var allowedExtensions = new string[] { ".cs", ".xaml", ".csproj" };

            var files = Directory.GetFiles(sourceDirectory, "*.*", SearchOption.AllDirectories)
                                 .Where(file => allowedExtensions.Contains(Path.GetExtension(file), StringComparer.OrdinalIgnoreCase))
                                 .ToList();

            foreach (var file in files)
            {
                // Get relative path from source directory
                string relativePath = Path.GetRelativePath(sourceDirectory, file);
                string destinationPath = Path.Combine(destinationDirectory, relativePath);

                // Ensure the directory exists in the destination
                Directory.CreateDirectory(Path.GetDirectoryName(destinationPath));

                // Copy the file
                File.Copy(file, destinationPath, true);
                Console.WriteLine($"Copied: {file} -> {destinationPath}");
            }

            Console.WriteLine("File copy operation completed.");
        }

        [Test]
        public static void AnalyzeLogs()
        {
            string logDirectory = @"D:\Programs\Attorney Online\logs";
            var logs = new Dictionary<string, string>();

            // Fetch all .log and .txt files recursively
            var files = Directory.GetFiles(logDirectory, "*.*", SearchOption.AllDirectories)
                                 .Where(f => f.EndsWith(".log", StringComparison.OrdinalIgnoreCase) || f.EndsWith(".txt", StringComparison.OrdinalIgnoreCase));

            // Read content and populate dictionary
            foreach (var file in files)
            {
                try
                {
                    string content = File.ReadAllText(file);
                    logs[file] = content;
                }
                catch (Exception ex)
                {
                    CustomConsole.WriteLine($"Failed to read {file}: {ex.Message}");
                }
            }

            // Compute statistics
            int totalCharacters = logs.Values.Sum(content => content.Length);
            int totalWords = logs.Values.Sum(content => content.Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries).Length);
            int totalLines = logs.Values.Sum(content => content.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries).Length);
            double avgCharacters = logs.Count > 0 ? (double)totalCharacters / logs.Count : 0;
            double avgWords = logs.Count > 0 ? (double)totalWords / logs.Count : 0;
            double avgLines = logs.Count > 0 ? (double)totalLines / logs.Count : 0;

            // Estimate token count (GPT-4 typically considers ~4 characters per token)
            int estimatedTokens = (int)(totalCharacters / 4.0);
            double tokenCountInThousands = estimatedTokens / 1000.0; // Convert to thousands of tokens

            // OpenAI fine-tuning pricing per 1K tokens
            Dictionary<string, double> fineTuningCosts = new Dictionary<string, double>
            {
                { "GPT-3.5-Turbo", 0.008 },  // $0.008 per 1K tokens
                { "GPT-4-Turbo", 0.06 },     // $0.06 per 1K tokens
                { "GPT-4-Base", 0.03 }       // $0.03 per 1K tokens
            };

            // Calculate fine-tuning cost for each model
            Dictionary<string, double> fineTuningPrices = fineTuningCosts.ToDictionary(
                model => model.Key,
                model => tokenCountInThousands * model.Value
            );

            // Log results using CustomConsole
            CustomConsole.WriteLine($"Total Files Processed: {logs.Count}");
            CustomConsole.WriteLine($"Total Characters: {totalCharacters}");
            CustomConsole.WriteLine($"Total Words: {totalWords}");
            CustomConsole.WriteLine($"Total Lines: {totalLines}");
            CustomConsole.WriteLine($"Average Characters per File: {avgCharacters:F2}");
            CustomConsole.WriteLine($"Average Words per File: {avgWords:F2}");
            CustomConsole.WriteLine($"Average Lines per File: {avgLines:F2}");
            CustomConsole.WriteLine($"Estimated Token Count (GPT-4): {estimatedTokens}");
            CustomConsole.WriteLine("Fine-Tuning Cost Estimates:");

            foreach (var model in fineTuningPrices)
            {
                CustomConsole.WriteLine($"  {model.Key}: ${model.Value:F2}");
            }
        }


        CountdownTimer timer;
        [Test]
        public async Task TestM_INITesting()
        {
            for (int i = 0; i < 3; i++)
            {
                CustomConsole.WriteLine("[[TIMER START]]");
                if (timer == null)
                {
                    timer = new CountdownTimer(TimeSpan.FromSeconds(10));
                    timer.TimerElapsed += () =>
                    {
                        CustomConsole.WriteLine("[[TIMER END]]");
                        Assert.Fail("[[TIMER END]]");
                    };
                }
                else
                {
                    timer.Reset(TimeSpan.FromSeconds(10));
                }

                await Task.Delay(2000);
            }
        }
    }
}
