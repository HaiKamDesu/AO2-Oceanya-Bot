
namespace AOBot_Testing.Structures
{
    public class Emote
    {
        public string Name { get; set; }
        public string PreAnimation { get; set; }
        public string Animation { get; set; }
        public string Modifier { get; set; }


        public string sfxName { get; set; } = "1";
        public int sfxDelay { get; set; } = 0;

        public static Emote ParseEmoteLine(string data)
        {
            var parts = data.Split('#');
            return new Emote
            {
                Name = parts.Length > 0 ? parts[0] : "",
                PreAnimation = parts.Length > 1 ? parts[1] : "",
                Animation = parts.Length > 2 ? parts[2] : "",
                Modifier = parts.Length > 3 ? parts[3] : "0",
            };
        }
    }
}


