using OceanyaClient.Utilities;
using System.Configuration;
using System.Data;
using System.Windows;
using System.Windows.Media;

namespace OceanyaClient;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private List<string> loadingMessages = new List<string>
    {
        "Starting completely unnecessary loading...",
        "Pretending to fetch important data...",
        "Looking busy for absolutely no reason...",
        "Connecting to imaginary server...",
        "Loading things you probably won't notice...",
        "Making it look like I'm working hard...",
        "Finalizing stuff that doesn't exist...",
        "Optimizing the slowdowns...",
        "Still loading... Why are you even waiting?",
        "Synchronizing with absolutely nothing...",
        "Establishing useless connections...",
        "Putting in effort into fake progress bars...",
        "Just a few more pointless tasks...",
        "Almost there, promise (maybe)...",
        "Wrapping up pointless loading...",
        "Escaping from Scorpio2's basement...",
        "Loading complete! (Or is it?)",
        "You can stop waiting now...",
        "Waiting for Dredd's coffee...",
        "Loading the loading screen...",
        "Loading the loading screen's loading screen...",
        "Wait, did i forget 7...?",
        "Waiting for GM's countdown...",
        "Waiting for a funni face...",
        "Waiting for Oceanya MMO to come out...",
        "Waiting for Scorpio2 to pass out...",
        "Waiting for Dredd to get another coffee...",
        "Loading Subway Surfers..."
    };


    public App()
    {
        this.DispatcherUnhandledException += App_DispatcherUnhandledException;
    }


    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Simulate loading operation
        await FakeLoadingAsync();

        // After loading finishes, show your main window
        InitialConfigurationWindow mainWindow = new InitialConfigurationWindow();
        mainWindow.Show();
        mainWindow.Activate();
        mainWindow.Focus();
    }

    private readonly Random _rand = new();

    private async Task FakeLoadingAsync()
    {
        // Randomly choose how many messages you'll display
        int stepsCount = _rand.Next(2, 9);

        // Shuffle the list and take the desired number of messages
        var selectedMessages = loadingScreenShuffle(loadingMessages).Take(stepsCount).ToList();

        await LoadingScreenManager.ShowFormAsync(selectedMessages[0]);

        for (int i = 0; i < selectedMessages.Count; i++)
        {
            LoadingScreenManager.SetSubtitle(selectedMessages[i]);
            var curProgress = (double)(i + 1) / stepsCount;
            LoadingScreenManager.SetProgress(curProgress);

            await Task.Delay(_rand.Next(500, 1200));
        }

        AudioPlayer.PlayEmbeddedSound("Resources/BellDing.mp3", 0.25f);
        LoadingScreenManager.SetSubtitle("Loading complete!");
        LoadingScreenManager.SetProgress(1);
        await Task.Delay(600);
        await LoadingScreenManager.CloseFormAsync();
    }

    private List<string> loadingScreenShuffle(List<string> messages)
    {
        return messages.OrderBy(x => _rand.Next()).ToList();
    }

    private List<string> SelectRandomMessages(List<string> source, int take)
    {
        return source.OrderBy(_ => _rand.Next()).Take(take).ToList();
    }


    private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
        OceanyaMessageBox.Show(e.Exception.Message, "Unhandled Error", MessageBoxButton.OK, MessageBoxImage.Error);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        // Ensure the WaitForm UI thread is properly shut down
        WaitForm.ShutdownThread();

        base.OnExit(e);
    }
}

