
namespace AOBot_Testing.Structures
{
    public class Emote
    {
        public string DisplayID { get => ID + ": " + Name; }
        public int ID { get; set; }
        public string Name { get; set; }
        public string PreAnimation { get; set; }
        public string Animation { get; set; }
        public string Modifier { get; set; }
        public int DeskMod { get; set; }

        public string PathToImage_off { get; set; }
        public string PathToImage_on { get; set; }

        public string sfxName { get; set; } = "1";
        public int sfxDelay { get; set; } = 0;

        public static List<string> charInisWithMoreThan5PartsInEmotes = new List<string>();
        public static bool trigger = false;
        public static Emote ParseEmoteLine(string data)
        {
            var parts = data.Split('#');
            trigger = parts.Length > 5 && !string.IsNullOrEmpty(parts[5]);

            return new Emote
            {
                Name = parts.Length > 0 ? parts[0] : "",
                PreAnimation = parts.Length > 1 ? parts[1] : "",
                Animation = parts.Length > 2 ? parts[2] : "",
                Modifier = parts.Length > 3 ? parts[3] : "0",
                DeskMod = parts.Length > 4 ? (int.TryParse(parts[4], out int newDeskmod) ? newDeskmod : 0) : 1
            };
        }
    }
}


