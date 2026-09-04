USE master;
GO

ALTER DATABASE RaceDayDB
SET SINGLE_USER
WITH ROLLBACK IMMEDIATE;
GO

DROP DATABASE RaceDayDB;
GO


CREATE DATABASE RaceDayDB;
GO

USE RaceDayDB;
GO


 --  Creating user table
 
CREATE TABLE Users (
    UserID          INT IDENTITY(1,1) PRIMARY KEY,
    FullName        NVARCHAR(100)   NOT NULL,
    Email           NVARCHAR(150)   NOT NULL UNIQUE,
    PasswordHash    NVARCHAR(255)   NOT NULL,
    Role            VARCHAR(20)     NOT NULL DEFAULT 'Participant'
                        CONSTRAINT CK_Users_Role CHECK (Role IN ('Organiser','Participant')),
    ProfilePictureUrl NVARCHAR(500) NULL,  
    CreatedAt       DATETIME        NOT NULL DEFAULT GETDATE()
);
GO


--  Creating Events Table (one Organiser -> many Events)

CREATE TABLE Events (
    EventID         INT IDENTITY(1,1) PRIMARY KEY,
    EventName       NVARCHAR(150)   NOT NULL,
    EventDate       DATE            NOT NULL,
    Location        NVARCHAR(150)   NOT NULL,
    Description     NVARCHAR(500)   NULL,
    BannerImageUrl  NVARCHAR(500)   NULL,  
    OrganiserID     INT             NOT NULL,
    CreatedAt       DATETIME        NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_Events_Organiser FOREIGN KEY (OrganiserID)
        REFERENCES Users(UserID)
);
GO


-- Creating  EventOrganisers Table  (many-to-many: Users <-> Events)

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


-- Categories  (one Event -> many Categories)

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


--  Creating Enrolments Table  (one Participant -> many Enrolments,
--                    one Category -> many Enrolments)
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


--   Creating  Results Table (one Enrolment -> zero or one Result)

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
