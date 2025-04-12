using AOBot_Testing.Structures;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnitTests
{
    [TestFixture]
    public class INIParserTests
    {
        [OneTimeSetUp]
        public void GatherAllINI()
        {
            CharacterFolder.RefreshCharacterList();
        }

        [Test]
        public void Test_CharacterINILoading()
        {
            // Verify characters were loaded
            Assert.That(CharacterFolder.FullList, Is.Not.Empty, "Character list should not be empty");
            
            // Check if at least one known character exists (Franziska is used in other tests)
            var franziska = CharacterFolder.FullList.FirstOrDefault(c => c.Name == "Franziska");
            Assert.That(franziska, Is.Not.Null, "Franziska character should exist in the character list");
            
            // Verify configuration was loaded properly
            Assert.That(franziska.configINI, Is.Not.Null, "Character config should be loaded");
            Assert.That(franziska.configINI.Emotions, Is.Not.Empty, "Character emotions should be loaded");
        }
        
        [Test]
        public void Test_CharacterEmotesLoading()
        {
            // Get a character with known emotes
            var character = CharacterFolder.FullList.FirstOrDefault(c => c.Name == "Franziska");
            Assert.That(character, Is.Not.Null, "Test character should exist");
            
            // Verify emotes collection
            Assert.That(character.configINI.Emotions, Is.Not.Empty, "Character should have emotes");
            
            // Check emote properties
            var firstEmote = character.configINI.Emotions.Values.First();
            Assert.That(firstEmote.ID, Is.GreaterThan(0), "Emote ID should be greater than 0");
            Assert.That(firstEmote.DisplayID, Is.Not.Empty, "Emote DisplayID should not be empty");
        }
        
        [Test]
        public void Test_CharacterConfigProperties()
        {
            // Test several characters to verify basic INI properties
            foreach (var character in CharacterFolder.FullList.Take(5))
            {
                Assert.Multiple(() =>
                {
                    Assert.That(character.Name, Is.Not.Empty, "Character name should not be empty");
                    Assert.That(character.PathToConfigIni, Is.Not.Empty, "Character config path should not be empty");
                    Assert.That(character.DirectoryPath, Is.Not.Empty, "Character directory path should not be empty");
                    Assert.That(File.Exists(character.PathToConfigIni), Is.True, $"Character config file {character.PathToConfigIni} should exist");
                    Assert.That(character.configINI, Is.Not.Null, "Character config INI should be loaded");
                });
            }
        }
        
        [Test]
        public void Test_CharacterUpdate()
        {
            // Get a character to test update functionality
            var character = CharacterFolder.FullList.First();
            
            // Store original values
            var originalName = character.Name;
            var originalPath = character.PathToConfigIni;
            
            // Update the character (should reload from the same path)
            character.Update(character.PathToConfigIni, true);
            
            // Verify update didn't change core properties
            Assert.Multiple(() =>
            {
                Assert.That(character.Name, Is.EqualTo(originalName), "Character name should remain the same after update");
                Assert.That(character.PathToConfigIni, Is.EqualTo(originalPath), "Character path should remain the same after update");
                Assert.That(character.configINI, Is.Not.Null, "Character config should still be loaded after update");
            });
        }
    }
}
