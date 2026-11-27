CREATE PROCEDURE [hr].[spEmployeeImport_Publish] @ImportBatchId bigint
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    SET TRANSACTION ISOLATION LEVEL SERIALIZABLE;
    BEGIN TRY
        BEGIN TRANSACTION;
        DECLARE @AsOfDate date, @Status nvarchar(30);
        SELECT @AsOfDate = SourceAsOfDate, @Status = Status
        FROM hr.ImportBatch WITH (UPDLOCK, HOLDLOCK)
        WHERE ImportBatchId = @ImportBatchId AND SourceType = 'Employees';
        IF @Status IS NULL THROW 50001, 'Employee import batch not found.', 1;
        IF @Status = 'Published' BEGIN COMMIT; RETURN; END;
        IF @Status <> 'Staged' OR EXISTS (SELECT 1 FROM hr.ImportIssue WHERE ImportBatchId = @ImportBatchId AND ResolvedOn IS NULL AND Severity = 'Error')
            THROW 50001, 'Resolve the source validation errors and stage a corrected file before publication.', 1;
        IF EXISTS (SELECT 1 FROM hr.EmployeeImportRow WHERE ImportBatchId = @ImportBatchId AND ValidationStatus <> 'Valid')
            THROW 50001, 'This batch contains invalid source rows.', 1;
        IF NOT EXISTS (SELECT 1 FROM hr.EmployeeImportRow WHERE ImportBatchId = @ImportBatchId)
            THROW 50001, 'This batch has no employee rows.', 1;
        DECLARE @RowId bigint, @Json nvarchar(max), @PersonId bigint, @EmploymentId bigint, @PreviousSnapshotId bigint, @SnapshotId bigint;
        DECLARE @EmployeeCode nvarchar(30);
        DECLARE @NationalId nvarchar(30);
        DECLARE @PassportNumber nvarchar(50);
        DECLARE @PassportCountryCode nvarchar(2);
        DECLARE @FirstName nvarchar(100);
        DECLARE @LastName nvarchar(100);
        DECLARE @DisplayName nvarchar(200);
        DECLARE @SourceEntityCode nvarchar(50);
        DECLARE @SourceReportsToCode nvarchar(30);
        DECLARE @GenderCode nvarchar(30);
        DECLARE @RacialGroupCode nvarchar(30);
        DECLARE @Disability nvarchar(100);
        DECLARE @CompanyCode nvarchar(30);
        DECLARE @CompanyName nvarchar(200);
        DECLARE @DepartmentCode nvarchar(30);
        DECLARE @DepartmentName nvarchar(200);
        DECLARE @CostCentreCode nvarchar(30);
        DECLARE @CostCentreName nvarchar(200);
        DECLARE @JobTitleCode nvarchar(30);
        DECLARE @JobTitleName nvarchar(200);
        DECLARE @PositionCode nvarchar(30);
        DECLARE @PositionName nvarchar(200);
        DECLARE @EmployeeStatusCode nvarchar(30);
        DECLARE @EmployeeStatusName nvarchar(200);
        DECLARE @EmploymentTypeCode nvarchar(30);
        DECLARE @EmploymentTypeName nvarchar(200);
        DECLARE @CompanyRuleCode nvarchar(30);
        DECLARE @CompanyRuleName nvarchar(200);
        DECLARE @RemunerationDefinitionCode nvarchar(50);
        DECLARE @RemunerationDefinitionName nvarchar(200);
        DECLARE @TerminationReasonCode nvarchar(30);
        DECLARE @TerminationReasonName nvarchar(200);
        DECLARE @DateEngaged date;
        DECLARE @TerminationDate date;
        DECLARE @PlannedEndDate date;
        DECLARE @IdentityExempt bit;
        DECLARE @DateOfBirth date;
        DECLARE @TransferDate date;
        DECLARE @CompanyId int;
        DECLARE @DepartmentId int;
        DECLARE @CostCentreId int;
        DECLARE @JobTitleId int;
        DECLARE @PositionId int;
        DECLARE @EmployeeStatusId int;
        DECLARE @EmploymentTypeId int;
        DECLARE @CompanyRuleId int;
        DECLARE @RemunerationDefinitionId int;
        DECLARE @TerminationReasonId int;
        DECLARE rows_cursor CURSOR LOCAL FAST_FORWARD FOR
            SELECT StagingRowId, NormalizedDataJson FROM hr.EmployeeImportRow WHERE ImportBatchId = @ImportBatchId ORDER BY TRY_CONVERT(date, JSON_VALUE(NormalizedDataJson, '$.DateEngaged')), SourceRowNumber;
        OPEN rows_cursor;
        FETCH NEXT FROM rows_cursor INTO @RowId, @Json;
        WHILE @@FETCH_STATUS = 0
        BEGIN
            SELECT @EmployeeCode = [EmployeeCode],
                @NationalId = [NationalId],
                @PassportNumber = [PassportNumber],
                @PassportCountryCode = [PassportCountryCode],
                @FirstName = [FirstName],
                @LastName = [LastName],
                @DisplayName = [DisplayName],
                @SourceEntityCode = [SourceEntityCode],
                @SourceReportsToCode = [SourceReportsToCode],
                @GenderCode = [GenderCode],
                @RacialGroupCode = [RacialGroupCode],
                @Disability = [Disability],
                @CompanyCode = [CompanyCode],
                @CompanyName = [CompanyName],
                @DepartmentCode = [DepartmentCode],
                @DepartmentName = [DepartmentName],
                @CostCentreCode = [CostCentreCode],
                @CostCentreName = [CostCentreName],
                @JobTitleCode = [JobTitleCode],
                @JobTitleName = [JobTitleName],
                @PositionCode = [PositionCode],
                @PositionName = [PositionName],
                @EmployeeStatusCode = [EmployeeStatusCode],
                @EmployeeStatusName = [EmployeeStatusName],
                @EmploymentTypeCode = [EmploymentTypeCode],
                @EmploymentTypeName = [EmploymentTypeName],
                @CompanyRuleCode = [CompanyRuleCode],
                @CompanyRuleName = [CompanyRuleName],
                @RemunerationDefinitionCode = [RemunerationDefinitionCode],
                @RemunerationDefinitionName = [RemunerationDefinitionName],
                @TerminationReasonCode = [TerminationReasonCode],
                @TerminationReasonName = [TerminationReasonName],
                @DateEngaged = [DateEngaged], @TerminationDate = [TerminationDate], @PlannedEndDate = [PlannedEndDate], @DateOfBirth = [DateOfBirth], @TransferDate = [TransferDate]
            FROM OPENJSON(@Json) WITH ([EmployeeCode] nvarchar(30),
                [NationalId] nvarchar(30),
                [PassportNumber] nvarchar(50),
                [PassportCountryCode] nvarchar(2),
                [FirstName] nvarchar(100),
                [LastName] nvarchar(100),
                [DisplayName] nvarchar(200),
                [SourceEntityCode] nvarchar(50),
                [SourceReportsToCode] nvarchar(30),
                [GenderCode] nvarchar(30),
                [RacialGroupCode] nvarchar(30),
                [Disability] nvarchar(100),
                [CompanyCode] nvarchar(30),
                [CompanyName] nvarchar(200),
                [DepartmentCode] nvarchar(30),
                [DepartmentName] nvarchar(200),
                [CostCentreCode] nvarchar(30),
                [CostCentreName] nvarchar(200),
                [JobTitleCode] nvarchar(30),
                [JobTitleName] nvarchar(200),
                [PositionCode] nvarchar(30),
                [PositionName] nvarchar(200),
                [EmployeeStatusCode] nvarchar(30),
                [EmployeeStatusName] nvarchar(200),
                [EmploymentTypeCode] nvarchar(30),
                [EmploymentTypeName] nvarchar(200),
                [CompanyRuleCode] nvarchar(30),
                [CompanyRuleName] nvarchar(200),
                [RemunerationDefinitionCode] nvarchar(50),
                [RemunerationDefinitionName] nvarchar(200),
                [TerminationReasonCode] nvarchar(30),
                [TerminationReasonName] nvarchar(200),
                [DateEngaged] date,
                [TerminationDate] date,
                [PlannedEndDate] date,
                [DateOfBirth] date,
                [TransferDate] date);
            IF @EmployeeStatusCode = 'A' OR @EmployeeStatusName = 'Active'
            BEGIN
                SET @PlannedEndDate = COALESCE(@PlannedEndDate, @TerminationDate);
                SET @TerminationDate = NULL;
            END;
            SET @IdentityExempt = CASE WHEN UPPER(@CompanyRuleCode) = 'ESS_APPR'
                OR (@CompanyRuleCode = '4' AND UPPER(LTRIM(RTRIM(LEFT(@CompanyRuleName, CHARINDEX(' - ', @CompanyRuleName + ' - ') - 1)))) = 'ESS_APPR') THEN 1 ELSE 0 END;
            IF @NationalId IS NULL AND (@PassportNumber IS NULL OR @PassportCountryCode IS NULL)
                AND NOT (@IdentityExempt = 1 AND @PassportNumber IS NULL)
                THROW 50001, 'A row has no reliable person identifier.', 1;
            IF @EmployeeCode IS NULL OR @CompanyCode IS NULL OR @EmployeeStatusCode IS NULL OR @DateEngaged IS NULL OR @FirstName IS NULL OR @LastName IS NULL
                THROW 50001, 'A row is missing required employee fields.', 1;
            SET @PersonId = NULL;
            IF (SELECT COUNT(*) FROM hr.Person WITH (UPDLOCK, HOLDLOCK)
                WHERE (@NationalId IS NOT NULL AND NationalId = @NationalId)
                   OR (@PassportNumber IS NOT NULL AND PassportNumber = @PassportNumber AND PassportCountryCode = @PassportCountryCode)) > 1
                THROW 50001, 'ID and passport match different people; HR must review the identity.', 1;
            SELECT @PersonId = PersonId FROM hr.Person WITH (UPDLOCK, HOLDLOCK)
            WHERE (@NationalId IS NOT NULL AND NationalId = @NationalId)
               OR (@PassportNumber IS NOT NULL AND PassportNumber = @PassportNumber AND PassportCountryCode = @PassportCountryCode);
            -- HR-approved ESS approvers may have no identity document. Reuse only
            -- their exact company/code/engagement binding; never match on a name
            -- or carry an undocumented identity across a new/recycled engagement.
            IF @PersonId IS NULL AND @IdentityExempt = 1
                SELECT @PersonId = e.PersonId FROM hr.Employment e WITH (UPDLOCK, HOLDLOCK)
                JOIN hr.Company c ON c.CompanyId = e.CompanyId
                WHERE c.Code = @CompanyCode AND e.EmployeeCode = @EmployeeCode AND e.DateEngaged = @DateEngaged;
            IF @PersonId IS NULL
            BEGIN
                INSERT hr.Person (NationalId, PassportNumber, PassportCountryCode, FirstName, LastName, DisplayName, DateOfBirth)
                VALUES (@NationalId, @PassportNumber, @PassportCountryCode, @FirstName, @LastName, @DisplayName, @DateOfBirth);
                SET @PersonId = SCOPE_IDENTITY();
            END
            ELSE
            BEGIN
                IF EXISTS (SELECT 1 FROM hr.Person WHERE PersonId = @PersonId AND
                   ((NationalId IS NOT NULL AND @NationalId IS NOT NULL AND NationalId <> @NationalId)
                    OR (PassportNumber IS NOT NULL AND @PassportNumber IS NOT NULL AND (PassportNumber <> @PassportNumber OR PassportCountryCode <> @PassportCountryCode))))
                    THROW 50001, 'A person identifier conflicts with an existing record; HR must review it.', 1;
                UPDATE hr.Person SET FirstName = @FirstName, LastName = @LastName, DisplayName = @DisplayName,
                    NationalId = COALESCE(NationalId, @NationalId), PassportNumber = COALESCE(PassportNumber, @PassportNumber),
                    PassportCountryCode = COALESCE(PassportCountryCode, @PassportCountryCode), DateOfBirth = COALESCE(@DateOfBirth, DateOfBirth)
                WHERE PersonId = @PersonId;
            END;
            SET @CompanyId = NULL;
            IF @CompanyCode IS NOT NULL
            BEGIN
                SELECT @CompanyId = CompanyId FROM hr.Company WITH (UPDLOCK, HOLDLOCK) WHERE Code = @CompanyCode;
                IF @CompanyId IS NULL
                BEGIN
                    INSERT hr.Company (Code, Name) VALUES (@CompanyCode, COALESCE(@CompanyName, @CompanyCode));
                    SET @CompanyId = SCOPE_IDENTITY();
                END
                ELSE IF @CompanyName IS NOT NULL
                    UPDATE hr.Company SET Name = @CompanyName WHERE CompanyId = @CompanyId AND Name <> @CompanyName;
            END;
            SET @DepartmentId = NULL;
            IF @DepartmentCode IS NOT NULL
            BEGIN
                SELECT @DepartmentId = DepartmentId FROM hr.Department WITH (UPDLOCK, HOLDLOCK) WHERE Code = @DepartmentCode AND CompanyId = @CompanyId;
                IF @DepartmentId IS NULL
                BEGIN
                    INSERT hr.Department (CompanyId, Code, Name) VALUES (@CompanyId, @DepartmentCode, COALESCE(@DepartmentName, @DepartmentCode));
                    SET @DepartmentId = SCOPE_IDENTITY();
                END
                ELSE IF @DepartmentName IS NOT NULL
                    UPDATE hr.Department SET Name = @DepartmentName WHERE DepartmentId = @DepartmentId AND Name <> @DepartmentName;
            END;
            SET @CostCentreId = NULL;
            IF @CostCentreCode IS NOT NULL
            BEGIN
                SELECT @CostCentreId = CostCentreId FROM hr.CostCentre WITH (UPDLOCK, HOLDLOCK) WHERE Code = @CostCentreCode AND CompanyId = @CompanyId;
                IF @CostCentreId IS NULL
                BEGIN
                    INSERT hr.CostCentre (CompanyId, Code, Name) VALUES (@CompanyId, @CostCentreCode, COALESCE(@CostCentreName, @CostCentreCode));
                    SET @CostCentreId = SCOPE_IDENTITY();
                END
                ELSE IF @CostCentreName IS NOT NULL
                    UPDATE hr.CostCentre SET Name = @CostCentreName WHERE CostCentreId = @CostCentreId AND Name <> @CostCentreName;
            END;
            SET @JobTitleId = NULL;
            IF @JobTitleCode IS NOT NULL
            BEGIN
                SELECT @JobTitleId = JobTitleId FROM hr.JobTitle WITH (UPDLOCK, HOLDLOCK) WHERE Code = @JobTitleCode AND CompanyId = @CompanyId;
                IF @JobTitleId IS NULL
                BEGIN
                    INSERT hr.JobTitle (CompanyId, Code, Name) VALUES (@CompanyId, @JobTitleCode, COALESCE(@JobTitleName, @JobTitleCode));
                    SET @JobTitleId = SCOPE_IDENTITY();
                END
                ELSE IF @JobTitleName IS NOT NULL
                    UPDATE hr.JobTitle SET Name = @JobTitleName WHERE JobTitleId = @JobTitleId AND Name <> @JobTitleName;
            END;
            SET @PositionId = NULL;
            IF @PositionCode IS NOT NULL
            BEGIN
                SELECT @PositionId = PositionId FROM hr.Position WITH (UPDLOCK, HOLDLOCK) WHERE Code = @PositionCode AND CompanyId = @CompanyId;
                IF @PositionId IS NULL
                BEGIN
                    INSERT hr.Position (CompanyId, Code, Name) VALUES (@CompanyId, @PositionCode, COALESCE(@PositionName, @PositionCode));
                    SET @PositionId = SCOPE_IDENTITY();
                END
                ELSE IF @PositionName IS NOT NULL
                    UPDATE hr.Position SET Name = @PositionName WHERE PositionId = @PositionId AND Name <> @PositionName;
            END;
            SET @EmployeeStatusId = NULL;
            IF @EmployeeStatusCode IS NOT NULL
            BEGIN
                SELECT @EmployeeStatusId = EmployeeStatusId FROM hr.EmployeeStatus WITH (UPDLOCK, HOLDLOCK) WHERE Code = @EmployeeStatusCode;
                IF @EmployeeStatusId IS NULL
                BEGIN
                    INSERT hr.EmployeeStatus (Code, Name) VALUES (@EmployeeStatusCode, COALESCE(@EmployeeStatusName, @EmployeeStatusCode));
                    SET @EmployeeStatusId = SCOPE_IDENTITY();
                END
                ELSE IF @EmployeeStatusName IS NOT NULL
                    UPDATE hr.EmployeeStatus SET Name = @EmployeeStatusName WHERE EmployeeStatusId = @EmployeeStatusId AND Name <> @EmployeeStatusName;
            END;
            SET @EmploymentTypeId = NULL;
            IF @EmploymentTypeCode IS NOT NULL
            BEGIN
                SELECT @EmploymentTypeId = EmploymentTypeId FROM hr.EmploymentType WITH (UPDLOCK, HOLDLOCK) WHERE Code = @EmploymentTypeCode;
                IF @EmploymentTypeId IS NULL
                BEGIN
                    INSERT hr.EmploymentType (Code, Name) VALUES (@EmploymentTypeCode, COALESCE(@EmploymentTypeName, @EmploymentTypeCode));
                    SET @EmploymentTypeId = SCOPE_IDENTITY();
                END
                ELSE IF @EmploymentTypeName IS NOT NULL
                    UPDATE hr.EmploymentType SET Name = @EmploymentTypeName WHERE EmploymentTypeId = @EmploymentTypeId AND Name <> @EmploymentTypeName;
            END;
            SET @CompanyRuleId = NULL;
            IF @CompanyRuleCode IS NOT NULL
            BEGIN
                SELECT @CompanyRuleId = CompanyRuleId FROM hr.CompanyRule WITH (UPDLOCK, HOLDLOCK) WHERE Code = @CompanyRuleCode AND CompanyId = @CompanyId;
                IF @CompanyRuleId IS NULL
                BEGIN
                    INSERT hr.CompanyRule (CompanyId, Code, Name) VALUES (@CompanyId, @CompanyRuleCode, COALESCE(@CompanyRuleName, @CompanyRuleCode));
                    SET @CompanyRuleId = SCOPE_IDENTITY();
                END
                ELSE IF @CompanyRuleName IS NOT NULL
                    UPDATE hr.CompanyRule SET Name = @CompanyRuleName WHERE CompanyRuleId = @CompanyRuleId AND Name <> @CompanyRuleName;
            END;
            SET @RemunerationDefinitionId = NULL;
            IF @RemunerationDefinitionCode IS NOT NULL
            BEGIN
                SELECT @RemunerationDefinitionId = RemunerationDefinitionId FROM hr.RemunerationDefinition WITH (UPDLOCK, HOLDLOCK) WHERE Code = @RemunerationDefinitionCode AND CompanyId = @CompanyId;
                IF @RemunerationDefinitionId IS NULL
                BEGIN
                    INSERT hr.RemunerationDefinition (CompanyId, Code, Name) VALUES (@CompanyId, @RemunerationDefinitionCode, COALESCE(@RemunerationDefinitionName, @RemunerationDefinitionCode));
                    SET @RemunerationDefinitionId = SCOPE_IDENTITY();
                END
                ELSE IF @RemunerationDefinitionName IS NOT NULL
                    UPDATE hr.RemunerationDefinition SET Name = @RemunerationDefinitionName WHERE RemunerationDefinitionId = @RemunerationDefinitionId AND Name <> @RemunerationDefinitionName;
            END;
            SET @TerminationReasonId = NULL;
            IF @TerminationReasonCode IS NOT NULL
            BEGIN
                SELECT @TerminationReasonId = TerminationReasonId FROM hr.TerminationReason WITH (UPDLOCK, HOLDLOCK) WHERE Code = @TerminationReasonCode;
                IF @TerminationReasonId IS NULL
                BEGIN
                    INSERT hr.TerminationReason (Code, Name) VALUES (@TerminationReasonCode, COALESCE(@TerminationReasonName, @TerminationReasonCode));
                    SET @TerminationReasonId = SCOPE_IDENTITY();
                END
                ELSE IF @TerminationReasonName IS NOT NULL
                    UPDATE hr.TerminationReason SET Name = @TerminationReasonName WHERE TerminationReasonId = @TerminationReasonId AND Name <> @TerminationReasonName;
            END;
            SET @EmploymentId = NULL;
            SELECT @EmploymentId = EmploymentId FROM hr.Employment WITH (UPDLOCK, HOLDLOCK)
            WHERE CompanyId = @CompanyId AND EmployeeCode = @EmployeeCode AND DateEngaged = @DateEngaged;
            IF @EmploymentId IS NOT NULL AND EXISTS (SELECT 1 FROM hr.Employment WHERE EmploymentId = @EmploymentId AND PersonId <> @PersonId)
                THROW 50001, 'An engagement key belongs to another person; HR must review it.', 1;
            IF @EmploymentId IS NULL
            BEGIN
                IF EXISTS (SELECT 1 FROM hr.Employment WHERE CompanyId = @CompanyId AND EmployeeCode = @EmployeeCode AND PersonId = @PersonId)
                    THROW 50001, 'An existing employee code has a different engagement date; do not create a duplicate engagement.', 1;
                IF EXISTS (SELECT 1 FROM hr.Employment WHERE CompanyId = @CompanyId AND EmployeeCode = @EmployeeCode AND (TerminationDate IS NULL OR TerminationDate >= @DateEngaged))
                    THROW 50001, 'A recycled employee code overlaps an existing engagement; HR must review it.', 1;
                INSERT hr.Employment (PersonId, CompanyId, EmployeeCode, DateEngaged, TerminationDate, PlannedEndDate, TerminationReasonId, SourceEntityCode, CreatedBatchId)
                VALUES (@PersonId, @CompanyId, @EmployeeCode, @DateEngaged, @TerminationDate, @PlannedEndDate, @TerminationReasonId, @SourceEntityCode, @ImportBatchId);
                SET @EmploymentId = SCOPE_IDENTITY();
            END;
            IF EXISTS (SELECT 1 FROM hr.EmployeeSnapshot WHERE EmploymentId = @EmploymentId AND SnapshotDate > @AsOfDate)
                THROW 50001, 'An older export cannot overwrite newer employee history. Historical backfill requires review.', 1;
            -- A new file cannot silently replace an already published snapshot for the same date.
            IF EXISTS (SELECT 1 FROM hr.EmployeeSnapshot WHERE EmploymentId = @EmploymentId AND SnapshotDate = @AsOfDate)
                THROW 50001, 'This engagement already has a snapshot for that date. Use the original batch or review the correction.', 1;
            UPDATE hr.Employment SET TerminationDate = @TerminationDate, PlannedEndDate = @PlannedEndDate, TerminationReasonId = @TerminationReasonId,
                SourceEntityCode = COALESCE(@SourceEntityCode, SourceEntityCode) WHERE EmploymentId = @EmploymentId;
            SET @PreviousSnapshotId = NULL;
            SELECT TOP (1) @PreviousSnapshotId = SnapshotId FROM hr.EmployeeSnapshot
            WHERE EmploymentId = @EmploymentId AND SnapshotDate < @AsOfDate ORDER BY SnapshotDate DESC;
            INSERT hr.EmployeeSnapshot (EmploymentId, SnapshotDate, DepartmentId, CostCentreId, JobTitleId, PositionId, EmploymentTypeId, EmployeeStatusId, CompanyRuleId, RemunerationDefinitionId, SourceReportsToCode, GenderCode, RacialGroupCode, Disability, TerminationDate, PlannedEndDate, TerminationReasonId, SourceBatchId)
            VALUES (@EmploymentId, @AsOfDate, @DepartmentId, @CostCentreId, @JobTitleId, @PositionId, @EmploymentTypeId, @EmployeeStatusId, @CompanyRuleId, @RemunerationDefinitionId, @SourceReportsToCode, @GenderCode, @RacialGroupCode, @Disability, @TerminationDate, @PlannedEndDate, @TerminationReasonId, @ImportBatchId);
            SET @SnapshotId = SCOPE_IDENTITY();
            IF @PreviousSnapshotId IS NOT NULL
            BEGIN
                INSERT hr.EmploymentChange (EmploymentId, PreviousSnapshotId, NewSnapshotId, ChangedField, OldValue, NewValue, ChangeType, EffectiveDate, DetectedOn, IsConfirmed, SourceBatchId)
                SELECT @EmploymentId, @PreviousSnapshotId, @SnapshotId, v.ChangedField, v.OldValue, v.NewValue,
                    CASE WHEN v.ChangedField IN ('DepartmentId','CostCentreId') AND @TransferDate IS NOT NULL THEN 'Transfer' ELSE 'DetectedChange' END,
                    CASE WHEN v.ChangedField IN ('DepartmentId','CostCentreId') THEN @TransferDate END,
                    SYSUTCDATETIME(), CASE WHEN v.ChangedField IN ('DepartmentId','CostCentreId') AND @TransferDate IS NOT NULL THEN 1 ELSE 0 END, @ImportBatchId
                FROM hr.EmployeeSnapshot p CROSS JOIN hr.EmployeeSnapshot n
                CROSS APPLY (VALUES ('DepartmentId', CONVERT(nvarchar(500), p.DepartmentId), CONVERT(nvarchar(500), n.DepartmentId)),
                        ('CostCentreId', CONVERT(nvarchar(500), p.CostCentreId), CONVERT(nvarchar(500), n.CostCentreId)),
                        ('JobTitleId', CONVERT(nvarchar(500), p.JobTitleId), CONVERT(nvarchar(500), n.JobTitleId)),
                        ('PositionId', CONVERT(nvarchar(500), p.PositionId), CONVERT(nvarchar(500), n.PositionId)),
                        ('EmploymentTypeId', CONVERT(nvarchar(500), p.EmploymentTypeId), CONVERT(nvarchar(500), n.EmploymentTypeId)),
                        ('EmployeeStatusId', CONVERT(nvarchar(500), p.EmployeeStatusId), CONVERT(nvarchar(500), n.EmployeeStatusId)),
                        ('CompanyRuleId', CONVERT(nvarchar(500), p.CompanyRuleId), CONVERT(nvarchar(500), n.CompanyRuleId)),
                        ('RemunerationDefinitionId', CONVERT(nvarchar(500), p.RemunerationDefinitionId), CONVERT(nvarchar(500), n.RemunerationDefinitionId)),
                        ('SourceReportsToCode', CONVERT(nvarchar(500), p.SourceReportsToCode), CONVERT(nvarchar(500), n.SourceReportsToCode)),
                        ('GenderCode', CONVERT(nvarchar(500), p.GenderCode), CONVERT(nvarchar(500), n.GenderCode)),
                        ('RacialGroupCode', CONVERT(nvarchar(500), p.RacialGroupCode), CONVERT(nvarchar(500), n.RacialGroupCode)),
                        ('Disability', CONVERT(nvarchar(500), p.Disability), CONVERT(nvarchar(500), n.Disability)),
                        ('PlannedEndDate', CONVERT(nvarchar(500), p.PlannedEndDate, 23), CONVERT(nvarchar(500), n.PlannedEndDate, 23)),
                        ('TerminationDate', CONVERT(nvarchar(500), p.TerminationDate, 23), CONVERT(nvarchar(500), n.TerminationDate, 23)),
                        ('TerminationReasonId', CONVERT(nvarchar(500), p.TerminationReasonId), CONVERT(nvarchar(500), n.TerminationReasonId))) v(ChangedField, OldValue, NewValue)
                WHERE p.SnapshotId = @PreviousSnapshotId AND n.SnapshotId = @SnapshotId
                  AND ISNULL(v.OldValue, '') <> ISNULL(v.NewValue, '');
            END;
            UPDATE hr.EmployeeImportRow SET ResolvedEmploymentId = @EmploymentId, ValidationStatus = 'Published' WHERE StagingRowId = @RowId;
            FETCH NEXT FROM rows_cursor INTO @RowId, @Json;
        END;
        CLOSE rows_cursor;
        DEALLOCATE rows_cursor;
        -- Validate the final intervals too: clearing an old termination must not reactivate a recycled code.
        IF EXISTS (
            SELECT 1 FROM hr.Employment a JOIN hr.Employment b
              ON a.CompanyId = b.CompanyId AND a.EmployeeCode = b.EmployeeCode AND a.EmploymentId < b.EmploymentId
            WHERE (a.TerminationDate IS NULL OR a.TerminationDate >= b.DateEngaged)
              AND (b.TerminationDate IS NULL OR b.TerminationDate >= a.DateEngaged)
              AND EXISTS (SELECT 1 FROM hr.EmployeeImportRow r WHERE r.ImportBatchId = @ImportBatchId
                          AND r.ResolvedEmploymentId IN (a.EmploymentId, b.EmploymentId)))
            THROW 50001, 'Recycled employee-code engagements overlap after these changes; HR must review them.', 1;
        -- Resolve manager codes after all employees are present, and only if one dated engagement matches.
        UPDATE s SET ManagerEmploymentId = m.ManagerId
        FROM hr.EmployeeSnapshot s JOIN hr.Employment e ON e.EmploymentId = s.EmploymentId
        CROSS APPLY (SELECT MIN(me.EmploymentId) ManagerId, COUNT(*) Matches FROM hr.Employment me
            WHERE me.CompanyId = e.CompanyId AND me.EmployeeCode = s.SourceReportsToCode
              AND me.DateEngaged <= s.SnapshotDate AND (me.TerminationDate IS NULL OR me.TerminationDate >= s.SnapshotDate)
              AND me.EmploymentId <> s.EmploymentId) m
        WHERE s.SourceBatchId = @ImportBatchId AND m.Matches = 1;
        UPDATE hr.ImportBatch SET Status = 'Published' WHERE ImportBatchId = @ImportBatchId;
        COMMIT;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK;
        THROW;
    END CATCH;
END;
