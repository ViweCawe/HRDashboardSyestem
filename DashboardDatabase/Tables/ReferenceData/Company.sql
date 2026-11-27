CREATE TABLE [hr].[Company]
(
    [CompanyId] int IDENTITY(1,1) NOT NULL,
    [Code] nvarchar(30) NOT NULL,
    [Name] nvarchar(200) NOT NULL,
    CONSTRAINT [PK_Company] PRIMARY KEY ([CompanyId]),
    CONSTRAINT [UQ_Company_0] UNIQUE ([Code])
);
