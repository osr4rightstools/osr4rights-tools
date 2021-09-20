CREATE TABLE [dbo].[Job] (
    [JobId]                     INT            IDENTITY (1, 1) NOT NULL,
    [LoginId]                   INT            NOT NULL,
    [OrigFileName]              NVARCHAR (MAX) NOT NULL,
    [DateTimeUtcUploaded]       DATETIME       NOT NULL,
    [JobStatusId]               INT            NULL,
    [VMId]                      INT            NULL,
    [DateTimeUtcJobStartedOnVM] DATETIME2 (7)  NULL,
    [DateTimeUtcJobEndedOnVM]   DATETIME2 (7)  NULL,
    [JobTypeId]                 INT            NOT NULL,
    CONSTRAINT [PK_Job] PRIMARY KEY CLUSTERED ([JobId] ASC)
);









