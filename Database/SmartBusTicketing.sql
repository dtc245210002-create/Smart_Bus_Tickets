/********************************************************************************
 * DATABASE SCRIPT: SmartBusTicketing (Schema & Data)
 * Exported from localhost on 2026-09-29 10:55:45
 ********************************************************************************/
USE [master];
GO
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'SmartBusTicketing')
BEGIN
    CREATE DATABASE [SmartBusTicketing];
END
GO
USE [SmartBusTicketing];
GO

-- ============================================================================
-- 1. DDL: TABLES & CONSTRAINTS
-- ============================================================================
/********************************************************************************
 * DATABASE SCRIPT: SmartBusTicketing (Schema & Data)
 * Exported from localhost on 2026-09-29 10:21:42
 ********************************************************************************/
USE [master];
GO
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'SmartBusTicketing')
BEGIN
    CREATE DATABASE [SmartBusTicketing];
END
GO
USE [SmartBusTicketing];
GO

-- ============================================================================
-- 1. DDL: TABLES & CONSTRAINTS
-- ============================================================================
/****** Object:  Table [dbo].[Booking]    Script Date: 9/29/2026 10:19:53 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Booking](
	[BookingId] [int] IDENTITY(1,1) NOT NULL,
	[UserId] [int] NOT NULL,
	[TripId] [int] NOT NULL,
	[BookingCode] [varchar](30) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[BookingTime] [datetime] NOT NULL,
	[TotalAmount] [decimal](12, 2) NOT NULL,
	[Status] [varchar](20) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
PRIMARY KEY CLUSTERED 
(
	[BookingId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
UNIQUE NONCLUSTERED 
(
	[BookingCode] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Bus]    Script Date: 9/29/2026 10:19:54 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Bus](
	[BusId] [int] IDENTITY(1,1) NOT NULL,
	[BusTypeId] [int] NOT NULL,
	[LicensePlate] [varchar](20) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[Capacity] [int] NOT NULL,
	[Status] [varchar](20) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
PRIMARY KEY CLUSTERED 
(
	[BusId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
UNIQUE NONCLUSTERED 
(
	[LicensePlate] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[BusStop]    Script Date: 9/29/2026 10:19:54 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[BusStop](
	[StopId] [int] IDENTITY(1,1) NOT NULL,
	[StopName] [nvarchar](150) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[Address] [nvarchar](255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Latitude] [decimal](10, 7) NULL,
	[Longitude] [decimal](10, 7) NULL,
PRIMARY KEY CLUSTERED 
(
	[StopId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[BusType]    Script Date: 9/29/2026 10:19:54 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[BusType](
	[BusTypeId] [int] IDENTITY(1,1) NOT NULL,
	[TypeName] [nvarchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[Capacity] [int] NOT NULL,
	[Description] [nvarchar](255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
PRIMARY KEY CLUSTERED 
(
	[BusTypeId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Driver]    Script Date: 9/29/2026 10:19:54 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Driver](
	[DriverId] [int] IDENTITY(1,1) NOT NULL,
	[UserId] [int] NOT NULL,
	[LicenseNo] [varchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[LicenseExpiryDate] [date] NULL,
	[Status] [varchar](20) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
PRIMARY KEY CLUSTERED 
(
	[DriverId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
UNIQUE NONCLUSTERED 
(
	[UserId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Notification]    Script Date: 9/29/2026 10:19:54 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Notification](
	[NotificationId] [bigint] IDENTITY(1,1) NOT NULL,
	[UserId] [int] NOT NULL,
	[Title] [nvarchar](200) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[Content] [nvarchar](max) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Type] [varchar](30) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[IsRead] [bit] NOT NULL,
	[CreatedAt] [datetime] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[NotificationId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Payment]    Script Date: 9/29/2026 10:19:54 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Payment](
	[PaymentId] [int] IDENTITY(1,1) NOT NULL,
	[BookingId] [int] NOT NULL,
	[Amount] [decimal](12, 2) NOT NULL,
	[PaymentMethod] [varchar](30) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[TransactionCode] [varchar](100) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[PaymentTime] [datetime] NULL,
	[Status] [varchar](20) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
PRIMARY KEY CLUSTERED 
(
	[PaymentId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Role]    Script Date: 9/29/2026 10:19:54 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Role](
	[RoleId] [int] IDENTITY(1,1) NOT NULL,
	[RoleName] [nvarchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[RoleId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
 CONSTRAINT [UQ__Role__8A2B61609C6ED659] UNIQUE NONCLUSTERED 
(
	[RoleName] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Route]    Script Date: 9/29/2026 10:19:54 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Route](
	[RouteId] [int] IDENTITY(1,1) NOT NULL,
	[RouteCode] [varchar](20) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[RouteName] [nvarchar](100) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[StartPoint] [nvarchar](150) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[EndPoint] [nvarchar](150) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[Distance] [decimal](10, 2) NULL,
	[EstimatedDuration] [int] NULL,
	[Status] [varchar](20) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
PRIMARY KEY CLUSTERED 
(
	[RouteId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
UNIQUE NONCLUSTERED 
(
	[RouteCode] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[RouteStop]    Script Date: 9/29/2026 10:19:54 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[RouteStop](
	[RouteStopId] [int] IDENTITY(1,1) NOT NULL,
	[RouteId] [int] NOT NULL,
	[StopId] [int] NOT NULL,
	[StopOrder] [int] NOT NULL,
	[ArrivalTime] [time](7) NULL,
	[DepartureTime] [time](7) NULL,
PRIMARY KEY CLUSTERED 
(
	[RouteStopId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
 CONSTRAINT [UQ_RouteStop_Route_Order] UNIQUE NONCLUSTERED 
(
	[RouteId] ASC,
	[StopOrder] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
 CONSTRAINT [UQ_RouteStop_Route_Stop] UNIQUE NONCLUSTERED 
(
	[RouteId] ASC,
	[StopId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Schedule]    Script Date: 9/29/2026 10:19:54 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Schedule](
	[ScheduleId] [int] IDENTITY(1,1) NOT NULL,
	[RouteId] [int] NOT NULL,
	[DepartureTime] [time](7) NOT NULL,
	[DayOfWeek] [int] NOT NULL,
	[Status] [varchar](20) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
PRIMARY KEY CLUSTERED 
(
	[ScheduleId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Ticket]    Script Date: 9/29/2026 10:19:54 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Ticket](
	[TicketId] [int] IDENTITY(1,1) NOT NULL,
	[BookingId] [int] NOT NULL,
	[TicketCode] [varchar](30) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[SeatNumber] [varchar](10) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Price] [decimal](12, 2) NOT NULL,
	[BoardingStopId] [int] NULL,
	[DropOffStopId] [int] NULL,
	[Status] [varchar](20) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
PRIMARY KEY CLUSTERED 
(
	[TicketId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
UNIQUE NONCLUSTERED 
(
	[TicketCode] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Trip]    Script Date: 9/29/2026 10:19:54 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Trip](
	[TripId] [int] IDENTITY(1,1) NOT NULL,
	[RouteId] [int] NOT NULL,
	[BusId] [int] NOT NULL,
	[DriverId] [int] NOT NULL,
	[ScheduleId] [int] NULL,
	[TripDate] [date] NOT NULL,
	[DepartureTime] [time](7) NOT NULL,
	[ArrivalTime] [time](7) NULL,
	[Status] [varchar](20) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
PRIMARY KEY CLUSTERED 
(
	[TripId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[User]    Script Date: 9/29/2026 10:19:54 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[User](
	[UserId] [int] IDENTITY(1,1) NOT NULL,
	[FullName] [nvarchar](100) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[Email] [varchar](150) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[Phone] [varchar](20) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[PasswordHash] [varchar](255) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[Status] [varchar](20) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[CreatedAt] [datetime] NULL,
PRIMARY KEY CLUSTERED 
(
	[UserId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
UNIQUE NONCLUSTERED 
(
	[Email] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[UserRole]    Script Date: 9/29/2026 10:19:54 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[UserRole](
	[UserId] [int] NOT NULL,
	[RoleId] [int] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[UserId] ASC,
	[RoleId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[VehicleLocation]    Script Date: 9/29/2026 10:19:54 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[VehicleLocation](
	[LocationId] [bigint] IDENTITY(1,1) NOT NULL,
	[TripId] [int] NOT NULL,
	[Latitude] [decimal](10, 7) NOT NULL,
	[Longitude] [decimal](10, 7) NOT NULL,
	[Speed] [decimal](6, 2) NULL,
	[RecordedAt] [datetime] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[LocationId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
ALTER TABLE [dbo].[Booking]  WITH CHECK ADD  CONSTRAINT [FK_Booking_Trip] FOREIGN KEY([TripId])
REFERENCES [dbo].[Trip] ([TripId])
GO
ALTER TABLE [dbo].[Booking] CHECK CONSTRAINT [FK_Booking_Trip]
GO
ALTER TABLE [dbo].[Booking]  WITH CHECK ADD  CONSTRAINT [FK_Booking_User] FOREIGN KEY([UserId])
REFERENCES [dbo].[User] ([UserId])
GO
ALTER TABLE [dbo].[Booking] CHECK CONSTRAINT [FK_Booking_User]
GO
ALTER TABLE [dbo].[Bus]  WITH CHECK ADD  CONSTRAINT [FK_Bus_BusType] FOREIGN KEY([BusTypeId])
REFERENCES [dbo].[BusType] ([BusTypeId])
GO
ALTER TABLE [dbo].[Bus] CHECK CONSTRAINT [FK_Bus_BusType]
GO
ALTER TABLE [dbo].[Driver]  WITH CHECK ADD  CONSTRAINT [FK_Driver_User] FOREIGN KEY([UserId])
REFERENCES [dbo].[User] ([UserId])
GO
ALTER TABLE [dbo].[Driver] CHECK CONSTRAINT [FK_Driver_User]
GO
ALTER TABLE [dbo].[Notification]  WITH CHECK ADD  CONSTRAINT [FK_Notification_User] FOREIGN KEY([UserId])
REFERENCES [dbo].[User] ([UserId])
GO
ALTER TABLE [dbo].[Notification] CHECK CONSTRAINT [FK_Notification_User]
GO
ALTER TABLE [dbo].[Payment]  WITH CHECK ADD  CONSTRAINT [FK_Payment_Booking] FOREIGN KEY([BookingId])
REFERENCES [dbo].[Booking] ([BookingId])
GO
ALTER TABLE [dbo].[Payment] CHECK CONSTRAINT [FK_Payment_Booking]
GO
ALTER TABLE [dbo].[RouteStop]  WITH CHECK ADD  CONSTRAINT [FK_RouteStop_BusStop] FOREIGN KEY([StopId])
REFERENCES [dbo].[BusStop] ([StopId])
GO
ALTER TABLE [dbo].[RouteStop] CHECK CONSTRAINT [FK_RouteStop_BusStop]
GO
ALTER TABLE [dbo].[RouteStop]  WITH CHECK ADD  CONSTRAINT [FK_RouteStop_Route] FOREIGN KEY([RouteId])
REFERENCES [dbo].[Route] ([RouteId])
GO
ALTER TABLE [dbo].[RouteStop] CHECK CONSTRAINT [FK_RouteStop_Route]
GO
ALTER TABLE [dbo].[Schedule]  WITH CHECK ADD  CONSTRAINT [FK_Schedule_Route] FOREIGN KEY([RouteId])
REFERENCES [dbo].[Route] ([RouteId])
GO
ALTER TABLE [dbo].[Schedule] CHECK CONSTRAINT [FK_Schedule_Route]
GO
ALTER TABLE [dbo].[Ticket]  WITH CHECK ADD  CONSTRAINT [FK_Ticket_BoardingStop] FOREIGN KEY([BoardingStopId])
REFERENCES [dbo].[BusStop] ([StopId])
GO
ALTER TABLE [dbo].[Ticket] CHECK CONSTRAINT [FK_Ticket_BoardingStop]
GO
ALTER TABLE [dbo].[Ticket]  WITH CHECK ADD  CONSTRAINT [FK_Ticket_Booking] FOREIGN KEY([BookingId])
REFERENCES [dbo].[Booking] ([BookingId])
GO
ALTER TABLE [dbo].[Ticket] CHECK CONSTRAINT [FK_Ticket_Booking]
GO
ALTER TABLE [dbo].[Ticket]  WITH CHECK ADD  CONSTRAINT [FK_Ticket_DropOffStop] FOREIGN KEY([DropOffStopId])
REFERENCES [dbo].[BusStop] ([StopId])
GO
ALTER TABLE [dbo].[Ticket] CHECK CONSTRAINT [FK_Ticket_DropOffStop]
GO
ALTER TABLE [dbo].[Trip]  WITH CHECK ADD  CONSTRAINT [FK_Trip_Bus] FOREIGN KEY([BusId])
REFERENCES [dbo].[Bus] ([BusId])
GO
ALTER TABLE [dbo].[Trip] CHECK CONSTRAINT [FK_Trip_Bus]
GO
ALTER TABLE [dbo].[Trip]  WITH CHECK ADD  CONSTRAINT [FK_Trip_Driver] FOREIGN KEY([DriverId])
REFERENCES [dbo].[Driver] ([DriverId])
GO
ALTER TABLE [dbo].[Trip] CHECK CONSTRAINT [FK_Trip_Driver]
GO
ALTER TABLE [dbo].[Trip]  WITH CHECK ADD  CONSTRAINT [FK_Trip_Route] FOREIGN KEY([RouteId])
REFERENCES [dbo].[Route] ([RouteId])
GO
ALTER TABLE [dbo].[Trip] CHECK CONSTRAINT [FK_Trip_Route]
GO
ALTER TABLE [dbo].[Trip]  WITH CHECK ADD  CONSTRAINT [FK_Trip_Schedule] FOREIGN KEY([ScheduleId])
REFERENCES [dbo].[Schedule] ([ScheduleId])
GO
ALTER TABLE [dbo].[Trip] CHECK CONSTRAINT [FK_Trip_Schedule]
GO
ALTER TABLE [dbo].[UserRole]  WITH CHECK ADD  CONSTRAINT [FK_UserRole_Role] FOREIGN KEY([RoleId])
REFERENCES [dbo].[Role] ([RoleId])
GO
ALTER TABLE [dbo].[UserRole] CHECK CONSTRAINT [FK_UserRole_Role]
GO
ALTER TABLE [dbo].[UserRole]  WITH CHECK ADD  CONSTRAINT [FK_UserRole_User] FOREIGN KEY([UserId])
REFERENCES [dbo].[User] ([UserId])
GO
ALTER TABLE [dbo].[UserRole] CHECK CONSTRAINT [FK_UserRole_User]
GO
ALTER TABLE [dbo].[VehicleLocation]  WITH CHECK ADD  CONSTRAINT [FK_VehicleLocation_Trip] FOREIGN KEY([TripId])
REFERENCES [dbo].[Trip] ([TripId])
GO
ALTER TABLE [dbo].[VehicleLocation] CHECK CONSTRAINT [FK_VehicleLocation_Trip]
GO
/****** Object:  StoredProcedure [dbo].[SearchTrips]    Script Date: 9/29/2026 10:19:54 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE   PROCEDURE SearchTrips
    @FromStopName VARCHAR(150),
    @ToStopName VARCHAR(150),
    @TripDate DATE
AS
BEGIN

    SELECT
        t.TripId,
        r.RouteCode,
        r.RouteName,

        bsFrom.StopName AS FromStop,
        bsTo.StopName AS ToStop,

        t.TripDate,
        t.DepartureTime,
        t.ArrivalTime,

        b.LicensePlate,
        b.Capacity,

        COUNT(tk.TicketId) AS SoldTickets,

        b.Capacity - COUNT(tk.TicketId) AS AvailableSeats

    FROM Trip t

    JOIN Route r
        ON t.RouteId = r.RouteId

    JOIN Bus b
        ON t.BusId = b.BusId

    JOIN RouteStop rsFrom
        ON rsFrom.RouteId = t.RouteId

    JOIN BusStop bsFrom
        ON bsFrom.StopId = rsFrom.StopId

    JOIN RouteStop rsTo
        ON rsTo.RouteId = t.RouteId

    JOIN BusStop bsTo
        ON bsTo.StopId = rsTo.StopId

    LEFT JOIN Booking bk
        ON bk.TripId = t.TripId
        AND bk.Status = 'Confirmed'

    LEFT JOIN Ticket tk
        ON tk.BookingId = bk.BookingId
        AND tk.Status = 'Confirmed'

    WHERE
        bsFrom.StopName = @FromStopName

        AND bsTo.StopName = @ToStopName

        AND rsFrom.StopOrder < rsTo.StopOrder

        AND t.TripDate = @TripDate

        AND t.Status = 'Scheduled'

    GROUP BY
        t.TripId,
        r.RouteCode,
        r.RouteName,
        bsFrom.StopName,
        bsTo.StopName,
        t.TripDate,
        t.DepartureTime,
        t.ArrivalTime,
        b.LicensePlate,
        b.Capacity

    HAVING
        b.Capacity - COUNT(tk.TicketId) > 0

    ORDER BY
        t.DepartureTime;

END;
GO


GO

-- ============================================================================
-- 2. DATA INSERTS
-- ============================================================================
EXEC sp_MSforeachtable 'ALTER TABLE ? NOCHECK CONSTRAINT ALL';
GO

-- Data for [Role] (3 rows)
SET IDENTITY_INSERT [Role] ON;
INSERT INTO [Role] ([RoleId], [RoleName]) VALUES (2, N'Khách hàng');
INSERT INTO [Role] ([RoleId], [RoleName]) VALUES (3, N'Quản trị viên');
INSERT INTO [Role] ([RoleId], [RoleName]) VALUES (1, N'Tài xế');
SET IDENTITY_INSERT [Role] OFF;
GO

-- Data for [User] (9 rows)
SET IDENTITY_INSERT [User] ON;
INSERT INTO [User] ([UserId], [FullName], [Email], [Phone], [PasswordHash], [Status], [CreatedAt]) VALUES (1, N'Nguyễn Văn An', N'driver1@gmail.com', N'0987654321', N'demo_hash', N'Active', '2026-09-28');
INSERT INTO [User] ([UserId], [FullName], [Email], [Phone], [PasswordHash], [Status], [CreatedAt]) VALUES (2, N'Trần Văn Bình', N'customer1@gmail.com', N'0912345678', N'demo_hash', N'Active', '2026-09-28');
INSERT INTO [User] ([UserId], [FullName], [Email], [Phone], [PasswordHash], [Status], [CreatedAt]) VALUES (3, N'Lê Văn Cường', N'admin@gmail.com', N'0901234567', N'demo_hash', N'Active', '2026-09-28');
INSERT INTO [User] ([UserId], [FullName], [Email], [Phone], [PasswordHash], [Status], [CreatedAt]) VALUES (4, N'Trần Đình Trọng', N'trong.tran@smartbus.vn', N'0988777999', N'PASS_HASH', N'ACTIVE', '2026-09-28');
INSERT INTO [User] ([UserId], [FullName], [Email], [Phone], [PasswordHash], [Status], [CreatedAt]) VALUES (5, N'Hoàng Văn Nam', N'nam.hoang@smartbus.vn', N'0977123456', N'PASS_HASH', N'ACTIVE', '2026-09-28');
INSERT INTO [User] ([UserId], [FullName], [Email], [Phone], [PasswordHash], [Status], [CreatedAt]) VALUES (6, N'Đặng Minh Tuấn', N'tuan.dang@smartbus.vn', N'0933666888', N'PASS_HASH', N'ACTIVE', '2026-09-28');
INSERT INTO [User] ([UserId], [FullName], [Email], [Phone], [PasswordHash], [Status], [CreatedAt]) VALUES (7, N'Lê Hoàng Nam', N'nam.le@smartbus.vn', N'0912345002', N'hashed_pass_8', N'ACTIVE', '2026-09-28');
INSERT INTO [User] ([UserId], [FullName], [Email], [Phone], [PasswordHash], [Status], [CreatedAt]) VALUES (8, N'Phạm Quốc Bảo', N'bao.pham@smartbus.vn', N'0912345003', N'hashed_pass_9', N'ACTIVE', '2026-09-28');
INSERT INTO [User] ([UserId], [FullName], [Email], [Phone], [PasswordHash], [Status], [CreatedAt]) VALUES (9, N'Đỗ Minh Quân', N'quan.do@smartbus.vn', N'0912345004', N'hashed_pass_10', N'ACTIVE', '2026-09-28');
SET IDENTITY_INSERT [User] OFF;
GO

-- Data for [UserRole] (3 rows)
INSERT INTO [UserRole] ([UserId], [RoleId]) VALUES (1, 3);
INSERT INTO [UserRole] ([UserId], [RoleId]) VALUES (2, 2);
INSERT INTO [UserRole] ([UserId], [RoleId]) VALUES (3, 1);
GO

-- Data for [Driver] (7 rows)
SET IDENTITY_INSERT [Driver] ON;
INSERT INTO [Driver] ([DriverId], [UserId], [LicenseNo], [LicenseExpiryDate], [Status]) VALUES (1, 1, N'B123456789', '2028-12-31', N'Active');
INSERT INTO [Driver] ([DriverId], [UserId], [LicenseNo], [LicenseExpiryDate], [Status]) VALUES (2, 4, N'FC-988214', '2028-12-31', N'ACTIVE');
INSERT INTO [Driver] ([DriverId], [UserId], [LicenseNo], [LicenseExpiryDate], [Status]) VALUES (3, 5, N'FC-554129', '2029-06-30', N'ACTIVE');
INSERT INTO [Driver] ([DriverId], [UserId], [LicenseNo], [LicenseExpiryDate], [Status]) VALUES (4, 6, N'FC-882103', '2027-10-15', N'ACTIVE');
INSERT INTO [Driver] ([DriverId], [UserId], [LicenseNo], [LicenseExpiryDate], [Status]) VALUES (6, 7, N'E-88776655', '2030-12-31', N'Active');
INSERT INTO [Driver] ([DriverId], [UserId], [LicenseNo], [LicenseExpiryDate], [Status]) VALUES (7, 8, N'E-77665544', '2030-12-31', N'Active');
INSERT INTO [Driver] ([DriverId], [UserId], [LicenseNo], [LicenseExpiryDate], [Status]) VALUES (8, 9, N'E-66554433', '2030-12-31', N'Active');
SET IDENTITY_INSERT [Driver] OFF;
GO

-- Data for [BusType] (5 rows)
SET IDENTITY_INSERT [BusType] ON;
INSERT INTO [BusType] ([BusTypeId], [TypeName], [Capacity], [Description]) VALUES (1, N'Xe 29 chỗ', 16, N'Xe khách 16 ch?');
INSERT INTO [BusType] ([BusTypeId], [TypeName], [Capacity], [Description]) VALUES (2, N'Xe 29 chỗ', 29, N'Xe khách 29 ch?');
INSERT INTO [BusType] ([BusTypeId], [TypeName], [Capacity], [Description]) VALUES (3, N'Xe 45 chỗ', 45, N'Xe khách 45 ch?');
INSERT INTO [BusType] ([BusTypeId], [TypeName], [Capacity], [Description]) VALUES (4, N'Limousine VIP 22 Phòng', 22, N'Khoang cung điện VIP riêng tư, massage, tivi, tai nghe');
INSERT INTO [BusType] ([BusTypeId], [TypeName], [Capacity], [Description]) VALUES (5, N'Giường Nằm 34 Phòng', 34, N'Giường nằm Green Express cao cấp, sạc Type-C, wifi tốc độ cao');
SET IDENTITY_INSERT [BusType] OFF;
GO

-- Data for [Bus] (24 rows)
SET IDENTITY_INSERT [Bus] ON;
INSERT INTO [Bus] ([BusId], [BusTypeId], [LicensePlate], [Capacity], [Status]) VALUES (1, 2, N'20A-12345', 29, N'Active');
INSERT INTO [Bus] ([BusId], [BusTypeId], [LicensePlate], [Capacity], [Status]) VALUES (2, 2, N'20A-67890', 29, N'Active');
INSERT INTO [Bus] ([BusId], [BusTypeId], [LicensePlate], [Capacity], [Status]) VALUES (3, 3, N'20B-11111', 45, N'Active');
INSERT INTO [Bus] ([BusId], [BusTypeId], [LicensePlate], [Capacity], [Status]) VALUES (4, 4, N'29B-888.68', 22, N'ACTIVE');
INSERT INTO [Bus] ([BusId], [BusTypeId], [LicensePlate], [Capacity], [Status]) VALUES (5, 1, N'15B-112.23', 29, N'ACTIVE');
INSERT INTO [Bus] ([BusId], [BusTypeId], [LicensePlate], [Capacity], [Status]) VALUES (6, 4, N'29B-666.99', 22, N'ACTIVE');
INSERT INTO [Bus] ([BusId], [BusTypeId], [LicensePlate], [Capacity], [Status]) VALUES (7, 5, N'51B-234.56', 34, N'ACTIVE');
INSERT INTO [Bus] ([BusId], [BusTypeId], [LicensePlate], [Capacity], [Status]) VALUES (8, 1, N'51B-789.10', 29, N'ACTIVE');
INSERT INTO [Bus] ([BusId], [BusTypeId], [LicensePlate], [Capacity], [Status]) VALUES (9, 1, N'43B-098.76', 29, N'ACTIVE');
INSERT INTO [Bus] ([BusId], [BusTypeId], [LicensePlate], [Capacity], [Status]) VALUES (10, 5, N'51B-333.44', 34, N'ACTIVE');
INSERT INTO [Bus] ([BusId], [BusTypeId], [LicensePlate], [Capacity], [Status]) VALUES (11, 5, N'29B-987.65', 34, N'Active');
INSERT INTO [Bus] ([BusId], [BusTypeId], [LicensePlate], [Capacity], [Status]) VALUES (12, 2, N'35B-123.45', 29, N'Active');
INSERT INTO [Bus] ([BusId], [BusTypeId], [LicensePlate], [Capacity], [Status]) VALUES (13, 4, N'26B-555.66', 22, N'Active');
INSERT INTO [Bus] ([BusId], [BusTypeId], [LicensePlate], [Capacity], [Status]) VALUES (14, 5, N'11B-777.88', 34, N'Active');
INSERT INTO [Bus] ([BusId], [BusTypeId], [LicensePlate], [Capacity], [Status]) VALUES (15, 4, N'73B-888.99', 22, N'Active');
INSERT INTO [Bus] ([BusId], [BusTypeId], [LicensePlate], [Capacity], [Status]) VALUES (16, 4, N'51B-666.11', 22, N'Active');
INSERT INTO [Bus] ([BusId], [BusTypeId], [LicensePlate], [Capacity], [Status]) VALUES (17, 5, N'77B-222.33', 34, N'Active');
INSERT INTO [Bus] ([BusId], [BusTypeId], [LicensePlate], [Capacity], [Status]) VALUES (18, 5, N'47B-999.55', 34, N'Active');
INSERT INTO [Bus] ([BusId], [BusTypeId], [LicensePlate], [Capacity], [Status]) VALUES (19, 2, N'86B-333.77', 29, N'Active');
INSERT INTO [Bus] ([BusId], [BusTypeId], [LicensePlate], [Capacity], [Status]) VALUES (20, 5, N'69B-888.22', 34, N'Active');
INSERT INTO [Bus] ([BusId], [BusTypeId], [LicensePlate], [Capacity], [Status]) VALUES (21, 5, N'67B-444.66', 34, N'Active');
INSERT INTO [Bus] ([BusId], [BusTypeId], [LicensePlate], [Capacity], [Status]) VALUES (22, 4, N'43B-111.99', 22, N'Active');
INSERT INTO [Bus] ([BusId], [BusTypeId], [LicensePlate], [Capacity], [Status]) VALUES (23, 5, N'43B-555.88', 34, N'Active');
INSERT INTO [Bus] ([BusId], [BusTypeId], [LicensePlate], [Capacity], [Status]) VALUES (24, 2, N'65B-222.44', 29, N'Active');
SET IDENTITY_INSERT [Bus] OFF;
GO

-- Data for [BusStop] (30 rows)
SET IDENTITY_INSERT [BusStop] ON;
INSERT INTO [BusStop] ([StopId], [StopName], [Address], [Latitude], [Longitude]) VALUES (1, N'Bến xe Mỹ Đình', N'Phạm Hùng, Nam Từ Liêm, Hà Nội', NULL, NULL);
INSERT INTO [BusStop] ([StopId], [StopName], [Address], [Latitude], [Longitude]) VALUES (2, N'Điểm Cầu Giấy', N'Đường Cầu Giấy, Cầu Giấy, Hà Nội', NULL, NULL);
INSERT INTO [BusStop] ([StopId], [StopName], [Address], [Latitude], [Longitude]) VALUES (3, N'Sân bay Nội Bài', N'Xã Phú Minh, Huyện Sóc Sơn, Hà Nội', NULL, NULL);
INSERT INTO [BusStop] ([StopId], [StopName], [Address], [Latitude], [Longitude]) VALUES (4, N'Bến xe Thái Nguyên', N'Đường Lương Ngọc Quyến, TP. Thái Nguyên', NULL, NULL);
INSERT INTO [BusStop] ([StopId], [StopName], [Address], [Latitude], [Longitude]) VALUES (5, N'Điểm đón Phổ Yên', N'Phường Ba Hàng, TP. Phổ Yên, Thái Nguyên', NULL, NULL);
INSERT INTO [BusStop] ([StopId], [StopName], [Address], [Latitude], [Longitude]) VALUES (6, N'Bến xe Nước Ngầm', N'Km 8 Giải Phóng, Hoàng Mai, Hà Nội', NULL, NULL);
INSERT INTO [BusStop] ([StopId], [StopName], [Address], [Latitude], [Longitude]) VALUES (7, N'Bến xe Giáp Bát', N'Giải Phóng, Hoàng Mai, Hà Nội', NULL, NULL);
INSERT INTO [BusStop] ([StopId], [StopName], [Address], [Latitude], [Longitude]) VALUES (8, N'Bến xe Cầu Rào', N'Ngô Gia Tự, Lê Chân, Hải Phòng', NULL, NULL);
INSERT INTO [BusStop] ([StopId], [StopName], [Address], [Latitude], [Longitude]) VALUES (9, N'Bến xe Sa Pa', N'Điện Biên Phủ, Sa Pa, Lào Cai', NULL, NULL);
INSERT INTO [BusStop] ([StopId], [StopName], [Address], [Latitude], [Longitude]) VALUES (10, N'Bến xe Bãi Cháy', N'Hạ Long, Quảng Ninh', NULL, NULL);
INSERT INTO [BusStop] ([StopId], [StopName], [Address], [Latitude], [Longitude]) VALUES (11, N'Bến xe Trung Tâm Đà Nẵng', N'Nam Trân, Liên Chiểu, Đà Nẵng', NULL, NULL);
INSERT INTO [BusStop] ([StopId], [StopName], [Address], [Latitude], [Longitude]) VALUES (12, N'Bến xe Phía Nam Huế', N'An Dương Vương, An Cựu, Huế', NULL, NULL);
INSERT INTO [BusStop] ([StopId], [StopName], [Address], [Latitude], [Longitude]) VALUES (13, N'Bến xe Miền Đông Mới', N'Hoàng Hữu Nam, TP. Thủ Đức, TP. Hồ Chí Minh', NULL, NULL);
INSERT INTO [BusStop] ([StopId], [StopName], [Address], [Latitude], [Longitude]) VALUES (14, N'Bến xe Miền Tây', N'Kinh Dương Vương, Bình Tân, TP. Hồ Chí Minh', NULL, NULL);
INSERT INTO [BusStop] ([StopId], [StopName], [Address], [Latitude], [Longitude]) VALUES (15, N'Bến xe Liên Tỉnh Đà Lạt', N'Số 1 Tô Hiến Thành, TP. Đà Lạt, Lâm Đồng', NULL, NULL);
INSERT INTO [BusStop] ([StopId], [StopName], [Address], [Latitude], [Longitude]) VALUES (16, N'Bến xe Vũng Tàu', N'Nam Kỳ Khởi Nghĩa, TP. Vũng Tàu', NULL, NULL);
INSERT INTO [BusStop] ([StopId], [StopName], [Address], [Latitude], [Longitude]) VALUES (17, N'Bến xe Cần Thơ', N'Đường dẫn cầu Cần Thơ, Cái Răng, Cần Thơ', NULL, NULL);
INSERT INTO [BusStop] ([StopId], [StopName], [Address], [Latitude], [Longitude]) VALUES (18, N'Bến xe Nha Trang', N'Đường 23/10, TP. Nha Trang, Khánh Hòa', NULL, NULL);
INSERT INTO [BusStop] ([StopId], [StopName], [Address], [Latitude], [Longitude]) VALUES (19, N'Bến xe Hà Giang', N'Phường Phương Thiện, TP. Hà Giang', 22.8021000, 104.9812000);
INSERT INTO [BusStop] ([StopId], [StopName], [Address], [Latitude], [Longitude]) VALUES (20, N'Bến xe Ninh Bình', N'Đường Lê Đạo, Phường Vân Gia, TP. Ninh Bình', 20.2520000, 105.9750000);
INSERT INTO [BusStop] ([StopId], [StopName], [Address], [Latitude], [Longitude]) VALUES (21, N'Bến xe Mộc Châu', N'Thị trấn Mộc Châu, Huyện Mộc Châu, Sơn La', 20.8410000, 104.6420000);
INSERT INTO [BusStop] ([StopId], [StopName], [Address], [Latitude], [Longitude]) VALUES (22, N'Bến xe Cao Bằng', N'Phường Đề Thám, TP. Cao Bằng', 22.6580000, 106.2570000);
INSERT INTO [BusStop] ([StopId], [StopName], [Address], [Latitude], [Longitude]) VALUES (23, N'Bến xe Đồng Hới (Quảng Bình)', N'Trần Hưng Đạo, TP. Đồng Hới, Quảng Bình', 17.4720000, 106.6020000);
INSERT INTO [BusStop] ([StopId], [StopName], [Address], [Latitude], [Longitude]) VALUES (24, N'Bến xe Quy Nhơn', N'Số 71 Tây Sơn, Phường Ghềnh Ráng, TP. Quy Nhơn, Bình Định', 13.7540000, 109.2130000);
INSERT INTO [BusStop] ([StopId], [StopName], [Address], [Latitude], [Longitude]) VALUES (25, N'Bến xe Buôn Ma Thuột', N'Đường Nguyễn Tất Thành, TP. Buôn Ma Thuột, Đắk Lắk', 12.6880000, 108.0580000);
INSERT INTO [BusStop] ([StopId], [StopName], [Address], [Latitude], [Longitude]) VALUES (26, N'Bến xe Phan Thiết (Bình Thuận)', N'Đường Từ Văn Tư, TP. Phan Thiết, Bình Thuận', 10.9320000, 108.0980000);
INSERT INTO [BusStop] ([StopId], [StopName], [Address], [Latitude], [Longitude]) VALUES (27, N'Bến xe Cà Mau', N'Quốc Lộ 1A, Lý Thường Kiệt, Khóm 5, TP. Cà Mau', 9.1760000, 105.1520000);
INSERT INTO [BusStop] ([StopId], [StopName], [Address], [Latitude], [Longitude]) VALUES (28, N'Bến xe Châu Đốc (An Giang)', N'Đường Lê Hồng Phong, Phường Châu Phú B, TP. Châu Đốc', 10.7020000, 105.1120000);
INSERT INTO [BusStop] ([StopId], [StopName], [Address], [Latitude], [Longitude]) VALUES (29, N'Bến xe Rạch Sỏi (Rạch Giá - Kiên Giang)', N'Mai Thị Hồng Hạnh, Phường Rạch Sỏi, TP. Rạch Giá', 9.9630000, 105.1450000);
INSERT INTO [BusStop] ([StopId], [StopName], [Address], [Latitude], [Longitude]) VALUES (30, N'Bến xe An Sương (TP.HCM)', N'Quốc Lộ 22, Xã Bà Điểm, Huyện Hóc Môn, TP. Hồ Chí Minh', 10.8490000, 106.6080000);
SET IDENTITY_INSERT [BusStop] OFF;
GO

-- Data for [Route] (24 rows)
SET IDENTITY_INSERT [Route] ON;
INSERT INTO [Route] ([RouteId], [RouteCode], [RouteName], [StartPoint], [EndPoint], [Distance], [EstimatedDuration], [Status]) VALUES (1, N'HN-TN-01', N'Hà Nội - Thái Nguyên', N'Hà Nội', N'Thái Nguyên', 80.00, 120, N'Active');
INSERT INTO [Route] ([RouteId], [RouteCode], [RouteName], [StartPoint], [EndPoint], [Distance], [EstimatedDuration], [Status]) VALUES (2, N'HN-DN-08', N'Hà Nội - Đà Nẵng (Cao tốc Bắc Nam)', N'Hà Nội', N'Đà Nẵng', 760.00, 750, N'ACTIVE');
INSERT INTO [Route] ([RouteId], [RouteCode], [RouteName], [StartPoint], [EndPoint], [Distance], [EstimatedDuration], [Status]) VALUES (3, N'HN-HP-02', N'Hà Nội - Hải Phòng (Cao tốc 5B)', N'Hà Nội', N'Hải Phòng', 105.00, 90, N'ACTIVE');
INSERT INTO [Route] ([RouteId], [RouteCode], [RouteName], [StartPoint], [EndPoint], [Distance], [EstimatedDuration], [Status]) VALUES (4, N'HN-SP-03', N'Hà Nội - Sa Pa (Cao tốc Lào Cai)', N'Hà Nội', N'Sa Pa', 320.00, 300, N'ACTIVE');
INSERT INTO [Route] ([RouteId], [RouteCode], [RouteName], [StartPoint], [EndPoint], [Distance], [EstimatedDuration], [Status]) VALUES (5, N'HN-QN-04', N'Hà Nội - Hạ Long (Quảng Ninh)', N'Hà Nội', N'Hạ Long', 160.00, 130, N'ACTIVE');
INSERT INTO [Route] ([RouteId], [RouteCode], [RouteName], [StartPoint], [EndPoint], [Distance], [EstimatedDuration], [Status]) VALUES (6, N'DN-HUE-05', N'Đà Nẵng - Huế (Hầm Hải Vân)', N'Đà Nẵng', N'Huế', 100.00, 110, N'ACTIVE');
INSERT INTO [Route] ([RouteId], [RouteCode], [RouteName], [StartPoint], [EndPoint], [Distance], [EstimatedDuration], [Status]) VALUES (7, N'SG-DL-06', N'TP. Hồ Chí Minh - Đà Lạt (Cung Điện VIP)', N'TP. Hồ Chí Minh', N'Đà Lạt', 305.00, 360, N'ACTIVE');
INSERT INTO [Route] ([RouteId], [RouteCode], [RouteName], [StartPoint], [EndPoint], [Distance], [EstimatedDuration], [Status]) VALUES (8, N'SG-VT-07', N'TP. Hồ Chí Minh - Vũng Tàu (Cao tốc)', N'TP. Hồ Chí Minh', N'Vũng Tàu', 100.00, 120, N'ACTIVE');
INSERT INTO [Route] ([RouteId], [RouteCode], [RouteName], [StartPoint], [EndPoint], [Distance], [EstimatedDuration], [Status]) VALUES (9, N'SG-CT-09', N'TP. Hồ Chí Minh - Cần Thơ (Cao tốc Mỹ Thuận)', N'TP. Hồ Chí Minh', N'Cần Thơ', 165.00, 180, N'ACTIVE');
INSERT INTO [Route] ([RouteId], [RouteCode], [RouteName], [StartPoint], [EndPoint], [Distance], [EstimatedDuration], [Status]) VALUES (10, N'DL-NT-10', N'Đà Lạt - Nha Trang (Đèo Khánh Lê)', N'Đà Lạt', N'Nha Trang', 135.00, 180, N'ACTIVE');
INSERT INTO [Route] ([RouteId], [RouteCode], [RouteName], [StartPoint], [EndPoint], [Distance], [EstimatedDuration], [Status]) VALUES (11, N'HN-HG-11', N'Hà Nội - Hà Giang (Cao nguyên đá Đồng Văn)', N'Hà Nội', N'Hà Giang', 300.00, 420, N'Active');
INSERT INTO [Route] ([RouteId], [RouteCode], [RouteName], [StartPoint], [EndPoint], [Distance], [EstimatedDuration], [Status]) VALUES (12, N'HN-NB-12', N'Hà Nội - Ninh Bình (Tràng An - Bái Đính)', N'Hà Nội', N'Ninh Bình', 95.00, 105, N'Active');
INSERT INTO [Route] ([RouteId], [RouteCode], [RouteName], [StartPoint], [EndPoint], [Distance], [EstimatedDuration], [Status]) VALUES (13, N'HN-MC-13', N'Hà Nội - Mộc Châu (Thung lũng Sơn La)', N'Hà Nội', N'Mộc Châu', 205.00, 250, N'Active');
INSERT INTO [Route] ([RouteId], [RouteCode], [RouteName], [StartPoint], [EndPoint], [Distance], [EstimatedDuration], [Status]) VALUES (14, N'HN-CB-14', N'Hà Nội - Cao Bằng (Thác Bản Giốc)', N'Hà Nội', N'Cao Bằng', 280.00, 360, N'Active');
INSERT INTO [Route] ([RouteId], [RouteCode], [RouteName], [StartPoint], [EndPoint], [Distance], [EstimatedDuration], [Status]) VALUES (15, N'HN-QB-15', N'Hà Nội - Quảng Bình (Đồng Hới - Phong Nha)', N'Hà Nội', N'Quảng Bình', 500.00, 540, N'Active');
INSERT INTO [Route] ([RouteId], [RouteCode], [RouteName], [StartPoint], [EndPoint], [Distance], [EstimatedDuration], [Status]) VALUES (16, N'SG-NT-16', N'TP. Hồ Chí Minh - Nha Trang (Biển Nha Trang)', N'TP. Hồ Chí Minh', N'Nha Trang', 430.00, 480, N'Active');
INSERT INTO [Route] ([RouteId], [RouteCode], [RouteName], [StartPoint], [EndPoint], [Distance], [EstimatedDuration], [Status]) VALUES (17, N'SG-QN-17', N'TP. Hồ Chí Minh - Quy Nhơn (Eo Gió - Kỳ Co)', N'TP. Hồ Chí Minh', N'Quy Nhơn', 650.00, 720, N'Active');
INSERT INTO [Route] ([RouteId], [RouteCode], [RouteName], [StartPoint], [EndPoint], [Distance], [EstimatedDuration], [Status]) VALUES (18, N'SG-BMT-18', N'TP. Hồ Chí Minh - Buôn Ma Thuột (Thủ phủ Cà Phê)', N'TP. Hồ Chí Minh', N'Buôn Ma Thuột', 350.00, 420, N'Active');
INSERT INTO [Route] ([RouteId], [RouteCode], [RouteName], [StartPoint], [EndPoint], [Distance], [EstimatedDuration], [Status]) VALUES (19, N'SG-PT-19', N'TP. Hồ Chí Minh - Phan Thiết (Mũi Né)', N'TP. Hồ Chí Minh', N'Phan Thiết', 200.00, 180, N'Active');
INSERT INTO [Route] ([RouteId], [RouteCode], [RouteName], [StartPoint], [EndPoint], [Distance], [EstimatedDuration], [Status]) VALUES (20, N'SG-CM-20', N'TP. Hồ Chí Minh - Cà Mau (Đất Mũi Cực Nam)', N'TP. Hồ Chí Minh', N'Cà Mau', 305.00, 420, N'Active');
INSERT INTO [Route] ([RouteId], [RouteCode], [RouteName], [StartPoint], [EndPoint], [Distance], [EstimatedDuration], [Status]) VALUES (21, N'SG-AG-21', N'TP. Hồ Chí Minh - Châu Đốc (Miếu Bà Núi Sam)', N'TP. Hồ Chí Minh', N'Châu Đốc', 240.00, 360, N'Active');
INSERT INTO [Route] ([RouteId], [RouteCode], [RouteName], [StartPoint], [EndPoint], [Distance], [EstimatedDuration], [Status]) VALUES (22, N'DN-QN-22', N'Đà Nẵng - Quy Nhơn (Duyên hải miền Trung)', N'Đà Nẵng', N'Quy Nhơn', 310.00, 330, N'Active');
INSERT INTO [Route] ([RouteId], [RouteCode], [RouteName], [StartPoint], [EndPoint], [Distance], [EstimatedDuration], [Status]) VALUES (23, N'DN-BMT-23', N'Đà Nẵng - Buôn Ma Thuột (Đường mòn Tây Nguyên)', N'Đà Nẵng', N'Buôn Ma Thuột', 530.00, 600, N'Active');
INSERT INTO [Route] ([RouteId], [RouteCode], [RouteName], [StartPoint], [EndPoint], [Distance], [EstimatedDuration], [Status]) VALUES (24, N'CT-RG-24', N'Cần Thơ - Rạch Giá (Kiên Giang)', N'Cần Thơ', N'Rạch Giá', 110.00, 135, N'Active');
SET IDENTITY_INSERT [Route] OFF;
GO

-- Data for [RouteStop] (5 rows)
SET IDENTITY_INSERT [RouteStop] ON;
INSERT INTO [RouteStop] ([RouteStopId], [RouteId], [StopId], [StopOrder], [ArrivalTime], [DepartureTime]) VALUES (1, 1, 1, 1, '07:00:00', '07:05:00');
INSERT INTO [RouteStop] ([RouteStopId], [RouteId], [StopId], [StopOrder], [ArrivalTime], [DepartureTime]) VALUES (2, 1, 2, 2, '07:20:00', '07:25:00');
INSERT INTO [RouteStop] ([RouteStopId], [RouteId], [StopId], [StopOrder], [ArrivalTime], [DepartureTime]) VALUES (3, 1, 3, 3, '07:50:00', '07:55:00');
INSERT INTO [RouteStop] ([RouteStopId], [RouteId], [StopId], [StopOrder], [ArrivalTime], [DepartureTime]) VALUES (4, 1, 5, 4, '08:40:00', '08:45:00');
INSERT INTO [RouteStop] ([RouteStopId], [RouteId], [StopId], [StopOrder], [ArrivalTime], [DepartureTime]) VALUES (5, 1, 4, 5, '09:00:00', '09:05:00');
SET IDENTITY_INSERT [RouteStop] OFF;
GO

-- Data for [Schedule] (4 rows)
SET IDENTITY_INSERT [Schedule] ON;
INSERT INTO [Schedule] ([ScheduleId], [RouteId], [DepartureTime], [DayOfWeek], [Status]) VALUES (1, 1, '07:00:00', 1, N'Active');
INSERT INTO [Schedule] ([ScheduleId], [RouteId], [DepartureTime], [DayOfWeek], [Status]) VALUES (2, 1, '09:00:00', 1, N'Active');
INSERT INTO [Schedule] ([ScheduleId], [RouteId], [DepartureTime], [DayOfWeek], [Status]) VALUES (3, 1, '13:00:00', 1, N'Active');
INSERT INTO [Schedule] ([ScheduleId], [RouteId], [DepartureTime], [DayOfWeek], [Status]) VALUES (4, 1, '17:00:00', 1, N'Active');
SET IDENTITY_INSERT [Schedule] OFF;
GO

-- Data for [Trip] (24 rows)
SET IDENTITY_INSERT [Trip] ON;
INSERT INTO [Trip] ([TripId], [RouteId], [BusId], [DriverId], [ScheduleId], [TripDate], [DepartureTime], [ArrivalTime], [Status]) VALUES (1, 1, 1, 1, 1, '2026-10-01', '07:00:00', '09:00:00', N'Scheduled');
INSERT INTO [Trip] ([TripId], [RouteId], [BusId], [DriverId], [ScheduleId], [TripDate], [DepartureTime], [ArrivalTime], [Status]) VALUES (2, 2, 4, 2, NULL, '2026-10-01', '19:30:00', '08:00:00', N'Scheduled');
INSERT INTO [Trip] ([TripId], [RouteId], [BusId], [DriverId], [ScheduleId], [TripDate], [DepartureTime], [ArrivalTime], [Status]) VALUES (3, 3, 5, 3, NULL, '2026-10-01', '08:00:00', '09:30:00', N'Scheduled');
INSERT INTO [Trip] ([TripId], [RouteId], [BusId], [DriverId], [ScheduleId], [TripDate], [DepartureTime], [ArrivalTime], [Status]) VALUES (4, 4, 6, 4, NULL, '2026-10-01', '07:00:00', '12:00:00', N'Scheduled');
INSERT INTO [Trip] ([TripId], [RouteId], [BusId], [DriverId], [ScheduleId], [TripDate], [DepartureTime], [ArrivalTime], [Status]) VALUES (5, 5, 5, 1, NULL, '2026-10-01', '08:30:00', '10:45:00', N'Scheduled');
INSERT INTO [Trip] ([TripId], [RouteId], [BusId], [DriverId], [ScheduleId], [TripDate], [DepartureTime], [ArrivalTime], [Status]) VALUES (6, 6, 9, 2, NULL, '2026-10-01', '09:00:00', '10:50:00', N'Scheduled');
INSERT INTO [Trip] ([TripId], [RouteId], [BusId], [DriverId], [ScheduleId], [TripDate], [DepartureTime], [ArrivalTime], [Status]) VALUES (7, 7, 7, 3, NULL, '2026-10-01', '23:00:00', '05:00:00', N'Scheduled');
INSERT INTO [Trip] ([TripId], [RouteId], [BusId], [DriverId], [ScheduleId], [TripDate], [DepartureTime], [ArrivalTime], [Status]) VALUES (8, 8, 8, 4, NULL, '2026-10-01', '07:30:00', '09:30:00', N'Scheduled');
INSERT INTO [Trip] ([TripId], [RouteId], [BusId], [DriverId], [ScheduleId], [TripDate], [DepartureTime], [ArrivalTime], [Status]) VALUES (9, 9, 10, 1, NULL, '2026-10-01', '06:30:00', '09:30:00', N'Scheduled');
INSERT INTO [Trip] ([TripId], [RouteId], [BusId], [DriverId], [ScheduleId], [TripDate], [DepartureTime], [ArrivalTime], [Status]) VALUES (10, 10, 7, 3, NULL, '2026-10-01', '13:30:00', '16:30:00', N'Scheduled');
INSERT INTO [Trip] ([TripId], [RouteId], [BusId], [DriverId], [ScheduleId], [TripDate], [DepartureTime], [ArrivalTime], [Status]) VALUES (12, 12, 12, 6, 1, '2026-10-01', '08:00:00', '09:45:00', N'Scheduled');
INSERT INTO [Trip] ([TripId], [RouteId], [BusId], [DriverId], [ScheduleId], [TripDate], [DepartureTime], [ArrivalTime], [Status]) VALUES (13, 13, 13, 7, 1, '2026-10-01', '07:30:00', '11:45:00', N'Scheduled');
INSERT INTO [Trip] ([TripId], [RouteId], [BusId], [DriverId], [ScheduleId], [TripDate], [DepartureTime], [ArrivalTime], [Status]) VALUES (14, 14, 14, 8, 1, '2026-10-01', '20:30:00', '04:00:00', N'Scheduled');
INSERT INTO [Trip] ([TripId], [RouteId], [BusId], [DriverId], [ScheduleId], [TripDate], [DepartureTime], [ArrivalTime], [Status]) VALUES (16, 16, 16, 6, 1, '2026-10-01', '22:30:00', '06:30:00', N'Scheduled');
INSERT INTO [Trip] ([TripId], [RouteId], [BusId], [DriverId], [ScheduleId], [TripDate], [DepartureTime], [ArrivalTime], [Status]) VALUES (17, 17, 17, 7, 1, '2026-10-01', '18:00:00', '06:30:00', N'Scheduled');
INSERT INTO [Trip] ([TripId], [RouteId], [BusId], [DriverId], [ScheduleId], [TripDate], [DepartureTime], [ArrivalTime], [Status]) VALUES (18, 18, 18, 8, 1, '2026-10-01', '22:00:00', '05:30:00', N'Scheduled');
INSERT INTO [Trip] ([TripId], [RouteId], [BusId], [DriverId], [ScheduleId], [TripDate], [DepartureTime], [ArrivalTime], [Status]) VALUES (20, 20, 20, 6, 1, '2026-10-01', '21:30:00', '05:00:00', N'Scheduled');
INSERT INTO [Trip] ([TripId], [RouteId], [BusId], [DriverId], [ScheduleId], [TripDate], [DepartureTime], [ArrivalTime], [Status]) VALUES (21, 21, 21, 7, 1, '2026-10-01', '23:00:00', '05:30:00', N'Scheduled');
INSERT INTO [Trip] ([TripId], [RouteId], [BusId], [DriverId], [ScheduleId], [TripDate], [DepartureTime], [ArrivalTime], [Status]) VALUES (22, 22, 22, 8, 1, '2026-10-01', '13:00:00', '18:30:00', N'Scheduled');
INSERT INTO [Trip] ([TripId], [RouteId], [BusId], [DriverId], [ScheduleId], [TripDate], [DepartureTime], [ArrivalTime], [Status]) VALUES (24, 24, 24, 6, 1, '2026-10-01', '09:00:00', '11:15:00', N'Scheduled');
INSERT INTO [Trip] ([TripId], [RouteId], [BusId], [DriverId], [ScheduleId], [TripDate], [DepartureTime], [ArrivalTime], [Status]) VALUES (25, 11, 11, 2, 1, '2026-10-01', '21:00:00', '04:30:00', N'Scheduled');
INSERT INTO [Trip] ([TripId], [RouteId], [BusId], [DriverId], [ScheduleId], [TripDate], [DepartureTime], [ArrivalTime], [Status]) VALUES (26, 15, 15, 2, 1, '2026-10-01', '19:00:00', '04:30:00', N'Scheduled');
INSERT INTO [Trip] ([TripId], [RouteId], [BusId], [DriverId], [ScheduleId], [TripDate], [DepartureTime], [ArrivalTime], [Status]) VALUES (27, 19, 19, 2, 1, '2026-10-01', '08:30:00', '11:30:00', N'Scheduled');
INSERT INTO [Trip] ([TripId], [RouteId], [BusId], [DriverId], [ScheduleId], [TripDate], [DepartureTime], [ArrivalTime], [Status]) VALUES (28, 23, 23, 2, 1, '2026-10-01', '19:30:00', '06:00:00', N'Scheduled');
SET IDENTITY_INSERT [Trip] OFF;
GO

-- Data for [Booking] (20 rows)
SET IDENTITY_INSERT [Booking] ON;
INSERT INTO [Booking] ([BookingId], [UserId], [TripId], [BookingCode], [BookingTime], [TotalAmount], [Status]) VALUES (1, 2, 1, N'BK001', '2026-09-28', 300000.00, N'Confirmed');
INSERT INTO [Booking] ([BookingId], [UserId], [TripId], [BookingCode], [BookingTime], [TotalAmount], [Status]) VALUES (2, 2, 2, N'BK-HNDN-001', '2026-09-28', 450000.00, N'CONFIRMED');
INSERT INTO [Booking] ([BookingId], [UserId], [TripId], [BookingCode], [BookingTime], [TotalAmount], [Status]) VALUES (3, 2, 3, N'BK-HNHP-001', '2026-09-28', 120000.00, N'CONFIRMED');
INSERT INTO [Booking] ([BookingId], [UserId], [TripId], [BookingCode], [BookingTime], [TotalAmount], [Status]) VALUES (4, 2, 4, N'BK-HNSP-001', '2026-09-28', 280000.00, N'CONFIRMED');
INSERT INTO [Booking] ([BookingId], [UserId], [TripId], [BookingCode], [BookingTime], [TotalAmount], [Status]) VALUES (5, 2, 7, N'BK-SGDL-001', '2026-09-28', 340000.00, N'CONFIRMED');
INSERT INTO [Booking] ([BookingId], [UserId], [TripId], [BookingCode], [BookingTime], [TotalAmount], [Status]) VALUES (6, 2, 8, N'BK-SGVT-001', '2026-09-28', 160000.00, N'CONFIRMED');
INSERT INTO [Booking] ([BookingId], [UserId], [TripId], [BookingCode], [BookingTime], [TotalAmount], [Status]) VALUES (8, 1, 12, N'BK-NB-001', '2026-09-28', 130000.00, N'PAID');
INSERT INTO [Booking] ([BookingId], [UserId], [TripId], [BookingCode], [BookingTime], [TotalAmount], [Status]) VALUES (9, 1, 13, N'BK-MC-001', '2026-09-28', 220000.00, N'PAID');
INSERT INTO [Booking] ([BookingId], [UserId], [TripId], [BookingCode], [BookingTime], [TotalAmount], [Status]) VALUES (10, 1, 14, N'BK-CB-001', '2026-09-28', 250000.00, N'PAID');
INSERT INTO [Booking] ([BookingId], [UserId], [TripId], [BookingCode], [BookingTime], [TotalAmount], [Status]) VALUES (12, 1, 16, N'BK-NT-001', '2026-09-28', 320000.00, N'PAID');
INSERT INTO [Booking] ([BookingId], [UserId], [TripId], [BookingCode], [BookingTime], [TotalAmount], [Status]) VALUES (13, 1, 17, N'BK-QN-001', '2026-09-28', 350000.00, N'PAID');
INSERT INTO [Booking] ([BookingId], [UserId], [TripId], [BookingCode], [BookingTime], [TotalAmount], [Status]) VALUES (14, 1, 18, N'BK-BMT-001', '2026-09-28', 290000.00, N'PAID');
INSERT INTO [Booking] ([BookingId], [UserId], [TripId], [BookingCode], [BookingTime], [TotalAmount], [Status]) VALUES (16, 1, 20, N'BK-CM-001', '2026-09-28', 260000.00, N'PAID');
INSERT INTO [Booking] ([BookingId], [UserId], [TripId], [BookingCode], [BookingTime], [TotalAmount], [Status]) VALUES (17, 1, 21, N'BK-AG-001', '2026-09-28', 210000.00, N'PAID');
INSERT INTO [Booking] ([BookingId], [UserId], [TripId], [BookingCode], [BookingTime], [TotalAmount], [Status]) VALUES (18, 1, 22, N'BK-DNQN-001', '2026-09-28', 220000.00, N'PAID');
INSERT INTO [Booking] ([BookingId], [UserId], [TripId], [BookingCode], [BookingTime], [TotalAmount], [Status]) VALUES (20, 1, 24, N'BK-CTRG-001', '2026-09-28', 140000.00, N'PAID');
INSERT INTO [Booking] ([BookingId], [UserId], [TripId], [BookingCode], [BookingTime], [TotalAmount], [Status]) VALUES (21, 1, 25, N'BK-HG-001', '2026-09-28', 280000.00, N'PAID');
INSERT INTO [Booking] ([BookingId], [UserId], [TripId], [BookingCode], [BookingTime], [TotalAmount], [Status]) VALUES (22, 1, 26, N'BK-QB-001', '2026-09-28', 380000.00, N'PAID');
INSERT INTO [Booking] ([BookingId], [UserId], [TripId], [BookingCode], [BookingTime], [TotalAmount], [Status]) VALUES (23, 1, 27, N'BK-PT-001', '2026-09-28', 180000.00, N'PAID');
INSERT INTO [Booking] ([BookingId], [UserId], [TripId], [BookingCode], [BookingTime], [TotalAmount], [Status]) VALUES (24, 1, 28, N'BK-DNBMT-001', '2026-09-28', 310000.00, N'PAID');
SET IDENTITY_INSERT [Booking] OFF;
GO

-- Data for [Payment] (1 rows)
SET IDENTITY_INSERT [Payment] ON;
INSERT INTO [Payment] ([PaymentId], [BookingId], [Amount], [PaymentMethod], [TransactionCode], [PaymentTime], [Status]) VALUES (1, 1, 300000.00, N'Banking', N'TRANS001', '2026-09-28', N'Paid');
SET IDENTITY_INSERT [Payment] OFF;
GO

-- Data for [Ticket] (22 rows)
SET IDENTITY_INSERT [Ticket] ON;
INSERT INTO [Ticket] ([TicketId], [BookingId], [TicketCode], [SeatNumber], [Price], [BoardingStopId], [DropOffStopId], [Status]) VALUES (1, 1, N'TK001', N'A01', 100000.00, 1, 4, N'Confirmed');
INSERT INTO [Ticket] ([TicketId], [BookingId], [TicketCode], [SeatNumber], [Price], [BoardingStopId], [DropOffStopId], [Status]) VALUES (2, 1, N'TK002', N'A02', 100000.00, 1, 4, N'Confirmed');
INSERT INTO [Ticket] ([TicketId], [BookingId], [TicketCode], [SeatNumber], [Price], [BoardingStopId], [DropOffStopId], [Status]) VALUES (3, 1, N'TK003', N'A03', 100000.00, 1, 4, N'Confirmed');
INSERT INTO [Ticket] ([TicketId], [BookingId], [TicketCode], [SeatNumber], [Price], [BoardingStopId], [DropOffStopId], [Status]) VALUES (4, 2, N'SBG-HN-DN-20251024-008', N'VIP-05', 450000.00, NULL, NULL, N'CONFIRMED');
INSERT INTO [Ticket] ([TicketId], [BookingId], [TicketCode], [SeatNumber], [Price], [BoardingStopId], [DropOffStopId], [Status]) VALUES (5, 3, N'SBG-HN-HP-001', N'A-08', 120000.00, NULL, NULL, N'CONFIRMED');
INSERT INTO [Ticket] ([TicketId], [BookingId], [TicketCode], [SeatNumber], [Price], [BoardingStopId], [DropOffStopId], [Status]) VALUES (6, 4, N'SBG-HN-SP-001', N'VIP-12', 280000.00, NULL, NULL, N'CONFIRMED');
INSERT INTO [Ticket] ([TicketId], [BookingId], [TicketCode], [SeatNumber], [Price], [BoardingStopId], [DropOffStopId], [Status]) VALUES (7, 5, N'SBG-SG-DL-001', N'VIP-02', 340000.00, NULL, NULL, N'CONFIRMED');
INSERT INTO [Ticket] ([TicketId], [BookingId], [TicketCode], [SeatNumber], [Price], [BoardingStopId], [DropOffStopId], [Status]) VALUES (8, 6, N'SBG-SG-VT-001', N'VIP-09', 160000.00, NULL, NULL, N'CONFIRMED');
INSERT INTO [Ticket] ([TicketId], [BookingId], [TicketCode], [SeatNumber], [Price], [BoardingStopId], [DropOffStopId], [Status]) VALUES (10, 8, N'TK-NB-01', N'A05', 130000.00, 1, 20, N'ISSUED');
INSERT INTO [Ticket] ([TicketId], [BookingId], [TicketCode], [SeatNumber], [Price], [BoardingStopId], [DropOffStopId], [Status]) VALUES (11, 9, N'TK-MC-01', N'VIP02', 220000.00, 1, 21, N'ISSUED');
INSERT INTO [Ticket] ([TicketId], [BookingId], [TicketCode], [SeatNumber], [Price], [BoardingStopId], [DropOffStopId], [Status]) VALUES (12, 10, N'TK-CB-01', N'B03', 250000.00, 1, 22, N'ISSUED');
INSERT INTO [Ticket] ([TicketId], [BookingId], [TicketCode], [SeatNumber], [Price], [BoardingStopId], [DropOffStopId], [Status]) VALUES (14, 12, N'TK-NT-01', N'VIP01', 320000.00, 13, 18, N'ISSUED');
INSERT INTO [Ticket] ([TicketId], [BookingId], [TicketCode], [SeatNumber], [Price], [BoardingStopId], [DropOffStopId], [Status]) VALUES (15, 13, N'TK-QN-01', N'B04', 350000.00, 13, 24, N'ISSUED');
INSERT INTO [Ticket] ([TicketId], [BookingId], [TicketCode], [SeatNumber], [Price], [BoardingStopId], [DropOffStopId], [Status]) VALUES (16, 14, N'TK-BMT-01', N'A08', 290000.00, 13, 25, N'ISSUED');
INSERT INTO [Ticket] ([TicketId], [BookingId], [TicketCode], [SeatNumber], [Price], [BoardingStopId], [DropOffStopId], [Status]) VALUES (18, 16, N'TK-CM-01', N'A12', 260000.00, 14, 27, N'ISSUED');
INSERT INTO [Ticket] ([TicketId], [BookingId], [TicketCode], [SeatNumber], [Price], [BoardingStopId], [DropOffStopId], [Status]) VALUES (19, 17, N'TK-AG-01', N'B02', 210000.00, 14, 28, N'ISSUED');
INSERT INTO [Ticket] ([TicketId], [BookingId], [TicketCode], [SeatNumber], [Price], [BoardingStopId], [DropOffStopId], [Status]) VALUES (20, 18, N'TK-DNQN-01', N'VIP04', 220000.00, 11, 24, N'ISSUED');
INSERT INTO [Ticket] ([TicketId], [BookingId], [TicketCode], [SeatNumber], [Price], [BoardingStopId], [DropOffStopId], [Status]) VALUES (22, 20, N'TK-CTRG-01', N'12', 140000.00, 17, 29, N'ISSUED');
INSERT INTO [Ticket] ([TicketId], [BookingId], [TicketCode], [SeatNumber], [Price], [BoardingStopId], [DropOffStopId], [Status]) VALUES (23, 21, N'TK-HG-01', N'A01', 280000.00, 1, 19, N'ISSUED');
INSERT INTO [Ticket] ([TicketId], [BookingId], [TicketCode], [SeatNumber], [Price], [BoardingStopId], [DropOffStopId], [Status]) VALUES (24, 22, N'TK-QB-01', N'VIP05', 380000.00, 1, 23, N'ISSUED');
INSERT INTO [Ticket] ([TicketId], [BookingId], [TicketCode], [SeatNumber], [Price], [BoardingStopId], [DropOffStopId], [Status]) VALUES (25, 23, N'TK-PT-01', N'04', 180000.00, 13, 26, N'ISSUED');
INSERT INTO [Ticket] ([TicketId], [BookingId], [TicketCode], [SeatNumber], [Price], [BoardingStopId], [DropOffStopId], [Status]) VALUES (26, 24, N'TK-DNBMT-01', N'A03', 310000.00, 11, 25, N'ISSUED');
SET IDENTITY_INSERT [Ticket] OFF;
GO

-- Data for [Notification] (1 rows)
SET IDENTITY_INSERT [Notification] ON;
INSERT INTO [Notification] ([NotificationId], [UserId], [Title], [Content], [Type], [IsRead], [CreatedAt]) VALUES (1, 2, N'Äáº·t vÃ© thÃ nh cÃ´ng', N'ChÃºc má»«ng quÃ½ khÃ¡ch Ä‘Ã£ Ä‘áº·t vÃ© chuyáº¿n HÃ  Ná»™i - ThÃ¡i NguyÃªn thÃ nh cÃ´ng!', N'Booking', N'False', '2026-09-28');
SET IDENTITY_INSERT [Notification] OFF;
GO

-- Data for [VehicleLocation] (2 rows)
SET IDENTITY_INSERT [VehicleLocation] ON;
INSERT INTO [VehicleLocation] ([LocationId], [TripId], [Latitude], [Longitude], [Speed], [RecordedAt]) VALUES (1, 1, 21.0285000, 105.8542000, 40.00, '2026-09-28');
INSERT INTO [VehicleLocation] ([LocationId], [TripId], [Latitude], [Longitude], [Speed], [RecordedAt]) VALUES (2, 1, 21.1450000, 105.8000000, 55.00, '2026-09-28');
SET IDENTITY_INSERT [VehicleLocation] OFF;
GO

EXEC sp_MSforeachtable 'ALTER TABLE ? WITH CHECK CHECK CONSTRAINT ALL';
GO



GO

-- ============================================================================
-- 2. DATA INSERTS
-- ============================================================================
EXEC sp_MSforeachtable 'ALTER TABLE ? NOCHECK CONSTRAINT ALL';
GO

-- Data for [Role] (3 rows)
SET IDENTITY_INSERT [Role] ON;
INSERT INTO [Role] ([RoleId], [RoleName]) VALUES (2, N'Khách hàng');
INSERT INTO [Role] ([RoleId], [RoleName]) VALUES (3, N'Quản trị viên');
INSERT INTO [Role] ([RoleId], [RoleName]) VALUES (1, N'Tài xế');
SET IDENTITY_INSERT [Role] OFF;
GO

-- Data for [User] (9 rows)
SET IDENTITY_INSERT [User] ON;
INSERT INTO [User] ([UserId], [FullName], [Email], [Phone], [PasswordHash], [Status], [CreatedAt]) VALUES (1, N'Nguyễn Văn An', N'driver1@gmail.com', N'0987654321', N'demo_hash', N'Active', '2026-09-28');
INSERT INTO [User] ([UserId], [FullName], [Email], [Phone], [PasswordHash], [Status], [CreatedAt]) VALUES (2, N'Trần Văn Bình', N'customer1@gmail.com', N'0912345678', N'demo_hash', N'Active', '2026-09-28');
INSERT INTO [User] ([UserId], [FullName], [Email], [Phone], [PasswordHash], [Status], [CreatedAt]) VALUES (3, N'Lê Văn Cường', N'admin@gmail.com', N'0901234567', N'demo_hash', N'Active', '2026-09-28');
INSERT INTO [User] ([UserId], [FullName], [Email], [Phone], [PasswordHash], [Status], [CreatedAt]) VALUES (4, N'Trần Đình Trọng', N'trong.tran@smartbus.vn', N'0988777999', N'PASS_HASH', N'ACTIVE', '2026-09-28');
INSERT INTO [User] ([UserId], [FullName], [Email], [Phone], [PasswordHash], [Status], [CreatedAt]) VALUES (5, N'Hoàng Văn Nam', N'nam.hoang@smartbus.vn', N'0977123456', N'PASS_HASH', N'ACTIVE', '2026-09-28');
INSERT INTO [User] ([UserId], [FullName], [Email], [Phone], [PasswordHash], [Status], [CreatedAt]) VALUES (6, N'Đặng Minh Tuấn', N'tuan.dang@smartbus.vn', N'0933666888', N'PASS_HASH', N'ACTIVE', '2026-09-28');
INSERT INTO [User] ([UserId], [FullName], [Email], [Phone], [PasswordHash], [Status], [CreatedAt]) VALUES (7, N'Lê Hoàng Nam', N'nam.le@smartbus.vn', N'0912345002', N'hashed_pass_8', N'ACTIVE', '2026-09-28');
INSERT INTO [User] ([UserId], [FullName], [Email], [Phone], [PasswordHash], [Status], [CreatedAt]) VALUES (8, N'Phạm Quốc Bảo', N'bao.pham@smartbus.vn', N'0912345003', N'hashed_pass_9', N'ACTIVE', '2026-09-28');
INSERT INTO [User] ([UserId], [FullName], [Email], [Phone], [PasswordHash], [Status], [CreatedAt]) VALUES (9, N'Đỗ Minh Quân', N'quan.do@smartbus.vn', N'0912345004', N'hashed_pass_10', N'ACTIVE', '2026-09-28');
SET IDENTITY_INSERT [User] OFF;
GO

-- Data for [UserRole] (3 rows)
INSERT INTO [UserRole] ([UserId], [RoleId]) VALUES (1, 3);
INSERT INTO [UserRole] ([UserId], [RoleId]) VALUES (2, 2);
INSERT INTO [UserRole] ([UserId], [RoleId]) VALUES (3, 1);
GO

-- Data for [Driver] (7 rows)
SET IDENTITY_INSERT [Driver] ON;
INSERT INTO [Driver] ([DriverId], [UserId], [LicenseNo], [LicenseExpiryDate], [Status]) VALUES (1, 1, N'B123456789', '2028-12-31', N'Active');
INSERT INTO [Driver] ([DriverId], [UserId], [LicenseNo], [LicenseExpiryDate], [Status]) VALUES (2, 4, N'FC-988214', '2028-12-31', N'ACTIVE');
INSERT INTO [Driver] ([DriverId], [UserId], [LicenseNo], [LicenseExpiryDate], [Status]) VALUES (3, 5, N'FC-554129', '2029-06-30', N'ACTIVE');
INSERT INTO [Driver] ([DriverId], [UserId], [LicenseNo], [LicenseExpiryDate], [Status]) VALUES (4, 6, N'FC-882103', '2027-10-15', N'ACTIVE');
INSERT INTO [Driver] ([DriverId], [UserId], [LicenseNo], [LicenseExpiryDate], [Status]) VALUES (6, 7, N'E-88776655', '2030-12-31', N'Active');
INSERT INTO [Driver] ([DriverId], [UserId], [LicenseNo], [LicenseExpiryDate], [Status]) VALUES (7, 8, N'E-77665544', '2030-12-31', N'Active');
INSERT INTO [Driver] ([DriverId], [UserId], [LicenseNo], [LicenseExpiryDate], [Status]) VALUES (8, 9, N'E-66554433', '2030-12-31', N'Active');
SET IDENTITY_INSERT [Driver] OFF;
GO

-- Data for [BusType] (5 rows)
SET IDENTITY_INSERT [BusType] ON;
INSERT INTO [BusType] ([BusTypeId], [TypeName], [Capacity], [Description]) VALUES (1, N'Xe 29 chỗ', 16, N'Xe khách 16 ch?');
INSERT INTO [BusType] ([BusTypeId], [TypeName], [Capacity], [Description]) VALUES (2, N'Xe 29 chỗ', 29, N'Xe khách 29 ch?');
INSERT INTO [BusType] ([BusTypeId], [TypeName], [Capacity], [Description]) VALUES (3, N'Xe 45 chỗ', 45, N'Xe khách 45 ch?');
INSERT INTO [BusType] ([BusTypeId], [TypeName], [Capacity], [Description]) VALUES (4, N'Limousine VIP 22 Phòng', 22, N'Khoang cung điện VIP riêng tư, massage, tivi, tai nghe');
INSERT INTO [BusType] ([BusTypeId], [TypeName], [Capacity], [Description]) VALUES (5, N'Giường Nằm 34 Phòng', 34, N'Giường nằm Green Express cao cấp, sạc Type-C, wifi tốc độ cao');
SET IDENTITY_INSERT [BusType] OFF;
GO

-- Data for [Bus] (24 rows)
SET IDENTITY_INSERT [Bus] ON;
INSERT INTO [Bus] ([BusId], [BusTypeId], [LicensePlate], [Capacity], [Status]) VALUES (1, 2, N'20A-12345', 29, N'Active');
INSERT INTO [Bus] ([BusId], [BusTypeId], [LicensePlate], [Capacity], [Status]) VALUES (2, 2, N'20A-67890', 29, N'Active');
INSERT INTO [Bus] ([BusId], [BusTypeId], [LicensePlate], [Capacity], [Status]) VALUES (3, 3, N'20B-11111', 45, N'Active');
INSERT INTO [Bus] ([BusId], [BusTypeId], [LicensePlate], [Capacity], [Status]) VALUES (4, 4, N'29B-888.68', 22, N'ACTIVE');
INSERT INTO [Bus] ([BusId], [BusTypeId], [LicensePlate], [Capacity], [Status]) VALUES (5, 1, N'15B-112.23', 29, N'ACTIVE');
INSERT INTO [Bus] ([BusId], [BusTypeId], [LicensePlate], [Capacity], [Status]) VALUES (6, 4, N'29B-666.99', 22, N'ACTIVE');
INSERT INTO [Bus] ([BusId], [BusTypeId], [LicensePlate], [Capacity], [Status]) VALUES (7, 5, N'51B-234.56', 34, N'ACTIVE');
INSERT INTO [Bus] ([BusId], [BusTypeId], [LicensePlate], [Capacity], [Status]) VALUES (8, 1, N'51B-789.10', 29, N'ACTIVE');
INSERT INTO [Bus] ([BusId], [BusTypeId], [LicensePlate], [Capacity], [Status]) VALUES (9, 1, N'43B-098.76', 29, N'ACTIVE');
INSERT INTO [Bus] ([BusId], [BusTypeId], [LicensePlate], [Capacity], [Status]) VALUES (10, 5, N'51B-333.44', 34, N'ACTIVE');
INSERT INTO [Bus] ([BusId], [BusTypeId], [LicensePlate], [Capacity], [Status]) VALUES (11, 5, N'29B-987.65', 34, N'Active');
INSERT INTO [Bus] ([BusId], [BusTypeId], [LicensePlate], [Capacity], [Status]) VALUES (12, 2, N'35B-123.45', 29, N'Active');
INSERT INTO [Bus] ([BusId], [BusTypeId], [LicensePlate], [Capacity], [Status]) VALUES (13, 4, N'26B-555.66', 22, N'Active');
INSERT INTO [Bus] ([BusId], [BusTypeId], [LicensePlate], [Capacity], [Status]) VALUES (14, 5, N'11B-777.88', 34, N'Active');
INSERT INTO [Bus] ([BusId], [BusTypeId], [LicensePlate], [Capacity], [Status]) VALUES (15, 4, N'73B-888.99', 22, N'Active');
INSERT INTO [Bus] ([BusId], [BusTypeId], [LicensePlate], [Capacity], [Status]) VALUES (16, 4, N'51B-666.11', 22, N'Active');
INSERT INTO [Bus] ([BusId], [BusTypeId], [LicensePlate], [Capacity], [Status]) VALUES (17, 5, N'77B-222.33', 34, N'Active');
INSERT INTO [Bus] ([BusId], [BusTypeId], [LicensePlate], [Capacity], [Status]) VALUES (18, 5, N'47B-999.55', 34, N'Active');
INSERT INTO [Bus] ([BusId], [BusTypeId], [LicensePlate], [Capacity], [Status]) VALUES (19, 2, N'86B-333.77', 29, N'Active');
INSERT INTO [Bus] ([BusId], [BusTypeId], [LicensePlate], [Capacity], [Status]) VALUES (20, 5, N'69B-888.22', 34, N'Active');
INSERT INTO [Bus] ([BusId], [BusTypeId], [LicensePlate], [Capacity], [Status]) VALUES (21, 5, N'67B-444.66', 34, N'Active');
INSERT INTO [Bus] ([BusId], [BusTypeId], [LicensePlate], [Capacity], [Status]) VALUES (22, 4, N'43B-111.99', 22, N'Active');
INSERT INTO [Bus] ([BusId], [BusTypeId], [LicensePlate], [Capacity], [Status]) VALUES (23, 5, N'43B-555.88', 34, N'Active');
INSERT INTO [Bus] ([BusId], [BusTypeId], [LicensePlate], [Capacity], [Status]) VALUES (24, 2, N'65B-222.44', 29, N'Active');
SET IDENTITY_INSERT [Bus] OFF;
GO

-- Data for [BusStop] (30 rows)
SET IDENTITY_INSERT [BusStop] ON;
INSERT INTO [BusStop] ([StopId], [StopName], [Address], [Latitude], [Longitude]) VALUES (1, N'Bến xe Mỹ Đình', N'Phạm Hùng, Nam Từ Liêm, Hà Nội', NULL, NULL);
INSERT INTO [BusStop] ([StopId], [StopName], [Address], [Latitude], [Longitude]) VALUES (2, N'Điểm Cầu Giấy', N'Đường Cầu Giấy, Cầu Giấy, Hà Nội', NULL, NULL);
INSERT INTO [BusStop] ([StopId], [StopName], [Address], [Latitude], [Longitude]) VALUES (3, N'Sân bay Nội Bài', N'Xã Phú Minh, Huyện Sóc Sơn, Hà Nội', NULL, NULL);
INSERT INTO [BusStop] ([StopId], [StopName], [Address], [Latitude], [Longitude]) VALUES (4, N'Bến xe Thái Nguyên', N'Đường Lương Ngọc Quyến, TP. Thái Nguyên', NULL, NULL);
INSERT INTO [BusStop] ([StopId], [StopName], [Address], [Latitude], [Longitude]) VALUES (5, N'Điểm đón Phổ Yên', N'Phường Ba Hàng, TP. Phổ Yên, Thái Nguyên', NULL, NULL);
INSERT INTO [BusStop] ([StopId], [StopName], [Address], [Latitude], [Longitude]) VALUES (6, N'Bến xe Nước Ngầm', N'Km 8 Giải Phóng, Hoàng Mai, Hà Nội', NULL, NULL);
INSERT INTO [BusStop] ([StopId], [StopName], [Address], [Latitude], [Longitude]) VALUES (7, N'Bến xe Giáp Bát', N'Giải Phóng, Hoàng Mai, Hà Nội', NULL, NULL);
INSERT INTO [BusStop] ([StopId], [StopName], [Address], [Latitude], [Longitude]) VALUES (8, N'Bến xe Cầu Rào', N'Ngô Gia Tự, Lê Chân, Hải Phòng', NULL, NULL);
INSERT INTO [BusStop] ([StopId], [StopName], [Address], [Latitude], [Longitude]) VALUES (9, N'Bến xe Sa Pa', N'Điện Biên Phủ, Sa Pa, Lào Cai', NULL, NULL);
INSERT INTO [BusStop] ([StopId], [StopName], [Address], [Latitude], [Longitude]) VALUES (10, N'Bến xe Bãi Cháy', N'Hạ Long, Quảng Ninh', NULL, NULL);
INSERT INTO [BusStop] ([StopId], [StopName], [Address], [Latitude], [Longitude]) VALUES (11, N'Bến xe Trung Tâm Đà Nẵng', N'Nam Trân, Liên Chiểu, Đà Nẵng', NULL, NULL);
INSERT INTO [BusStop] ([StopId], [StopName], [Address], [Latitude], [Longitude]) VALUES (12, N'Bến xe Phía Nam Huế', N'An Dương Vương, An Cựu, Huế', NULL, NULL);
INSERT INTO [BusStop] ([StopId], [StopName], [Address], [Latitude], [Longitude]) VALUES (13, N'Bến xe Miền Đông Mới', N'Hoàng Hữu Nam, TP. Thủ Đức, TP. Hồ Chí Minh', NULL, NULL);
INSERT INTO [BusStop] ([StopId], [StopName], [Address], [Latitude], [Longitude]) VALUES (14, N'Bến xe Miền Tây', N'Kinh Dương Vương, Bình Tân, TP. Hồ Chí Minh', NULL, NULL);
INSERT INTO [BusStop] ([StopId], [StopName], [Address], [Latitude], [Longitude]) VALUES (15, N'Bến xe Liên Tỉnh Đà Lạt', N'Số 1 Tô Hiến Thành, TP. Đà Lạt, Lâm Đồng', NULL, NULL);
INSERT INTO [BusStop] ([StopId], [StopName], [Address], [Latitude], [Longitude]) VALUES (16, N'Bến xe Vũng Tàu', N'Nam Kỳ Khởi Nghĩa, TP. Vũng Tàu', NULL, NULL);
INSERT INTO [BusStop] ([StopId], [StopName], [Address], [Latitude], [Longitude]) VALUES (17, N'Bến xe Cần Thơ', N'Đường dẫn cầu Cần Thơ, Cái Răng, Cần Thơ', NULL, NULL);
INSERT INTO [BusStop] ([StopId], [StopName], [Address], [Latitude], [Longitude]) VALUES (18, N'Bến xe Nha Trang', N'Đường 23/10, TP. Nha Trang, Khánh Hòa', NULL, NULL);
INSERT INTO [BusStop] ([StopId], [StopName], [Address], [Latitude], [Longitude]) VALUES (19, N'Bến xe Hà Giang', N'Phường Phương Thiện, TP. Hà Giang', 22.8021000, 104.9812000);
INSERT INTO [BusStop] ([StopId], [StopName], [Address], [Latitude], [Longitude]) VALUES (20, N'Bến xe Ninh Bình', N'Đường Lê Đạo, Phường Vân Gia, TP. Ninh Bình', 20.2520000, 105.9750000);
INSERT INTO [BusStop] ([StopId], [StopName], [Address], [Latitude], [Longitude]) VALUES (21, N'Bến xe Mộc Châu', N'Thị trấn Mộc Châu, Huyện Mộc Châu, Sơn La', 20.8410000, 104.6420000);
INSERT INTO [BusStop] ([StopId], [StopName], [Address], [Latitude], [Longitude]) VALUES (22, N'Bến xe Cao Bằng', N'Phường Đề Thám, TP. Cao Bằng', 22.6580000, 106.2570000);
INSERT INTO [BusStop] ([StopId], [StopName], [Address], [Latitude], [Longitude]) VALUES (23, N'Bến xe Đồng Hới (Quảng Bình)', N'Trần Hưng Đạo, TP. Đồng Hới, Quảng Bình', 17.4720000, 106.6020000);
INSERT INTO [BusStop] ([StopId], [StopName], [Address], [Latitude], [Longitude]) VALUES (24, N'Bến xe Quy Nhơn', N'Số 71 Tây Sơn, Phường Ghềnh Ráng, TP. Quy Nhơn, Bình Định', 13.7540000, 109.2130000);
INSERT INTO [BusStop] ([StopId], [StopName], [Address], [Latitude], [Longitude]) VALUES (25, N'Bến xe Buôn Ma Thuột', N'Đường Nguyễn Tất Thành, TP. Buôn Ma Thuột, Đắk Lắk', 12.6880000, 108.0580000);
INSERT INTO [BusStop] ([StopId], [StopName], [Address], [Latitude], [Longitude]) VALUES (26, N'Bến xe Phan Thiết (Bình Thuận)', N'Đường Từ Văn Tư, TP. Phan Thiết, Bình Thuận', 10.9320000, 108.0980000);
INSERT INTO [BusStop] ([StopId], [StopName], [Address], [Latitude], [Longitude]) VALUES (27, N'Bến xe Cà Mau', N'Quốc Lộ 1A, Lý Thường Kiệt, Khóm 5, TP. Cà Mau', 9.1760000, 105.1520000);
INSERT INTO [BusStop] ([StopId], [StopName], [Address], [Latitude], [Longitude]) VALUES (28, N'Bến xe Châu Đốc (An Giang)', N'Đường Lê Hồng Phong, Phường Châu Phú B, TP. Châu Đốc', 10.7020000, 105.1120000);
INSERT INTO [BusStop] ([StopId], [StopName], [Address], [Latitude], [Longitude]) VALUES (29, N'Bến xe Rạch Sỏi (Rạch Giá - Kiên Giang)', N'Mai Thị Hồng Hạnh, Phường Rạch Sỏi, TP. Rạch Giá', 9.9630000, 105.1450000);
INSERT INTO [BusStop] ([StopId], [StopName], [Address], [Latitude], [Longitude]) VALUES (30, N'Bến xe An Sương (TP.HCM)', N'Quốc Lộ 22, Xã Bà Điểm, Huyện Hóc Môn, TP. Hồ Chí Minh', 10.8490000, 106.6080000);
SET IDENTITY_INSERT [BusStop] OFF;
GO

-- Data for [Route] (24 rows)
SET IDENTITY_INSERT [Route] ON;
INSERT INTO [Route] ([RouteId], [RouteCode], [RouteName], [StartPoint], [EndPoint], [Distance], [EstimatedDuration], [Status]) VALUES (1, N'HN-TN-01', N'Hà Nội - Thái Nguyên', N'Hà Nội', N'Thái Nguyên', 80.00, 120, N'Active');
INSERT INTO [Route] ([RouteId], [RouteCode], [RouteName], [StartPoint], [EndPoint], [Distance], [EstimatedDuration], [Status]) VALUES (2, N'HN-DN-08', N'Hà Nội - Đà Nẵng (Cao tốc Bắc Nam)', N'Hà Nội', N'Đà Nẵng', 760.00, 750, N'ACTIVE');
INSERT INTO [Route] ([RouteId], [RouteCode], [RouteName], [StartPoint], [EndPoint], [Distance], [EstimatedDuration], [Status]) VALUES (3, N'HN-HP-02', N'Hà Nội - Hải Phòng (Cao tốc 5B)', N'Hà Nội', N'Hải Phòng', 105.00, 90, N'ACTIVE');
INSERT INTO [Route] ([RouteId], [RouteCode], [RouteName], [StartPoint], [EndPoint], [Distance], [EstimatedDuration], [Status]) VALUES (4, N'HN-SP-03', N'Hà Nội - Sa Pa (Cao tốc Lào Cai)', N'Hà Nội', N'Sa Pa', 320.00, 300, N'ACTIVE');
INSERT INTO [Route] ([RouteId], [RouteCode], [RouteName], [StartPoint], [EndPoint], [Distance], [EstimatedDuration], [Status]) VALUES (5, N'HN-QN-04', N'Hà Nội - Hạ Long (Quảng Ninh)', N'Hà Nội', N'Hạ Long', 160.00, 130, N'ACTIVE');
INSERT INTO [Route] ([RouteId], [RouteCode], [RouteName], [StartPoint], [EndPoint], [Distance], [EstimatedDuration], [Status]) VALUES (6, N'DN-HUE-05', N'Đà Nẵng - Huế (Hầm Hải Vân)', N'Đà Nẵng', N'Huế', 100.00, 110, N'ACTIVE');
INSERT INTO [Route] ([RouteId], [RouteCode], [RouteName], [StartPoint], [EndPoint], [Distance], [EstimatedDuration], [Status]) VALUES (7, N'SG-DL-06', N'TP. Hồ Chí Minh - Đà Lạt (Cung Điện VIP)', N'TP. Hồ Chí Minh', N'Đà Lạt', 305.00, 360, N'ACTIVE');
INSERT INTO [Route] ([RouteId], [RouteCode], [RouteName], [StartPoint], [EndPoint], [Distance], [EstimatedDuration], [Status]) VALUES (8, N'SG-VT-07', N'TP. Hồ Chí Minh - Vũng Tàu (Cao tốc)', N'TP. Hồ Chí Minh', N'Vũng Tàu', 100.00, 120, N'ACTIVE');
INSERT INTO [Route] ([RouteId], [RouteCode], [RouteName], [StartPoint], [EndPoint], [Distance], [EstimatedDuration], [Status]) VALUES (9, N'SG-CT-09', N'TP. Hồ Chí Minh - Cần Thơ (Cao tốc Mỹ Thuận)', N'TP. Hồ Chí Minh', N'Cần Thơ', 165.00, 180, N'ACTIVE');
INSERT INTO [Route] ([RouteId], [RouteCode], [RouteName], [StartPoint], [EndPoint], [Distance], [EstimatedDuration], [Status]) VALUES (10, N'DL-NT-10', N'Đà Lạt - Nha Trang (Đèo Khánh Lê)', N'Đà Lạt', N'Nha Trang', 135.00, 180, N'ACTIVE');
INSERT INTO [Route] ([RouteId], [RouteCode], [RouteName], [StartPoint], [EndPoint], [Distance], [EstimatedDuration], [Status]) VALUES (11, N'HN-HG-11', N'Hà Nội - Hà Giang (Cao nguyên đá Đồng Văn)', N'Hà Nội', N'Hà Giang', 300.00, 420, N'Active');
INSERT INTO [Route] ([RouteId], [RouteCode], [RouteName], [StartPoint], [EndPoint], [Distance], [EstimatedDuration], [Status]) VALUES (12, N'HN-NB-12', N'Hà Nội - Ninh Bình (Tràng An - Bái Đính)', N'Hà Nội', N'Ninh Bình', 95.00, 105, N'Active');
INSERT INTO [Route] ([RouteId], [RouteCode], [RouteName], [StartPoint], [EndPoint], [Distance], [EstimatedDuration], [Status]) VALUES (13, N'HN-MC-13', N'Hà Nội - Mộc Châu (Thung lũng Sơn La)', N'Hà Nội', N'Mộc Châu', 205.00, 250, N'Active');
INSERT INTO [Route] ([RouteId], [RouteCode], [RouteName], [StartPoint], [EndPoint], [Distance], [EstimatedDuration], [Status]) VALUES (14, N'HN-CB-14', N'Hà Nội - Cao Bằng (Thác Bản Giốc)', N'Hà Nội', N'Cao Bằng', 280.00, 360, N'Active');
INSERT INTO [Route] ([RouteId], [RouteCode], [RouteName], [StartPoint], [EndPoint], [Distance], [EstimatedDuration], [Status]) VALUES (15, N'HN-QB-15', N'Hà Nội - Quảng Bình (Đồng Hới - Phong Nha)', N'Hà Nội', N'Quảng Bình', 500.00, 540, N'Active');
INSERT INTO [Route] ([RouteId], [RouteCode], [RouteName], [StartPoint], [EndPoint], [Distance], [EstimatedDuration], [Status]) VALUES (16, N'SG-NT-16', N'TP. Hồ Chí Minh - Nha Trang (Biển Nha Trang)', N'TP. Hồ Chí Minh', N'Nha Trang', 430.00, 480, N'Active');
INSERT INTO [Route] ([RouteId], [RouteCode], [RouteName], [StartPoint], [EndPoint], [Distance], [EstimatedDuration], [Status]) VALUES (17, N'SG-QN-17', N'TP. Hồ Chí Minh - Quy Nhơn (Eo Gió - Kỳ Co)', N'TP. Hồ Chí Minh', N'Quy Nhơn', 650.00, 720, N'Active');
INSERT INTO [Route] ([RouteId], [RouteCode], [RouteName], [StartPoint], [EndPoint], [Distance], [EstimatedDuration], [Status]) VALUES (18, N'SG-BMT-18', N'TP. Hồ Chí Minh - Buôn Ma Thuột (Thủ phủ Cà Phê)', N'TP. Hồ Chí Minh', N'Buôn Ma Thuột', 350.00, 420, N'Active');
INSERT INTO [Route] ([RouteId], [RouteCode], [RouteName], [StartPoint], [EndPoint], [Distance], [EstimatedDuration], [Status]) VALUES (19, N'SG-PT-19', N'TP. Hồ Chí Minh - Phan Thiết (Mũi Né)', N'TP. Hồ Chí Minh', N'Phan Thiết', 200.00, 180, N'Active');
INSERT INTO [Route] ([RouteId], [RouteCode], [RouteName], [StartPoint], [EndPoint], [Distance], [EstimatedDuration], [Status]) VALUES (20, N'SG-CM-20', N'TP. Hồ Chí Minh - Cà Mau (Đất Mũi Cực Nam)', N'TP. Hồ Chí Minh', N'Cà Mau', 305.00, 420, N'Active');
INSERT INTO [Route] ([RouteId], [RouteCode], [RouteName], [StartPoint], [EndPoint], [Distance], [EstimatedDuration], [Status]) VALUES (21, N'SG-AG-21', N'TP. Hồ Chí Minh - Châu Đốc (Miếu Bà Núi Sam)', N'TP. Hồ Chí Minh', N'Châu Đốc', 240.00, 360, N'Active');
INSERT INTO [Route] ([RouteId], [RouteCode], [RouteName], [StartPoint], [EndPoint], [Distance], [EstimatedDuration], [Status]) VALUES (22, N'DN-QN-22', N'Đà Nẵng - Quy Nhơn (Duyên hải miền Trung)', N'Đà Nẵng', N'Quy Nhơn', 310.00, 330, N'Active');
INSERT INTO [Route] ([RouteId], [RouteCode], [RouteName], [StartPoint], [EndPoint], [Distance], [EstimatedDuration], [Status]) VALUES (23, N'DN-BMT-23', N'Đà Nẵng - Buôn Ma Thuột (Đường mòn Tây Nguyên)', N'Đà Nẵng', N'Buôn Ma Thuột', 530.00, 600, N'Active');
INSERT INTO [Route] ([RouteId], [RouteCode], [RouteName], [StartPoint], [EndPoint], [Distance], [EstimatedDuration], [Status]) VALUES (24, N'CT-RG-24', N'Cần Thơ - Rạch Giá (Kiên Giang)', N'Cần Thơ', N'Rạch Giá', 110.00, 135, N'Active');
SET IDENTITY_INSERT [Route] OFF;
GO

-- Data for [RouteStop] (5 rows)
SET IDENTITY_INSERT [RouteStop] ON;
INSERT INTO [RouteStop] ([RouteStopId], [RouteId], [StopId], [StopOrder], [ArrivalTime], [DepartureTime]) VALUES (1, 1, 1, 1, '07:00:00', '07:05:00');
INSERT INTO [RouteStop] ([RouteStopId], [RouteId], [StopId], [StopOrder], [ArrivalTime], [DepartureTime]) VALUES (2, 1, 2, 2, '07:20:00', '07:25:00');
INSERT INTO [RouteStop] ([RouteStopId], [RouteId], [StopId], [StopOrder], [ArrivalTime], [DepartureTime]) VALUES (3, 1, 3, 3, '07:50:00', '07:55:00');
INSERT INTO [RouteStop] ([RouteStopId], [RouteId], [StopId], [StopOrder], [ArrivalTime], [DepartureTime]) VALUES (4, 1, 5, 4, '08:40:00', '08:45:00');
INSERT INTO [RouteStop] ([RouteStopId], [RouteId], [StopId], [StopOrder], [ArrivalTime], [DepartureTime]) VALUES (5, 1, 4, 5, '09:00:00', '09:05:00');
SET IDENTITY_INSERT [RouteStop] OFF;
GO

-- Data for [Schedule] (4 rows)
SET IDENTITY_INSERT [Schedule] ON;
INSERT INTO [Schedule] ([ScheduleId], [RouteId], [DepartureTime], [DayOfWeek], [Status]) VALUES (1, 1, '07:00:00', 1, N'Active');
INSERT INTO [Schedule] ([ScheduleId], [RouteId], [DepartureTime], [DayOfWeek], [Status]) VALUES (2, 1, '09:00:00', 1, N'Active');
INSERT INTO [Schedule] ([ScheduleId], [RouteId], [DepartureTime], [DayOfWeek], [Status]) VALUES (3, 1, '13:00:00', 1, N'Active');
INSERT INTO [Schedule] ([ScheduleId], [RouteId], [DepartureTime], [DayOfWeek], [Status]) VALUES (4, 1, '17:00:00', 1, N'Active');
SET IDENTITY_INSERT [Schedule] OFF;
GO

-- Data for [Trip] (72 rows)
SET IDENTITY_INSERT [Trip] ON;
INSERT INTO [Trip] ([TripId], [RouteId], [BusId], [DriverId], [ScheduleId], [TripDate], [DepartureTime], [ArrivalTime], [Status]) VALUES (1, 1, 1, 1, 1, '2026-10-01', '07:00:00', '09:00:00', N'Scheduled');
INSERT INTO [Trip] ([TripId], [RouteId], [BusId], [DriverId], [ScheduleId], [TripDate], [DepartureTime], [ArrivalTime], [Status]) VALUES (2, 2, 4, 2, NULL, '2026-10-01', '19:30:00', '08:00:00', N'Scheduled');
INSERT INTO [Trip] ([TripId], [RouteId], [BusId], [DriverId], [ScheduleId], [TripDate], [DepartureTime], [ArrivalTime], [Status]) VALUES (3, 3, 5, 3, NULL, '2026-10-01', '08:00:00', '09:30:00', N'Scheduled');
INSERT INTO [Trip] ([TripId], [RouteId], [BusId], [DriverId], [ScheduleId], [TripDate], [DepartureTime], [ArrivalTime], [Status]) VALUES (4, 4, 6, 4, NULL, '2026-10-01', '07:00:00', '12:00:00', N'Scheduled');
INSERT INTO [Trip] ([TripId], [RouteId], [BusId], [DriverId], [ScheduleId], [TripDate], [DepartureTime], [ArrivalTime], [Status]) VALUES (5, 5, 5, 1, NULL, '2026-10-01', '08:30:00', '10:45:00', N'Scheduled');
INSERT INTO [Trip] ([TripId], [RouteId], [BusId], [DriverId], [ScheduleId], [TripDate], [DepartureTime], [ArrivalTime], [Status]) VALUES (6, 6, 9, 2, NULL, '2026-10-01', '09:00:00', '10:50:00', N'Scheduled');
INSERT INTO [Trip] ([TripId], [RouteId], [BusId], [DriverId], [ScheduleId], [TripDate], [DepartureTime], [ArrivalTime], [Status]) VALUES (7, 7, 7, 3, NULL, '2026-10-01', '23:00:00', '05:00:00', N'Scheduled');
INSERT INTO [Trip] ([TripId], [RouteId], [BusId], [DriverId], [ScheduleId], [TripDate], [DepartureTime], [ArrivalTime], [Status]) VALUES (8, 8, 8, 4, NULL, '2026-10-01', '07:30:00', '09:30:00', N'Scheduled');
INSERT INTO [Trip] ([TripId], [RouteId], [BusId], [DriverId], [ScheduleId], [TripDate], [DepartureTime], [ArrivalTime], [Status]) VALUES (9, 9, 10, 1, NULL, '2026-10-01', '06:30:00', '09:30:00', N'Scheduled');
INSERT INTO [Trip] ([TripId], [RouteId], [BusId], [DriverId], [ScheduleId], [TripDate], [DepartureTime], [ArrivalTime], [Status]) VALUES (10, 10, 7, 3, NULL, '2026-10-01', '13:30:00', '16:30:00', N'Scheduled');
INSERT INTO [Trip] ([TripId], [RouteId], [BusId], [DriverId], [ScheduleId], [TripDate], [DepartureTime], [ArrivalTime], [Status]) VALUES (12, 12, 12, 6, 1, '2026-10-01', '08:00:00', '09:45:00', N'Scheduled');
INSERT INTO [Trip] ([TripId], [RouteId], [BusId], [DriverId], [ScheduleId], [TripDate], [DepartureTime], [ArrivalTime], [Status]) VALUES (13, 13, 13, 7, 1, '2026-10-01', '07:30:00', '11:45:00', N'Scheduled');
INSERT INTO [Trip] ([TripId], [RouteId], [BusId], [DriverId], [ScheduleId], [TripDate], [DepartureTime], [ArrivalTime], [Status]) VALUES (14, 14, 14, 8, 1, '2026-10-01', '20:30:00', '04:00:00', N'Scheduled');
INSERT INTO [Trip] ([TripId], [RouteId], [BusId], [DriverId], [ScheduleId], [TripDate], [DepartureTime], [ArrivalTime], [Status]) VALUES (16, 16, 16, 6, 1, '2026-10-01', '22:30:00', '06:30:00', N'Scheduled');
INSERT INTO [Trip] ([TripId], [RouteId], [BusId], [DriverId], [ScheduleId], [TripDate], [DepartureTime], [ArrivalTime], [Status]) VALUES (17, 17, 17, 7, 1, '2026-10-01', '18:00:00', '06:30:00', N'Scheduled');
INSERT INTO [Trip] ([TripId], [RouteId], [BusId], [DriverId], [ScheduleId], [TripDate], [DepartureTime], [ArrivalTime], [Status]) VALUES (18, 18, 18, 8, 1, '2026-10-01', '22:00:00', '05:30:00', N'Scheduled');
INSERT INTO [Trip] ([TripId], [RouteId], [BusId], [DriverId], [ScheduleId], [TripDate], [DepartureTime], [ArrivalTime], [Status]) VALUES (20, 20, 20, 6, 1, '2026-10-01', '21:30:00', '05:00:00', N'Scheduled');
INSERT INTO [Trip] ([TripId], [RouteId], [BusId], [DriverId], [ScheduleId], [TripDate], [DepartureTime], [ArrivalTime], [Status]) VALUES (21, 21, 21, 7, 1, '2026-10-01', '23:00:00', '05:30:00', N'Scheduled');
INSERT INTO [Trip] ([TripId], [RouteId], [BusId], [DriverId], [ScheduleId], [TripDate], [DepartureTime], [ArrivalTime], [Status]) VALUES (22, 22, 22, 8, 1, '2026-10-01', '13:00:00', '18:30:00', N'Scheduled');
INSERT INTO [Trip] ([TripId], [RouteId], [BusId], [DriverId], [ScheduleId], [TripDate], [DepartureTime], [ArrivalTime], [Status]) VALUES (24, 24, 24, 6, 1, '2026-10-01', '09:00:00', '11:15:00', N'Scheduled');
INSERT INTO [Trip] ([TripId], [RouteId], [BusId], [DriverId], [ScheduleId], [TripDate], [DepartureTime], [ArrivalTime], [Status]) VALUES (25, 11, 11, 2, 1, '2026-10-01', '21:00:00', '04:30:00', N'Scheduled');
INSERT INTO [Trip] ([TripId], [RouteId], [BusId], [DriverId], [ScheduleId], [TripDate], [DepartureTime], [ArrivalTime], [Status]) VALUES (26, 15, 15, 2, 1, '2026-10-01', '19:00:00', '04:30:00', N'Scheduled');
INSERT INTO [Trip] ([TripId], [RouteId], [BusId], [DriverId], [ScheduleId], [TripDate], [DepartureTime], [ArrivalTime], [Status]) VALUES (27, 19, 19, 2, 1, '2026-10-01', '08:30:00', '11:30:00', N'Scheduled');
INSERT INTO [Trip] ([TripId], [RouteId], [BusId], [DriverId], [ScheduleId], [TripDate], [DepartureTime], [ArrivalTime], [Status]) VALUES (28, 23, 23, 2, 1, '2026-10-01', '19:30:00', '06:00:00', N'Scheduled');
INSERT INTO [Trip] ([TripId], [RouteId], [BusId], [DriverId], [ScheduleId], [TripDate], [DepartureTime], [ArrivalTime], [Status]) VALUES (29, 1, 1, 1, 1, '2026-09-29', '07:00:00', '09:00:00', N'Scheduled');
INSERT INTO [Trip] ([TripId], [RouteId], [BusId], [DriverId], [ScheduleId], [TripDate], [DepartureTime], [ArrivalTime], [Status]) VALUES (30, 2, 4, 2, NULL, '2026-09-29', '19:30:00', '08:00:00', N'Scheduled');
INSERT INTO [Trip] ([TripId], [RouteId], [BusId], [DriverId], [ScheduleId], [TripDate], [DepartureTime], [ArrivalTime], [Status]) VALUES (31, 3, 5, 3, NULL, '2026-09-29', '08:00:00', '09:30:00', N'Scheduled');
INSERT INTO [Trip] ([TripId], [RouteId], [BusId], [DriverId], [ScheduleId], [TripDate], [DepartureTime], [ArrivalTime], [Status]) VALUES (32, 4, 6, 4, NULL, '2026-09-29', '07:00:00', '12:00:00', N'Scheduled');
INSERT INTO [Trip] ([TripId], [RouteId], [BusId], [DriverId], [ScheduleId], [TripDate], [DepartureTime], [ArrivalTime], [Status]) VALUES (33, 5, 5, 1, NULL, '2026-09-29', '08:30:00', '10:45:00', N'Scheduled');
INSERT INTO [Trip] ([TripId], [RouteId], [BusId], [DriverId], [ScheduleId], [TripDate], [DepartureTime], [ArrivalTime], [Status]) VALUES (34, 6, 9, 2, NULL, '2026-09-29', '09:00:00', '10:50:00', N'Scheduled');
INSERT INTO [Trip] ([TripId], [RouteId], [BusId], [DriverId], [ScheduleId], [TripDate], [DepartureTime], [ArrivalTime], [Status]) VALUES (35, 7, 7, 3, NULL, '2026-09-29', '23:00:00', '05:00:00', N'Scheduled');
INSERT INTO [Trip] ([TripId], [RouteId], [BusId], [DriverId], [ScheduleId], [TripDate], [DepartureTime], [ArrivalTime], [Status]) VALUES (36, 8, 8, 4, NULL, '2026-09-29', '07:30:00', '09:30:00', N'Scheduled');
INSERT INTO [Trip] ([TripId], [RouteId], [BusId], [DriverId], [ScheduleId], [TripDate], [DepartureTime], [ArrivalTime], [Status]) VALUES (37, 9, 10, 1, NULL, '2026-09-29', '06:30:00', '09:30:00', N'Scheduled');
INSERT INTO [Trip] ([TripId], [RouteId], [BusId], [DriverId], [ScheduleId], [TripDate], [DepartureTime], [ArrivalTime], [Status]) VALUES (38, 10, 7, 3, NULL, '2026-09-29', '13:30:00', '16:30:00', N'Scheduled');
INSERT INTO [Trip] ([TripId], [RouteId], [BusId], [DriverId], [ScheduleId], [TripDate], [DepartureTime], [ArrivalTime], [Status]) VALUES (39, 12, 12, 6, 1, '2026-09-29', '08:00:00', '09:45:00', N'Scheduled');
INSERT INTO [Trip] ([TripId], [RouteId], [BusId], [DriverId], [ScheduleId], [TripDate], [DepartureTime], [ArrivalTime], [Status]) VALUES (40, 13, 13, 7, 1, '2026-09-29', '07:30:00', '11:45:00', N'Scheduled');
INSERT INTO [Trip] ([TripId], [RouteId], [BusId], [DriverId], [ScheduleId], [TripDate], [DepartureTime], [ArrivalTime], [Status]) VALUES (41, 14, 14, 8, 1, '2026-09-29', '20:30:00', '04:00:00', N'Scheduled');
INSERT INTO [Trip] ([TripId], [RouteId], [BusId], [DriverId], [ScheduleId], [TripDate], [DepartureTime], [ArrivalTime], [Status]) VALUES (42, 16, 16, 6, 1, '2026-09-29', '22:30:00', '06:30:00', N'Scheduled');
INSERT INTO [Trip] ([TripId], [RouteId], [BusId], [DriverId], [ScheduleId], [TripDate], [DepartureTime], [ArrivalTime], [Status]) VALUES (43, 17, 17, 7, 1, '2026-09-29', '18:00:00', '06:30:00', N'Scheduled');
INSERT INTO [Trip] ([TripId], [RouteId], [BusId], [DriverId], [ScheduleId], [TripDate], [DepartureTime], [ArrivalTime], [Status]) VALUES (44, 18, 18, 8, 1, '2026-09-29', '22:00:00', '05:30:00', N'Scheduled');
INSERT INTO [Trip] ([TripId], [RouteId], [BusId], [DriverId], [ScheduleId], [TripDate], [DepartureTime], [ArrivalTime], [Status]) VALUES (45, 20, 20, 6, 1, '2026-09-29', '21:30:00', '05:00:00', N'Scheduled');
INSERT INTO [Trip] ([TripId], [RouteId], [BusId], [DriverId], [ScheduleId], [TripDate], [DepartureTime], [ArrivalTime], [Status]) VALUES (46, 21, 21, 7, 1, '2026-09-29', '23:00:00', '05:30:00', N'Scheduled');
INSERT INTO [Trip] ([TripId], [RouteId], [BusId], [DriverId], [ScheduleId], [TripDate], [DepartureTime], [ArrivalTime], [Status]) VALUES (47, 22, 22, 8, 1, '2026-09-29', '13:00:00', '18:30:00', N'Scheduled');
INSERT INTO [Trip] ([TripId], [RouteId], [BusId], [DriverId], [ScheduleId], [TripDate], [DepartureTime], [ArrivalTime], [Status]) VALUES (48, 24, 24, 6, 1, '2026-09-29', '09:00:00', '11:15:00', N'Scheduled');
INSERT INTO [Trip] ([TripId], [RouteId], [BusId], [DriverId], [ScheduleId], [TripDate], [DepartureTime], [ArrivalTime], [Status]) VALUES (49, 11, 11, 2, 1, '2026-09-29', '21:00:00', '04:30:00', N'Scheduled');
INSERT INTO [Trip] ([TripId], [RouteId], [BusId], [DriverId], [ScheduleId], [TripDate], [DepartureTime], [ArrivalTime], [Status]) VALUES (50, 15, 15, 2, 1, '2026-09-29', '19:00:00', '04:30:00', N'Scheduled');
INSERT INTO [Trip] ([TripId], [RouteId], [BusId], [DriverId], [ScheduleId], [TripDate], [DepartureTime], [ArrivalTime], [Status]) VALUES (51, 19, 19, 2, 1, '2026-09-29', '08:30:00', '11:30:00', N'Scheduled');
INSERT INTO [Trip] ([TripId], [RouteId], [BusId], [DriverId], [ScheduleId], [TripDate], [DepartureTime], [ArrivalTime], [Status]) VALUES (52, 23, 23, 2, 1, '2026-09-29', '19:30:00', '06:00:00', N'Scheduled');
INSERT INTO [Trip] ([TripId], [RouteId], [BusId], [DriverId], [ScheduleId], [TripDate], [DepartureTime], [ArrivalTime], [Status]) VALUES (53, 1, 1, 1, 1, '2026-09-30', '07:00:00', '09:00:00', N'Scheduled');
INSERT INTO [Trip] ([TripId], [RouteId], [BusId], [DriverId], [ScheduleId], [TripDate], [DepartureTime], [ArrivalTime], [Status]) VALUES (54, 2, 4, 2, NULL, '2026-09-30', '19:30:00', '08:00:00', N'Scheduled');
INSERT INTO [Trip] ([TripId], [RouteId], [BusId], [DriverId], [ScheduleId], [TripDate], [DepartureTime], [ArrivalTime], [Status]) VALUES (55, 3, 5, 3, NULL, '2026-09-30', '08:00:00', '09:30:00', N'Scheduled');
INSERT INTO [Trip] ([TripId], [RouteId], [BusId], [DriverId], [ScheduleId], [TripDate], [DepartureTime], [ArrivalTime], [Status]) VALUES (56, 4, 6, 4, NULL, '2026-09-30', '07:00:00', '12:00:00', N'Scheduled');
INSERT INTO [Trip] ([TripId], [RouteId], [BusId], [DriverId], [ScheduleId], [TripDate], [DepartureTime], [ArrivalTime], [Status]) VALUES (57, 5, 5, 1, NULL, '2026-09-30', '08:30:00', '10:45:00', N'Scheduled');
INSERT INTO [Trip] ([TripId], [RouteId], [BusId], [DriverId], [ScheduleId], [TripDate], [DepartureTime], [ArrivalTime], [Status]) VALUES (58, 6, 9, 2, NULL, '2026-09-30', '09:00:00', '10:50:00', N'Scheduled');
INSERT INTO [Trip] ([TripId], [RouteId], [BusId], [DriverId], [ScheduleId], [TripDate], [DepartureTime], [ArrivalTime], [Status]) VALUES (59, 7, 7, 3, NULL, '2026-09-30', '23:00:00', '05:00:00', N'Scheduled');
INSERT INTO [Trip] ([TripId], [RouteId], [BusId], [DriverId], [ScheduleId], [TripDate], [DepartureTime], [ArrivalTime], [Status]) VALUES (60, 8, 8, 4, NULL, '2026-09-30', '07:30:00', '09:30:00', N'Scheduled');
INSERT INTO [Trip] ([TripId], [RouteId], [BusId], [DriverId], [ScheduleId], [TripDate], [DepartureTime], [ArrivalTime], [Status]) VALUES (61, 9, 10, 1, NULL, '2026-09-30', '06:30:00', '09:30:00', N'Scheduled');
INSERT INTO [Trip] ([TripId], [RouteId], [BusId], [DriverId], [ScheduleId], [TripDate], [DepartureTime], [ArrivalTime], [Status]) VALUES (62, 10, 7, 3, NULL, '2026-09-30', '13:30:00', '16:30:00', N'Scheduled');
INSERT INTO [Trip] ([TripId], [RouteId], [BusId], [DriverId], [ScheduleId], [TripDate], [DepartureTime], [ArrivalTime], [Status]) VALUES (63, 12, 12, 6, 1, '2026-09-30', '08:00:00', '09:45:00', N'Scheduled');
INSERT INTO [Trip] ([TripId], [RouteId], [BusId], [DriverId], [ScheduleId], [TripDate], [DepartureTime], [ArrivalTime], [Status]) VALUES (64, 13, 13, 7, 1, '2026-09-30', '07:30:00', '11:45:00', N'Scheduled');
INSERT INTO [Trip] ([TripId], [RouteId], [BusId], [DriverId], [ScheduleId], [TripDate], [DepartureTime], [ArrivalTime], [Status]) VALUES (65, 14, 14, 8, 1, '2026-09-30', '20:30:00', '04:00:00', N'Scheduled');
INSERT INTO [Trip] ([TripId], [RouteId], [BusId], [DriverId], [ScheduleId], [TripDate], [DepartureTime], [ArrivalTime], [Status]) VALUES (66, 16, 16, 6, 1, '2026-09-30', '22:30:00', '06:30:00', N'Scheduled');
INSERT INTO [Trip] ([TripId], [RouteId], [BusId], [DriverId], [ScheduleId], [TripDate], [DepartureTime], [ArrivalTime], [Status]) VALUES (67, 17, 17, 7, 1, '2026-09-30', '18:00:00', '06:30:00', N'Scheduled');
INSERT INTO [Trip] ([TripId], [RouteId], [BusId], [DriverId], [ScheduleId], [TripDate], [DepartureTime], [ArrivalTime], [Status]) VALUES (68, 18, 18, 8, 1, '2026-09-30', '22:00:00', '05:30:00', N'Scheduled');
INSERT INTO [Trip] ([TripId], [RouteId], [BusId], [DriverId], [ScheduleId], [TripDate], [DepartureTime], [ArrivalTime], [Status]) VALUES (69, 20, 20, 6, 1, '2026-09-30', '21:30:00', '05:00:00', N'Scheduled');
INSERT INTO [Trip] ([TripId], [RouteId], [BusId], [DriverId], [ScheduleId], [TripDate], [DepartureTime], [ArrivalTime], [Status]) VALUES (70, 21, 21, 7, 1, '2026-09-30', '23:00:00', '05:30:00', N'Scheduled');
INSERT INTO [Trip] ([TripId], [RouteId], [BusId], [DriverId], [ScheduleId], [TripDate], [DepartureTime], [ArrivalTime], [Status]) VALUES (71, 22, 22, 8, 1, '2026-09-30', '13:00:00', '18:30:00', N'Scheduled');
INSERT INTO [Trip] ([TripId], [RouteId], [BusId], [DriverId], [ScheduleId], [TripDate], [DepartureTime], [ArrivalTime], [Status]) VALUES (72, 24, 24, 6, 1, '2026-09-30', '09:00:00', '11:15:00', N'Scheduled');
INSERT INTO [Trip] ([TripId], [RouteId], [BusId], [DriverId], [ScheduleId], [TripDate], [DepartureTime], [ArrivalTime], [Status]) VALUES (73, 11, 11, 2, 1, '2026-09-30', '21:00:00', '04:30:00', N'Scheduled');
INSERT INTO [Trip] ([TripId], [RouteId], [BusId], [DriverId], [ScheduleId], [TripDate], [DepartureTime], [ArrivalTime], [Status]) VALUES (74, 15, 15, 2, 1, '2026-09-30', '19:00:00', '04:30:00', N'Scheduled');
INSERT INTO [Trip] ([TripId], [RouteId], [BusId], [DriverId], [ScheduleId], [TripDate], [DepartureTime], [ArrivalTime], [Status]) VALUES (75, 19, 19, 2, 1, '2026-09-30', '08:30:00', '11:30:00', N'Scheduled');
INSERT INTO [Trip] ([TripId], [RouteId], [BusId], [DriverId], [ScheduleId], [TripDate], [DepartureTime], [ArrivalTime], [Status]) VALUES (76, 23, 23, 2, 1, '2026-09-30', '19:30:00', '06:00:00', N'Scheduled');
SET IDENTITY_INSERT [Trip] OFF;
GO

-- Data for [Booking] (20 rows)
SET IDENTITY_INSERT [Booking] ON;
INSERT INTO [Booking] ([BookingId], [UserId], [TripId], [BookingCode], [BookingTime], [TotalAmount], [Status]) VALUES (1, 2, 1, N'BK001', '2026-09-28', 300000.00, N'Confirmed');
INSERT INTO [Booking] ([BookingId], [UserId], [TripId], [BookingCode], [BookingTime], [TotalAmount], [Status]) VALUES (2, 2, 2, N'BK-HNDN-001', '2026-09-28', 450000.00, N'CONFIRMED');
INSERT INTO [Booking] ([BookingId], [UserId], [TripId], [BookingCode], [BookingTime], [TotalAmount], [Status]) VALUES (3, 2, 3, N'BK-HNHP-001', '2026-09-28', 120000.00, N'CONFIRMED');
INSERT INTO [Booking] ([BookingId], [UserId], [TripId], [BookingCode], [BookingTime], [TotalAmount], [Status]) VALUES (4, 2, 4, N'BK-HNSP-001', '2026-09-28', 280000.00, N'CONFIRMED');
INSERT INTO [Booking] ([BookingId], [UserId], [TripId], [BookingCode], [BookingTime], [TotalAmount], [Status]) VALUES (5, 2, 7, N'BK-SGDL-001', '2026-09-28', 340000.00, N'CONFIRMED');
INSERT INTO [Booking] ([BookingId], [UserId], [TripId], [BookingCode], [BookingTime], [TotalAmount], [Status]) VALUES (6, 2, 8, N'BK-SGVT-001', '2026-09-28', 160000.00, N'CONFIRMED');
INSERT INTO [Booking] ([BookingId], [UserId], [TripId], [BookingCode], [BookingTime], [TotalAmount], [Status]) VALUES (8, 1, 12, N'BK-NB-001', '2026-09-28', 130000.00, N'PAID');
INSERT INTO [Booking] ([BookingId], [UserId], [TripId], [BookingCode], [BookingTime], [TotalAmount], [Status]) VALUES (9, 1, 13, N'BK-MC-001', '2026-09-28', 220000.00, N'PAID');
INSERT INTO [Booking] ([BookingId], [UserId], [TripId], [BookingCode], [BookingTime], [TotalAmount], [Status]) VALUES (10, 1, 14, N'BK-CB-001', '2026-09-28', 250000.00, N'PAID');
INSERT INTO [Booking] ([BookingId], [UserId], [TripId], [BookingCode], [BookingTime], [TotalAmount], [Status]) VALUES (12, 1, 16, N'BK-NT-001', '2026-09-28', 320000.00, N'PAID');
INSERT INTO [Booking] ([BookingId], [UserId], [TripId], [BookingCode], [BookingTime], [TotalAmount], [Status]) VALUES (13, 1, 17, N'BK-QN-001', '2026-09-28', 350000.00, N'PAID');
INSERT INTO [Booking] ([BookingId], [UserId], [TripId], [BookingCode], [BookingTime], [TotalAmount], [Status]) VALUES (14, 1, 18, N'BK-BMT-001', '2026-09-28', 290000.00, N'PAID');
INSERT INTO [Booking] ([BookingId], [UserId], [TripId], [BookingCode], [BookingTime], [TotalAmount], [Status]) VALUES (16, 1, 20, N'BK-CM-001', '2026-09-28', 260000.00, N'PAID');
INSERT INTO [Booking] ([BookingId], [UserId], [TripId], [BookingCode], [BookingTime], [TotalAmount], [Status]) VALUES (17, 1, 21, N'BK-AG-001', '2026-09-28', 210000.00, N'PAID');
INSERT INTO [Booking] ([BookingId], [UserId], [TripId], [BookingCode], [BookingTime], [TotalAmount], [Status]) VALUES (18, 1, 22, N'BK-DNQN-001', '2026-09-28', 220000.00, N'PAID');
INSERT INTO [Booking] ([BookingId], [UserId], [TripId], [BookingCode], [BookingTime], [TotalAmount], [Status]) VALUES (20, 1, 24, N'BK-CTRG-001', '2026-09-28', 140000.00, N'PAID');
INSERT INTO [Booking] ([BookingId], [UserId], [TripId], [BookingCode], [BookingTime], [TotalAmount], [Status]) VALUES (21, 1, 25, N'BK-HG-001', '2026-09-28', 280000.00, N'PAID');
INSERT INTO [Booking] ([BookingId], [UserId], [TripId], [BookingCode], [BookingTime], [TotalAmount], [Status]) VALUES (22, 1, 26, N'BK-QB-001', '2026-09-28', 380000.00, N'PAID');
INSERT INTO [Booking] ([BookingId], [UserId], [TripId], [BookingCode], [BookingTime], [TotalAmount], [Status]) VALUES (23, 1, 27, N'BK-PT-001', '2026-09-28', 180000.00, N'PAID');
INSERT INTO [Booking] ([BookingId], [UserId], [TripId], [BookingCode], [BookingTime], [TotalAmount], [Status]) VALUES (24, 1, 28, N'BK-DNBMT-001', '2026-09-28', 310000.00, N'PAID');
SET IDENTITY_INSERT [Booking] OFF;
GO

-- Data for [Payment] (1 rows)
SET IDENTITY_INSERT [Payment] ON;
INSERT INTO [Payment] ([PaymentId], [BookingId], [Amount], [PaymentMethod], [TransactionCode], [PaymentTime], [Status]) VALUES (1, 1, 300000.00, N'Banking', N'TRANS001', '2026-09-28', N'Paid');
SET IDENTITY_INSERT [Payment] OFF;
GO

-- Data for [Ticket] (22 rows)
SET IDENTITY_INSERT [Ticket] ON;
INSERT INTO [Ticket] ([TicketId], [BookingId], [TicketCode], [SeatNumber], [Price], [BoardingStopId], [DropOffStopId], [Status]) VALUES (1, 1, N'TK001', N'A01', 100000.00, 1, 4, N'Confirmed');
INSERT INTO [Ticket] ([TicketId], [BookingId], [TicketCode], [SeatNumber], [Price], [BoardingStopId], [DropOffStopId], [Status]) VALUES (2, 1, N'TK002', N'A02', 100000.00, 1, 4, N'Confirmed');
INSERT INTO [Ticket] ([TicketId], [BookingId], [TicketCode], [SeatNumber], [Price], [BoardingStopId], [DropOffStopId], [Status]) VALUES (3, 1, N'TK003', N'A03', 100000.00, 1, 4, N'Confirmed');
INSERT INTO [Ticket] ([TicketId], [BookingId], [TicketCode], [SeatNumber], [Price], [BoardingStopId], [DropOffStopId], [Status]) VALUES (4, 2, N'SBG-HN-DN-20251024-008', N'VIP-05', 450000.00, NULL, NULL, N'CONFIRMED');
INSERT INTO [Ticket] ([TicketId], [BookingId], [TicketCode], [SeatNumber], [Price], [BoardingStopId], [DropOffStopId], [Status]) VALUES (5, 3, N'SBG-HN-HP-001', N'A-08', 120000.00, NULL, NULL, N'CONFIRMED');
INSERT INTO [Ticket] ([TicketId], [BookingId], [TicketCode], [SeatNumber], [Price], [BoardingStopId], [DropOffStopId], [Status]) VALUES (6, 4, N'SBG-HN-SP-001', N'VIP-12', 280000.00, NULL, NULL, N'CONFIRMED');
INSERT INTO [Ticket] ([TicketId], [BookingId], [TicketCode], [SeatNumber], [Price], [BoardingStopId], [DropOffStopId], [Status]) VALUES (7, 5, N'SBG-SG-DL-001', N'VIP-02', 340000.00, NULL, NULL, N'CONFIRMED');
INSERT INTO [Ticket] ([TicketId], [BookingId], [TicketCode], [SeatNumber], [Price], [BoardingStopId], [DropOffStopId], [Status]) VALUES (8, 6, N'SBG-SG-VT-001', N'VIP-09', 160000.00, NULL, NULL, N'CONFIRMED');
INSERT INTO [Ticket] ([TicketId], [BookingId], [TicketCode], [SeatNumber], [Price], [BoardingStopId], [DropOffStopId], [Status]) VALUES (10, 8, N'TK-NB-01', N'A05', 130000.00, 1, 20, N'ISSUED');
INSERT INTO [Ticket] ([TicketId], [BookingId], [TicketCode], [SeatNumber], [Price], [BoardingStopId], [DropOffStopId], [Status]) VALUES (11, 9, N'TK-MC-01', N'VIP02', 220000.00, 1, 21, N'ISSUED');
INSERT INTO [Ticket] ([TicketId], [BookingId], [TicketCode], [SeatNumber], [Price], [BoardingStopId], [DropOffStopId], [Status]) VALUES (12, 10, N'TK-CB-01', N'B03', 250000.00, 1, 22, N'ISSUED');
INSERT INTO [Ticket] ([TicketId], [BookingId], [TicketCode], [SeatNumber], [Price], [BoardingStopId], [DropOffStopId], [Status]) VALUES (14, 12, N'TK-NT-01', N'VIP01', 320000.00, 13, 18, N'ISSUED');
INSERT INTO [Ticket] ([TicketId], [BookingId], [TicketCode], [SeatNumber], [Price], [BoardingStopId], [DropOffStopId], [Status]) VALUES (15, 13, N'TK-QN-01', N'B04', 350000.00, 13, 24, N'ISSUED');
INSERT INTO [Ticket] ([TicketId], [BookingId], [TicketCode], [SeatNumber], [Price], [BoardingStopId], [DropOffStopId], [Status]) VALUES (16, 14, N'TK-BMT-01', N'A08', 290000.00, 13, 25, N'ISSUED');
INSERT INTO [Ticket] ([TicketId], [BookingId], [TicketCode], [SeatNumber], [Price], [BoardingStopId], [DropOffStopId], [Status]) VALUES (18, 16, N'TK-CM-01', N'A12', 260000.00, 14, 27, N'ISSUED');
INSERT INTO [Ticket] ([TicketId], [BookingId], [TicketCode], [SeatNumber], [Price], [BoardingStopId], [DropOffStopId], [Status]) VALUES (19, 17, N'TK-AG-01', N'B02', 210000.00, 14, 28, N'ISSUED');
INSERT INTO [Ticket] ([TicketId], [BookingId], [TicketCode], [SeatNumber], [Price], [BoardingStopId], [DropOffStopId], [Status]) VALUES (20, 18, N'TK-DNQN-01', N'VIP04', 220000.00, 11, 24, N'ISSUED');
INSERT INTO [Ticket] ([TicketId], [BookingId], [TicketCode], [SeatNumber], [Price], [BoardingStopId], [DropOffStopId], [Status]) VALUES (22, 20, N'TK-CTRG-01', N'12', 140000.00, 17, 29, N'ISSUED');
INSERT INTO [Ticket] ([TicketId], [BookingId], [TicketCode], [SeatNumber], [Price], [BoardingStopId], [DropOffStopId], [Status]) VALUES (23, 21, N'TK-HG-01', N'A01', 280000.00, 1, 19, N'ISSUED');
INSERT INTO [Ticket] ([TicketId], [BookingId], [TicketCode], [SeatNumber], [Price], [BoardingStopId], [DropOffStopId], [Status]) VALUES (24, 22, N'TK-QB-01', N'VIP05', 380000.00, 1, 23, N'ISSUED');
INSERT INTO [Ticket] ([TicketId], [BookingId], [TicketCode], [SeatNumber], [Price], [BoardingStopId], [DropOffStopId], [Status]) VALUES (25, 23, N'TK-PT-01', N'04', 180000.00, 13, 26, N'ISSUED');
INSERT INTO [Ticket] ([TicketId], [BookingId], [TicketCode], [SeatNumber], [Price], [BoardingStopId], [DropOffStopId], [Status]) VALUES (26, 24, N'TK-DNBMT-01', N'A03', 310000.00, 11, 25, N'ISSUED');
SET IDENTITY_INSERT [Ticket] OFF;
GO

-- Data for [Notification] (1 rows)
SET IDENTITY_INSERT [Notification] ON;
INSERT INTO [Notification] ([NotificationId], [UserId], [Title], [Content], [Type], [IsRead], [CreatedAt]) VALUES (1, 2, N'Äáº·t vÃ© thÃ nh cÃ´ng', N'ChÃºc má»«ng quÃ½ khÃ¡ch Ä‘Ã£ Ä‘áº·t vÃ© chuyáº¿n HÃ  Ná»™i - ThÃ¡i NguyÃªn thÃ nh cÃ´ng!', N'Booking', N'False', '2026-09-28');
SET IDENTITY_INSERT [Notification] OFF;
GO

-- Data for [VehicleLocation] (2 rows)
SET IDENTITY_INSERT [VehicleLocation] ON;
INSERT INTO [VehicleLocation] ([LocationId], [TripId], [Latitude], [Longitude], [Speed], [RecordedAt]) VALUES (1, 1, 21.0285000, 105.8542000, 40.00, '2026-09-28');
INSERT INTO [VehicleLocation] ([LocationId], [TripId], [Latitude], [Longitude], [Speed], [RecordedAt]) VALUES (2, 1, 21.1450000, 105.8000000, 55.00, '2026-09-28');
SET IDENTITY_INSERT [VehicleLocation] OFF;
GO

EXEC sp_MSforeachtable 'ALTER TABLE ? WITH CHECK CHECK CONSTRAINT ALL';
GO

