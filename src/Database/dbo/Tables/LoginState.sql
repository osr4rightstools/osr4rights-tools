CREATE TABLE [dbo].[LoginState] (
    [LoginStateId] INT            NOT NULL,
    [Name]         NVARCHAR (MAX) NOT NULL,
    CONSTRAINT [PK_LoginState] PRIMARY KEY CLUSTERED ([LoginStateId] ASC)
);

