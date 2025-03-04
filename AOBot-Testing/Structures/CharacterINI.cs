using System.Text.Json;

namespace AOBot_Testing.Structures
{
    public class CharacterINI
    {
        #region Static methods
        static string characterFolder = "D:\\Programs\\Attorney Online\\base\\characters";
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
                        Console.WriteLine($"Loaded {characterConfigs.Count} characters from cache.");
                    }
                    else
                    {
                        RefreshCharacterList();
                    }
                }

                return characterConfigs;
            }
        }

        public static void RefreshCharacterList()
        {
            characterConfigs = new List<CharacterINI>();
            var directories = Directory.GetDirectories(characterFolder);

            foreach (var directory in directories)
            {
                var iniFilePath = Path.Combine(directory, "char.ini");
                if (File.Exists(iniFilePath))
                {
                    var config = CharacterINI.Parse(iniFilePath);
                    Console.WriteLine("Parsed Character: " + config.Name);
                    characterConfigs.Add(config);
                }
            }

            // Save to JSON file for fast future loading
            SaveToJson(cacheFile, characterConfigs);
            Console.WriteLine("Character list saved to cache.");
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

        public static CharacterINI Parse(string filePath)
        {
            var config = new CharacterINI();
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
                                config.Emotions[emotionId] = new Emote();
                            config.Emotions[emotionId] = Emote.ParseEmoteLine(value);
                        }
                        break;

                    case "soundn":
                        if (int.TryParse(key, out int soundId))
                        {
                            if (!config.Emotions.ContainsKey(soundId))
                                config.Emotions[soundId] = new Emote();
                            config.Emotions[soundId].sfxName = value;
                        }
                        break;

                    case "soundt":
                        if (int.TryParse(key, out int soundTimeId) && int.TryParse(value, out int timeValue))
                        {
                            if (!config.Emotions.ContainsKey(soundTimeId))
                                config.Emotions[soundTimeId] = new Emote();
                            config.Emotions[soundTimeId].sfxDelay = timeValue;
                        }
                        break;
                }
            }
            config.Name = Path.GetFileName(Path.GetDirectoryName(filePath));
            config.PathToIni = filePath;
            return config;
        }
    }
}