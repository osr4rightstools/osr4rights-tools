CREATE TABLE [dbo].[IPAddressInfo] (
    [IPAddressInfoId] INT            IDENTITY (1, 1) NOT NULL,
    [IPAddress]       NVARCHAR (50)  NOT NULL,
    [City]            NVARCHAR (MAX) NULL,
    [RegionName]      NVARCHAR (MAX) NULL,
    [Country]         NVARCHAR (MAX) NULL,
    [CountryCode]     NVARCHAR (50)  NULL,
    [Continent]       NVARCHAR (50)  NULL,
    [Zip]             NVARCHAR (50)  NULL,
    [Lat]             DECIMAL (9, 6) NULL,
    [Long]            DECIMAL (9, 6) NULL,
    [ISP]             NVARCHAR (MAX) NULL,
    [Org]             NVARCHAR (MAX) NULL,
    [ASx]             NVARCHAR (MAX) NULL,
    [ASName]          NVARCHAR (MAX) NULL,
    [Mobile]          BIT            NULL,
    [Proxy]           BIT            NULL,
    [Hosting]         BIT            NULL,
    CONSTRAINT [PK_IPAddressInfo] PRIMARY KEY CLUSTERED ([IPAddressInfoId] ASC)
);



