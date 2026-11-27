-- Run only in the Identity (DefaultConnection) database after its migrations.
-- Set an existing confirmed account's ID. This script does not register or confirm users.
DECLARE @UserId nvarchar(450) = N'';
DECLARE @RoleName nvarchar(256) = N'HRAdmin'; -- HRAdmin or HRReader
IF @RoleName NOT IN (N'HRAdmin', N'HRReader') THROW 50001, 'Choose HRAdmin or HRReader.', 1;
IF NOT EXISTS (SELECT 1 FROM dbo.AspNetUsers WHERE Id = @UserId AND EmailConfirmed = 1)
    THROW 50001, 'Supply the ID of an existing confirmed user.', 1;
SET XACT_ABORT ON;
BEGIN TRANSACTION;
DECLARE @RoleId nvarchar(450);
SELECT @RoleId = Id FROM dbo.AspNetRoles WITH (UPDLOCK, HOLDLOCK) WHERE NormalizedName = UPPER(@RoleName);
IF @RoleId IS NULL
BEGIN
    SET @RoleId = CONVERT(nvarchar(450), NEWID());
    INSERT dbo.AspNetRoles (Id, Name, NormalizedName, ConcurrencyStamp)
    VALUES (@RoleId, @RoleName, UPPER(@RoleName), CONVERT(nvarchar(36), NEWID()));
END;
IF NOT EXISTS (SELECT 1 FROM dbo.AspNetUserRoles WHERE UserId = @UserId AND RoleId = @RoleId)
    INSERT dbo.AspNetUserRoles (UserId, RoleId) VALUES (@UserId, @RoleId);
UPDATE dbo.AspNetUsers SET SecurityStamp = CONVERT(nvarchar(36), NEWID()) WHERE Id = @UserId;
COMMIT;
-- Sign out and sign in again to refresh role claims.
