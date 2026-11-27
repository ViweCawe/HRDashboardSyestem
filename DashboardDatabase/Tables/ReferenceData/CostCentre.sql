CREATE TABLE [hr].[CostCentre]
(
    [CostCentreId] int IDENTITY(1,1) NOT NULL,
    [CompanyId] int NOT NULL,
    [Code] nvarchar(30) NOT NULL,
    [Name] nvarchar(200) NOT NULL,
    CONSTRAINT [PK_CostCentre] PRIMARY KEY ([CostCentreId]),
    CONSTRAINT [UQ_CostCentre_0] UNIQUE ([CompanyId], [Code]),
    CONSTRAINT [FK_CostCentre_1] FOREIGN KEY ([CompanyId]) REFERENCES [hr].[Company] ([CompanyId])
);
