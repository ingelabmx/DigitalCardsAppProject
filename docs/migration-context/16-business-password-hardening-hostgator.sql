CREATE TABLE IF NOT EXISTS `ModernBusinessCredential` (
  `BusinessID` int(11) NOT NULL,
  `PasswordHash` varchar(512) COLLATE utf8mb4_unicode_ci NOT NULL,
  `CreatedAt` datetime NOT NULL,
  `UpdatedAt` datetime NOT NULL,
  PRIMARY KEY (`BusinessID`),
  CONSTRAINT `ModernBusinessCredential_ibfk_1`
    FOREIGN KEY (`BusinessID`) REFERENCES `Business` (`BusinessID`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
