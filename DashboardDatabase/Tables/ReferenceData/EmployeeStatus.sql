CREATE TABLE [hr].[EmployeeStatus]
(
    [EmployeeStatusId] int IDENTITY(1,1) NOT NULL,
    [Code] nvarchar(30) NOT NULL,
    [Name] nvarchar(200) NOT NULL,
    CONSTRAINT [PK_EmployeeStatus] PRIMARY KEY ([EmployeeStatusId]),
    CONSTRAINT [UQ_EmployeeStatus_0] UNIQUE ([Code])
);
