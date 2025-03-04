using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit;
using NUnit.Framework;
using AOBot_Testing.Structures;
using AOBot_Testing.Agents;

namespace AOBot_Testing.Tests
{
    [TestFixture]
    public class BotTests
    {
        AOBot? testingBot = null;
        bool listeningForConfirmation = false;
        bool receivedMessage = false;
        string message = "";

        [OneTimeSetUp]
        public void Setup()
        {
            testingBot = new AOBot(Globals.IPs[Globals.Servers.ChillAndDices], "Basement/testing");
            testingBot.Connect().Wait();

            testingBot.OnMessageReceived += (string chatLogType, string characterName, string showName, string message, int iniPuppetID) =>
            {
                if(!listeningForConfirmation)
                {
                    return;
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
        public async Task TestNormalMessages()
        {
            await testingBot.SendICMessage("Testing IC Message");

            await CheckUserInput();

            await testingBot.SendOOCMessage("Testing OOC Message");

            await CheckUserInput();
        }

        [Test]
        public async Task TestShowname()
        {
            await testingBot.SendICMessage("TestShownameIC", "Testing Showname: This should be TestShownameIC");

            await CheckUserInput();

            await testingBot.SendOOCMessage("TestShownameOOC", "Testing Showname: This should be TestShownameIC");

            await CheckUserInput();
        }

        [Test]
        public async Task TestDeskMod()
        {
            var deskModValues = Enum.GetValues(typeof(ICMessage.DeskMods)).Cast<ICMessage.DeskMods>().ToList();
            foreach (var deskMod in deskModValues)
            {
                testingBot.deskMod = deskMod;

                await testingBot.SendICMessage($"Testing DeskMod: {deskMod.ToString()}");

                await CheckUserInput();
            }
        }

        [Test]
        public async Task TestEmoteMod()
        {
            var emoteModValues = Enum.GetValues(typeof(ICMessage.EmoteModifiers)).Cast<ICMessage.EmoteModifiers>().ToList();
            foreach (var emoteMod in emoteModValues)
            {
                testingBot.emoteMod = emoteMod;

                await testingBot.SendICMessage($"Testing EmoteMod: {emoteMod.ToString()}");

                await CheckUserInput();
            }
        }

        [Test]
        public async Task TestShoutModifiers()
        {
            var shoutModifiersValues = Enum.GetValues(typeof(ICMessage.ShoutModifiers)).Cast<ICMessage.ShoutModifiers>().ToList();
            foreach (var shoutModifier in shoutModifiersValues)
            {
                testingBot.shoutModifiers = shoutModifier;

                await testingBot.SendICMessage($"Testing ShoutModifier: {shoutModifier.ToString()}");

                await CheckUserInput();
            }
        }

        [Test]
        public async Task TestFlip()
        {
            testingBot.flip = true;
            await testingBot.SendICMessage("Testing Flip: true");

            await CheckUserInput();

            testingBot.flip = false;
            await testingBot.SendICMessage("Testing Flip: false");

            await CheckUserInput();
        }

        [Test]
        public async Task TestRealization()
        {
            testingBot.realization = true;
            await testingBot.SendICMessage("Testing Realization: true");

            await CheckUserInput();

            testingBot.realization = false;
            await testingBot.SendICMessage("Testing Realization: false");

            await CheckUserInput();
        }

        [Test]
        public async Task TestTextColor()
        {
            var textColorValues = Enum.GetValues(typeof(ICMessage.TextColors)).Cast<ICMessage.TextColors>().ToList();
            foreach (var textColor in textColorValues)
            {
                testingBot.textColor = textColor;

                await testingBot.SendICMessage($"Testing TextColor: {textColor.ToString()}");

                await CheckUserInput();
            }
        }

        [Test]
        public async Task TestImmediate()
        {
            testingBot.Immediate = false;
            await testingBot.SendICMessage("Testing Immediate: false");

            await CheckUserInput();

            testingBot.Immediate = true;
            await testingBot.SendICMessage("Testing Immediate: true");

            await CheckUserInput();
        }

        [Test]
        public async Task TestAdditive()
        {
            testingBot.Additive = false;
            await testingBot.SendICMessage("Testing Additive: false");

            //await CheckUserInput();

            testingBot.Additive = true;
            await testingBot.SendICMessage("Testing Additive: true");

            await CheckUserInput();
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
            testingBot.realization = false;
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
