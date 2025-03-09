using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

public class CountdownTimer
{
    private Stopwatch stopwatch;
    private TimeSpan duration;
    private CancellationTokenSource cancellationTokenSource;

    public event Action TimerElapsed;

    public CountdownTimer(TimeSpan duration)
    {
        this.duration = duration;
        stopwatch = new Stopwatch();
    }

    public void Start()
    {
        if (stopwatch.IsRunning)
            return; // Avoid restarting an already running timer

        stopwatch.Restart();
        cancellationTokenSource = new CancellationTokenSource();
        Task.Run(async () =>
        {
            while (stopwatch.Elapsed < duration)
            {
                await Task.Delay(100);
                if (cancellationTokenSource.Token.IsCancellationRequested)
                    return;
            }
            TimerElapsed?.Invoke();
            stopwatch.Stop();
        }, cancellationTokenSource.Token);
    }

    public void Stop()
    {
        cancellationTokenSource?.Cancel();
        stopwatch.Stop();
    }

    public TimeSpan GetRemainingTime()
    {
        var remainingTime = duration - stopwatch.Elapsed;
        return remainingTime < TimeSpan.Zero ? TimeSpan.Zero : remainingTime;
    }


    public bool IsRunning => stopwatch.IsRunning;
}
