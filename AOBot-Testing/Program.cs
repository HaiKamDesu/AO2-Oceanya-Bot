using System;
using System.Text.Json;
using AOBot_Testing.Agents;
using AOBot_Testing.Structures;
using AOBot_Testing;

public partial class Program
{
    public static async Task Main(string[] args)
    {
        //CharacterINI.RefreshCharacterList();

        #region Create the bot and connect to the server
        AOBot bot = new AOBot(Globals.IPs[Globals.Servers.ChillAndDices], "Basement/testing");
        await bot.Connect();
        #endregion

        #region Start the GPT Client
        string? apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY", EnvironmentVariableTarget.User);
        if (string.IsNullOrEmpty(apiKey))
        {
            throw new Exception("OpenAI API key is not set in the environment variables.");
        }
        GPTClient gptClient = new GPTClient(apiKey);
        gptClient.SetSystemInstructions(new List<string> { Globals.AI_SYSTEM_PROMPT });
        gptClient.systemVariables = new Dictionary<string, string>()
        {
            { "[[[current_character]]]", bot.currentINI.Name },
            { "[[[current_emote]]]", bot.currentEmote.Name }
        };
        #endregion

        ChatLogManager chatLog = new ChatLogManager(MaxChatHistory: 20);

        bot.OnMessageReceived += async (string chatLogType, string characterName, string showName, string message, int iniPuppetID) =>
        {
            chatLog.AddMessage(chatLogType, characterName, showName, message);

            if (!Globals.UseOpenAIAPI) return;
            
            switch (chatLogType)
            {
                case "IC":
                    if (showName == bot.ICShowname && characterName == bot.currentINI.Name && iniPuppetID == bot.selectedCharacterIndex)
                    {
                        return;
                    }
                    break;
                case "OOC":
                    if (showName == bot.ICShowname)
                    {
                        return;
                    }
                    break;
            }
            
            
            int maxRetries = 3; // Prevent infinite loops
            int attempt = 0;
            bool success = false;

            while (attempt < maxRetries && !success)
            {
                attempt++;
                if (Globals.DebugMode) Console.WriteLine($"Prompting AI..." + (attempt > 0 ? " (Attempt {attempt})" : ""));
                string response = await gptClient.GetResponseAsync(chatLog.GetFormattedChatHistory());
                if (Globals.DebugMode) Console.WriteLine("Received AI response: " + response);

                success = await ValidateJsonResponse(bot, response);
            }

            if (!success)
            {
                Console.WriteLine("ERROR: AI failed to return a valid response after multiple attempts.");
            }
        };

        await Task.Delay(-1);
    }

    private static async Task<bool> ValidateJsonResponse(AOBot bot, string response)
    {
        bool success = false;

        // Ensure response is not empty and not SYSTEM_WAIT()
        if (string.IsNullOrWhiteSpace(response) || response == "SYSTEM_WAIT()")
            return success;

        try
        {
            var responseJson = JsonSerializer.Deserialize<Dictionary<string, object>>(response);
            if (responseJson == null)
            {
                Console.WriteLine("ERROR: AI response is not valid JSON. Retrying...");
                return success;
            }

            // Validate required fields
            if (!responseJson.ContainsKey("message") || !responseJson.ContainsKey("chatlog") ||
                !responseJson.ContainsKey("showname") || !responseJson.ContainsKey("current_character") ||
                !responseJson.ContainsKey("modifiers"))
            {
                Console.WriteLine("ERROR: AI response is missing required fields. Retrying...");
                return success;
            }

            string botMessage = responseJson["message"].ToString();
            string chatlogType = responseJson["chatlog"].ToString();
            string newShowname = responseJson["showname"].ToString();
            string newCharacter = responseJson["current_character"].ToString();

            // Ensure chatlogType is either "IC" or "OOC"
            if (chatlogType != "IC" && chatlogType != "OOC")
            {
                Console.WriteLine($"ERROR: Invalid chatlog type '{chatlogType}', retrying...");
                return success;
            }

            // Apply showname change if valid
            if (!string.IsNullOrEmpty(newShowname))
            {
                bot.SetICShowname(newShowname);
            }

            // Apply character switch if valid
            if (!string.IsNullOrEmpty(newCharacter))
            {
                bot.SetCharacter(newCharacter);
            }

            // Apply modifiers with strict validation
            if (responseJson["modifiers"] is JsonElement modifiersElement)
            {
                var modifiers = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(modifiersElement.GetRawText());

                if (modifiers != null)
                {
                    if (modifiers.ContainsKey("deskMod") && modifiers["deskMod"].TryGetInt32(out int deskModValue))
                        bot.deskMod = (ICMessage.DeskMods)deskModValue;
                    else
                    {
                        Console.WriteLine("ERROR: Invalid deskMod value. Retrying...");
                        return success;
                    }

                    if (modifiers.ContainsKey("emoteMod") && modifiers["emoteMod"].TryGetInt32(out int emoteModValue))
                        bot.emoteMod = (ICMessage.EmoteModifiers)emoteModValue;
                    else
                    {
                        Console.WriteLine("ERROR: Invalid emoteMod value. Retrying...");
                        return success;
                    }

                    if (modifiers.ContainsKey("shoutModifiers") && modifiers["shoutModifiers"].TryGetInt32(out int shoutModValue))
                        bot.shoutModifiers = (ICMessage.ShoutModifiers)shoutModValue;
                    else
                    {
                        Console.WriteLine("ERROR: Invalid shoutModifiers value. Retrying...");
                        return success;
                    }

                    if (modifiers.ContainsKey("flip") && modifiers["flip"].TryGetInt32(out int flipValue))
                        bot.flip = flipValue == 1;
                    else
                    {
                        Console.WriteLine("ERROR: Invalid flip value. Retrying...");
                        return success;
                    }

                    if (modifiers.ContainsKey("realization") && modifiers["realization"].TryGetInt32(out int realizationValue))
                        bot.realization = realizationValue == 1;
                    else
                    {
                        Console.WriteLine("ERROR: Invalid realization value. Retrying...");
                        return success;
                    }

                    if (modifiers.ContainsKey("textColor") && modifiers["textColor"].TryGetInt32(out int textColorValue))
                        bot.textColor = (ICMessage.TextColors)textColorValue;
                    else
                    {
                        Console.WriteLine("ERROR: Invalid textColor value. Retrying...");
                        return success;
                    }

                    if (modifiers.ContainsKey("immediate") && modifiers["immediate"].TryGetInt32(out int immediateValue))
                        bot.Immediate = immediateValue == 1;
                    else
                    {
                        Console.WriteLine("ERROR: Invalid immediate value. Retrying...");
                        return success;
                    }

                    if (modifiers.ContainsKey("additive") && modifiers["additive"].TryGetInt32(out int additiveValue))
                        bot.Additive = additiveValue == 1;
                    else
                    {
                        Console.WriteLine("ERROR: Invalid additive value. Retrying...");
                        return success;
                    }
                }
                else
                {
                    Console.WriteLine("ERROR: AI response modifiers section is invalid. Retrying...");
                    return success;
                }
            }

            // Send response based on chatlog type
            if (!string.IsNullOrEmpty(botMessage))
            {
                switch (chatlogType)
                {
                    case "IC":
                        await bot.SendICMessage(botMessage);
                        break;
                    case "OOC":
                        await bot.SendOOCMessage(bot.ICShowname, botMessage);
                        break;
                }
            }
            else
            {
                Console.WriteLine("ERROR: AI response message is empty. Retrying...");
                return success;
            }

            success = true; // If we reach here, response was valid and handled successfully
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR: Exception while processing AI response - {ex.Message}. Retrying...");
        }

        return success;
    }
}
