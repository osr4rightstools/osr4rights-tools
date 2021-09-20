CREATE TABLE [dbo].[Log] (
    [LogId]       INT            IDENTITY (1, 1) NOT NULL,
    [JobId]       INT            NOT NULL,
    [Text]        NVARCHAR (MAX) NOT NULL,
    [DateTimeUtc] DATETIME2 (7)  CONSTRAINT [DF_Log_DateTime] DEFAULT (getutcdate()) NOT NULL,
    CONSTRAINT [PK_Log] PRIMARY KEY CLUSTERED ([LogId] ASC),
    CONSTRAINT [FK_Log_Job] FOREIGN KEY ([JobId]) REFERENCES [dbo].[Job] ([JobId])
);






GO
CREATE NONCLUSTERED INDEX [IX_Log]
    ON [dbo].[Log]([JobId] ASC);

