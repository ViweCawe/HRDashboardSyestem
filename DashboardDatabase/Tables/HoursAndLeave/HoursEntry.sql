CREATE TABLE [hr].[HoursEntry]
(
    [HoursEntryId] bigint IDENTITY(1,1) NOT NULL,
    [EmploymentId] bigint NOT NULL,
    [ReportingPeriodId] int NOT NULL,
    [HoursCategoryId] int NOT NULL,
    [WorkDate] date NULL,
    [MinutesQuantity] int NOT NULL,
    [SourceBatchId] bigint NOT NULL,
    CONSTRAINT [PK_HoursEntry] PRIMARY KEY ([HoursEntryId]),
    CONSTRAINT [CK_HoursEntry_0] CHECK ([MinutesQuantity] >= 0),
    CONSTRAINT [FK_HoursEntry_1] FOREIGN KEY ([EmploymentId]) REFERENCES [hr].[Employment] ([EmploymentId]),
    CONSTRAINT [FK_HoursEntry_2] FOREIGN KEY ([ReportingPeriodId]) REFERENCES [hr].[ReportingPeriod] ([ReportingPeriodId]),
    CONSTRAINT [FK_HoursEntry_3] FOREIGN KEY ([HoursCategoryId]) REFERENCES [hr].[HoursCategory] ([HoursCategoryId]),
    CONSTRAINT [FK_HoursEntry_4] FOREIGN KEY ([SourceBatchId]) REFERENCES [hr].[ImportBatch] ([ImportBatchId])
);

GO
CREATE UNIQUE INDEX [UX_HoursEntry_Dated] ON [hr].[HoursEntry] ([EmploymentId], [ReportingPeriodId], [HoursCategoryId], [WorkDate]) WHERE [WorkDate] IS NOT NULL;
GO
CREATE UNIQUE INDEX [UX_HoursEntry_Period] ON [hr].[HoursEntry] ([EmploymentId], [ReportingPeriodId], [HoursCategoryId]) WHERE [WorkDate] IS NULL;
