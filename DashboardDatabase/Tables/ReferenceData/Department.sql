CREATE TABLE [hr].[Department]
(
    [DepartmentId] int IDENTITY(1,1) NOT NULL,
    [CompanyId] int NOT NULL,
    [Code] nvarchar(30) NOT NULL,
    [Name] nvarchar(200) NOT NULL,
    CONSTRAINT [PK_Department] PRIMARY KEY ([DepartmentId]),
    CONSTRAINT [UQ_Department_0] UNIQUE ([CompanyId], [Code]),
    CONSTRAINT [FK_Department_1] FOREIGN KEY ([CompanyId]) REFERENCES [hr].[Company] ([CompanyId])
);
