/* =========================================================
   RaceDay Database Schema
   Run in SQL Server Management Studio (SSMS) on a clean instance.
   ========================================================= */

IF DB_ID('RaceDayDB') IS NOT NULL
BEGIN
    ALTER DATABASE RaceDayDB SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE RaceDayDB;
END
GO

CREATE DATABASE RaceDayDB;
GO

USE RaceDayDB;
GO

/* ---------------------------------------------------------
   1. Users
   --------------------------------------------------------- */
CREATE TABLE Users (
    UserID          INT IDENTITY(1,1) PRIMARY KEY,
    FullName        NVARCHAR(100)   NOT NULL,
    Email           NVARCHAR(150)   NOT NULL UNIQUE,
    PasswordHash    NVARCHAR(255)   NOT NULL,
    Role            VARCHAR(20)     NOT NULL DEFAULT 'Participant'
                        CONSTRAINT CK_Users_Role CHECK (Role IN ('Organiser','Participant')),
    ProfilePictureUrl NVARCHAR(500) NULL,  -- Azure Blob Storage URL, set in Part 3
    CreatedAt       DATETIME        NOT NULL DEFAULT GETDATE()
);
GO

/* ---------------------------------------------------------
   2. Events  (one Organiser -> many Events)
   --------------------------------------------------------- */
CREATE TABLE Events (
    EventID         INT IDENTITY(1,1) PRIMARY KEY,
    EventName       NVARCHAR(150)   NOT NULL,
    EventDate       DATE            NOT NULL,
    Location        NVARCHAR(150)   NOT NULL,
    Description     NVARCHAR(500)   NULL,
    BannerImageUrl  NVARCHAR(500)   NULL,  -- Azure Blob Storage URL, set in Part 3
    OrganiserID     INT             NOT NULL,
    CreatedAt       DATETIME        NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_Events_Organiser FOREIGN KEY (OrganiserID)
        REFERENCES Users(UserID)
);
GO

/* ---------------------------------------------------------
   3. EventOrganisers  (many-to-many: Users <-> Events)
   --------------------------------------------------------- */
CREATE TABLE EventOrganisers (
    EventOrganiserID INT IDENTITY(1,1) PRIMARY KEY,
    EventID          INT NOT NULL,
    UserID           INT NOT NULL,
    AssignedRole     VARCHAR(20) NOT NULL DEFAULT 'Assistant',
    CONSTRAINT FK_EO_Event FOREIGN KEY (EventID) REFERENCES Events(EventID),
    CONSTRAINT FK_EO_User  FOREIGN KEY (UserID)  REFERENCES Users(UserID),
    CONSTRAINT UQ_EO_Event_User UNIQUE (EventID, UserID)
);
GO

/* ---------------------------------------------------------
   4. Categories  (one Event -> many Categories)
   --------------------------------------------------------- */
CREATE TABLE Categories (
    CategoryID      INT IDENTITY(1,1) PRIMARY KEY,
    EventID         INT             NOT NULL,
    CategoryName    NVARCHAR(100)   NOT NULL,
    DistanceKM      DECIMAL(5,2)    NOT NULL,
    MaxParticipants INT             NOT NULL,
    EntryFee        DECIMAL(8,2)    NOT NULL DEFAULT 0,
    CONSTRAINT FK_Categories_Event FOREIGN KEY (EventID)
        REFERENCES Events(EventID)
);
GO

/* ---------------------------------------------------------
   5. Enrolments  (one Participant -> many Enrolments,
                    one Category -> many Enrolments)
   --------------------------------------------------------- */
CREATE TABLE Enrolments (
    EnrolmentID     INT IDENTITY(1,1) PRIMARY KEY,
    ParticipantID   INT             NOT NULL,
    CategoryID      INT             NOT NULL,
    BibNumber       VARCHAR(20)     NOT NULL UNIQUE,
    EnrolmentDate   DATETIME        NOT NULL DEFAULT GETDATE(),
    Status          VARCHAR(20)     NOT NULL DEFAULT 'Registered',
    CONSTRAINT FK_Enrolments_Participant FOREIGN KEY (ParticipantID)
        REFERENCES Users(UserID),
    CONSTRAINT FK_Enrolments_Category FOREIGN KEY (CategoryID)
        REFERENCES Categories(CategoryID),
    CONSTRAINT UQ_Enrolments_Participant_Category UNIQUE (ParticipantID, CategoryID)
);
GO

/* ---------------------------------------------------------
   6. Results  (one Enrolment -> zero or one Result)
   --------------------------------------------------------- */
CREATE TABLE Results (
    ResultID        INT IDENTITY(1,1) PRIMARY KEY,
    EnrolmentID     INT             NOT NULL UNIQUE,
    FinishTime      TIME            NULL,
    Position        INT             NULL,
    Status          VARCHAR(20)     NOT NULL DEFAULT 'Finished',
    CONSTRAINT FK_Results_Enrolment FOREIGN KEY (EnrolmentID)
        REFERENCES Enrolments(EnrolmentID)
);
GO

/* =========================================================
   Seed data
   ========================================================= */

-- Organisers (2)
INSERT INTO Users (FullName, Email, PasswordHash, Role) VALUES
('Naledi Khumalo', 'naledi.khumalo@raceday.co.za', 'hashed_pw_1', 'Organiser'),
('Ryan Fischer',   'ryan.fischer@raceday.co.za',   'hashed_pw_2', 'Organiser');

-- Participants (2)
INSERT INTO Users (FullName, Email, PasswordHash, Role) VALUES
('Thabo Mokoena',  'thabo.mokoena@example.com',  'hashed_pw_3', 'Participant'),
('Sarah van Wyk',  'sarah.vanwyk@example.com',   'hashed_pw_4', 'Participant');

-- Events (3)
INSERT INTO Events (EventName, EventDate, Location, Description, BannerImageUrl, OrganiserID) VALUES
('Johannesburg City Marathon', '2026-04-18', 'Johannesburg, Gauteng', 'Annual road marathon through the city centre.', 'https://racedaystorage.blob.core.windows.net/event-banners/jhb-marathon.jpg', 1),
('Cape Town Trail Run',        '2026-05-09', 'Table Mountain, Cape Town', 'Scenic trail run with multiple distance options.', 'https://racedaystorage.blob.core.windows.net/event-banners/ct-trail.jpg', 2),
('Pretoria Fun Run',           '2026-06-20', 'Pretoria, Gauteng', 'Family-friendly fun run in support of local charities.', NULL, 1);

-- Link a second (assistant) organiser to one event to demonstrate the many-to-many relationship
INSERT INTO EventOrganisers (EventID, UserID, AssignedRole) VALUES
(1, 2, 'Assistant');

-- Categories (at least one per event)
INSERT INTO Categories (EventID, CategoryName, DistanceKM, MaxParticipants, EntryFee) VALUES
(1, '10km Fun Run', 10.00, 500, 150.00),
(1, '42.2km Full Marathon', 42.20, 1000, 350.00),
(2, '21km Half Trail', 21.00, 300, 250.00),
(2, '10km Trail', 10.00, 300, 180.00),
(3, '5km Fun Run', 5.00, 400, 80.00);

-- Sample enrolments
INSERT INTO Enrolments (ParticipantID, CategoryID, BibNumber, Status) VALUES
(3, 1, 'BIB-1001', 'Registered'),
(4, 2, 'BIB-1002', 'Registered'),
(3, 3, 'BIB-2001', 'Registered'),
(4, 5, 'BIB-3001', 'Registered');

-- Sample results (for enrolments that have taken place)
INSERT INTO Results (EnrolmentID, FinishTime, Position, Status) VALUES
(1, '00:52:14', 1, 'Finished'),
(2, '03:45:02', 5, 'Finished');
GO

/* Quick sanity check */
SELECT 'Users' AS TableName, COUNT(*) AS Rows FROM Users
UNION ALL SELECT 'Events', COUNT(*) FROM Events
UNION ALL SELECT 'EventOrganisers', COUNT(*) FROM EventOrganisers
UNION ALL SELECT 'Categories', COUNT(*) FROM Categories
UNION ALL SELECT 'Enrolments', COUNT(*) FROM Enrolments
UNION ALL SELECT 'Results', COUNT(*) FROM Results;
GO
