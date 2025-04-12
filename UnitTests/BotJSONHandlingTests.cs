using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using AOBot_Testing;
using AOBot_Testing.Structures;
using NUnit.Framework;

namespace UnitTests
{
    [TestFixture]
    public class BotJSONHandlingTests
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

        [Test]
        public void Test_PlayerListParsing()
        {
            // Sample OOC message containing player list
            string playerListMessage = @"People in this area: 
=================
[CM] [1] Phoenix (JusticeForAll)
[2] Franziska (WhipUser)
[3] Miles (Prosecutor)
=================";

            // Parse the player list
            List<Player> players = AO2Parser.ParseGetArea(playerListMessage);

            // Verify parsing worked correctly
            Assert.That(players, Has.Count.EqualTo(3), "Should parse 3 players");

            // Check first player (CM)
            Assert.Multiple(() =>
            {
                Assert.That(players[0].IsCM, Is.True, "First player should be a CM");
                Assert.That(players[0].PlayerID, Is.EqualTo(1), "First player ID should be 1");
                Assert.That(players[0].ICCharacterName, Is.EqualTo("Phoenix"), "First player character should be Phoenix");
                Assert.That(players[0].OOCShowname, Is.EqualTo("JusticeForAll"), "First player showname should be JusticeForAll");
            });

            // Check second player (normal)
            Assert.Multiple(() =>
            {
                Assert.That(players[1].IsCM, Is.False, "Second player should not be a CM");
                Assert.That(players[1].PlayerID, Is.EqualTo(2), "Second player ID should be 2");
                Assert.That(players[1].ICCharacterName, Is.EqualTo("Franziska"), "Second player character should be Franziska");
                Assert.That(players[1].OOCShowname, Is.EqualTo("WhipUser"), "Second player showname should be WhipUser");
            });
        }

        [Test]
        public void Test_PlayerListParsing_WithoutShownames()
        {
            // Sample OOC message containing player list without shownames
            string playerListMessage = @"People in this area: 
=================
[1] Phoenix
[2] Franziska
[3] Miles
=================";

            // Parse the player list
            List<Player> players = AO2Parser.ParseGetArea(playerListMessage);

            // Verify parsing worked correctly
            Assert.That(players, Has.Count.EqualTo(3), "Should parse 3 players");

            // Check players
            Assert.Multiple(() =>
            {
                Assert.That(players[0].ICCharacterName, Is.EqualTo("Phoenix"), "First player character should be Phoenix");
                Assert.That(players[0].OOCShowname, Is.Null, "First player showname should be null");
                
                Assert.That(players[1].ICCharacterName, Is.EqualTo("Franziska"), "Second player character should be Franziska");
                Assert.That(players[1].OOCShowname, Is.Null, "Second player showname should be null");
            });
        }

        [Test]
        public void Test_PlayerListParsing_EmptyList()
        {
            // Sample OOC message containing empty player list
            string playerListMessage = @"People in this area: 
=================
=================";

            // Parse the player list
            List<Player> players = AO2Parser.ParseGetArea(playerListMessage);

            // Verify parsing worked correctly
            Assert.That(players, Is.Empty, "Player list should be empty");
        }

        [Test]
        public void Test_PlayerListParsing_InvalidFormat()
        {
            // Sample message with invalid format
            string invalidMessage = "This is not a player list message";

            // Parse the invalid message
            List<Player> players = AO2Parser.ParseGetArea(invalidMessage);

            // Verify parsing doesn't crash and returns empty list
            Assert.That(players, Is.Empty, "Invalid message should return empty player list");
        }

        [Test]
        public void Test_JSON_Serialization()
        {
            // Create sample JSON with default values
            string json = string.Format(baseJson, 
                message, chatlog, showname, currentCharacter, 
                deskMod, emoteMod, shoutModifiers, flip, 
                realization, textColor, immediate, additive);
            
            // Verify the JSON is valid
            var exception = Assert.Catch<Exception>(() => 
            {
                JsonDocument.Parse(json);
            });
            
            Assert.That(exception, Is.Null, "JSON should be valid and parseable");
        }

        [Test]
        public void Test_JSON_WithSpecialCharacters()
        {
            // Create sample JSON with special characters that need escaping
            string specialMessage = "Test \"quoted\" message with \\ backslash";
            
            string json = string.Format(baseJson, 
                specialMessage, chatlog, showname, currentCharacter, 
                deskMod, emoteMod, shoutModifiers, flip, 
                realization, textColor, immediate, additive);
            
            // Verify the JSON is valid
            var exception = Assert.Catch<Exception>(() => 
            {
                JsonDocument.Parse(json);
            });
            
            Assert.That(exception, Is.Null, "JSON with special characters should be valid");
        }
    }
}
