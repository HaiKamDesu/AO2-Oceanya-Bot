using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnitTests
{
    class BotJSONHandlingTests
    {
        private static readonly string baseJson = @"{
            ""message"": ""{0}"",
            ""chatlog"": ""{1}"",
            ""showname"": ""{2}"",
            ""current_character"": ""{3}"",
            ""modifiers"": {
                ""deskMod"": {4},
                ""emoteMod"": {5},
                ""shoutModifiers"": {6},
                ""flip"": {7},
                ""realization"": {8},
                ""textColor"": {9},
                ""immediate"": {10},
                ""additive"": {11}
            }
        }";

        #region defaults
        string message = "Test Message";
        string chatlog = "IC";
        string showname = "TestShowname";
        string currentCharacter = "TestCharacter";
        int deskMod = 0;
        int emoteMod = 0;
        int shoutModifiers = 0;
        int flip = 0;
        int realization = 0;
        int textColor = 0;
        int immediate = 0;
        int additive = 0;
        #endregion
    }
}
