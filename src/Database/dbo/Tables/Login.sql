CREATE TABLE [dbo].[Login] (
    [LoginId]                                  INT              IDENTITY (1, 1) NOT NULL,
    [Email]                                    NVARCHAR (MAX)   NOT NULL,
    [PasswordHash]                             NVARCHAR (MAX)   NOT NULL,
    [RoleId]                                   INT              NULL,
    [LoginStateId]                             INT              NOT NULL,
    [PasswordFailedAttempts]                   INT              CONSTRAINT [DF_Login_EmailFailedPasswordAttempts] DEFAULT ((0)) NOT NULL,
    [PasswordResetVerificationCode]            UNIQUEIDENTIFIER NULL,
    [PasswordResetVerificationSentDateTimeUtc] DATETIME2 (7)    NULL,
    [MfaFailedAttempts]                        INT              CONSTRAINT [DF_Login_Email2FAFailedAttempts] DEFAULT ((0)) NOT NULL,
    [MfaCode]                                  INT              NULL,
    [MfaSentDateTimeUtc]                       DATETIME2 (7)    NULL,
    [EmailAddressConfirmationCode]             UNIQUEIDENTIFIER NULL,
    [DateTimeUtcCreated]                       DATETIME2 (7)    CONSTRAINT [DF_Login_DateTimeCreated] DEFAULT (getutcdate()) NOT NULL,
    CONSTRAINT [PK_Login] PRIMARY KEY CLUSTERED ([LoginId] ASC),
    CONSTRAINT [FK_Login_LoginState] FOREIGN KEY ([LoginStateId]) REFERENCES [dbo].[LoginState] ([LoginStateId])
);









