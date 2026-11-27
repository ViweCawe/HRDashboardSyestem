CREATE TABLE [hr].[Position]
(
    [PositionId] int IDENTITY(1,1) NOT NULL,
    [CompanyId] int NOT NULL,
    [Code] nvarchar(30) NOT NULL,
    [Name] nvarchar(200) NOT NULL,
    CONSTRAINT [PK_Position] PRIMARY KEY ([PositionId]),
    CONSTRAINT [UQ_Position_0] UNIQUE ([CompanyId], [Code]),
    CONSTRAINT [FK_Position_1] FOREIGN KEY ([CompanyId]) REFERENCES [hr].[Company] ([CompanyId])
);
