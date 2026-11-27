CREATE TABLE [hr].[EmploymentChange]
(
    [EmploymentChangeId] bigint IDENTITY(1,1) NOT NULL,
    [EmploymentId] bigint NOT NULL,
    [PreviousSnapshotId] bigint NOT NULL,
    [NewSnapshotId] bigint NOT NULL,
    [ChangedField] nvarchar(100) NOT NULL,
    [OldValue] nvarchar(500) NULL,
    [NewValue] nvarchar(500) NULL,
    [ChangeType] nvarchar(40) NOT NULL,
    [EffectiveDate] date NULL,
    [DetectedOn] datetime2 NOT NULL,
    [IsConfirmed] bit NOT NULL,
    [SourceBatchId] bigint NOT NULL,
    CONSTRAINT [PK_EmploymentChange] PRIMARY KEY ([EmploymentChangeId]),
    CONSTRAINT [UQ_EmploymentChange_0] UNIQUE ([NewSnapshotId], [ChangedField]),
    CONSTRAINT [FK_EmploymentChange_1] FOREIGN KEY ([EmploymentId]) REFERENCES [hr].[Employment] ([EmploymentId]),
    CONSTRAINT [FK_EmploymentChange_2] FOREIGN KEY ([PreviousSnapshotId]) REFERENCES [hr].[EmployeeSnapshot] ([SnapshotId]),
    CONSTRAINT [FK_EmploymentChange_3] FOREIGN KEY ([NewSnapshotId]) REFERENCES [hr].[EmployeeSnapshot] ([SnapshotId]),
    CONSTRAINT [FK_EmploymentChange_4] FOREIGN KEY ([SourceBatchId]) REFERENCES [hr].[ImportBatch] ([ImportBatchId])
);

GO
CREATE INDEX [IX_EmploymentChange_EmploymentId] ON [hr].[EmploymentChange] ([EmploymentId]);
