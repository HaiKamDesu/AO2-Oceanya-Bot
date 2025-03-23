using Common;
using System.Drawing;

namespace AOBot_Testing.Structures
{
    public class ICMessage
    {
        public DeskMods DeskMod { get; set; }
        public string PreAnim { get; set; }
        public string Character { get; set; }
        public string Emote { get; set; }
        public string Message { get; set; }
        public string Side { get; set; }
        public string SfxName { get; set; }
        public EmoteModifiers EmoteModifier { get; set; }
        public int CharId { get; set; }
        public int SfxDelay { get; set; }
        public ShoutModifiers ShoutModifier { get; set; }
        public string EvidenceID { get; set; }
        public bool Flip { get; set; }
        public bool Realization { get; set; }
        public TextColors TextColor { get; set; }
        public string ShowName { get; set; }
        public int OtherCharId { get; set; }
        public string OtherName { get; set; }
        public string OtherEmote { get; set; }
        public (int Horizontal, int Vertical) SelfOffset { get; set; }
        public int OtherOffset { get; set; }
        public bool OtherFlip { get; set; }
        public bool NonInterruptingPreAnim { get; set; } // Changed to bool
        public bool SfxLooping { get; set; }
        public bool ScreenShake { get; set; }
        public string FramesShake { get; set; }
        public string FramesRealization { get; set; }
        public string FramesSfx { get; set; }
        public bool Additive { get; set; }
        public Effects Effect { get; set; }
        public string EffectString
        {
            get
            {
                string effect = "";
                switch (Effect)
                {
                    default:
                    case Effects.None:
                        effect = "||";
                        break;
                    case Effects.Realization:
                        effect = "realization||sfx-realization";
                        break;
                    case Effects.Hearts:
                        effect = "hearts||sfx-squee";
                        break;
                    case Effects.Reaction:
                        effect = "reaction||sfx-reactionding";
                        break;
                    case Effects.Impact:
                        effect = "impact||sfx-fan";
                        break;
                }
                return effect;
            }
            set
            {
                switch (value)
                {
                    default:
                    case "":
                        Effect = Effects.None;
                        break;
                    case "realization||sfx-realization":
                        Effect = Effects.Realization;
                        break;
                    case "hearts||sfx-squee":
                        Effect = Effects.Hearts;
                        break;
                    case "reaction||sfx-reactionding":
                        Effect = Effects.Reaction;
                        break;
                    case "impact||sfx-fan":
                        Effect = Effects.Impact;
                        break;
                }
            }
        }
        public string Blips { get; set; }
        public string OriginalCommand { get; set; }

        #region Enums
        public enum DeskMods
        {
            Hidden = 0, // desk is hidden
            Shown = 1, // desk is shown
            HiddenDuringPreanimShownAfter = 2, // desk is hidden during preanim, shown when it ends
            ShownDuringPreanimHiddenAfter = 3, // desk is shown during preanim, hidden when it ends
            HiddenDuringPreanimCenteredAfter = 4, // desk is hidden during preanim, character is centered and pairing is ignored, when it ends desk is shown and pairing is restored
            ShownDuringPreanimCenteredAfter = 5, // desk is shown during preanim, when it ends character is centered and pairing is ignored
            Chat = 99 // chat mode. AKA depends on your position.
        }

        public enum EmoteModifiers
        {
            NoPreanimation = 0, // do not play preanimation; overridden to 2 by a non-0 objection modifier
            PlayPreanimation = 1, // play preanimation (and sfx)
            PlayPreanimationAndObjection = 2, // play preanimation and play objection
            Unused3 = 3, // unused
            Unused4 = 4, // unused
            NoPreanimationAndZoom = 5, // no preanimation and zoom
            ObjectionAndZoomNoPreanim = 6 // objection and zoom, no preanim
        }

        public enum ShoutModifiers
        {
            Nothing = 0, // nothing
            HoldIt = 1, // "Hold it!"
            Objection = 2, // "Objection!"
            TakeThat = 3, // "Take that!"
            Custom = 4 // custom shout
        }

        public enum TextColors
        {
            White = 0, // white
            Green = 1, // green
            Red = 2, // red
            Orange = 3, // orange
            Blue = 4, // blue (disables talking animation)
            Yellow = 5, // yellow
            Magenta = 6, // previously rainbow (removed in 2.8)
            Cyan = 7,
            Gray = 8,
        }

        public enum Effects
        {
            None = 0,
            Realization = 1,
            Hearts = 2,
            Reaction = 3,
            Impact = 4,
        }
        #endregion

        public ICMessage()
        {
            DeskMod = DeskMods.Chat;
            PreAnim = "";
            Character = "";
            Emote = "";
            Message = "";
            Side = "";
            SfxName = "";
            EmoteModifier = EmoteModifiers.NoPreanimation;
            CharId = -1;
            SfxDelay = 0;
            ShoutModifier = ShoutModifiers.Nothing;
            EvidenceID = "";
            Flip = false;
            Realization = false;
            TextColor = TextColors.White;
            ShowName = "";
            OtherCharId = -1;
            OtherName = "";
            OtherEmote = "";
            SelfOffset = (0, 0);
            OtherOffset = 0;
            OtherFlip = false;
            NonInterruptingPreAnim = false; // Changed to bool
            SfxLooping = false;
            ScreenShake = false;
            FramesShake = "";
            FramesRealization = "";
            FramesSfx = "";
            Additive = false;
            Effect = Effects.None;
            Blips = "";
            OriginalCommand = "";
        }

        public static ICMessage? FromConsoleLine(string message)
        {
            if (!message.StartsWith("MS#"))
            {
                CustomConsole.WriteLine("❌ Invalid IC message format.");
                return null;
            }

            string[] parts = message.Split('#');
            if (parts.Length < 31) // Ensure the message has all expected fields
            {
                CustomConsole.WriteLine("⚠️ Incomplete IC message received.");
                return null;
            }

            try
            {
                var selfOffsetParts = parts[20].Split('<', '>');
                var selfOffset = (Horizontal: int.TryParse(selfOffsetParts[0], out int horizontal) ? horizontal : 0,
                                    Vertical: int.TryParse(selfOffsetParts[1], out int vertical) ? vertical : 0);

                DeskMods deskMod;
                if (parts[8] == "chat")
                {
                    deskMod = DeskMods.Chat;
                }
                else
                {
                    deskMod = int.TryParse(parts[8], out int deskModifier) ? (DeskMods)deskModifier : DeskMods.Hidden;
                }

                return new ICMessage
                {
                    DeskMod = deskMod,
                    PreAnim = parts[2],
                    Character = parts[3],
                    Emote = parts[4],
                    Message = Globals.ReplaceTextForSymbols(parts[5]),
                    Side = parts[6],
                    SfxName = parts[7],
                    EmoteModifier = int.TryParse(parts[8], out int emoteModifier) ? (EmoteModifiers)emoteModifier : EmoteModifiers.NoPreanimation,
                    CharId = int.TryParse(parts[9], out int charId) ? charId : -1,
                    SfxDelay = int.TryParse(parts[10], out int sfxDelay) ? sfxDelay : 0,
                    ShoutModifier = int.TryParse(parts[11], out int shoutModifier) ? (ShoutModifiers)shoutModifier : ShoutModifiers.Nothing,
                    EvidenceID = parts[12],
                    Flip = parts[13] == "1",
                    Realization = parts[14] == "1",
                    TextColor = int.TryParse(parts[15], out int textColor) ? (TextColors)textColor : TextColors.White,
                    ShowName = string.IsNullOrEmpty(parts[16]) ? CharacterFolder.FullList.First(ini => ini.Name == parts[3]).configINI.ShowName : Globals.ReplaceTextForSymbols(parts[16]),
                    OtherCharId = int.TryParse(parts[17], out int otherCharId) ? otherCharId : -1,
                    OtherName = parts[18],
                    OtherEmote = parts[19],
                    SelfOffset = selfOffset,
                    OtherOffset = int.TryParse(parts[21], out int otherOffset) ? otherOffset : 0,
                    OtherFlip = parts[22] == "1",
                    NonInterruptingPreAnim = parts[23] == "1", // Changed to bool
                    SfxLooping = parts[24] == "1",
                    ScreenShake = parts[25] == "1",
                    FramesShake = parts[26],
                    FramesRealization = parts[27],
                    FramesSfx = parts[28],
                    Additive = parts[29] == "1",
                    EffectString = parts[30],
                    Blips = parts.Length > 31 ? parts[31].TrimEnd('%') : "",
                    OriginalCommand = message
                };
            }
            catch (Exception ex)
            {
                CustomConsole.WriteLine($"❌ Error parsing IC message: {ex.Message}");
                return null;
            }
        }

        public static string GetCommand(ICMessage message)
        {
            return $"MS#" +
                    $"{(message.DeskMod == DeskMods.Chat ? "chat" : ((int)message.DeskMod).ToString())}#" +
                    $"{message.PreAnim}#" +
                    $"{message.Character}#" +
                    $"{message.Emote}#" +
                    $"{Globals.ReplaceSymbolsForText(message.Message)}#" +
                    $"{message.Side}#" +
                    $"{message.SfxName}#" +
                    $"{(int)message.EmoteModifier}#" +
                    $"{message.CharId}#" +
                    $"{message.SfxDelay}#" +
                    $"{(int)message.ShoutModifier}#" +
                    $"{message.EvidenceID}#" +
                    $"{(message.Flip ? "1" : "0")}#" +
                    $"{(message.Realization ? "1" : "0")}#" +
                    $"{(int)message.TextColor}#" +
                    $"{Globals.ReplaceSymbolsForText(message.ShowName)}#" +
                    $"{message.OtherCharId}#" +
                    $"{message.SelfOffset.Horizontal}<and>{message.SelfOffset.Vertical}#" +
                    $"{(message.NonInterruptingPreAnim ? "1" : "0")}#" + // Changed to bool
                    $"{(message.SfxLooping ? "1" : "0")}#" +
                    $"{(message.ScreenShake ? "1" : "0")}#" +
                    $"{message.FramesShake}#" +
                    $"{message.FramesRealization}#" +
                    $"{message.FramesSfx}#" +
                    $"{(message.Additive ? "1" : "0")}#" +
                    $"{message.EffectString}#" +
                    $"{(string.IsNullOrEmpty(message.Blips) ? "%" : "#%")}";
        }

        public static Color GetColorFromTextColor(TextColors textColor)
        {
            return textColor switch
            {
                TextColors.White => Color.FromArgb(247, 247, 247),
                TextColors.Green => Color.FromArgb(0, 247, 0),
                TextColors.Red => Color.FromArgb(247, 0, 57),
                TextColors.Orange => Color.FromArgb(247, 115, 57),
                TextColors.Blue => Color.FromArgb(107, 198, 247),
                TextColors.Yellow => Color.FromArgb(247, 247, 0),
                TextColors.Magenta => Color.FromArgb(247, 115, 247),
                TextColors.Cyan => Color.FromArgb(128, 247, 247),
                TextColors.Gray => Color.FromArgb(160, 181, 205),
                _ => Color.FromArgb(247, 247, 247),
            };
        }
    }
}
