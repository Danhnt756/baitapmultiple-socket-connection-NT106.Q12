USE master;
DROP DATABASE UserDB;
GO
CREATE DATABASE UserDB;
GO
USE UserDB;
GO

-- Tạo bảng Users
CREATE TABLE Users (
    UserId INT IDENTITY(1,1) PRIMARY KEY,
    Username NVARCHAR(100) NOT NULL UNIQUE,
    PasswordHash NVARCHAR(256) NOT NULL,
    Email NVARCHAR(256) NULL,
    FullName NVARCHAR(200) NULL,
    Birthday DATE NULL
);

-- Tạo bảng Tokens (tùy chọn)
CREATE TABLE UserTokens (
    TokenId UNIQUEIDENTIFIER PRIMARY KEY,
    UserId INT NOT NULL,
    ExpireAt DATETIME NOT NULL,
    FOREIGN KEY (UserId) REFERENCES Users(UserId)
);