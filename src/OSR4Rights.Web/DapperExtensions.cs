using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Polly;
using Dapper;
using Polly.Contrib.WaitAndRetry;
using Serilog;

namespace OSR4Rights.Web
{
    // from https://hyr.mn/dapper-and-polly/
    // and https://hyr.mn/Polly-wait-and-retry

    public static class DapperExtensions
    {
        public static async Task<IEnumerable<T>> QueryAsyncWithRetry<T>(this IDbConnection cnn, string sql, object param = null!)
        {
            var maxDelay = TimeSpan.FromSeconds(45);

            var delay = Backoff.DecorrelatedJitterBackoffV2(medianFirstRetryDelay: TimeSpan.FromSeconds(1), retryCount: 50)
                .Select(s => TimeSpan.FromTicks(Math.Min(s.Ticks, maxDelay.Ticks)));

            var retryPolicy = Policy
                .Handle<SqlException>(SqlServerTransientExceptionDetector.ShouldRetryOn)
                .Or<TimeoutException>()
                .OrInner<Win32Exception>(SqlServerTransientExceptionDetector.ShouldRetryOn)
                .WaitAndRetryAsync(delay, // notice Async here
                    (exception, timeSpan, retryCount) =>
                    {
                        Log.Warning(
                            exception,
                            $"QueryAsyncWithRetry: Error talking to Db, will retry after {timeSpan}. Retry attempt {retryCount}"
                        );
                    });

            return await retryPolicy.ExecuteAsync(async () => await cnn.QueryAsync<T>(sql, param));
        }

        public static async Task<int> ExecuteAsyncWithRetry(this IDbConnection cnn, string sql, object param = null!)
        {
            var maxDelay = TimeSpan.FromSeconds(45);

            var delay = Backoff.DecorrelatedJitterBackoffV2(medianFirstRetryDelay: TimeSpan.FromSeconds(1), retryCount: 50)
                .Select(s => TimeSpan.FromTicks(Math.Min(s.Ticks, maxDelay.Ticks)));

            var retryPolicy = Policy
                .Handle<SqlException>(SqlServerTransientExceptionDetector.ShouldRetryOn)
                .Or<TimeoutException>()
                .OrInner<Win32Exception>(SqlServerTransientExceptionDetector.ShouldRetryOn)
                .WaitAndRetryAsync(delay,
                    (exception, timeSpan, retryCount) =>
                    {
                        Log.Warning(
                            exception,
                            $"ExecuteAsyncWithRetry: Error talking to Db, will retry after {timeSpan}. Retry attempt {retryCount}"
                        );
                    });

            // have tweeted Ben about this.
            // https://gist.github.com/hyrmn/ce124e9b1f50dbf9d241390ebc8f6df3
            //return await retryPolicy.ExecuteAsync(async () => await cnn.ExecuteAsync(sql, param));
            return await retryPolicy.ExecuteAsync(() => cnn.ExecuteAsync(sql, param));
        }

        // Non async 
        public static int ExecuteWithRetry(this IDbConnection cnn, string sql, object param = null!)
        {
            // https://github.com/Polly-Contrib/Polly.Contrib.WaitAndRetry/blob/master/README.md#using-the-new-jitter-formula-with-a-large-number-of-retries
            // ceiling on the max delay
            var maxDelay = TimeSpan.FromSeconds(45);

            // retry up to 50 times with a max delay of 45 seconds
            var delay = Backoff.DecorrelatedJitterBackoffV2(medianFirstRetryDelay: TimeSpan.FromSeconds(1), retryCount: 50)
                .Select(s => TimeSpan.FromTicks(Math.Min(s.Ticks, maxDelay.Ticks)));

            var retryPolicy = Policy
                .Handle<SqlException>(SqlServerTransientExceptionDetector.ShouldRetryOn) // only transient SQL Exceptions which should be retried
                                                                                         //.Handle<SqlException>() // handle all SQL Exceptions
                .Or<TimeoutException>() // any sort of timeout exception should retry
                .OrInner<Win32Exception>(SqlServerTransientExceptionDetector.ShouldRetryOn) // 2 more transient exceptions which MSSQL can throw
                .WaitAndRetry(delay,
                    (exception, timeSpan, retryCount) =>
                    {
                        Log.Warning(
                            exception,
                            $"ExecuteWithRetry - Error talking to Db, will retry after {timeSpan}. Retry attempt {retryCount} "
                        );
                    });

            return retryPolicy.Execute(() => cnn.Execute(sql, param));
        }

    }

    // This file comes from:
    // https://raw.githubusercontent.com/aspnet/EntityFrameworkCore/master/src/EFCore.SqlServer/Storage/Internal/SqlServerTransientExceptionDetector.cs
    // and 
    // https://github.com/Azure/elastic-db-tools/blob/master/Src/ElasticScale.Client/ElasticScale.Common/TransientFaultHandling/Implementation/SqlDatabaseTransientErrorDetectionStrategy.cs
    // With the addition of
    // SQL Error 11001 (connection failed)

    /// <summary>
    ///     Detects the exceptions caused by SQL Server transient failures.
    /// </summary>
    public static class SqlServerTransientExceptionDetector
    {
        public static bool ShouldRetryOn(SqlException ex)
        {
            foreach (SqlError err in ex.Errors)
            {
                Log.Warning($" ** {err.Number}");
                switch (err.Number)
                {
                    // SQL Error Code: 49920
                    // Cannot process request. Too many operations in progress for subscription "%ld".
                    // The service is busy processing multiple requests for this subscription.
                    // Requests are currently blocked for resource optimization. Query sys.dm_operation_status for operation status.
                    // Wait until pending requests are complete or delete one of your pending requests and retry your request later.
                    case 49920:
                    // SQL Error Code: 49919
                    // Cannot process create or update request. Too many create or update operations in progress for subscription "%ld".
                    // The service is busy processing multiple create or update requests for your subscription or server.
                    // Requests are currently blocked for resource optimization. Query sys.dm_operation_status for pending operations.
                    // Wait till pending create or update requests are complete or delete one of your pending requests and
                    // retry your request later.
                    case 49919:
                    // SQL Error Code: 49918
                    // Cannot process request. Not enough resources to process request.
                    // The service is currently busy.Please retry the request later.
                    case 49918:
                    // SQL Error Code: 41839
                    // Transaction exceeded the maximum number of commit dependencies.
                    case 41839:
                    // SQL Error Code: 41325
                    // The current transaction failed to commit due to a serializable validation failure.
                    case 41325:
                    // SQL Error Code: 41305
                    // The current transaction failed to commit due to a repeatable read validation failure.
                    case 41305:
                    // SQL Error Code: 41302
                    // The current transaction attempted to update a record that has been updated since the transaction started.
                    case 41302:
                    // SQL Error Code: 41301
                    // Dependency failure: a dependency was taken on another transaction that later failed to commit.
                    case 41301:
                    // SQL Error Code: 40613
                    // Database XXXX on server YYYY is not currently available. Please retry the connection later.
                    // If the problem persists, contact customer support, and provide them the session tracing ID of ZZZZZ.
                    case 40613:
                    // SQL Error Code: 40501
                    // The service is currently busy. Retry the request after 10 seconds. Code: (reason code to be decoded).
                    case 40501:
                    // SQL Error Code: 40197
                    // The service has encountered an error processing your request. Please try again.
                    case 40197:
                    // SQL Error Code: 11001
                    // A connection attempt failed
                    case 11001:
                    // SQL Error Code: 10929
                    // Resource ID: %d. The %s minimum guarantee is %d, maximum limit is %d and the current usage for the database is %d.
                    // However, the server is currently too busy to support requests greater than %d for this database.
                    // For more information, see http://go.microsoft.com/fwlink/?LinkId=267637. Otherwise, please try again.
                    case 10929:
                    // SQL Error Code: 10928
                    // Resource ID: %d. The %s limit for the database is %d and has been reached. For more information,
                    // see http://go.microsoft.com/fwlink/?LinkId=267637.
                    case 10928:
                    // SQL Error Code: 10060
                    // A network-related or instance-specific error occurred while establishing a connection to SQL Server.
                    // The server was not found or was not accessible. Verify that the instance name is correct and that SQL Server
                    // is configured to allow remote connections. (provider: TCP Provider, error: 0 - A connection attempt failed
                    // because the connected party did not properly respond after a period of time, or established connection failed
                    // because connected host has failed to respond.)"}
                    case 10060:
                    // SQL Error Code: 10054
                    // A transport-level error has occurred when sending the request to the server.
                    // (provider: TCP Provider, error: 0 - An existing connection was forcibly closed by the remote host.)
                    case 10054:
                    // SQL Error Code: 10053
                    // A transport-level error has occurred when receiving results from the server.
                    // An established connection was aborted by the software in your host machine.
                    case 10053:
                    // SQL Error Code: 1205
                    // Deadlock
                    case 1205:
                    // SQL Error Code: 233
                    // The client was unable to establish a connection because of an error during connection initialization process before login.
                    // Possible causes include the following: the client tried to connect to an unsupported version of SQL Server;
                    // the server was too busy to accept new connections; or there was a resource limitation (insufficient memory or maximum
                    // allowed connections) on the server. (provider: TCP Provider, error: 0 - An existing connection was forcibly closed by
                    // the remote host.)
                    case 233:
                    // SQL Error Code: 121
                    // The semaphore timeout period has expired
                    case 121:
                    // SQL Error Code: 64
                    // A connection was successfully established with the server, but then an error occurred during the login process.
                    // (provider: TCP Provider, error: 0 - The specified network name is no longer available.)
                    case 64:
                    // DBNETLIB Error Code: 20
                    // The instance of SQL Server you attempted to connect to does not support encryption.
                    case 20:
                        return true;
                        // This exception can be thrown even if the operation completed successfully, so it's safer to let the application fail.
                        // DBNETLIB Error Code: -2
                        // Timeout expired. The timeout period elapsed prior to completion of the operation or the server is not responding. The statement has been terminated.
                        //case -2:
                }
            }

            return false;
        }

        public static bool ShouldRetryOn(Win32Exception ex)
        {
            switch (ex.NativeErrorCode)
            {
                // Timeout expired
                case 0x102:
                    // Semaphore timeout expired
                    Log.Warning("0x102 Timeout");
                    return true;
                case 0x121:
                    Log.Warning("0x121 Semaphore Timeout");
                    return true;
                default:
                    return false;
            }
        }
    }
}

//private static readonly IEnumerable<TimeSpan> RetryTimes = new[]
//{
//    TimeSpan.FromSeconds(1),
//    TimeSpan.FromSeconds(2),
//    TimeSpan.FromSeconds(3)
//};

//private static readonly AsyncRetryPolicy RetryPolicy = Policy
//    .Handle<SqlException>(SqlServerTransientExceptionDetector.ShouldRetryOn)
//    .Or<TimeoutException>()
//    .OrInner<Win32Exception>(SqlServerTransientExceptionDetector.ShouldRetryOn)
//    .WaitAndRetryAsync(RetryTimes,
//        (exception, timeSpan, retryCount, context) =>
//        {
//            Log.Warning(
//                exception,
//                $"WARNING: Error talking to ReportingDb, will retry after {timeSpan}. Retry attempt {retryCount}"
//            );
//        });

//public static async Task<int> ExecuteAsyncWithRetry(this IDbConnection cnn, string sql, object param = null,
//    IDbTransaction transaction = null, int? commandTimeout = null,
//    CommandType? commandType = null) =>
//    await RetryPolicy.ExecuteAsync(async () =>
//        await cnn.ExecuteAsync(sql, param, transaction, commandTimeout, commandType));

//public static async Task<IEnumerable<T>> QueryAsyncWithRetry<T>(this IDbConnection cnn, string sql,
//    object param = null,
//    IDbTransaction transaction = null, int? commandTimeout = null,
//    CommandType? commandType = null) =>
//    await RetryPolicy.ExecuteAsync(async () =>
//        await cnn.QueryAsync<T>(sql, param, transaction, commandTimeout, commandType));

// non async
//private static readonly RetryPolicy RetryPolicy2 = Policy
//    .Handle<SqlException>(SqlServerTransientExceptionDetector.ShouldRetryOn)
//    .Or<TimeoutException>()
//    .OrInner<Win32Exception>(SqlServerTransientExceptionDetector.ShouldRetryOn)
//    .WaitAndRetry(RetryTimes,
//        (exception, timeSpan, retryCount, context) =>
//        {
//            Log.Warning(
//                exception,
//                $"WARNING: Error talking to ReportingDb, will retry after {timeSpan}. Retry attempt {retryCount}"
//            );
//        });

//public static int ExecuteWithRetry(this IDbConnection cnn, string sql, object param = null,
//    IDbTransaction transaction = null, int? commandTimeout = null,
//    CommandType? commandType = null)
//{
//    return RetryPolicy2.Execute(() =>
//        cnn.Execute(sql, param, transaction, commandTimeout, commandType));
//}

// linear backoff
// https://hyr.mn/Polly-wait-and-retry
// **HERE**** need to figure out syntax

//private static readonly RetryPolicy linearBackoff = Backoff.LinearBackoff(TimeSpan.FromMilliseconds(50), retryCount: 4, fastFirst: true);

//private static readonly RetryPolicy RetryPolicy3 = Policy
//    .Handle<SqlException>(SqlServerTransientExceptionDetector.ShouldRetryOn)
//    .Or<TimeoutException>()
//    .OrInner<Win32Exception>(SqlServerTransientExceptionDetector.ShouldRetryOn)
//    .WaitAndRetry(RetryTimes,
//        (exception, timeSpan, retryCount, context) =>
//        {
//            Log.Warning(
//                exception,
//                $"WARNING: Error talking to ReportingDb, will retry after {timeSpan}. Retry attempt {retryCount}"
//            );
//        });

