using Microsoft.Data.SqlClient;
using System.ComponentModel;

namespace ViabilityIQ.Infrastructure.DbFactory;

public static class DatabaseExceptionClassifier
{
    private static readonly HashSet<int> TransientSqlErrorNumbers =
    [
        -2,
        2,
        20,
        40,
        53,
        64,
        233,
        258,
        1205,
        4060,
        10928,
        10929,
        40197,
        40501,
        40613,
        49918,
        49919,
        49920,
        10053,
        10054,
        10060
    ];

    public static bool IsTimeout(Exception exception) =>
        Enumerate(exception).Any(current =>
            current is TimeoutException
            || current is SqlException sqlException
                && sqlException.Errors.Cast<SqlError>().Any(error => error.Number == -2)
            || current is Win32Exception win32Exception
                && win32Exception.NativeErrorCode == 258);

    public static bool IsTransient(Exception exception) =>
        IsTimeout(exception)
        || Enumerate(exception).Any(current =>
            current is SqlException sqlException
            && sqlException.Errors.Cast<SqlError>()
                .Any(error => TransientSqlErrorNumbers.Contains(error.Number)));

    public static bool IsDatabaseException(Exception exception) =>
        Enumerate(exception).Any(current =>
            current is SqlException
            || current is System.Data.DataException);

    private static IEnumerable<Exception> Enumerate(Exception exception)
    {
        var pending = new Queue<Exception>();
        pending.Enqueue(exception);

        while (pending.Count > 0)
        {
            var current = pending.Dequeue();
            yield return current;

            if (current is AggregateException aggregateException)
            {
                foreach (var innerException in aggregateException.InnerExceptions)
                    pending.Enqueue(innerException);
            }
            else if (current.InnerException is not null)
            {
                pending.Enqueue(current.InnerException);
            }
        }
    }
}
