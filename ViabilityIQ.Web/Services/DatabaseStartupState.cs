namespace ViabilityIQ.Web.Services;

public enum DatabaseStartupStatus
{
    Initializing,
    Retrying,
    Ready,
    Failed
}

public sealed class DatabaseStartupState
{
    private readonly object syncRoot = new();
    private DatabaseStartupStatus status = DatabaseStartupStatus.Initializing;
    private int attempt;
    private int maxAttempts;
    private string? errorReference;

    public DatabaseStartupStatus Status
    {
        get
        {
            lock (syncRoot)
                return status;
        }
    }

    public int Attempt
    {
        get
        {
            lock (syncRoot)
                return attempt;
        }
    }

    public int MaxAttempts
    {
        get
        {
            lock (syncRoot)
                return maxAttempts;
        }
    }

    public string? ErrorReference
    {
        get
        {
            lock (syncRoot)
                return errorReference;
        }
    }

    public void MarkInitializing(int currentAttempt, int configuredMaxAttempts)
    {
        lock (syncRoot)
        {
            status = DatabaseStartupStatus.Initializing;
            attempt = currentAttempt;
            maxAttempts = configuredMaxAttempts;
            errorReference = null;
        }
    }

    public void MarkRetrying(int currentAttempt, int configuredMaxAttempts)
    {
        lock (syncRoot)
        {
            status = DatabaseStartupStatus.Retrying;
            attempt = currentAttempt;
            maxAttempts = configuredMaxAttempts;
        }
    }

    public void MarkReady()
    {
        lock (syncRoot)
        {
            status = DatabaseStartupStatus.Ready;
            errorReference = null;
        }
    }

    public string MarkFailed()
    {
        lock (syncRoot)
        {
            status = DatabaseStartupStatus.Failed;
            errorReference = Guid.NewGuid().ToString("N");
            return errorReference;
        }
    }
}
