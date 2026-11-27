CREATE TABLE [hr].[RemunerationDefinition]
(
    [RemunerationDefinitionId] int IDENTITY(1,1) NOT NULL,
    [CompanyId] int NOT NULL,
    [Code] nvarchar(50) NOT NULL,
    [Name] nvarchar(200) NOT NULL,
    CONSTRAINT [PK_RemunerationDefinition] PRIMARY KEY ([RemunerationDefinitionId]),
    CONSTRAINT [UQ_RemunerationDefinition_0] UNIQUE ([CompanyId], [Code]),
    CONSTRAINT [FK_RemunerationDefinition_1] FOREIGN KEY ([CompanyId]) REFERENCES [hr].[Company] ([CompanyId])
);
