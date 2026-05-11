-- Apple Wallet Web Service support tables for DigitalCards modern app.
-- MySQL 5.7 compatible. Run manually against HostGator only after backup.
-- This script does not modify legacy ApplePass or Web Forms tables.

CREATE TABLE IF NOT EXISTS AppleWalletPass (
  PassTypeIdentifier varchar(255) NOT NULL,
  SerialNumber varchar(64) NOT NULL,
  CardID int(11) NOT NULL,
  AuthTokenHash char(64) NOT NULL,
  UpdateTag varchar(32) NOT NULL,
  CreatedAt datetime(6) NOT NULL,
  UpdatedAt datetime(6) NOT NULL,
  PRIMARY KEY (PassTypeIdentifier, SerialNumber),
  KEY IX_AppleWalletPass_CardID (CardID),
  KEY IX_AppleWalletPass_UpdateTag (UpdateTag)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS AppleWalletDevice (
  DeviceLibraryIdentifier varchar(255) NOT NULL,
  PushToken varchar(255) NOT NULL,
  CreatedAt datetime(6) NOT NULL,
  UpdatedAt datetime(6) NOT NULL,
  PRIMARY KEY (DeviceLibraryIdentifier)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS AppleWalletRegistration (
  DeviceLibraryIdentifier varchar(255) NOT NULL,
  PassTypeIdentifier varchar(255) NOT NULL,
  SerialNumber varchar(64) NOT NULL,
  CreatedAt datetime(6) NOT NULL,
  PRIMARY KEY (DeviceLibraryIdentifier, PassTypeIdentifier, SerialNumber),
  KEY IX_AppleWalletRegistration_Pass (PassTypeIdentifier, SerialNumber)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
