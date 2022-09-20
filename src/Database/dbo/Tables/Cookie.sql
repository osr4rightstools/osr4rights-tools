CREATE TABLE [dbo].[Cookie] (
    [CookieId]    INT            IDENTITY (1, 1) NOT NULL,
    [CookieValue] NVARCHAR (MAX) NOT NULL,
    [LoginId]     INT            NOT NULL,
    [IssuedUtc]   DATETIME2 (7)  NOT NULL,
    [ExpiresUtc]  DATETIME2 (7)  NOT NULL,
    CONSTRAINT [PK_Cookie] PRIMARY KEY CLUSTERED ([CookieId] ASC)
);

