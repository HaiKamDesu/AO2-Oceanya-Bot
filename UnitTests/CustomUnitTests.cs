using Common;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AOBot_Testing.Structures;
using static AOBot_Testing.Structures.ICMessage;

namespace UnitTests
{
    [TestFixture]
    public class CustomUnitTests
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
                    CustomConsole.Error($"Failed to read {file}", ex);
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
            CustomConsole.Info($"Total Files Processed: {logs.Count}");
            CustomConsole.Info($"Total Characters: {totalCharacters}");
            CustomConsole.Info($"Total Words: {totalWords}");
            CustomConsole.Info($"Total Lines: {totalLines}");
            CustomConsole.Info($"Average Characters per File: {avgCharacters:F2}");
            CustomConsole.Info($"Average Words per File: {avgWords:F2}");
            CustomConsole.Info($"Average Lines per File: {avgLines:F2}");
            CustomConsole.Info($"Estimated Token Count (GPT-4): {estimatedTokens}");
            CustomConsole.Info("Fine-Tuning Cost Estimates:");

            foreach (var model in fineTuningPrices)
            {
                CustomConsole.Info($"  {model.Key}: ${model.Value:F2}");
            }
        }

        CountdownTimer timer;
        [Test]
        public async Task TestM_INITesting()
        {
            for (int i = 0; i < 3; i++)
            {
                CustomConsole.Info("[[TIMER START]]");
                if (timer == null)
                {
                    timer = new CountdownTimer(TimeSpan.FromSeconds(10));
                    timer.TimerElapsed += () =>
                    {
                        CustomConsole.Info("[[TIMER END]]");
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

    [TestFixture]
    public class ICMessageTests
    {
        private ICMessage CreateSampleMessage()
        {
            return new ICMessage
            {
                DeskMod = DeskMods.Shown,
                PreAnim = "happy",
                Character = "Franziska",
                Emote = "normal",
                Message = "This is a test message",
                Side = "wit",
                SfxName = "whip1",
                EmoteModifier = EmoteModifiers.PlayPreanimation,
                CharId = 1,
                SfxDelay = 0,
                ShoutModifier = ShoutModifiers.Objection,
                EvidenceID = "0",
                Flip = false,
                Realization = false,
                TextColor = TextColors.White,
                ShowName = "TestShowname",
                OtherCharId = -1,
                SelfOffset = (0, 0),
                NonInterruptingPreAnim = false,
                SfxLooping = false,
                ScreenShake = false,
                FramesShake = "happy^(b)normal^(a)normal^",
                FramesRealization = "happy^(b)normal^(a)normal^",
                FramesSfx = "happy^(b)normal^(a)normal^",
                Additive = false,
                Effect = Effects.None,
                Blips = ""
            };
        }

        [Test]
        public void Test_ICMessage_ToCommand()
        {
            // Create a sample message
            ICMessage message = CreateSampleMessage();

            // Get command string
            string command = ICMessage.GetCommand(message);

            // Test command starts with MS# and ends with %%
            Assert.That(command, Does.StartWith("MS#"), "Command should start with MS#");
            Assert.That(command, Does.EndWith("%"), "Command should end with %");

            // Test command contains the expected number of fields (31 fields, 30 # separators)
            int fieldCount = command.Count(c => c == '#');
            Assert.That(fieldCount, Is.EqualTo(31), "Command should have 31 fields");

            // Test key fields
            Assert.That(command, Does.Contain($"#{message.Character}#"), "Command should contain character name");
            Assert.That(command, Does.Contain($"#{message.Emote}#"), "Command should contain emote name");
            Assert.That(command, Does.Contain($"#{message.Message}#"), "Command should contain message text");
        }

        [Test]
        public void Test_ICMessage_FromConsoleLine()
        {
            // Create a sample message
            ICMessage originalMessage = CreateSampleMessage();
            
            // Convert to command
            string command = ICMessage.GetCommand(originalMessage);
            
            // Parse back from command
            ICMessage parsedMessage = ICMessage.FromConsoleLine(command);
            
            // Verify parsing worked correctly
            Assert.That(parsedMessage, Is.Not.Null, "Parsed message should not be null");
            
            // Verify key properties match
            Assert.Multiple(() =>
            {
                Assert.That(parsedMessage.Character, Is.EqualTo(originalMessage.Character), "Character should match");
                Assert.That(parsedMessage.Emote, Is.EqualTo(originalMessage.Emote), "Emote should match");
                Assert.That(parsedMessage.Message, Is.EqualTo(originalMessage.Message), "Message should match");
                Assert.That(parsedMessage.DeskMod, Is.EqualTo(originalMessage.DeskMod), "DeskMod should match");
                Assert.That(parsedMessage.EmoteModifier, Is.EqualTo(originalMessage.EmoteModifier), "EmoteModifier should match");
                Assert.That(parsedMessage.ShoutModifier, Is.EqualTo(originalMessage.ShoutModifier), "ShoutModifier should match");
                Assert.That(parsedMessage.Flip, Is.EqualTo(originalMessage.Flip), "Flip should match");
                Assert.That(parsedMessage.TextColor, Is.EqualTo(originalMessage.TextColor), "TextColor should match");
            });
        }

        [Test]
        public void Test_ICMessage_WithInvalidFormat()
        {
            // Test with invalid header
            string invalidHeader = "INVALID#1#pre#char#emote#message#wit#sfx#0#1#0#2#0#0#0#0#showname#-1#name#emote#0<and>0#0#0#0#0#0#frames#frames#frames#0#||#%";
            ICMessage result1 = ICMessage.FromConsoleLine(invalidHeader);
            Assert.That(result1, Is.Null, "Message with invalid header should return null");
            
            // Test with too few fields
            string tooFewFields = "MS#1#pre#char#emote#message#%";
            ICMessage result2 = ICMessage.FromConsoleLine(tooFewFields);
            Assert.That(result2, Is.Null, "Message with too few fields should return null");
        }
        
        [Test]
        public void Test_ICMessage_SpecialTextColors()
        {
            // Test red text color special handling
            ICMessage message = CreateSampleMessage();
            message.TextColor = TextColors.Red;
            message.Message = "Test red text";
            
            string command = ICMessage.GetCommand(message);
            ICMessage parsedMessage = ICMessage.FromConsoleLine(command);
            
            Assert.That(parsedMessage.TextColor, Is.EqualTo(TextColors.Red), "Red text color should be preserved");
            // Red text is surrounded by ~ in the actual message
            Assert.That(command, Does.Contain("~Test red text~"), "Red text should be wrapped with ~");
        }
        
        [Test]
        public void Test_ICMessage_SelfOffset()
        {
            // Test self offset parsing
            ICMessage message = CreateSampleMessage();
            message.SelfOffset = (50, -30);
            
            string command = ICMessage.GetCommand(message);
            ICMessage parsedMessage = ICMessage.FromConsoleLine(command);
            
            Assert.Multiple(() =>
            {
                Assert.That(parsedMessage.SelfOffset.Horizontal, Is.EqualTo(50), "Horizontal offset should match");
                Assert.That(parsedMessage.SelfOffset.Vertical, Is.EqualTo(-30), "Vertical offset should match");
            });
        }
        
        [Test]
        public void Test_ICMessage_Effects()
        {
            // Test each effect type
            foreach (Effects effect in Enum.GetValues(typeof(Effects)))
            {
                ICMessage message = CreateSampleMessage();
                message.Effect = effect;
                
                string command = ICMessage.GetCommand(message);
                ICMessage parsedMessage = ICMessage.FromConsoleLine(command);
                
                Assert.That(parsedMessage.Effect, Is.EqualTo(effect), $"Effect {effect} should be preserved");
                
                // If it's realization effect, make sure Realization property is also true
                if (effect == Effects.Realization)
                {
                    Assert.That(message.Realization, Is.True, "Realization property should be true for Realization effect");
                }
            }
        }
        
        [Test]
        public void Test_GetColorFromTextColor()
        {
            // Test color conversion for all TextColors
            foreach (TextColors color in Enum.GetValues(typeof(TextColors)))
            {
                var drawingColor = ICMessage.GetColorFromTextColor(color);
                Assert.That(drawingColor, Is.Not.Null, $"Color for {color} should not be null");
            }
        }
    }

    [TestFixture]
    public class CountdownTimerTests
    {
        [Test]
        public async Task Test_CountdownTimer_Basic()
        {
            // Create timer with 500ms
            var timer = new CountdownTimer(TimeSpan.FromMilliseconds(500));
            bool elapsed = false;
            
            timer.TimerElapsed += () => elapsed = true;
            timer.Start();
            
            // Timer should not have elapsed yet
            Assert.That(elapsed, Is.False, "Timer should not have elapsed immediately");
            
            // Wait for timer to elapse
            await Task.Delay(600); // Add buffer to ensure timer completes
            
            // Timer should have elapsed
            Assert.That(elapsed, Is.True, "Timer should have elapsed after delay");
        }
        
        [Test]
        public async Task Test_CountdownTimer_Reset()
        {
            // Create timer with 1 second
            var timer = new CountdownTimer(TimeSpan.FromSeconds(1));
            bool elapsed = false;
            
            timer.TimerElapsed += () => elapsed = true;
            timer.Start();
            
            // Wait 500ms
            await Task.Delay(500);
            
            // Reset timer to 500ms (effectively giving it 1 second total)
            timer.Reset(TimeSpan.FromMilliseconds(500));
            
            // Wait 300ms - timer should not have elapsed yet (800ms total)
            await Task.Delay(300);
            Assert.That(elapsed, Is.False, "Timer should not have elapsed yet after reset");
            
            // Wait 300ms more - timer should have elapsed (1100ms total)
            await Task.Delay(300);
            Assert.That(elapsed, Is.True, "Timer should have elapsed after full duration");
        }
        
        [Test]
        public async Task Test_CountdownTimer_GetRemainingTime()
        {
            // Create timer with 2 seconds
            var timer = new CountdownTimer(TimeSpan.FromSeconds(2));
            timer.Start();
            
            // Wait 500ms
            await Task.Delay(500);
            
            // Get remaining time
            var remaining = timer.GetRemainingTime();
            
            // Should have approximately 1.5 seconds remaining (allow 100ms margin for timing variations)
            Assert.That(remaining.TotalMilliseconds, Is.InRange(1400, 1600), 
                "Remaining time should be approximately 1.5 seconds");
        }
        
        [Test]
        public async Task Test_CountdownTimer_Stop()
        {
            // Create timer with 1 second
            var timer = new CountdownTimer(TimeSpan.FromSeconds(1));
            bool elapsed = false;
            
            timer.TimerElapsed += () => elapsed = true;
            timer.Start();
            
            // Wait 500ms
            await Task.Delay(500);
            
            // Stop timer
            timer.Stop();
            
            // Wait past when timer would have elapsed
            await Task.Delay(600);
            
            // Timer should not have elapsed because it was stopped
            Assert.That(elapsed, Is.False, "Timer should not elapse after being stopped");
        }
    }
}