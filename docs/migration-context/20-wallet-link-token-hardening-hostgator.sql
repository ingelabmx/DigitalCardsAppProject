-- Wallet link opaque token table for DigitalCards modern app.
-- MySQL 5.7 compatible. Run manually against HostGator only after backup.
-- This script does not modify Web Forms tables or existing ClientCard rows.

CREATE TABLE IF NOT EXISTS `WalletLinkToken` (
  `ID` int(11) NOT NULL AUTO_INCREMENT,
  `TokenHash` char(64) COLLATE utf8mb4_unicode_ci NOT NULL,
  `TokenSuffix` varchar(16) COLLATE utf8mb4_unicode_ci NOT NULL,
  `CardID` int(11) NOT NULL,
  `Purpose` varchar(50) COLLATE utf8mb4_unicode_ci NOT NULL,
  `CreatedAt` datetime(6) NOT NULL,
  `LastUsedAt` datetime(6) DEFAULT NULL,
  `RevokedAt` datetime(6) DEFAULT NULL,
  PRIMARY KEY (`ID`),
  UNIQUE KEY `UX_WalletLinkToken_TokenHash_Purpose` (`TokenHash`, `Purpose`),
  KEY `IX_WalletLinkToken_CardID_Purpose_RevokedAt` (`CardID`, `Purpose`, `RevokedAt`),
  KEY `IX_WalletLinkToken_TokenSuffix` (`TokenSuffix`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
