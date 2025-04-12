using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit;
using NUnit.Framework;
using AOBot_Testing.Structures;
using AOBot_Testing.Agents;
using static AOBot_Testing.Structures.ICMessage;
using Common;

namespace AOBot_Testing.Tests
{
    [TestFixture]
    [Category("RequiresConnection")]
    [Ignore("These tests require an actual connection to an AO2 server")]
    public class BotTests
    {
        AOClient? testingBot = null;
        bool listeningForConfirmation = false;
        bool receivedMessage = false;
        string message = "";

        [OneTimeSetUp]
        public void Setup()
        {
            testingBot = new AOClient(Globals.IPs[Globals.Servers.ChillAndDices], "Basement/testing");
            testingBot.Connect().Wait();
            testingBot.SetCharacter("Franziska");

            testingBot.OnMessageReceived += (string chatLogType, string characterName, string showName, string message, int iniPuppetID) =>
            {
                if (!listeningForConfirmation)
                {
                    return;
                }

                switch (chatLogType)
                {
                    case "IC":
                        if (showName == testingBot.ICShowname && characterName == testingBot.currentINI.Name && iniPuppetID == testingBot.iniPuppetID)
                        {
                            return;
                        }
                        break;
                    case "OOC":
                        if (showName == testingBot.OOCShowname)
                        {
                            return;
                        }
                        break;
                }

                receivedMessage = true;
                this.message = message;
            };
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            testingBot.Disconnect().Wait();
        }

        [Test]
        public async Task TestA_NormalMessages()
        {
            await testingBot.SendICMessage("Testing IC Message");

            await CheckUserInput();

            await testingBot.SendOOCMessage("Testing OOC Message");

            await CheckUserInput();

            await Task.Delay(2000);
        }

        [Test]
        public async Task TestB_Showname()
        {
            await testingBot.SendICMessage("TestShownameIC", "Testing Showname: This should be TestShownameIC");

            await CheckUserInput();

            await testingBot.SendOOCMessage("TestShownameOOC", "Testing Showname: This should be TestShownameOOC");

            await CheckUserInput();

            await Task.Delay(2000);
        }

        [Test]
        public async Task TestC_DeskMod()
        {
            var deskModValues = Enum.GetValues(typeof(ICMessage.DeskMods)).Cast<ICMessage.DeskMods>().ToList();
            foreach (var deskMod in deskModValues)
            {
                testingBot.deskMod = deskMod;

                await testingBot.SendICMessage($"Testing DeskMod: {deskMod.ToString()}");

                await CheckUserInput();
            }

            await Task.Delay(2000);
        }

        [Test]
        public async Task TestD_EmoteMod()
        {
            var emoteModValues = Enum.GetValues(typeof(ICMessage.EmoteModifiers)).Cast<ICMessage.EmoteModifiers>().ToList();
            foreach (var emoteMod in emoteModValues)
            {
                testingBot.emoteMod = emoteMod;

                await testingBot.SendICMessage($"Testing EmoteMod: {emoteMod.ToString()}");

                await CheckUserInput();
            }

            await Task.Delay(2000);
        }

        [Test]
        public async Task TestE_ShoutModifiers()
        {
            var shoutModifiersValues = Enum.GetValues(typeof(ICMessage.ShoutModifiers)).Cast<ICMessage.ShoutModifiers>().ToList();
            foreach (var shoutModifier in shoutModifiersValues)
            {
                testingBot.shoutModifiers = shoutModifier;

                await testingBot.SendICMessage($"Testing ShoutModifier: {shoutModifier.ToString()}");

                await CheckUserInput();
            }

            await Task.Delay(2000);
        }

        [Test]
        public async Task TestF_Flip()
        {
            testingBot.flip = true;
            await testingBot.SendICMessage("Testing Flip: true");

            await CheckUserInput();

            testingBot.flip = false;
            await testingBot.SendICMessage("Testing Flip: false");

            await CheckUserInput();

            await Task.Delay(2000);
        }

        [Test]
        public async Task TestG_Effects()
        {
            var effectValues = Enum.GetValues(typeof(ICMessage.Effects)).Cast<ICMessage.Effects>().ToList();
            foreach (var effect in effectValues)
            {
                testingBot.effect = effect;

                await testingBot.SendICMessage($"Testing Effect: {effect.ToString()}");

                await CheckUserInput();
            }

            await Task.Delay(2000);
        }

        [Test]
        public async Task TestH_TextColor()
        {
            var textColorValues = Enum.GetValues(typeof(ICMessage.TextColors)).Cast<ICMessage.TextColors>().ToList();
            foreach (var textColor in textColorValues)
            {
                testingBot.textColor = textColor;

                await testingBot.SendICMessage($"Testing TextColor: {textColor.ToString()}");

                await CheckUserInput();
            }

            await Task.Delay(2000);
        }

        [Test]
        public async Task TestI_Immediate()
        {
            testingBot.Immediate = false;
            await testingBot.SendICMessage("Testing Immediate: false");

            await CheckUserInput();

            testingBot.Immediate = true;
            await testingBot.SendICMessage("Testing Immediate: true");

            await CheckUserInput();

            await Task.Delay(2000);
        }

        [Test]
        public async Task TestJ_Additive()
        {
            testingBot.Additive = false;
            await testingBot.SendICMessage("Testing Additive: false");

            //await CheckUserInput();

            testingBot.Additive = true;
            await testingBot.SendICMessage("Testing Additive: true");

            await CheckUserInput();

            await Task.Delay(2000);
        }

        [Test]
        public async Task TestK_INITesting()
        {
            foreach (var ini in new List<string> { "Franziska", "KamLoremaster" })
            {
                testingBot.SetCharacter(ini);

                foreach (var kvp in testingBot.currentINI.configINI.Emotions)
                {
                    var emote = kvp.Value;

                    testingBot.SetEmote(emote.DisplayID);

                    await testingBot.SendICMessage($"Testing Emote: {ini} - {emote.DisplayID}");

                    await CheckUserInput();
                }
            }
            

            await Task.Delay(2000);
        }


        private void Timer_TimerElapsed()
        {
            throw new NotImplementedException();
        }

        [TearDown]
        public void ResetVariables()
        {
            testingBot.OOCShowname = "OceanyaBot";
            testingBot.ICShowname = "";
            testingBot.deskMod = ICMessage.DeskMods.Chat;
            testingBot.emoteMod = ICMessage.EmoteModifiers.NoPreanimation;
            testingBot.shoutModifiers = ICMessage.ShoutModifiers.Nothing;
            testingBot.flip = false;
            testingBot.effect = ICMessage.Effects.None;
            testingBot.textColor = ICMessage.TextColors.White;
            testingBot.Immediate = false;
            testingBot.Additive = false;
        }

        public async Task CheckUserInput()
        {
            #region Wait for message
            listeningForConfirmation = true;
            receivedMessage = false;

            while (!receivedMessage)
            {
                await Task.Delay(100);
            }
            receivedMessage = false;
            #endregion

            if (message.ToLower().Trim() != "pass")
            {
                Assert.Fail(message);
            }
        }
    }
}