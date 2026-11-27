CREATE TABLE [hr].[ReportingPeriod]
(
    [ReportingPeriodId] int IDENTITY(1,1) NOT NULL,
    [StartDate] date NOT NULL,
    [EndDate] date NOT NULL,
    [Grain] nvarchar(20) NOT NULL,
    CONSTRAINT [PK_ReportingPeriod] PRIMARY KEY ([ReportingPeriodId]),
    CONSTRAINT [UQ_ReportingPeriod_0] UNIQUE ([StartDate], [EndDate], [Grain]),
    CONSTRAINT [CK_ReportingPeriod_1] CHECK ([EndDate] >= [StartDate])
);
