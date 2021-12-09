CREATE TABLE [dbo].[WebLog] (
    [WebLogId]        INT             IDENTITY (1, 1) NOT NULL,
    [WebLogTypeId]    INT             NOT NULL,
    [DateTimeUtc]     DATETIME2 (7)   NOT NULL,
    [IPAddress]       NVARCHAR (50)   NULL,
    [Verb]            NVARCHAR (50)   NOT NULL,
    [Path]            NVARCHAR (MAX)  NOT NULL,
    [QueryString]     NVARCHAR (MAX)  NULL,
    [StatusCode]      INT             NOT NULL,
    [ElapsedTimeInMs] INT             NOT NULL,
    [Referer]         NVARCHAR (MAX)  NULL,
    [DisplayReferer]  BIT             NULL,
    [UserAgent]       NVARCHAR (MAX)  NULL,
    [HttpVersion]     NVARCHAR (50)   NULL,
    [LoginId]         INT             NULL,
    [Email]           NVARCHAR (1024) NULL,
    [RoleName]        NVARCHAR (50)   NULL,
    CONSTRAINT [PK_WebLog] PRIMARY KEY CLUSTERED ([WebLogId] ASC)
);





