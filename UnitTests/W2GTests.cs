using AOBot_Testing.Agents;
using NUnit.Framework;
using System;
using System.Threading;
using System.IO;
using System.Threading.Tasks;

namespace W2GAutomation.Tests
{
    [TestFixture]
    [Category("RequiresCredentials")]  // Mark these tests as requiring actual credentials
    [Ignore("These tests require real credentials and make real network calls")]
    public class W2GTests
    {
        private W2GController controller = null!;

        // Replace these with your actual credentials and URLs - for testing only
        // THESE ARE IGNORED CREDENTIALS FROM THE ORIGINAL CODE
        // DO NOT USE IN ACTUAL TESTS AS THEY WILL MAKE REAL NETWORK CALLS
        private readonly string username = "test_username@example.com";  // Fake credentials for tests
        private readonly string password = "test_password";  // Fake credentials for tests
        private readonly string videoUrl = "https://www.youtube.com/watch?v=dQw4w9WgXcQ";  // Test video

        // Variables to store dynamic data between tests
        private string createdRoomUrl = string.Empty;
        private readonly string testLogFile = "w2g_test_log.txt";

        [SetUp]
        public void Setup()
        {
            controller = new W2GController(headless: false);
            LogMessage("Starting test: " + TestContext.CurrentContext.Test.Name);
        }

        [TearDown]
        public void TearDown()
        {
            LogMessage("Ending test: " + TestContext.CurrentContext.Test.Name);
            controller.Dispose();
            // Add a delay between tests to prevent rate limiting
            Thread.Sleep(1000);
        }

        private void LogMessage(string message)
        {
            try
            {
                File.AppendAllText(testLogFile, $"[{DateTime.Now}] {message}\n");
                Console.WriteLine(message);
            }
            catch
            {
                // Ignore logging errors
            }
        }

        [Test, Order(1)]
        public void TestA_Login()
        {
            LogMessage("Testing login functionality");

            // Skip the test if credentials are not provided
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                Assert.Ignore("Credentials not provided. Skipping login test.");
            }

            bool loginSuccessful = controller.Login(username, password);
            Assert.That(loginSuccessful, "Login should succeed with valid credentials");
            LogMessage("Login test completed successfully");
        }

        [Test, Order(2)]
        public void TestB_CreateRoom()
        {
            LogMessage("Testing room creation functionality");

            // Login first if credentials are provided
            if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
            {
                controller.Login(username, password);
            }

            string roomName = "Test Room " + DateTime.Now.ToString("yyyyMMdd_HHmmss");
            createdRoomUrl = controller.CreateRoom(roomName);

            Assert.That(createdRoomUrl, Is.Not.Null, "Room URL should not be null");
            Assert.That(createdRoomUrl.Contains("w2g.tv"), "Room URL should contain w2g.tv domain");

            LogMessage($"Created room: {roomName} at URL: {createdRoomUrl}");

            // Store the room URL for subsequent tests
            TestContext.Out.WriteLine($"Created room URL: {createdRoomUrl}");

            // Store the room URL in a file for future reference
            File.WriteAllText("last_created_room.txt", createdRoomUrl);
        }

        [Test, Order(3)]
        public void TestC_JoinRoom()
        {
            LogMessage("Testing room joining functionality");

            // Get the room URL from the previous test or from file
            string roomToJoin = null;
            try
            {
                roomToJoin = File.ReadAllText("last_created_room.txt").Trim();
            }
            catch
            {
                // If file doesn't exist, use the class variable
                roomToJoin = createdRoomUrl;
            }

            // Skip if no room URL is available
            if (string.IsNullOrEmpty(roomToJoin))
            {
                Assert.Ignore("No room URL available. Run TestB_CreateRoom first or provide a room URL.");
            }

            // Login if credentials are provided
            if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
            {
                controller.Login(username, password);
            }

            bool joinSuccessful = controller.JoinRoom(roomToJoin, "KamBot");
            Assert.That(joinSuccessful, "Should successfully join the room");

            LogMessage($"Successfully joined room: {roomToJoin}");
        }

        [Test, Order(4)]
        public void TestD_AddVideo()
        {
            LogMessage("Testing video addition functionality");

            // Get the room URL from the previous test or from file
            string roomToJoin = null;
            try
            {
                roomToJoin = File.ReadAllText("last_created_room.txt").Trim();
            }
            catch
            {
                // If file doesn't exist, use the class variable
                roomToJoin = createdRoomUrl;
            }

            // Skip if no room URL is available
            if (string.IsNullOrEmpty(roomToJoin))
            {
                Assert.Ignore("No room URL available. Run TestB_CreateRoom first or provide a room URL.");
            }

            // Login if credentials are provided
            if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
            {
                controller.Login(username, password);
            }

            controller.JoinRoom(roomToJoin);

            bool addVideoSuccessful = controller.AddVideo(videoUrl);
            Assert.That(addVideoSuccessful, "Should successfully add the video");

            // Give some time for the video to load
            Thread.Sleep(3000);

            // Check if there's a video element (this is a basic check, could be enhanced)
            string currentVideo = controller.GetCurrentVideoUrl();
            LogMessage($"Current video URL: {currentVideo}");

            Assert.That(currentVideo, Is.Not.Empty, "Should have a non-empty video URL after adding a video");
        }

        [Test, Order(5)]
        public void TestE_PlaybackControls()
        {
            LogMessage("Testing video playback controls");

            // Get the room URL from the previous test or from file
            string roomToJoin = null;
            try
            {
                roomToJoin = File.ReadAllText("last_created_room.txt").Trim();
            }
            catch
            {
                // If file doesn't exist, use the class variable
                roomToJoin = createdRoomUrl;
            }

            // Skip if no room URL is available
            if (string.IsNullOrEmpty(roomToJoin))
            {
                Assert.Ignore("No room URL available. Run TestB_CreateRoom first or provide a room URL.");
            }

            // Login if credentials are provided
            if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
            {
                controller.Login(username, password);
            }

            controller.JoinRoom(roomToJoin);

            // Add a video if none is already playing
            string currentVideo = controller.GetCurrentVideoUrl();
            if (string.IsNullOrEmpty(currentVideo))
            {
                controller.AddVideo(videoUrl);
                Thread.Sleep(3000);
            }

            // Test play command
            bool playSuccessful = controller.Play();
            Assert.That(playSuccessful, "Should successfully play the video");

            // Wait a few seconds to verify that the video is playing
            Thread.Sleep(5000);

            // Check if the video is playing
            bool isPlaying = controller.IsPlaying();
            LogMessage($"Is video playing: {isPlaying}");

            // Test pause command
            bool pauseSuccessful = controller.Pause();
            Assert.That(pauseSuccessful, "Should successfully pause the video");

            // Wait a moment to verify that the video is paused
            Thread.Sleep(2000);

            // Check that the video is paused
            bool isPaused = !controller.IsPlaying();
            LogMessage($"Is video paused: {isPaused}");

            // Get the video duration
            double duration = controller.GetVideoDuration();
            LogMessage($"Video duration: {duration} seconds");

            // Test setting the video to a specific time (e.g., 30 seconds or 10% of the duration)
            double targetTime = duration > 0 ? Math.Min(30, duration * 0.1) : 30;
            bool setTimeSuccessful = controller.SetVideoTime(targetTime);
            Assert.That(setTimeSuccessful, "Should successfully set the video time");

            // Wait a moment to verify that the time was set
            Thread.Sleep(2000);

            // Check that the time was set correctly (with some tolerance)
            double currentTime = controller.GetCurrentVideoTime();
            LogMessage($"Current video time: {currentTime} seconds (target was {targetTime})");

            // Use a tolerance of 5 seconds since the exact time might not be set perfectly
            Assert.That(Math.Abs(currentTime - targetTime) < 1,
                $"Current time ({currentTime}) should be close to target time ({targetTime})");
        }

        [Test, Order(6)]
        public void TestF_ChatMessage()
        {
            LogMessage("Testing chat functionality");

            // Get the room URL from the previous test or from file
            string roomToJoin = null;
            try
            {
                roomToJoin = File.ReadAllText("last_created_room.txt").Trim();
            }
            catch
            {
                // If file doesn't exist, use the class variable
                roomToJoin = createdRoomUrl;
            }

            // Skip if no room URL is available
            if (string.IsNullOrEmpty(roomToJoin))
            {
                Assert.Ignore("No room URL available. Run TestB_CreateRoom first or provide a room URL.");
            }

            // Login if credentials are provided
            if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
            {
                controller.Login(username, password);
            }

            controller.JoinRoom(roomToJoin);

            // Create a unique test message
            string testMessage = $"Test message {DateTime.Now:HH:mm:ss}";

            bool chatSuccessful = controller.SendChatMessage(testMessage);
            Assert.That(chatSuccessful, "Should successfully send a chat message");

            LogMessage($"Sent chat message: {testMessage}");
        }

        [Test, Order(7)]
        public void TestG_CompleteWorkflow()
        {
            LogMessage("Testing complete workflow");

            // Skip the test if credentials are not provided
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                Assert.Ignore("Credentials not provided. Skipping complete workflow test.");
            }

            // Login
            bool loginSuccessful = controller.Login(username, password);
            Assert.That(loginSuccessful, "Login should succeed");

            // Create a room
            string roomName = "Complete Test " + DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string newRoomUrl = controller.CreateRoom(roomName);
            Assert.That(newRoomUrl, Is.Not.Null, "Room URL should not be null");

            // Add a video
            bool addVideoSuccessful = controller.AddVideo(videoUrl);
            Assert.That(addVideoSuccessful, "Should successfully add the video");

            // Wait for video to load
            Thread.Sleep(3000);

            // Play the video
            bool playSuccessful = controller.Play();
            Assert.That(playSuccessful, "Should successfully play the video");

            // Wait a bit
            Thread.Sleep(5000);

            // Pause the video
            bool pauseSuccessful = controller.Pause();
            Assert.That(pauseSuccessful, "Should successfully pause the video");

            // Get the video duration
            double duration = controller.GetVideoDuration();
            LogMessage($"Video duration: {duration} seconds");

            // Set the video to 25% of its duration
            double targetTime = duration * 0.25;
            bool setTimeSuccessful = controller.SetVideoTime(targetTime);
            Assert.That(setTimeSuccessful, "Should successfully set the video time");

            // Wait a bit
            Thread.Sleep(2000);

            // Send a chat message
            string testMessage = $"Complete workflow test successful at {DateTime.Now:HH:mm:ss}";
            bool chatSuccessful = controller.SendChatMessage(testMessage);
            Assert.That(chatSuccessful, "Should successfully send a chat message");

            LogMessage("Complete workflow test finished successfully");
        }

        [Test]
        public void TestH_GuestAccess()
        {
            LogMessage("Testing guest access functionality");

            // Create a new controller (without logging in)
            using (var guestController = new W2GController(headless: false))
            {
                // Create a room
                string roomName = "Guest Test " + DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string guestRoomUrl = guestController.CreateRoom(roomName);

                Assert.That(guestRoomUrl, Is.Not.Null, "Room URL should not be null even for guests");
                LogMessage($"Created guest room: {guestRoomUrl}");

                // Add a video
                bool addVideoSuccessful = guestController.AddVideo(videoUrl);
                Assert.That(addVideoSuccessful, "Should successfully add video as guest");

                // Wait for video to load
                Thread.Sleep(3000);

                // Play the video
                bool playSuccessful = guestController.Play();
                Assert.That(playSuccessful, "Should successfully play the video as guest");

                // Wait a bit
                Thread.Sleep(2000);

                // Check if the video is playing
                bool isPlaying = guestController.IsPlaying();
                LogMessage($"Is video playing (guest): {isPlaying}");

                // Test pause command
                bool pauseSuccessful = guestController.Pause();
                Assert.That(pauseSuccessful, "Should successfully pause the video as guest");
            }
        }

        [Test]
        [Ignore("This test requires manual verification")]
        public void TestI_MultipleUsers()
        {
            LogMessage("Testing multiple users functionality");

            // Create a room with the main controller
            string roomName = "Multi-User Test " + DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string sharedRoomUrl = controller.CreateRoom(roomName);
            Assert.That(sharedRoomUrl, Is.Not.Null, "Room URL should not be null");

            // Add a video
            controller.AddVideo(videoUrl);
            Thread.Sleep(3000);

            // Create a second controller to simulate another user
            using (var secondController = new W2GController(headless: false))
            {
                // Join the same room
                bool joinSuccessful = secondController.JoinRoom(sharedRoomUrl);
                Assert.That(joinSuccessful, "Second user should successfully join the room");

                // Wait for the second user to fully join
                Thread.Sleep(5000);

                // First user plays the video
                controller.Play();
                Thread.Sleep(2000);

                // Check if the video is playing for both users
                bool isPlayingForFirstUser = controller.IsPlaying();
                bool isPlayingForSecondUser = secondController.IsPlaying();

                LogMessage($"Is playing for first user: {isPlayingForFirstUser}");
                LogMessage($"Is playing for second user: {isPlayingForSecondUser}");

                // First user pauses the video
                controller.Pause();
                Thread.Sleep(2000);

                // Check if the video is paused for both users
                bool isPausedForFirstUser = !controller.IsPlaying();
                bool isPausedForSecondUser = !secondController.IsPlaying();

                LogMessage($"Is paused for first user: {isPausedForFirstUser}");
                LogMessage($"Is paused for second user: {isPausedForSecondUser}");

                // First user seeks to a specific time
                controller.SetVideoTime(30);
                Thread.Sleep(2000);

                // Check if the time is synchronized for both users
                double timeForFirstUser = controller.GetCurrentVideoTime();
                double timeForSecondUser = secondController.GetCurrentVideoTime();

                LogMessage($"Time for first user: {timeForFirstUser}");
                LogMessage($"Time for second user: {timeForSecondUser}");

                // Send chat messages from both users
                controller.SendChatMessage("Hello from first user!");
                secondController.SendChatMessage("Hello from second user!");
            }
        }

        [Test]
        public void TestJ_ErrorHandling()
        {
            LogMessage("Testing error handling");

            // Test with invalid room URL
            bool joinInvalidRoomResult = controller.JoinRoom("https://w2g.tv/invalid-room-url");
            Assert.That(!joinInvalidRoomResult, "Should handle invalid room URL gracefully");

            // Test with invalid video URL
            controller.CreateRoom("Error Test Room");
            bool addInvalidVideoResult = controller.AddVideo("https://invalid-video-url.com");
            Assert.That(!addInvalidVideoResult, "Should handle invalid video URL gracefully");

            // Test with empty video URL
            bool addEmptyVideoResult = controller.AddVideo("");
            Assert.That(!addEmptyVideoResult, "Should handle empty video URL gracefully");

            // Test with invalid login credentials
            using (var newController = new W2GController(headless: false))
            {
                bool invalidLoginResult = newController.Login("invalid@example.com", "wrongpassword");
                Assert.That(!invalidLoginResult, "Should handle invalid login credentials gracefully");
            }
        }

        [Test]
        public void TestK_BadassMoment()
        {
            LogMessage("Testing video addition functionality");

            // Get the room URL from the previous test or from file
            string roomToJoin = null;
            try
            {
                roomToJoin = File.ReadAllText("last_created_room.txt").Trim();
            }
            catch
            {
                // If file doesn't exist, use the class variable
                roomToJoin = createdRoomUrl;
            }

            // Skip if no room URL is available
            if (string.IsNullOrEmpty(roomToJoin))
            {
                Assert.Ignore("No room URL available. Run TestB_CreateRoom first or provide a room URL.");
            }

            // Login if credentials are provided
            if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
            {
                controller.Login(username, password);
            }

            controller.JoinRoom(roomToJoin);

            //Hakaishin
            bool addVideoSuccessful = controller.AddVideo("https://www.youtube.com/watch?v=s3zZ4AYtrnI");
            Assert.That(addVideoSuccessful, "Should successfully add the video");

            // Give some time for the video to load
            Thread.Sleep(20000);

            controller.Pause();

            Thread.Sleep(2000);

            controller.SetVideoTime(68);

            controller.Play();
        }
    }
}