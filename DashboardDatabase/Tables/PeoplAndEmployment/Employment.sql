CREATE TABLE [hr].[Employment]
(
    [EmploymentId] bigint IDENTITY(1,1) NOT NULL,
    [PersonId] bigint NOT NULL,
    [CompanyId] int NOT NULL,
    [EmployeeCode] nvarchar(30) NOT NULL,
    [DateEngaged] date NOT NULL,
    [TerminationDate] date NULL,
    [PlannedEndDate] date NULL,
    [TerminationReasonId] int NULL,
    [SourceEntityCode] nvarchar(50) NULL,
    [CreatedBatchId] bigint NOT NULL,
    CONSTRAINT [PK_Employment] PRIMARY KEY ([EmploymentId]),
    CONSTRAINT [UQ_Employment_0] UNIQUE ([CompanyId], [EmployeeCode], [DateEngaged]),
    CONSTRAINT [CK_Employment_1] CHECK ([TerminationDate] IS NULL OR [TerminationDate] >= [DateEngaged]),
    CONSTRAINT [CK_Employment_PlannedEndDate] CHECK ([PlannedEndDate] IS NULL OR [PlannedEndDate] >= [DateEngaged]),
    CONSTRAINT [FK_Employment_2] FOREIGN KEY ([PersonId]) REFERENCES [hr].[Person] ([PersonId]),
    CONSTRAINT [FK_Employment_3] FOREIGN KEY ([CompanyId]) REFERENCES [hr].[Company] ([CompanyId]),
    CONSTRAINT [FK_Employment_4] FOREIGN KEY ([TerminationReasonId]) REFERENCES [hr].[TerminationReason] ([TerminationReasonId]),
    CONSTRAINT [FK_Employment_5] FOREIGN KEY ([CreatedBatchId]) REFERENCES [hr].[ImportBatch] ([ImportBatchId])
);

GO
CREATE INDEX [IX_Employment_PersonId] ON [hr].[Employment] ([PersonId]);
