CREATE TABLE [hr].[CompanyRule]
(
    [CompanyRuleId] int IDENTITY(1,1) NOT NULL,
    [CompanyId] int NOT NULL,
    [Code] nvarchar(30) NOT NULL,
    [Name] nvarchar(200) NOT NULL,
    CONSTRAINT [PK_CompanyRule] PRIMARY KEY ([CompanyRuleId]),
    CONSTRAINT [UQ_CompanyRule_0] UNIQUE ([CompanyId], [Code]),
    CONSTRAINT [FK_CompanyRule_1] FOREIGN KEY ([CompanyId]) REFERENCES [hr].[Company] ([CompanyId])
);
