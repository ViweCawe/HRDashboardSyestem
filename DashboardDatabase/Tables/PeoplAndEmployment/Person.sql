CREATE TABLE [hr].[Person]
(
    [PersonId] bigint IDENTITY(1,1) NOT NULL,
    [NationalId] nvarchar(30) NULL,
    [PassportNumber] nvarchar(50) NULL,
    [PassportCountryCode] char(2) NULL,
    [FirstName] nvarchar(100) NOT NULL,
    [LastName] nvarchar(100) NOT NULL,
    [DisplayName] nvarchar(200) NULL,
    [DateOfBirth] date NULL,
    CONSTRAINT [PK_Person] PRIMARY KEY ([PersonId]),
    CONSTRAINT [CK_Person_0] CHECK ([PassportNumber] IS NULL OR [PassportCountryCode] IS NOT NULL)
);

GO
CREATE UNIQUE INDEX [UX_Person_NationalId] ON [hr].[Person] ([NationalId]) WHERE [NationalId] IS NOT NULL;
GO
CREATE UNIQUE INDEX [UX_Person_Passport] ON [hr].[Person] ([PassportCountryCode], [PassportNumber]) WHERE [PassportNumber] IS NOT NULL;
