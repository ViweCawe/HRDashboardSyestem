CREATE TABLE [hr].[LeaveBalance]
(
    [LeaveBalanceId] bigint IDENTITY(1,1) NOT NULL,
    [EmploymentId] bigint NOT NULL,
    [ReportingPeriodId] int NOT NULL,
    [LeaveTypeId] int NOT NULL,
    [Entitlement] decimal(18,4) NOT NULL,
    [BalanceBroughtForward] decimal(18,4) NOT NULL,
    [AccruedThisPeriod] decimal(18,4) NOT NULL,
    [Taken] decimal(18,4) NOT NULL,
    [BalanceCarriedForward] decimal(18,4) NOT NULL,
    [TakenBasis] nvarchar(20) NOT NULL,
    [SourceBatchId] bigint NOT NULL,
    CONSTRAINT [PK_LeaveBalance] PRIMARY KEY ([LeaveBalanceId]),
    CONSTRAINT [UQ_LeaveBalance_0] UNIQUE ([EmploymentId], [ReportingPeriodId], [LeaveTypeId]),
    CONSTRAINT [CK_LeaveBalance_1] CHECK ([TakenBasis] IN ('Period','YTD','Cycle','Unknown')),
    CONSTRAINT [FK_LeaveBalance_2] FOREIGN KEY ([EmploymentId]) REFERENCES [hr].[Employment] ([EmploymentId]),
    CONSTRAINT [FK_LeaveBalance_3] FOREIGN KEY ([ReportingPeriodId]) REFERENCES [hr].[ReportingPeriod] ([ReportingPeriodId]),
    CONSTRAINT [FK_LeaveBalance_4] FOREIGN KEY ([LeaveTypeId]) REFERENCES [hr].[LeaveType] ([LeaveTypeId]),
    CONSTRAINT [FK_LeaveBalance_5] FOREIGN KEY ([SourceBatchId]) REFERENCES [hr].[ImportBatch] ([ImportBatchId])
);
