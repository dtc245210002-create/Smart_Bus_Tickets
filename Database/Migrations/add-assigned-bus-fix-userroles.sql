-- =========================================================================
-- MIGRATION: Thêm cột AssignedBusId cho Driver & Phân công xe cố định
-- =========================================================================

-- BƯỚC 1: Thêm cột AssignedBusId nếu chưa có
IF COL_LENGTH(N'dbo.Driver', N'AssignedBusId') IS NULL
BEGIN
    ALTER TABLE [dbo].[Driver] ADD [AssignedBusId] int NULL;
    
    ALTER TABLE [dbo].[Driver] ADD CONSTRAINT [FK_Driver_AssignedBus]
        FOREIGN KEY ([AssignedBusId]) REFERENCES [dbo].[Bus] ([BusId])
        ON DELETE SET NULL;
        
    PRINT N'Đã thêm cột AssignedBusId thành công.';
END
ELSE
BEGIN
    PRINT N'Cột AssignedBusId đã tồn tại.';
END
GO

-- BƯỚC 2: Cập nhật UserRole cho các tài xế trong bảng User
-- driver1 (UserId=1) -> RoleId=1 (Tài xế)
IF NOT EXISTS (SELECT 1 FROM [dbo].[UserRole] WHERE [UserId] = 1 AND [RoleId] = 1)
    INSERT INTO [dbo].[UserRole] ([UserId], [RoleId]) VALUES (1, 1);

-- admin (UserId=3) -> RoleId=3 (Quản trị viên)
IF NOT EXISTS (SELECT 1 FROM [dbo].[UserRole] WHERE [UserId] = 3 AND [RoleId] = 3)
    INSERT INTO [dbo].[UserRole] ([UserId], [RoleId]) VALUES (3, 3);

-- Các tài xế khác (UserId 4..9) -> RoleId=1
IF NOT EXISTS (SELECT 1 FROM [dbo].[UserRole] WHERE [UserId] = 4 AND [RoleId] = 1)
    INSERT INTO [dbo].[UserRole] ([UserId], [RoleId]) VALUES (4, 1);
IF NOT EXISTS (SELECT 1 FROM [dbo].[UserRole] WHERE [UserId] = 5 AND [RoleId] = 1)
    INSERT INTO [dbo].[UserRole] ([UserId], [RoleId]) VALUES (5, 1);
IF NOT EXISTS (SELECT 1 FROM [dbo].[UserRole] WHERE [UserId] = 6 AND [RoleId] = 1)
    INSERT INTO [dbo].[UserRole] ([UserId], [RoleId]) VALUES (6, 1);
IF NOT EXISTS (SELECT 1 FROM [dbo].[UserRole] WHERE [UserId] = 7 AND [RoleId] = 1)
    INSERT INTO [dbo].[UserRole] ([UserId], [RoleId]) VALUES (7, 1);
IF NOT EXISTS (SELECT 1 FROM [dbo].[UserRole] WHERE [UserId] = 8 AND [RoleId] = 1)
    INSERT INTO [dbo].[UserRole] ([UserId], [RoleId]) VALUES (8, 1);
IF NOT EXISTS (SELECT 1 FROM [dbo].[UserRole] WHERE [UserId] = 9 AND [RoleId] = 1)
    INSERT INTO [dbo].[UserRole] ([UserId], [RoleId]) VALUES (9, 1);
GO

-- BƯỚC 3: Phân công xe cố định (1 tài xế = 1 xe)
-- Gán xe đầu tiên cho DriverId=1 (Nguyễn Văn An)
UPDATE [dbo].[Driver] SET [AssignedBusId] = (
    SELECT TOP 1 [BusId] FROM [dbo].[Bus] WHERE [Status] = 'Active' ORDER BY [BusId]
) WHERE [DriverId] = 1 AND [AssignedBusId] IS NULL;

-- Gán xe thứ 2 cho DriverId=2 (Trần Đình Trọng)
UPDATE [dbo].[Driver] SET [AssignedBusId] = (
    SELECT TOP 1 [BusId] FROM [dbo].[Bus] WHERE [Status] = 'Active' 
    AND [BusId] NOT IN (SELECT ISNULL([AssignedBusId], 0) FROM [dbo].[Driver])
    ORDER BY [BusId]
) WHERE [DriverId] = 2 AND [AssignedBusId] IS NULL;

-- Gán xe thứ 3 cho DriverId=3 (Hoàng Văn Nam)
UPDATE [dbo].[Driver] SET [AssignedBusId] = (
    SELECT TOP 1 [BusId] FROM [dbo].[Bus] WHERE [Status] = 'Active' 
    AND [BusId] NOT IN (SELECT ISNULL([AssignedBusId], 0) FROM [dbo].[Driver])
    ORDER BY [BusId]
) WHERE [DriverId] = 3 AND [AssignedBusId] IS NULL;

PRINT N'Hoàn tất phân công xe cố định cho tài xế.';
GO
