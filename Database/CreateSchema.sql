-- Bowling Game Database Schema
-- MySQL

-- Create Games table
CREATE TABLE IF NOT EXISTS Games (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    PlayerName VARCHAR(100) NOT NULL,
    CreatedAt DATETIME NOT NULL DEFAULT (UTC_TIMESTAMP()),
    Status TINYINT NOT NULL DEFAULT 0, -- 0 = Active, 1 = Completed
    FinalScore INT NULL
) ENGINE=InnoDB;

-- Create Rolls table
CREATE TABLE IF NOT EXISTS Rolls (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    GameId INT NOT NULL,
    Pins TINYINT NOT NULL,
    RollIndex INT NOT NULL,
    CONSTRAINT FK_Rolls_Games FOREIGN KEY (GameId) REFERENCES Games(Id) ON DELETE CASCADE,
    CONSTRAINT CHK_Pins CHECK (Pins >= 0 AND Pins <= 10)
) ENGINE=InnoDB;

-- Index on GameId and RollIndex for fast lookups and ordered retrieval of rolls by game
CREATE INDEX IX_Rolls_GameId_RollIndex ON Rolls(GameId, RollIndex);

-- Composite index for leaderboard queries and active games lookup (Status filter + FinalScore sorting)
CREATE INDEX IX_Games_Status_FinalScore ON Games(Status, FinalScore DESC);
