CREATE TABLE [hr].[HoursCategory]
(
    [HoursCategoryId] int IDENTITY(1,1) NOT NULL,
    [Code] nvarchar(30) NOT NULL,
    [Name] nvarchar(100) NOT NULL,
    [QuantityBasis] nvarchar(30) NOT NULL,
    CONSTRAINT [PK_HoursCategory] PRIMARY KEY ([HoursCategoryId]),
    CONSTRAINT [UQ_HoursCategory_0] UNIQUE ([Code])
);
