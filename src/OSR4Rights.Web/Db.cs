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

    public record DashboardRealPage(
           DateTime DateTimeUtc,
           string IPAddress,
           string Path,
           string? UserAgent,
           string? Email
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


    public record Job(
        int JobId,
        int LoginId,
        string OrigFileName,
        DateTime DateTimeUtcUploaded,
        int? JobStatusId,
        int? VMId,
        DateTime? DateTimeUtcJobStartedOnVm,
        DateTime? DateTimeUtcJobEndedOnVm,
        int JobTypeId
    );

    public record JobViewModel(
        int JobId,
        int LoginId,
        string OrigFileName,
        DateTime DateTimeUtcUploaded,
        int? JobStatusId,
        string? JobStatusString,
        int? VMId,
        DateTime? DateTimeUtcJobStartedOnVm,
        DateTime? DateTimeUtcJobEndedOnVm,
        int JobTypeId,
        string? JobType
    );

    public record LogSmall(
        int LogId,
        string Text,
        DateTime DateTimeUtc
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

        public static async Task<int> InsertJobWithOrigFileNameAndReturnJobId(string connectionString, int loginId, string origFileName, int jobTypeId)
        {
            using var conn = GetOpenConnection(connectionString);

            var result = await conn.QueryAsyncWithRetry<int>(@"
                insert into Job (LoginId, OrigFileName, DateTimeUtcUploaded, JobStatusId, JobTypeId)
                output inserted.JobId
                values (@LoginId, @OrigFileName, GETUTCDATE(), @JobStatusId, @JobTypeId)
                ", new { loginId, origFileName, jobStatusId = JobStatusId.WaitingToStart, jobTypeId });

            return result.Single();
        }

        public static class JobTypeId
        {
            public const int FaceSearch = 1;
            public const int HateSpeech = 2;
        }

        public static class JobStatusId
        {
            public const int WaitingToStart = 1;
            public const int Running = 2;
            public const int Completed = 3;
            public const int CancelledByUser = 4;
            public const int Exception = 9;
        }

        public static class VMStatusId
        {
            public const int CreatingVM = 1;
            public const int ReadyToRunJobOnVM = 2;
            public const int RunningJobOnVM = 3;
            public const int DeletingVM = 4;
            public const int Deleted = 5;
        }

        public static class VMTypeId
        {
            public const int FaceSearchGPU = 1;
            public const int HateSpeechCPU = 2;
        }

        public static async Task UpdateJobIdToStatusId(string connectionString, int jobId, int jobStatusId)
        {
            if (jobId != 0)
            {
                using var conn = GetOpenConnection(connectionString);

                await conn.ExecuteAsyncWithRetry(@"
                update Job
                set JobStatusId = @JobStatusId
                where JobId = @JobId
                ", new { jobId, jobStatusId });
                return;
            }

            Log.Warning($"{nameof(UpdateJobIdToStatusId)} has an jobId of 0 passed to it. Could be because of an Exception in a FileProcessingService");
        }

        public static async Task UpdateJobToStatusCompleted(string connectionString, int jobId)
        {
            using var conn = GetOpenConnection(connectionString);

            await conn.ExecuteAsyncWithRetry(@"
                update Job
                set JobStatusId = @JobStatusId
                where JobId = @JobId
                ", new { jobId, jobStatusId = JobStatusId.Completed });
        }

        public static async Task<int> GetJobStatusId(string connectionString, int jobId)
        {
            using var conn = GetOpenConnection(connectionString);

            var result = await conn.QueryAsyncWithRetry<int>(@"
                select JobStatusId
                from Job
                where JobId = @JobId
                ", new { jobId });

            return result.Single();
        }

        public static async Task<List<LogSmall>> GetLogsForJobId(string connectionString, int jobId)
        {
            using var conn = GetOpenConnection(connectionString);

            var result = await conn.QueryAsyncWithRetry<LogSmall>(@"
                select LogId, [Text], DateTimeUtc
                from Log
                where JobId = @JobId
                order by DateTimeUtc desc
                ", new { jobId });

            return result.ToList();
        }

        // normal log insert
        public static async Task InsertLog(string connectionString, int jobId, string text)
        {
            using var conn = GetOpenConnection(connectionString);

            await conn.ExecuteAsyncWithRetry(@"
                insert into Log(JobId, [Text])
                values(@JobId, @Text)
                ", new { jobId, text });

            // should never happen except on very exceptional circumstances
            if (jobId == 0) Log.Warning($"{nameof(InsertLog)} inserted a log into the db with jobId 0 and text: {text}.");
        }

        // Used by the event coming back from remote server
        public static void InsertLogNotAsyncWithRetry(string connectionString, int jobId, string text)
        {
            using var conn = GetOpenConnection(connectionString);

            conn.ExecuteWithRetry(@"
                insert into Log(JobId, [Text])
                values(@JobId, @Text)
                ", new { jobId, text });
        }

        // only used by polly-test to get SQL Azure to fail
        //public static void InsertLogNotAsync(string connectionString, int jobId, string text)
        //{
        //    using var conn = GetOpenConnection(connectionString);

        //    conn.Execute(@"
        //    insert into Log(JobId, [Text])
        //    values(@JobId, @Text)
        //    ", new { jobId, text });
        //}

        public static async Task<VMFromDb?> GetFreeVMIfExists(string connectionString, int vmTypeId)
        {
            using var conn = GetOpenConnection(connectionString);

            var result = await conn.QueryAsyncWithRetry<VMFromDb?>(@"
                select * 
                from VM
                where VMStatusId = 2 -- ReadyToReceiveJobOnVM
                and VMTypeId = @VMTypeId
                ", new { vmTypeId });

            return result.SingleOrDefault();
        }


        public record VMFromDb(int VMId, int VMStatusId, string? ResourceGroupName, DateTime DateTimeUtcCreated,
            DateTime? DateTimeUtcDeleted, string Password, int VMTypeId);

        public static async Task<VMFromDb> CreateNewVM(string connectionString, string passwordVM, int vmTypeId)
        {
            using var conn = GetOpenConnection(connectionString);

            var result = await conn.QueryAsyncWithRetry<int>(@"
                insert into VM (VMStatusId, DateTimeUtcCreated, Password, VMTypeId)
                output inserted.VMId as VMId
                values (1, GETUTCDATE(), @PasswordVM, @VMTypeId)
                ", new { passwordVM, vmTypeId });

            var vmId = result.Single();

            // The Id of the VM is the same as the DB assigned identity seed
            string resourceGroupName;

            if (vmTypeId == VMTypeId.FaceSearchGPU)
                resourceGroupName = $"webfacesearchgpu{vmId}";

            else if (vmTypeId == VMTypeId.HateSpeechCPU)
                resourceGroupName = $"webhatespeechcpu{vmId}";

            else
                throw new ApplicationException("Unexpected VMTypeId");


            var foo = await conn.QueryAsyncWithRetry<VMFromDb>(@"
                update VM
                set ResourceGroupName = @ResourceGroupName
                where VMId = @VMId

                select *
                from VM
                where VMId = @VMId
                ", new { vmId, resourceGroupName });

            return foo.Single();
        }

        public static async Task UpdateVMStatusId(string connectionString, int vmId, int vmStatusId)
        {
            using var conn = GetOpenConnection(connectionString);

            await conn.ExecuteAsyncWithRetry(@"
                update VM
                set VMStatusId = @VMStatusID
                where VMId = @VMId
                ", new { vmId, vmStatusId });

            // should never happen
            if (vmId == 0) Log.Warning($"{nameof(UpdateVMStatusId)} has an vmId of 0 passed to it.");
        }

        public static async Task UpdateJobVMIdDetails(string connectionString, int jobId, int vmId)
        {
            using var conn = GetOpenConnection(connectionString);

            await conn.ExecuteAsyncWithRetry(@"
                update Job
                set VMId = @VMId,
                    DateTimeUtcJobStartedOnVM = GETUTCDATE()
                where JobId = @JobId
                ", new { jobId, vmId });
        }

        public static async Task<int> GetCountOfAllVMs(string connectionString)
        {
            using var conn = GetOpenConnection(connectionString);

            var result = await conn.QueryAsyncWithRetry<int>(@"
                select count(*) from VM
                ");

            // should always be something, but just in case there are no records in VM return 0
            return result.SingleOrDefault();
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

        public static async Task UpdateJobIdDateTimeUtcJobEndedOnVM(string connectionString, int jobId)
        {
            using var conn = GetOpenConnection(connectionString);

            await conn.ExecuteAsyncWithRetry(@"
                update job
                set DateTimeUtcJobEndedOnVM = GETUTCDATE()
                where jobId = @JobId
                ", new { jobId });
        }

        public static async Task UpdateVMDateTimeUtcDeletedToNow(string connectionString, int vmId)
        {
            using var conn = GetOpenConnection(connectionString);

            await conn.ExecuteAsyncWithRetry(@"
                update vm
                set DateTimeUtcDeleted = GETUTCDATE()
                where vmId = @VmId
                ", new { vmId });

            // should only happen under unusual circumstances eg a catch/finally from a service 
            if (vmId == 0) Log.Warning($"{nameof(UpdateVMDateTimeUtcDeletedToNow)} has an vmId of 0 passed to it.");
        }

        public static async Task<List<Job>> GetJobsForLoginId(string connectionString, int loginId)
        {
            using var conn = GetOpenConnection(connectionString);

            var result = await conn.QueryAsyncWithRetry<Job>(@"
                select *
                from Job
                where LoginId = @LoginId
                ", new { loginId });

            return result.ToList();
        }

        public static async Task<bool> CheckIfLoginIdIsAllowedToViewThisJobId(string connectionString, int loginId, int jobId)
        {
            using var conn = GetOpenConnection(connectionString);

            var result = await conn.QueryAsyncWithRetry<bool>(@"
                select count(*) 
                from Job
                where LoginId = @LoginId
                    and JobId = @JobId
                ", new { loginId, jobId });

            // https://stackoverflow.com/a/31282196/26086
            // When  0 is returned Dapper will return False
            return result.FirstOrDefault();
        }

        public static async Task<Job> GetJobByJobId(string connectionString, int jobId)
        {
            using var conn = GetOpenConnection(connectionString);

            var result = await conn.QueryAsyncWithRetry<Job>(@"
                select *
                from Job
                where JobId = @JobId
                ", new { jobId });

            // Want it to throw if none found
            return result.Single();
        }

        public static async Task<int> GetVmIdByResourceGroupName(string connectionString, string resourceGroupName)
        {
            using var conn = GetOpenConnection(connectionString);

            var result = await conn.QueryAsyncWithRetry<int>(@"
                select VmId
                from Vm
                where ResourceGroupName = @ResourceGroupName
                ", new { resourceGroupName });

            return result.Single();
        }

        public static async Task<string> GetEmailByJobId(string connectionString, int jobId)
        {
            using var conn = GetOpenConnection(connectionString);

            var result = await conn.QueryAsyncWithRetry<string>(@"
                select l.Email
                from Login l
                join Job j on j.LoginId = l.LoginId
                where j.JobId = @JobId
                ", new { jobId });

            return result.Single();
        }

        public static async Task<List<int>> GetVmIdsStatus2ReadyToRunJob(string connectionString)
        {
            using var conn = GetOpenConnection(connectionString);

            var result = await conn.QueryAsyncWithRetry<int>(@"
                select VMid
                from VM
                where VMStatusId = 2
                ");

            return result.ToList();
        }

        public static async Task<DateTime?> GetMostRecentLogDateTimeUtcForMostRecentJobRunningOnVmId(string connectionString, int vmId)
        {
            using var conn = GetOpenConnection(connectionString);

            // will get all jobs which have been run on the VM (if multiple)
            // only return the last log entry
            var result = await conn.QueryAsyncWithRetry<DateTime?>(@"
                select top 1 l.DateTimeUtc
                from Log l
                join Job j on j.JobId = l.JobId
                where VMId = @VMId
                order by l.DateTimeUtc desc
                ", new { vmId });

            // if no log entry, send back default
            return result.SingleOrDefault();
        }

        public static async Task<Job?> GetMostRecentJobOnVmId(string connectionString, int vmId)
        {
            using var conn = GetOpenConnection(connectionString);

            var result = await conn.QueryAsyncWithRetry<Job?>(@"
                select top 1 * 
                from Job 
                where VMId = @VMId
                order by DateTimeUtcJobStartedOnVM desc
                ", new { vmId });

            return result.SingleOrDefault();
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
                select DateTimeUtc, Path, Email 
                from weblog
                where StatusCode = 500
                order by DateTimeUtc desc
            ");

            return result.ToList();
        }

        public static async Task<List<DashboardRealPage>> GetDashboardRealPages(string connectionString)
        {
            using var conn = GetOpenConnection(connectionString);

            var result = await conn.QueryAsyncWithRetry<DashboardRealPage>(@"
                select DateTimeUtc, IPAddress, Path, UserAgent, Email
                from weblog
                where WebLogTypeId = 1
                order by DateTimeUtc desc
            ");

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

    }
}