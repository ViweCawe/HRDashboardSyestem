CREATE TABLE [hr].[JobTitle]
(
    [JobTitleId] int IDENTITY(1,1) NOT NULL,
    [CompanyId] int NOT NULL,
    [Code] nvarchar(30) NOT NULL,
    [Name] nvarchar(200) NOT NULL,
    CONSTRAINT [PK_JobTitle] PRIMARY KEY ([JobTitleId]),
    CONSTRAINT [UQ_JobTitle_0] UNIQUE ([CompanyId], [Code]),
    CONSTRAINT [FK_JobTitle_1] FOREIGN KEY ([CompanyId]) REFERENCES [hr].[Company] ([CompanyId])
);
