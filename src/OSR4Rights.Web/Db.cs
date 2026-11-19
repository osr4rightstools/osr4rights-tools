using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Serilog;

namespace OSR4Rights.Web
{

    public record Dashboard500VM(
        DateTime DateTimeUtc,
        string Path,
        string? Email
    );

    public record Dashboard404VM(
        DateTime DateTimeUtc,
        string IPAddress,
        string Path,
        string? UserAgent,
        string? Email
    );


    public record DashboardLoginAndJob(
           string? Email, // make mapping easier by keeping this nullable
           DateTime DateTimeUtcJobStartedOnVM,
           int JobTypeId,
           string OrigFileName,
           int TimeTakenInS
       );


    public record DashboardRealPage(
           DateTime DateTimeUtc,
           string IPAddress,
           string Path,
           string? UserAgent,
           string? Email
    );

    public record DashboardRequest(
        int WebLogId,
        int WebLogTypeId,
        DateTime DateTimeUtc,
        string? IpAddress,
        string Verb,
        string Path,
        string? QueryString,
        int StatusCode,
        int ElapsedTimeInMs,
        string? Referer,
        bool? DisplayReferer,
        string? UserAgent,
        string HttpVersion,
        int? LoginId,
        string? Email,
        string? RoleName
    );

    public record Login(
        int LoginId,
        string Email,
        string PasswordHash,
        int? RoleId,
        int LoginStateId,
        int PasswordFailedAttempts,
        Guid? PasswordResetVerificationCode,
        DateTime? PasswordResetVerificationSentDateTimeUtc,
        int MfaFailedAttempts,
        int? MfaCode,
        DateTime? MfaSentDateTimeUtc,
        Guid EmailAddressConfirmationCode,
        DateTime DateTimeUtcCreated
        );

    public record LoginSmall(
        // not nullable in db, but useful for this concept of initiating
        int? LoginId,
        string Email,
        string PasswordHash,
        int LoginStateId,
        // nice default set here
        int? RoleId = null

     );


    public record LoginAdminViewModel(
        // not nullable in db, but useful for this concept of initiating
        int? LoginId,
        string Email,
        string PasswordHash,
        int LoginStateId,
        string LoginStateName,
        // nice default set here
        string? RoleName,
        int? RoleId = null
    );

    public record LoginState(
        int LoginStateId,
        string Name
    );


    public record OSREmail(
        string ToEmailAddress,
        string Subject,
        string TextBody,
        string HtmlBody
    );

    public static class LoginStateId
    {
        public const int WaitingToBeInitiallyVerifiedByEmail = 1;
        public const int InUse = 2;
        public const int PasswordResetSent = 3;
        public const int LockedOutDueTo3WrongPasswords = 4;

        public const int Disabled = 99;
    }

    public static class RoleId
    {
        // Registered but email not manually verified yet
        // can use the application in a limited way
        public const int Tier1 = 1;

        // Email has been manually verified
        // Can use the application fully
        public const int Tier2 = 2;

        public const int Admin = 9;
    }

    public static class CDRole
    {
        public const string Tier1 = "Tier1";
        public const string Tier2 = "Tier2";
        public const string Admin = "Admin";
    }

    public static class Db
    {
        public static IDbConnection GetOpenConnection(string connectionString)
        {
            if (connectionString == null) throw new ArgumentException("ConnectionString can't be null");
            DbConnection cnn = new SqlConnection(connectionString);
            return cnn;
        }

        public static async Task<LoginSmall?> GetLoginByEmail(string connectionString, string email)
        {
            using var conn = GetOpenConnection(connectionString);

            // emails in the db may be in upper and lower case
            // we don't allow dupe records in our db
            // davemateer@gmail.com
            // DaveMateer@gmail.com

            // so lower whatever is coming in
            email = email.ToLower();

            var result = await conn.QueryAsyncWithRetry<LoginSmall?>(@"
                select LoginId, Email, PasswordHash, LoginStateId, RoleId
                from login
                -- @Email is lower, and we are lowering whatever is in the Db
                where LOWER(email) = @Email
                ", new { email });

            result = result.ToArray();

            if (result.Count() > 1)
                throw new ApplicationException("Cannot have duplicate emails in database");

            return result.SingleOrDefault();
        }

        public static async Task IncrementNumberOfFailedLoginsForEmailLogin(string connectionString, string email)
        {
            using var conn = GetOpenConnection(connectionString);

            await conn.ExecuteAsyncWithRetry(@"
              update Login 
              set PasswordFailedAttempts = PasswordFailedAttempts + 1
              where Email = @Email 
              ", new { email });
        }

        public static async Task<bool> CheckIfNeedToLockAccountForEmailLogin(string connectionString, string email)
        {
            using var conn = GetOpenConnection(connectionString);

            var numberOfFailedLogins = await conn.QueryAsyncWithRetry<int>(@"
                select PasswordFailedAttempts
                from Login
                where Email = @Email
                ", new { email });

            var foo = numberOfFailedLogins.First();

            if (foo > 3)
            {
                var lockedOut = LoginStateId.LockedOutDueTo3WrongPasswords;
                await conn.ExecuteAsyncWithRetry(@"
                    update login
                    set LoginStateId = @LockedOut
                    where Email = @Email
                    ", new { lockedOut, email });
                return true;
            }

            return false;
        }

        public static async Task ResetFailedLoginsForEmailLogin(string connectionString, string email)
        {
            using var conn = GetOpenConnection(connectionString);

            await conn.ExecuteAsyncWithRetry(@"
                update login
                set PasswordFailedAttempts = 0
                where Email = @Email
                ", new { email });
        }

        public static async Task<Login> InsertLogin(string connectionString, LoginSmall login)
        {
            using var conn = GetOpenConnection(connectionString);

            var result = await conn.QueryAsyncWithRetry<Login>(@"
                insert Login(Email, PasswordHash, LoginStateId)
                output inserted.*
                values(@Email, @PasswordHash, @LoginStateId)
                ", login);

            return result.First();
        }



        public static async Task UpdateLoginEmailAddressConfirmationCode(string connectionString, int loginId,
            Guid guid)
        {
            using var conn = GetOpenConnection(connectionString);

            await conn.ExecuteAsyncWithRetry(@"
                update Login
                set EmailAddressConfirmationCode = @Guid
                where LoginId = @LoginId
                ", new { loginId, guid });
        }

        public static async Task DeleteLoginWithEmail(string connectionString, string email)
        {
            using var conn = GetOpenConnection(connectionString);

            await conn.ExecuteAsyncWithRetry(@"
                delete from login
                where email = @Email
                ", new { email });
        }

        public static async Task<Login?> GetLoginByEmailConfirmationCode(string connectionString,
            Guid emailAddressConfirmationCode)
        {
            using var conn = GetOpenConnection(connectionString);

            var result = await conn.QueryAsyncWithRetry<Login?>(@"
                select * from login
                where EmailAddressConfirmationCode = @EmailAddressConfirmationCode
                    and GETUTCDATE() < dateadd(hh, 1, DateTimeUtcCreated)
                    and loginStateId = @LoginStateId
                ",
                new { emailAddressConfirmationCode, loginStateId = LoginStateId.WaitingToBeInitiallyVerifiedByEmail });

            return result.SingleOrDefault();
        }

        public static async Task UpdateLoginIdWithLoginStateId(string connectionString, int loginId, int loginStateId)
        {
            using var conn = GetOpenConnection(connectionString);

            await conn.ExecuteAsyncWithRetry(@"
                update login
                set loginStateId = @LoginStateId
                where loginId = @LoginId
                ", new { loginId, loginStateId });
        }

        public static async Task UpdateLoginIdWithRoleId(string connectionString, int loginId, int roleId)
        {
            using var conn = GetOpenConnection(connectionString);

            await conn.ExecuteAsyncWithRetry(@"
                update login
                set RoleId = @RoleId
                where loginId = @LoginId
                ", new { loginId, roleId });
        }

        public static async Task UpdateLoginIdForgotPasswordResetWithTimeAndGuid(string connectionString, int loginId,
            Guid guid)
        {
            using var conn = GetOpenConnection(connectionString);

            await conn.ExecuteAsyncWithRetry(@"
                update login
                set LoginStateId = 3, -- Password Reset Code Sent 
                    PasswordResetVerificationCode = @Guid,
                    PasswordResetVerificationSentDateTimeUtc = GETUTCDATE()
                where loginId = @LoginId
                ", new { loginId, guid });
        }

        public static async Task<LoginSmall?> GetLoginByPasswordResetVerificationCode(string connectionString,
            Guid guid)
        {
            using var conn = GetOpenConnection(connectionString);

            var result = await conn.QueryAsyncWithRetry<LoginSmall?>(@"
                select LoginId, Email, PasswordHash, LoginStateId, RoleId
                from login
                where PasswordResetVerificationCode = @Guid
                  and GETUTCDATE() < dateadd(hh, 1, PasswordResetVerificationSentDateTimeUtc)
                ", new { guid });

            return result.FirstOrDefault();
        }

        public static async Task UpdateLoginPasswordAndResetFailedLoginsAndVerificationCode(string connectionString,
            int loginId, string newPasswordHash)
        {
            using var conn = GetOpenConnection(connectionString);

            await conn.ExecuteAsyncWithRetry(@"
                update Login 
                set 
                    PasswordHash = @NewPasswordHash,
                    LoginStateId = 2, -- InUse
                    PasswordFailedAttempts = 0,
                    PasswordResetVerificationCode = null,
                    PasswordResetVerificationSentDateTimeUtc = null
                where LoginId = @LoginId
                ", new { loginId, newPasswordHash });

        }

        public static async Task UpdateLoginIdSetEmailAddressConfirmationCodeToNull(string connectionString,
            int loginId)
        {
            using var conn = GetOpenConnection(connectionString);

            await conn.ExecuteAsyncWithRetry(@"
                update Login 
                set 
                    EmailAddressConfirmationCode = null
                where LoginId = @LoginId
                ", new { loginId });
        }

        public static async Task<List<LoginSmall>> GetAllLogins(string connectionString)
        {
            using var conn = GetOpenConnection(connectionString);

            var result = await conn.QueryAsyncWithRetry<LoginSmall>(@"
                select LoginId, Email, PasswordHash, LoginStateId, RoleId
                from login
                ");

            return result.ToList();
        }

        public static async Task<List<LoginState>> GetAllLoginStates(string connectionString)
        {
            using var conn = GetOpenConnection(connectionString);

            var result = await conn.QueryAsyncWithRetry<LoginState>(@"
                select * 
                from LoginState 
                order by LoginStateId 
                ");

            return result.ToList();
        }

        public static async Task<LoginSmall> GetLoginByLoginId(string connectionString, int loginId)
        {
            using var conn = GetOpenConnection(connectionString);

            var result = await conn.QueryAsyncWithRetry<LoginSmall>(@"
                select LoginId, Email, PasswordHash, LoginStateId, RoleId
                from login
                where loginId = @LoginId
                ", new { loginId });

            return result.Single();
        }

        public static async Task UpdateLoginStateIdAndRoleIdByLoginId(string connectionString, int loginId, int loginStateId, int? roleId)
        {
            using var conn = GetOpenConnection(connectionString);

            var result = await conn.ExecuteAsyncWithRetry(@"
                update login
                set LoginStateId = @LoginStateId,
                    RoleId = @RoleId
                where loginId = @LoginId
                ", new { loginId, loginStateId, roleId });
        }

        // used by startup.cs to insert into custom weblog table
        // this should be a fast insert!
        public static async Task InsertWebLog(string connectionString,
            int webLogTypeId,
            string? ipAddress,
            string verb,
            string path,
            string? queryString,
            int statusCode,
            int elapsedTimeInMs,
            string? referer,
            string? userAgent,
            string httpVersion,
            int? loginId,
            string? email,
            string? roleName)
        {
            using var conn = GetOpenConnection(connectionString);

            await conn.ExecuteAsyncWithRetry(@"
                INSERT INTO [dbo].[WebLog]
                       ([WebLogTypeId]
                       ,[DateTimeUtc]
                       ,[IPAddress]
                       ,[Verb]
                       ,[Path]
                       ,[QueryString]
                       ,[StatusCode]
                       ,[ElapsedTimeInMs]
                       ,[Referer]
                       ,[UserAgent]
                       ,[HttpVersion]
                       ,[LoginId]
                       ,[Email]
                       ,[RoleName])
                 VALUES
                       (@WebLogTypeId
                       ,GETUTCDATE()
                       ,@IPAddress
                       ,@Verb
                       ,@Path
                       ,@QueryString
                       ,@StatusCode
                       ,@ElapsedTimeInMs
                       ,@Referer
                       ,@UserAgent
                       ,@HttpVersion
                       ,@LoginId
                       ,@Email
                       ,@RoleName)
            ", new
            {
                webLogTypeId,
                ipAddress,
                verb,
                path,
                queryString,
                statusCode,
                elapsedTimeInMs,
                referer,
                userAgent,
                httpVersion,
                loginId,
                email,
                roleName
            });
        }


        public static async Task<List<Dashboard500VM>> GetDashboard500VMs(string connectionString)
        {
            using var conn = GetOpenConnection(connectionString);

            var result = await conn.QueryAsyncWithRetry<Dashboard500VM>(@"
                select top 10 
                DateTimeUtc, Path, Email 
                from weblog
                where StatusCode = 500
                order by DateTimeUtc desc");

            return result.ToList();
        }

        public static async Task<List<Dashboard404VM>> GetDashboard404VMs(string connectionString)
        {
            using var conn = GetOpenConnection(connectionString);

            var result = await conn.QueryAsyncWithRetry<Dashboard404VM>(@"
                select top 30 
                DateTimeUtc, IPAddress, Path, UserAgent, Email
                from weblog
                where StatusCode = 404
                order by DateTimeUtc desc");

            return result.ToList();
        }

        public static async Task<List<DashboardLoginAndJob>> GetDashboardLoginsAndJobs(string connectionString)
        {
            using var conn = GetOpenConnection(connectionString);

            var result = await conn.QueryAsyncWithRetry<DashboardLoginAndJob>(@"
                select top 10
                l.Email, j.DateTimeUtcJobStartedOnVM, j.JobTypeId, OrigFileName,
                datediff(second,j.DateTimeUtcJobStartedOnVM,j.DateTimeUtcJobEndedOnVM)  as TimeTakenInS
                from job j
                join login l on l.LoginId = j.LoginId
                order by DateTimeUtcJobStartedOnVM desc");

            return result.ToList();
        }

        public static async Task<List<DashboardRealPage>> GetDashboardRealPages(string connectionString)
        {
            using var conn = GetOpenConnection(connectionString);

            var result = await conn.QueryAsyncWithRetry<DashboardRealPage>(@"
                select top 20
                DateTimeUtc, IPAddress, Path, UserAgent, Email
                from weblog
                where WebLogTypeId = 1
                order by DateTimeUtc desc");

            return result.ToList();
        }

        public static async Task<List<DashboardRequest>> GetDashboardAllRequests(string connectionString)
        {
            using var conn = GetOpenConnection(connectionString);

            var result = await conn.QueryAsyncWithRetry<DashboardRequest>(@"
                select top 30
                *
                from weblog
                order by DateTimeUtc desc");

            return result.ToList();
        }


        public static class WebLogTypeId
        {
            public const int Page = 1;
            public const int Asset = 2;
            public const int HealthCheckPage = 3;
            public const int RobotsTxt = 4;
            public const int SitemapXml = 5;
            public const int FaviconIco = 6;
            public const int TusFiles = 7;
            public const int Downloads = 8;
        }

        public static async Task InsertUpdateCookie(string connectionString,
            string? cookieValue,
            string? loginId,
            DateTimeOffset? issuedUtc,
            DateTimeOffset? expiresUtc)
        {
            using var conn = GetOpenConnection(connectionString);

            await conn.ExecuteAsyncWithRetry(@"
             -- The same cookie is in the db already
             IF EXISTS (SELECT 1 FROM Cookie WHERE LoginId = @LoginId AND expiresUtc = @ExpiresUtc)
             BEGIN
                -- do nothing
                SELECT 1
             END
             ELSE
             BEGIN
                 -- There may be an expired cookie so delete
                 DELETE FROM Cookie WHERE LoginId = @LoginID

                 INSERT INTO Cookie
                    (CookieValue,LoginId,IssuedUtc,ExpiresUtc)
                 VALUES (@CookieValue, @LoginId, @IssuedUtc, @ExpiresUtc)
             END

            ", new
            {
                cookieValue,
                loginId,
                issuedUtc,
                expiresUtc
            });

        }

    }
}