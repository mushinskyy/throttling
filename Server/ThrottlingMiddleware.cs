using System.Collections.Concurrent;

public interface IThrottlingMiddleware
{
    public bool CheckAndIncrease(string key);
}

public class ThrottlingMiddleware : IThrottlingMiddleware
{
    private ConcurrentDictionary<string, ThrottlingWindow> throttlingWindowByKey;
    private ConcurrentDictionary<string, object> locksByKey;

    private int windowLengthMS;
    private int maximumCount;

    private Task memoryCleanerTask;
    private static int memoryCleanerIntervalMS = 5000;
    public ThrottlingMiddleware(CancellationToken token, int windowLengthMS, int maximumCount)
    {
        throttlingWindowByKey = new ConcurrentDictionary<string, ThrottlingWindow>();
        locksByKey = new ConcurrentDictionary<string, object>();
        this.windowLengthMS = windowLengthMS;
        this.maximumCount = maximumCount;

        this.memoryCleanerTask = Task.Run(() => CleanUnusedWindows(token, memoryCleanerIntervalMS));
    }

    public bool CheckAndIncrease(string key)
    {
        lock (GetOrCreateLock(key))
        {
            if (!throttlingWindowByKey.ContainsKey(key))
            {
                throttlingWindowByKey[key] = new ThrottlingWindow();
            }

            ThrottlingWindow throttlingWindow = throttlingWindowByKey[key];

            if (IsWindowExpired(throttlingWindow))
            {
                throttlingWindow.Reset();
                throttlingWindow.Increase();
                return true;
            }
            else
            {
                if (throttlingWindow.Counter <= maximumCount)
                {
                    throttlingWindow.Increase();
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }
    }

    private bool IsWindowExpired(ThrottlingWindow throttlingWindow)
    {
        return (DateTime.Now - throttlingWindow.startTime).TotalMilliseconds > windowLengthMS;
    }

    private async void CleanUnusedWindows(CancellationToken token, int interval)
    {
        var immutableWindowKeys = throttlingWindowByKey.Keys.ToList();

        foreach (string windowKey in immutableWindowKeys)
        {
            if (token.IsCancellationRequested) break;

            bool shouldDeleteLock = true;
            lock (GetOrCreateLock(windowKey))
            {
                try {
                    ThrottlingWindow throttlingWindow = throttlingWindowByKey[windowKey];
                    if (IsWindowExpired(throttlingWindow))
                    {
                        shouldDeleteLock &= throttlingWindowByKey.TryRemove(windowKey, out throttlingWindow);
                    }
                    else 
                    {
                        shouldDeleteLock = false;
                    }
                }
                catch (Exception ex)
                {
                    shouldDeleteLock = false;
                }
            }

            if (shouldDeleteLock)
            {
                locksByKey.Remove(windowKey, out var lockObject);
            }
        }

        Thread.Sleep(interval);

        if (!token.IsCancellationRequested)
        {
            this.memoryCleanerTask = Task.Run(() => CleanUnusedWindows(token, memoryCleanerIntervalMS));
        }
    }

    private object GetOrCreateLock(string key)
    {
        if (!locksByKey.ContainsKey(key)) locksByKey[key] = new object();
        return locksByKey[key];
    }
}