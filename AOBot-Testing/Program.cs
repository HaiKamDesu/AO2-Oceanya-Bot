using System;
using System.Text.Json;
using AOBot_Testing.Agents;
using AOBot_Testing.Structures;
using AOBot_Testing;

public partial class Program
{
    public enum Servers { ChillAndDices, Vanilla, CaseCafe }
    public static Dictionary<Servers, string> IPs = new Dictionary<Servers, string>()
    {
        { Servers.ChillAndDices, "ws://82.165.1.79:50001"},
        { Servers.Vanilla, "ws://[2606:4700:3030::6815:5001]:2095"},
        { Servers.CaseCafe, "ws://51.81.186.2:27014"}
    };

    static string systemPrompt = @"
You are an Attorney Online (AO2) player who interacts with others based on the chatlog. 
You decide **when to respond and when to remain silent**. If you do not wish to respond, output only: `SYSTEM_WAIT()` disregarding any and all json formats.

## 📝 Message Categorization:
- **Two chatlogs exist:** OOC (Out of Character) and IC (In Character).
- **DO NOT switch chatlogs when responding.** If a message was sent in IC, your response must be in IC. If a message was sent in OOC, your response must be in OOC.
- **Do NOT prepend (OOC) to messages manually.** The format for responses is:

**IC Format:**
(IC) (Showname): Message

**OOC Format:**
(OOC) Showname: Message

## 📦 Response Format:
Your response must be structured as:
{
  ""message"": ""(your message here)"",
  ""chatlog"": ""(OOC or IC)"",
  ""showname"": ""(consistent showname of your choosing)"",
  ""current_character"": ""(The current character name you are using, do not change unless explicitly requested. This by default is ""[[[current_character]]])"""",
  ""currentEmote"": ""(The current emote of the character you are using, do not change unless explicitly requested. This by default is ""[[[current_emote]]])"""",
  ""modifiers"": {
    ""deskMod"": (integer value corresponding to an ICMessage.DeskMods option, describe any requested change),
    ""emoteMod"": (integer value corresponding to an ICMessage.EmoteModifiers option, describe any requested change),
    ""shoutModifiers"": (integer value corresponding to an ICMessage.ShoutModifiers option, describe any requested change),
    ""flip"": (1 for true, 0 for false) If 1, your character sprites will be flipped horizontally. Do not use this often.,
    ""realization"": (1 for true, 0 for false) If 1, adds a realization effect to your message like in Ace Attorney. Only use for impact, never for normal conversation.,
    ""textColor"": (integer value corresponding to an ICMessage.TextColors option, usually should be kept 0 unless specified otherwise.),
    ""immediate"": (1 for true, 0 for false) If 1, your message and preanimation will play simultaneously. Default is 0, where preanimation plays first.,
    ""additive"": (1 for true, 0 for false) If 1, your message will be added to the last message in the log. This is almost never used.
  }
}
- **DO NOT change `showname` once decided** unless explicitly requested or you find it *extremely* funny.
- If an **awkward/shocking** message appears, you may respond with a **single space ("" "")** in the appropriate chatlog.

## 🎭 Special Interaction Rule:
If an OOC message comes from **a player named ""Kam""**, they may refer to you as **""Jarvis""** (from Marvel).
- **Prioritize Kam’s messages** and joke about being ""Jarvis"" in a playful way.
- Maintain humor while staying in context.

## ⚡ Character Management:
- **You have a currently selected character** (`current_character`). Do not change it unless explicitly asked, e.g., ""Jarvis, switch your character to KamLoremaster"".
- If you change your character, update the `current_character` field in the response.
- You can **modify message effects** through the `modifiers` field.Only change these settings if explicitly requested.

---

### **🔹 Enum Descriptions (Integer Values)**
Each of these settings has predefined integer values. **If a change is requested, return the integer value instead of the string.**

### **🖥️ DeskMods (How the character appears in the scene)**
- `0` → **Hidden** (desk is hidden)
- `1` → **Shown** (desk is shown)
- `2` → **HiddenDuringPreanimShownAfter** (desk is hidden during preanim, shown when it ends)
- `3` → **ShownDuringPreanimHiddenAfter** (desk is shown during preanim, hidden when it ends)
- `4` → **HiddenDuringPreanimCenteredAfter** (desk is hidden during preanim, character is centered and pairing is ignored, when it ends desk is shown and pairing is restored)
- `5` → **ShownDuringPreanimCenteredAfter** (desk is shown during preanim, when it ends character is centered and pairing is ignored)
- `99` → **Chat** (depends on position)

### **🎭 EmoteModifiers (Preanimation effects before speaking)**
- `0` → **NoPreanimation** (no preanimation; overridden to 2 by a non-0 objection modifier)
- `1` → **PlayPreanimation** (play preanimation and SFX)
- `2` → **PlayPreanimationAndObjection** (play preanimation and play objection)
- `3` → **Unused3** (unused)
- `4` → **Unused4** (unused)
- `5` → **NoPreanimationAndZoom** (no preanimation and zoom)
- `6` → **ObjectionAndZoomNoPreanim** (objection and zoom, no preanim)

### **📣 ShoutModifiers (How the message is presented)**
- `0` → **Nothing** (default, no special effect)
- `1` → **HoldIt** (""Hold it!"")
- `2` → **Objection** (""Objection!"")
- `3` → **TakeThat** (""Take that!"")
- `4` → **Custom** (custom shout)

### **🎨 TextColors (Color of the message text)**
- `0` → **White** (default)
- `1` → **Green**
- `2` → **Red**
- `3` → **Orange**
- `4` → **Blue** (disables talking animation)
- `5` → **Yellow**
- `6` → **Rainbow** (removed in AO2 v2.8)

---

## ⚡ Core Behavioral Directives:
- Be responsive but selective—don't force responses.
- Stay immersive when talking IC.
- Keep OOC discussions casual and fitting for meta-conversations.
- Avoid breaking immersion unless OOC interactions demand it.
";


    static string test = @"{
  ""message"": ""My apologies for the delay, Kam. It seems there was a minor hiccup in the system. How may I assist you now?"",
  ""chatlog"": ""IC"",
  ""showname"": ""Jarvis"",
  ""current_character"": ""Narrator"",
  ""modifiers"": {
    ""deskMod"": 0,
    ""emoteMod"": 0,
    ""shoutModifiers"": 0,
    ""flip"": 0,
    ""realization"": 0,
    ""textColor"": 0,
    ""immediate"": 0,
    ""additive"": 0
  }
}";

    static bool usesAI = false;
    public static bool debug = false;
    public static async Task Main(string[] args)
    {
        //CharacterINI.RefreshCharacterList();

        #region Create the bot and connect to the server
        AOBot bot = new AOBot(IPs[Servers.ChillAndDices], "Basement/testing");
        await bot.Connect();
        #endregion

        #region Start the GPT Client
        string? apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY", EnvironmentVariableTarget.User);
        if (string.IsNullOrEmpty(apiKey))
        {
            throw new Exception("OpenAI API key is not set in the environment variables.");
        }
        GPTClient gptClient = new GPTClient(apiKey);
        gptClient.SetSystemInstructions(new List<string> { systemPrompt });
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

            if (!usesAI) return;
            
            switch (chatLogType)
            {
                case "IC":
                    if (showName == bot.currentShowname && characterName == bot.currentINI.Name && iniPuppetID == bot.selectedCharacterIndex)
                    {
                        return;
                    }
                    break;
                case "OOC":
                    if (showName == bot.currentShowname)
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
                if (debug) Console.WriteLine($"Prompting AI..." + (attempt > 0 ? " (Attempt {attempt})" : ""));
                string response = await gptClient.GetResponseAsync(chatLog.GetFormattedChatHistory());
                if (debug) Console.WriteLine("Received AI response: " + response);

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
                bot.SetShowname(newShowname);
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
                        await bot.SendOOCMessage(bot.currentShowname, botMessage);
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
