using System.IO;
using System.Text.Json;
using Common;

namespace AOBot_Testing.Structures
{
    public class CharacterINI
    {
        #region Static methods
        public static List<string> CharacterFolders => Globals.BaseFolders.Select(x => Path.Combine(x, "characters")).ToList();
        static string cacheFile = Path.Combine(Path.GetTempPath(), "characters.json");
        static List<CharacterINI> characterConfigs = new List<CharacterINI>();
        public static List<CharacterINI> FullList
        {
            get
            {
                if (characterConfigs.Count == 0)
                {
                    // If JSON cache exists, load from it instead of parsing INI files
                    if (File.Exists(cacheFile))
                    {
                        characterConfigs = LoadFromJson(cacheFile);
                        CustomConsole.WriteLine($"Loaded {characterConfigs.Count} characters from cache.");
                    }
                    else
                    {
                        RefreshCharacterList();
                    }
                }

                return characterConfigs;
            }
        }

        public static void RefreshCharacterList(Action<CharacterINI> onParsedCharacter = null, Action<string> onChangedMountPath = null)
        {
            characterConfigs = new List<CharacterINI>();

            foreach (var CharacterFolder in CharacterFolders)
            {
                onChangedMountPath?.Invoke(CharacterFolder);
                var directories = Directory.GetDirectories(CharacterFolder);

                foreach (var directory in directories)
                {
                    var iniFilePath = Path.Combine(directory, "char.ini");
                    if (File.Exists(iniFilePath))
                    {
                        var config = CharacterINI.Parse(iniFilePath);
                        if(!characterConfigs.Any(x => x.Name == config.Name))
                        {
                            CustomConsole.WriteLine("Parsed Character: " + config.Name + $" ({CharacterFolder})");
                            characterConfigs.Add(config);
                            onParsedCharacter?.Invoke(config);
                        }
                    }
                }

            }

            // Save to JSON file for fast future loading
            SaveToJson(cacheFile, characterConfigs);
            CustomConsole.WriteLine("Character list saved to cache.");
        }
        // **Save all characters to a single JSON file**
        static void SaveToJson(string filePath, List<CharacterINI> characters)
        {
            var json = JsonSerializer.Serialize(characters, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(filePath, json);
        }

        // **Load all characters from a single JSON file**
        static List<CharacterINI> LoadFromJson(string filePath)
        {
            var json = File.ReadAllText(filePath);
            return JsonSerializer.Deserialize<List<CharacterINI>>(json);
        }
        #endregion

        public string PathToIni { get; set; }
        public string Name { get; set; }
        public string ShowName { get; set; }
        public string Gender { get; set; }
        public string Side { get; set; }
        public int PreAnimationTime { get; set; }
        public int EmotionsCount { get; set; }
        public Dictionary<int, Emote> Emotions { get; set; } = new();
        public string CharIconPath { get; set; }
        public string SoundListPath { get; set; }
        public static CharacterINI Parse(string filePath)
        {
            var config = new CharacterINI();
            config.SoundListPath = Path.Combine(Path.GetDirectoryName(filePath), "soundlist.ini");

            var lines = File.ReadAllLines(filePath);
            string section = "";

            foreach (var line in lines.Select(l => l.Trim()))
            {
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith(";")) continue;

                if (line.StartsWith("[") && line.EndsWith("]"))
                {
                    section = line.Trim('[', ']').ToLower();
                    continue;
                }

                var split = line.Split('=', 2);
                if (split.Length != 2) continue;

                string key = split[0].Trim().ToLower();
                string value = split[1].Trim();

                switch (section)
                {
                    case "options":
                        if (key == "showname") config.ShowName = value;
                        else if (key == "gender") config.Gender = value;
                        else if (key == "side") config.Side = value;
                        break;

                    case "time":
                        if (key == "preanim" && int.TryParse(value, out int preanimTime))
                            config.PreAnimationTime = preanimTime;
                        break;

                    case "emotions":
                        if (key == "number" && int.TryParse(value, out int emotionCount))
                            config.EmotionsCount = emotionCount;
                        else if (int.TryParse(key, out int emotionId))
                        {
                            if (!config.Emotions.ContainsKey(emotionId))
                                config.Emotions[emotionId] = new Emote(emotionId);
                            config.Emotions[emotionId] = Emote.ParseEmoteLine(value);
                            config.Emotions[emotionId].ID = emotionId;
                        }
                        break;

                    case "soundn":
                        if (int.TryParse(key, out int soundId))
                        {
                            if (!config.Emotions.ContainsKey(soundId))
                                config.Emotions[soundId] = new Emote(soundId);
                            config.Emotions[soundId].sfxName = string.IsNullOrEmpty(value) ? "1" : value;
                        }
                        break;

                    case "soundt":
                        if (int.TryParse(key, out int soundTimeId) && int.TryParse(value, out int timeValue))
                        {
                            if (!config.Emotions.ContainsKey(soundTimeId))
                                config.Emotions[soundTimeId] = new Emote(soundTimeId);
                            config.Emotions[soundTimeId].sfxDelay = timeValue;
                        }
                        break;
                }
            }
            config.Name = Path.GetFileName(Path.GetDirectoryName(filePath));
            config.PathToIni = filePath;
            string baseCharacterPath = Path.GetDirectoryName(filePath);

            var extensions = new List<string> { "png", "jpg", "webp", "gif", "pdn" };
            foreach (var extension in extensions)
            {
                string curPath = Path.Combine(baseCharacterPath, "char_icon." + extension);
                if (File.Exists(curPath)) 
                {
                    config.CharIconPath = curPath;
                }
            }

            if (string.IsNullOrEmpty(config.CharIconPath))
            {
                config.CharIconPath = Path.Combine(baseCharacterPath, "char_icon.png");
            }

            #region Gather Buttons
            string buttonPath = Path.Combine(baseCharacterPath, "Emotions");

            if (Directory.Exists(buttonPath))
            {

            }
            foreach (var item in config.Emotions)
            {
                int id = item.Key;

                foreach (var extension in extensions)
                {
                    string currentButtonPath_off = Path.Combine(buttonPath, $"button{id}_off."+extension);
                    if (File.Exists(currentButtonPath_off) && string.IsNullOrEmpty(item.Value.PathToImage_off))
                    {
                        item.Value.PathToImage_off = currentButtonPath_off;
                    }

                    string currentButtonPath_on = Path.Combine(buttonPath, $"button{id}_on." + extension);
                    if (File.Exists(currentButtonPath_on) && string.IsNullOrEmpty(item.Value.PathToImage_on))
                    {
                        item.Value.PathToImage_on = currentButtonPath_on;
                    }
                }
            }
            #endregion

            if(config.EmotionsCount != config.Emotions.Count)
            {
                for (int i = 1; i <= config.EmotionsCount; i++)
                {
                    if (!config.Emotions.ContainsKey(i))
                    {
                        //add an empty emote, since this is how the AO client works.
                        config.Emotions.Add(i, new Emote(i));
                    }
                }

                config.Emotions = config.Emotions.Take(config.EmotionsCount).ToDictionary(x => x.Key, x => x.Value);
            }

            return config;
        }
    }
}