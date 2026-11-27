CREATE TABLE [hr].[LeaveType]
(
    [LeaveTypeId] int IDENTITY(1,1) NOT NULL,
    [Code] nvarchar(30) NOT NULL,
    [Name] nvarchar(100) NOT NULL,
    [UnitCode] nvarchar(20) NOT NULL,
    CONSTRAINT [PK_LeaveType] PRIMARY KEY ([LeaveTypeId]),
    CONSTRAINT [UQ_LeaveType_0] UNIQUE ([Code])
);
