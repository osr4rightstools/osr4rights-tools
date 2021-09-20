CREATE TABLE [dbo].[VM] (
    [VMId]               INT            IDENTITY (1, 1) NOT NULL,
    [VMStatusId]         INT            NOT NULL,
    [ResourceGroupName]  NVARCHAR (MAX) NULL,
    [DateTimeUtcCreated] DATETIME2 (7)  NOT NULL,
    [DateTimeUtcDeleted] DATETIME2 (7)  NULL,
    [Password]           NVARCHAR (MAX) NOT NULL,
    [VMTypeId]           INT            NOT NULL,
    CONSTRAINT [PK_VM] PRIMARY KEY CLUSTERED ([VMId] ASC)
);







