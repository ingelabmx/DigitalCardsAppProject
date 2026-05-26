-- MySQL dump 10.13  Distrib 5.7.44-48, for Linux (x86_64)
--
-- Host: localhost    Database: alltrac1_dcards
-- ------------------------------------------------------
-- Server version	5.7.44-48

/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!40101 SET NAMES utf8mb4 */;
/*!40103 SET @OLD_TIME_ZONE=@@TIME_ZONE */;
/*!40103 SET TIME_ZONE='+00:00' */;
/*!40014 SET @OLD_UNIQUE_CHECKS=@@UNIQUE_CHECKS, UNIQUE_CHECKS=0 */;
/*!40014 SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0 */;
/*!40101 SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='NO_AUTO_VALUE_ON_ZERO' */;
/*!40111 SET @OLD_SQL_NOTES=@@SQL_NOTES, SQL_NOTES=0 */;
/*!50717 SELECT COUNT(*) INTO @rocksdb_has_p_s_session_variables FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'performance_schema' AND TABLE_NAME = 'session_variables' */;
/*!50717 SET @rocksdb_get_is_supported = IF (@rocksdb_has_p_s_session_variables, 'SELECT COUNT(*) INTO @rocksdb_is_supported FROM performance_schema.session_variables WHERE VARIABLE_NAME=\'rocksdb_bulk_load\'', 'SELECT 0') */;
/*!50717 PREPARE s FROM @rocksdb_get_is_supported */;
/*!50717 EXECUTE s */;
/*!50717 DEALLOCATE PREPARE s */;
/*!50717 SET @rocksdb_enable_bulk_load = IF (@rocksdb_is_supported, 'SET SESSION rocksdb_bulk_load = 1', 'SET @rocksdb_dummy_bulk_load = 0') */;
/*!50717 PREPARE s FROM @rocksdb_enable_bulk_load */;
/*!50717 EXECUTE s */;
/*!50717 DEALLOCATE PREPARE s */;

--
-- Table structure for table `ApplePass`
--

DROP TABLE IF EXISTS `ApplePass`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `ApplePass` (
  `IDPass` int(11) NOT NULL AUTO_INCREMENT,
  `SerialNumber` varchar(40) COLLATE utf8_unicode_ci NOT NULL,
  `AuthToken` varchar(40) COLLATE utf8_unicode_ci NOT NULL,
  `PushToken` varchar(40) COLLATE utf8_unicode_ci NOT NULL,
  `CreationDate` datetime NOT NULL,
  `CheckQTY` int(11) NOT NULL,
  `LastCheck` datetime NOT NULL,
  `IDUser` int(11) NOT NULL,
  `BusinessID` int(11) NOT NULL,
  PRIMARY KEY (`IDPass`)
) ENGINE=InnoDB AUTO_INCREMENT=3 DEFAULT CHARSET=utf8 COLLATE=utf8_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `ApplePass`
--

LOCK TABLES `ApplePass` WRITE;
/*!40000 ALTER TABLE `ApplePass` DISABLE KEYS */;
INSERT INTO `ApplePass` (`IDPass`, `SerialNumber`, `AuthToken`, `PushToken`, `CreationDate`, `CheckQTY`, `LastCheck`, `IDUser`, `BusinessID`) VALUES (2,'bfca1e8e-21cc-4d36-b807-c00a53b2e3dc','6195bde2905f41c084963165ff7a9c95','','0000-00-00 00:00:00',0,'0000-00-00 00:00:00',0,0);
/*!40000 ALTER TABLE `ApplePass` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `AppleWalletDevice`
--

DROP TABLE IF EXISTS `AppleWalletDevice`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `AppleWalletDevice` (
  `DeviceLibraryIdentifier` varchar(255) COLLATE utf8mb4_unicode_ci NOT NULL,
  `PushToken` varchar(255) COLLATE utf8mb4_unicode_ci NOT NULL,
  `CreatedAt` datetime(6) NOT NULL,
  `UpdatedAt` datetime(6) NOT NULL,
  PRIMARY KEY (`DeviceLibraryIdentifier`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `AppleWalletDevice`
--

LOCK TABLES `AppleWalletDevice` WRITE;
/*!40000 ALTER TABLE `AppleWalletDevice` DISABLE KEYS */;
INSERT INTO `AppleWalletDevice` (`DeviceLibraryIdentifier`, `PushToken`, `CreatedAt`, `UpdatedAt`) VALUES ('ca4361a32b32e0082e7f3194481045c1','f79dcbbf7dd52e864095b40e380d2c9a8ac8423165b165af6b0792b96caff3f7','2026-05-11 22:27:47.964457','2026-05-11 22:27:47.964457'),('d1d8769e37b363186bfeef1f0d64858a','f79dcbbf7dd52e864095b40e380d2c9a8ac8423165b165af6b0792b96caff3f7','2026-05-15 14:55:45.084390','2026-05-15 14:55:45.084390');
/*!40000 ALTER TABLE `AppleWalletDevice` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `AppleWalletPass`
--

DROP TABLE IF EXISTS `AppleWalletPass`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `AppleWalletPass` (
  `PassTypeIdentifier` varchar(255) COLLATE utf8mb4_unicode_ci NOT NULL,
  `SerialNumber` varchar(64) COLLATE utf8mb4_unicode_ci NOT NULL,
  `CardID` int(11) NOT NULL,
  `AuthTokenHash` char(64) COLLATE utf8mb4_unicode_ci NOT NULL,
  `UpdateTag` varchar(32) COLLATE utf8mb4_unicode_ci NOT NULL,
  `CreatedAt` datetime(6) NOT NULL,
  `UpdatedAt` datetime(6) NOT NULL,
  PRIMARY KEY (`PassTypeIdentifier`,`SerialNumber`),
  KEY `IX_AppleWalletPass_CardID` (`CardID`),
  KEY `IX_AppleWalletPass_UpdateTag` (`UpdateTag`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `AppleWalletPass`
--

LOCK TABLES `AppleWalletPass` WRITE;
/*!40000 ALTER TABLE `AppleWalletPass` DISABLE KEYS */;
INSERT INTO `AppleWalletPass` (`PassTypeIdentifier`, `SerialNumber`, `CardID`, `AuthTokenHash`, `UpdateTag`, `CreatedAt`, `UpdatedAt`) VALUES ('pass.DigitalCardsApple','00000000000000000000000000000049',73,'003b28880b4f5edec2cfe0a5a9a39650677ab2c9dada64e70d8944c27d97744f','1779336278237','2026-05-11 21:03:49.723460','2026-05-21 04:04:38.237741');
/*!40000 ALTER TABLE `AppleWalletPass` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `AppleWalletRegistration`
--

DROP TABLE IF EXISTS `AppleWalletRegistration`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `AppleWalletRegistration` (
  `DeviceLibraryIdentifier` varchar(255) COLLATE utf8mb4_unicode_ci NOT NULL,
  `PassTypeIdentifier` varchar(255) COLLATE utf8mb4_unicode_ci NOT NULL,
  `SerialNumber` varchar(64) COLLATE utf8mb4_unicode_ci NOT NULL,
  `CreatedAt` datetime(6) NOT NULL,
  PRIMARY KEY (`DeviceLibraryIdentifier`,`PassTypeIdentifier`,`SerialNumber`),
  KEY `IX_AppleWalletRegistration_Pass` (`PassTypeIdentifier`,`SerialNumber`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `AppleWalletRegistration`
--

LOCK TABLES `AppleWalletRegistration` WRITE;
/*!40000 ALTER TABLE `AppleWalletRegistration` DISABLE KEYS */;
INSERT INTO `AppleWalletRegistration` (`DeviceLibraryIdentifier`, `PassTypeIdentifier`, `SerialNumber`, `CreatedAt`) VALUES ('ca4361a32b32e0082e7f3194481045c1','pass.DigitalCardsApple','00000000000000000000000000000049','2026-05-11 22:27:47.964457'),('d1d8769e37b363186bfeef1f0d64858a','pass.DigitalCardsApple','00000000000000000000000000000049','2026-05-15 14:55:45.084390');
/*!40000 ALTER TABLE `AppleWalletRegistration` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `Business`
--

DROP TABLE IF EXISTS `Business`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `Business` (
  `BusinessID` int(11) NOT NULL AUTO_INCREMENT,
  `BusinessName` varchar(30) COLLATE utf8_unicode_ci DEFAULT NULL,
  `BusinessPassword` varchar(25) COLLATE utf8_unicode_ci DEFAULT NULL,
  `BusinessEmail` varchar(30) COLLATE utf8_unicode_ci DEFAULT NULL,
  `BusinessLogo` varchar(100) COLLATE utf8_unicode_ci DEFAULT NULL,
  PRIMARY KEY (`BusinessID`),
  UNIQUE KEY `BusinessName` (`BusinessName`),
  UNIQUE KEY `BusinessEmail` (`BusinessEmail`)
) ENGINE=InnoDB AUTO_INCREMENT=43 DEFAULT CHARSET=utf8 COLLATE=utf8_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `Business`
--

LOCK TABLES `Business` WRITE;
/*!40000 ALTER TABLE `Business` DISABLE KEYS */;
INSERT INTO `Business` (`BusinessID`, `BusinessName`, `BusinessPassword`, `BusinessEmail`, `BusinessLogo`) VALUES (40,'Balboa Water','9f86d081884c7d659a2feaa0c','ingelabmx@gmail.com','~/Logos/Coast_20250415145706.png');
/*!40000 ALTER TABLE `Business` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `BusinessEnrollmentLinkToken`
--

DROP TABLE IF EXISTS `BusinessEnrollmentLinkToken`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `BusinessEnrollmentLinkToken` (
  `TokenHash` char(64) COLLATE utf8mb4_unicode_ci NOT NULL,
  `TokenSuffix` varchar(16) COLLATE utf8mb4_unicode_ci NOT NULL,
  `BusinessID` int(11) NOT NULL,
  `CreatedAt` datetime NOT NULL,
  `LastUsedAt` datetime DEFAULT NULL,
  `RevokedAt` datetime DEFAULT NULL,
  PRIMARY KEY (`TokenHash`),
  KEY `IX_BusinessEnrollmentLinkToken_BusinessID_RevokedAt` (`BusinessID`,`RevokedAt`),
  KEY `IX_BusinessEnrollmentLinkToken_TokenSuffix` (`TokenSuffix`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `BusinessEnrollmentLinkToken`
--

LOCK TABLES `BusinessEnrollmentLinkToken` WRITE;
/*!40000 ALTER TABLE `BusinessEnrollmentLinkToken` DISABLE KEYS */;
INSERT INTO `BusinessEnrollmentLinkToken` (`TokenHash`, `TokenSuffix`, `BusinessID`, `CreatedAt`, `LastUsedAt`, `RevokedAt`) VALUES ('2785c2c72f5345404fb501943bf46c61f34853aa53487944f254075c809b30aa','9sTLDMwk',40,'2026-05-18 14:50:54',NULL,'2026-05-23 20:11:59'),('ed493111a2d5894aeaa8d4db56e9756a1cd5782f4c0f45a11ec216f351edaf2c','cD4ymNOs',40,'2026-05-23 20:12:00','2026-05-23 20:12:33',NULL),('ffb3ec86dcc6bae27e0379b47af01707bd507bd3e41e2b17f56965e8128804f9','DsCGpId4',40,'2026-05-13 23:19:14',NULL,'2026-05-18 14:50:54');
/*!40000 ALTER TABLE `BusinessEnrollmentLinkToken` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `ClientCard`
--

DROP TABLE IF EXISTS `ClientCard`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `ClientCard` (
  `CardID` int(11) NOT NULL AUTO_INCREMENT,
  `CardIDGoogle` varchar(10) COLLATE utf8_unicode_ci DEFAULT NULL,
  `CreationDate` datetime DEFAULT NULL,
  `CheckQTY` int(11) DEFAULT NULL,
  `LastCheck` datetime DEFAULT NULL,
  `UserID` int(11) DEFAULT NULL,
  `BusinessID` int(11) DEFAULT NULL,
  `HistoricCheckQTY` int(11) DEFAULT '0',
  PRIMARY KEY (`CardID`),
  KEY `UserID` (`UserID`),
  KEY `BusinessID` (`BusinessID`),
  CONSTRAINT `ClientCard_ibfk_1` FOREIGN KEY (`UserID`) REFERENCES `UserClient` (`UserID`),
  CONSTRAINT `ClientCard_ibfk_2` FOREIGN KEY (`BusinessID`) REFERENCES `Business` (`BusinessID`)
) ENGINE=InnoDB AUTO_INCREMENT=76 DEFAULT CHARSET=utf8 COLLATE=utf8_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `ClientCard`
--

LOCK TABLES `ClientCard` WRITE;
/*!40000 ALTER TABLE `ClientCard` DISABLE KEYS */;
INSERT INTO `ClientCard` (`CardID`, `CardIDGoogle`, `CreationDate`, `CheckQTY`, `LastCheck`, `UserID`, `BusinessID`, `HistoricCheckQTY`) VALUES (65,'GhUY6Uuoeo','2025-04-15 16:01:19',3,'2026-05-13 18:54:02',20,40,13),(73,'0000000049','2026-05-11 21:03:27',0,'2026-05-23 04:54:15',19,40,57),(75,NULL,'2026-05-23 20:12:37',1,'2026-05-23 20:12:37',33,40,1);
/*!40000 ALTER TABLE `ClientCard` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `ModernAdminCredential`
--

DROP TABLE IF EXISTS `ModernAdminCredential`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `ModernAdminCredential` (
  `UserID` int(11) NOT NULL,
  `PasswordHash` varchar(512) COLLATE utf8mb4_unicode_ci NOT NULL,
  `CreatedAt` datetime(6) NOT NULL,
  `UpdatedAt` datetime(6) NOT NULL,
  PRIMARY KEY (`UserID`),
  CONSTRAINT `ModernAdminCredential_ibfk_1` FOREIGN KEY (`UserID`) REFERENCES `UserClient` (`UserID`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `ModernAdminCredential`
--

LOCK TABLES `ModernAdminCredential` WRITE;
/*!40000 ALTER TABLE `ModernAdminCredential` DISABLE KEYS */;
INSERT INTO `ModernAdminCredential` (`UserID`, `PasswordHash`, `CreatedAt`, `UpdatedAt`) VALUES (1,'AQAAAAIAAYagAAAAENfSXg41vZEBFMwZbUE5poBmXRA1VwIkB9X8mcDXhDLBBCothH1MhDViRIGwHpfBmQ==','2026-05-13 13:14:05.905506','2026-05-13 13:14:05.905506');
/*!40000 ALTER TABLE `ModernAdminCredential` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `ModernAuditEvent`
--

DROP TABLE IF EXISTS `ModernAuditEvent`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `ModernAuditEvent` (
  `ID` bigint(20) NOT NULL AUTO_INCREMENT,
  `EventType` varchar(64) COLLATE utf8mb4_unicode_ci NOT NULL,
  `ActorAdminUserID` int(11) NOT NULL,
  `BusinessID` int(11) DEFAULT NULL,
  `UserID` int(11) DEFAULT NULL,
  `CardID` int(11) DEFAULT NULL,
  `TargetAdminUserID` int(11) DEFAULT NULL,
  `Summary` varchar(500) COLLATE utf8mb4_unicode_ci NOT NULL,
  `CreatedAt` datetime NOT NULL,
  PRIMARY KEY (`ID`),
  KEY `IX_ModernAuditEvent_EventType_CreatedAt` (`EventType`,`CreatedAt`),
  KEY `IX_ModernAuditEvent_Actor_CreatedAt` (`ActorAdminUserID`,`CreatedAt`),
  KEY `IX_ModernAuditEvent_Business_CreatedAt` (`BusinessID`,`CreatedAt`),
  KEY `IX_ModernAuditEvent_User_CreatedAt` (`UserID`,`CreatedAt`),
  KEY `IX_ModernAuditEvent_Card_CreatedAt` (`CardID`,`CreatedAt`)
) ENGINE=InnoDB AUTO_INCREMENT=45 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `ModernAuditEvent`
--

LOCK TABLES `ModernAuditEvent` WRITE;
/*!40000 ALTER TABLE `ModernAuditEvent` DISABLE KEYS */;
INSERT INTO `ModernAuditEvent` (`ID`, `EventType`, `ActorAdminUserID`, `BusinessID`, `UserID`, `CardID`, `TargetAdminUserID`, `Summary`, `CreatedAt`) VALUES (1,'BusinessCreated',1,42,NULL,NULL,NULL,'Business Test Coffee created. Pilot enabled: False.','2026-05-14 16:29:30'),(2,'BusinessBrandingUpdated',1,40,NULL,NULL,NULL,'Business branding updated for Balboa Water.','2026-05-14 16:46:18'),(3,'BusinessDeleted',1,1,NULL,NULL,NULL,'Business test permanently deleted.','2026-05-14 18:15:12'),(4,'BusinessDeleted',1,39,NULL,NULL,NULL,'Business test60 permanently deleted.','2026-05-14 18:15:32'),(5,'PilotBusinessChanged',1,41,NULL,NULL,NULL,'Business test92 pilot state changed to Inactive.','2026-05-14 18:15:46'),(6,'CutoverStatusChanged',1,41,NULL,NULL,NULL,'Business test92 activation changed from LegacyOnly to Inactive.','2026-05-14 18:15:46'),(7,'BusinessDeleted',1,41,NULL,NULL,NULL,'Business test92 permanently deleted.','2026-05-14 18:15:58'),(8,'BusinessDeleted',1,36,NULL,NULL,NULL,'Business testo permanently deleted.','2026-05-14 18:16:12'),(9,'BusinessDeleted',1,30,NULL,NULL,NULL,'Business testpresentacion permanently deleted.','2026-05-14 18:16:28'),(10,'BusinessDeleted',1,37,NULL,NULL,NULL,'Business testoto permanently deleted.','2026-05-14 18:16:43'),(11,'PilotBusinessChanged',1,40,NULL,NULL,NULL,'Business Balboa Water pilot state changed to ModernPrimary.','2026-05-14 22:28:05'),(12,'CutoverStatusChanged',1,40,NULL,NULL,NULL,'Business Balboa Water activation changed from None to ModernPrimary.','2026-05-14 22:28:05'),(13,'PilotBusinessChanged',1,42,NULL,NULL,NULL,'Business Test Coffee pilot state changed to ModernPrimary.','2026-05-14 22:57:15'),(14,'CutoverStatusChanged',1,42,NULL,NULL,NULL,'Business Test Coffee activation changed from None to ModernPrimary.','2026-05-14 22:57:16'),(15,'PilotBusinessChanged',1,42,NULL,NULL,NULL,'Business Test Coffee pilot state changed to Inactive.','2026-05-14 22:57:27'),(16,'CutoverStatusChanged',1,42,NULL,NULL,NULL,'Business Test Coffee activation changed from ModernPrimary to Inactive.','2026-05-14 22:57:28'),(17,'BusinessDeleted',1,42,NULL,NULL,NULL,'Business Test Coffee permanently deleted.','2026-05-14 22:58:47'),(18,'PilotBusinessChanged',1,40,NULL,NULL,NULL,'Business Balboa Water pilot state changed to Inactive.','2026-05-15 13:19:31'),(19,'CutoverStatusChanged',1,40,NULL,NULL,NULL,'Business Balboa Water activation changed from ModernPrimary to Inactive.','2026-05-15 13:19:32'),(20,'PilotBusinessChanged',1,40,NULL,NULL,NULL,'Business Balboa Water pilot state changed to ModernPrimary.','2026-05-15 13:19:35'),(21,'CutoverStatusChanged',1,40,NULL,NULL,NULL,'Business Balboa Water activation changed from Inactive to ModernPrimary.','2026-05-15 13:19:36'),(22,'ClientDeleted',1,NULL,11,NULL,NULL,'Client alfredo60fil permanently deleted.','2026-05-15 13:24:48'),(23,'ClientDeleted',1,NULL,11,NULL,NULL,'Client alfredo60fil permanently deleted.','2026-05-15 13:25:18'),(24,'ClientDeleted',1,NULL,11,NULL,NULL,'Client alfredo60fil permanently deleted.','2026-05-15 14:44:58'),(25,'ClientDeleted',1,NULL,9,NULL,NULL,'Client Ejim984 permanently deleted.','2026-05-15 14:45:05'),(26,'ClientDeleted',1,NULL,8,NULL,NULL,'Client EloyTest permanently deleted.','2026-05-15 14:45:11'),(27,'ClientDeleted',1,NULL,30,NULL,NULL,'Client eric13 permanently deleted.','2026-05-15 14:45:19'),(28,'ClientDeleted',1,NULL,26,NULL,NULL,'Client full611e398b permanently deleted.','2026-05-15 14:45:29'),(29,'ClientDeleted',1,NULL,25,NULL,NULL,'Client gwsql360ecfc permanently deleted.','2026-05-15 14:45:38'),(30,'ClientDeleted',1,NULL,27,NULL,NULL,'Client gwsql69871ef permanently deleted.','2026-05-15 14:45:44'),(31,'ClientDeleted',1,NULL,16,NULL,NULL,'Client Hola123 permanently deleted.','2026-05-15 14:45:51'),(32,'ClientDeleted',1,NULL,23,NULL,NULL,'Client m75ecf684ba2 permanently deleted.','2026-05-15 14:46:04'),(33,'ClientDeleted',1,NULL,13,NULL,NULL,'Client MannyTest permanently deleted.','2026-05-15 14:46:10'),(34,'ClientDeleted',1,NULL,15,NULL,NULL,'Client Mantest permanently deleted.','2026-05-15 14:46:18'),(35,'ClientDeleted',1,NULL,18,NULL,NULL,'Client Manuel Flores permanently deleted.','2026-05-15 14:46:32'),(36,'ClientDeleted',1,NULL,14,NULL,NULL,'Client OtroTest permanently deleted.','2026-05-15 14:46:39'),(37,'ClientDeleted',1,NULL,24,NULL,NULL,'Client pd9d7a016c69 permanently deleted.','2026-05-15 14:47:40'),(38,'ClientDeleted',1,NULL,21,NULL,NULL,'Client test25 permanently deleted.','2026-05-15 14:47:46'),(39,'ClientDeleted',1,NULL,7,NULL,NULL,'Client test50 permanently deleted.','2026-05-15 14:47:53'),(40,'ClientDeleted',1,NULL,17,NULL,NULL,'Client Veloraptor17 permanently deleted.','2026-05-15 14:48:41'),(41,'ClientDeleted',1,NULL,29,NULL,NULL,'Client wallet7f6090 permanently deleted.','2026-05-15 14:48:48'),(42,'ClientDeleted',1,NULL,28,NULL,NULL,'Client webapple1 permanently deleted.','2026-05-15 14:48:56'),(43,'BusinessUpdated',1,40,NULL,NULL,NULL,'Business Balboa Water profile updated.','2026-05-26 21:13:09'),(44,'CutoverStatusChanged',1,40,NULL,NULL,NULL,'Business Balboa Water activation changed from ModernPrimary to Inactive.','2026-05-26 21:13:09');
/*!40000 ALTER TABLE `ModernAuditEvent` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `ModernBusinessBranding`
--

DROP TABLE IF EXISTS `ModernBusinessBranding`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `ModernBusinessBranding` (
  `BusinessID` int(11) NOT NULL,
  `PublicName` varchar(80) COLLATE utf8mb4_unicode_ci NOT NULL,
  `LogoPath` varchar(200) COLLATE utf8mb4_unicode_ci NOT NULL,
  `PrimaryColor` varchar(7) COLLATE utf8mb4_unicode_ci NOT NULL,
  `SecondaryColor` varchar(7) COLLATE utf8mb4_unicode_ci NOT NULL,
  `CustomFieldColor` varchar(7) COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT '#FFFFFF',
  `StampGoal` int(11) NOT NULL DEFAULT '10',
  `ProgramName` varchar(80) COLLATE utf8mb4_unicode_ci NOT NULL,
  `ProgramDescription` varchar(280) COLLATE utf8mb4_unicode_ci NOT NULL,
  `UpdatedAt` datetime(6) NOT NULL,
  `UpdatedByAdminUserID` int(11) DEFAULT NULL,
  PRIMARY KEY (`BusinessID`),
  KEY `IX_ModernBusinessBranding_UpdatedAt` (`UpdatedAt`),
  KEY `ModernBusinessBranding_Admin_fk` (`UpdatedByAdminUserID`),
  CONSTRAINT `ModernBusinessBranding_Admin_fk` FOREIGN KEY (`UpdatedByAdminUserID`) REFERENCES `UserClient` (`UserID`),
  CONSTRAINT `ModernBusinessBranding_Business_fk` FOREIGN KEY (`BusinessID`) REFERENCES `Business` (`BusinessID`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `ModernBusinessBranding`
--

LOCK TABLES `ModernBusinessBranding` WRITE;
/*!40000 ALTER TABLE `ModernBusinessBranding` DISABLE KEYS */;
INSERT INTO `ModernBusinessBranding` (`BusinessID`, `PublicName`, `LogoPath`, `PrimaryColor`, `SecondaryColor`, `CustomFieldColor`, `StampGoal`, `ProgramName`, `ProgramDescription`, `UpdatedAt`, `UpdatedByAdminUserID`) VALUES (40,'Runni Cafe','/uploads/business-logos/00000000000000000000000000000028/58ae5a431b2cb976498cda978013061f/logo.png','#fcfbf7','#000000','#5E4540',10,'Tarjeta de lealtad','Cafe gratis','2026-05-15 17:13:49.193521',NULL);
/*!40000 ALTER TABLE `ModernBusinessBranding` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `ModernBusinessCredential`
--

DROP TABLE IF EXISTS `ModernBusinessCredential`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `ModernBusinessCredential` (
  `BusinessID` int(11) NOT NULL,
  `PasswordHash` varchar(512) COLLATE utf8mb4_unicode_ci NOT NULL,
  `CreatedAt` datetime NOT NULL,
  `UpdatedAt` datetime NOT NULL,
  PRIMARY KEY (`BusinessID`),
  CONSTRAINT `ModernBusinessCredential_ibfk_1` FOREIGN KEY (`BusinessID`) REFERENCES `Business` (`BusinessID`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `ModernBusinessCredential`
--

LOCK TABLES `ModernBusinessCredential` WRITE;
/*!40000 ALTER TABLE `ModernBusinessCredential` DISABLE KEYS */;
INSERT INTO `ModernBusinessCredential` (`BusinessID`, `PasswordHash`, `CreatedAt`, `UpdatedAt`) VALUES (40,'AQAAAAIAAYagAAAAEH0Bcxn4u6gLGb/YaUPFwSTYbTPs6/jLjcrWY22bpWHABPRcDRSSZ7Qk55Rm5W6szQ==','2026-05-12 14:01:26','2026-05-12 14:01:26');
/*!40000 ALTER TABLE `ModernBusinessCredential` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `ModernClientCardStatus`
--

DROP TABLE IF EXISTS `ModernClientCardStatus`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `ModernClientCardStatus` (
  `CardID` int(11) NOT NULL,
  `BusinessID` int(11) NOT NULL,
  `IsActive` tinyint(1) NOT NULL DEFAULT '1',
  `DisabledAt` datetime DEFAULT NULL,
  `UpdatedAt` datetime NOT NULL,
  `UpdatedByBusinessID` int(11) DEFAULT NULL,
  PRIMARY KEY (`CardID`),
  KEY `IX_ModernClientCardStatus_BusinessID` (`BusinessID`),
  KEY `IX_ModernClientCardStatus_IsActive` (`IsActive`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `ModernClientCardStatus`
--

LOCK TABLES `ModernClientCardStatus` WRITE;
/*!40000 ALTER TABLE `ModernClientCardStatus` DISABLE KEYS */;
INSERT INTO `ModernClientCardStatus` (`CardID`, `BusinessID`, `IsActive`, `DisabledAt`, `UpdatedAt`, `UpdatedByBusinessID`) VALUES (73,40,1,NULL,'2026-05-14 18:05:24',40);
/*!40000 ALTER TABLE `ModernClientCardStatus` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `ModernClientConsent`
--

DROP TABLE IF EXISTS `ModernClientConsent`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `ModernClientConsent` (
  `ID` bigint(20) NOT NULL AUTO_INCREMENT,
  `UserID` int(11) NOT NULL,
  `BusinessID` int(11) DEFAULT NULL,
  `PolicyVersion` varchar(64) COLLATE utf8mb4_unicode_ci NOT NULL,
  `Source` varchar(64) COLLATE utf8mb4_unicode_ci NOT NULL,
  `AcceptedAt` datetime NOT NULL,
  PRIMARY KEY (`ID`),
  KEY `IX_ModernClientConsent_User_AcceptedAt` (`UserID`,`AcceptedAt`),
  KEY `IX_ModernClientConsent_Business_AcceptedAt` (`BusinessID`,`AcceptedAt`),
  KEY `IX_ModernClientConsent_PolicyVersion` (`PolicyVersion`)
) ENGINE=InnoDB AUTO_INCREMENT=2 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `ModernClientConsent`
--

LOCK TABLES `ModernClientConsent` WRITE;
/*!40000 ALTER TABLE `ModernClientConsent` DISABLE KEYS */;
INSERT INTO `ModernClientConsent` (`ID`, `UserID`, `BusinessID`, `PolicyVersion`, `Source`, `AcceptedAt`) VALUES (1,33,40,'privacy-2026-05','PublicBusinessEnrollment','2026-05-23 20:12:42');
/*!40000 ALTER TABLE `ModernClientConsent` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `ModernClientCredential`
--

DROP TABLE IF EXISTS `ModernClientCredential`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `ModernClientCredential` (
  `UserID` int(11) NOT NULL,
  `PasswordHash` varchar(512) COLLATE utf8mb4_unicode_ci NOT NULL,
  `CreatedAt` datetime(6) NOT NULL,
  `UpdatedAt` datetime(6) NOT NULL,
  PRIMARY KEY (`UserID`),
  CONSTRAINT `ModernClientCredential_ibfk_1` FOREIGN KEY (`UserID`) REFERENCES `UserClient` (`UserID`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `ModernClientCredential`
--

LOCK TABLES `ModernClientCredential` WRITE;
/*!40000 ALTER TABLE `ModernClientCredential` DISABLE KEYS */;
INSERT INTO `ModernClientCredential` (`UserID`, `PasswordHash`, `CreatedAt`, `UpdatedAt`) VALUES (19,'AQAAAAIAAYagAAAAEIyhmtJOtX2sxbLZBHJDs3bYZ5ChopFNBIwItt0Z4lW7gO+bEwTfDF9t4RTN+ldsDA==','2026-05-13 21:35:50.196664','2026-05-13 21:35:50.196664'),(33,'AQAAAAIAAYagAAAAEHmBXz2bI7/z6Ig8TYQ8uz6jCGSEQyvKFjpLypAErGFESRwtOVJBYkuV6EHfEBIz5A==','2026-05-23 20:12:35.584815','2026-05-23 20:12:35.584815');
/*!40000 ALTER TABLE `ModernClientCredential` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `ModernCutoverSmoke`
--

DROP TABLE IF EXISTS `ModernCutoverSmoke`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `ModernCutoverSmoke` (
  `ID` bigint(20) NOT NULL AUTO_INCREMENT,
  `BusinessID` int(11) NOT NULL,
  `AdminUserID` int(11) NOT NULL,
  `HealthOk` bit(1) NOT NULL DEFAULT b'0',
  `ReadyOk` bit(1) NOT NULL DEFAULT b'0',
  `EmailOk` bit(1) NOT NULL DEFAULT b'0',
  `AppleWalletOk` bit(1) NOT NULL DEFAULT b'0',
  `GoogleWalletOk` bit(1) NOT NULL DEFAULT b'0',
  `ModernStampOk` bit(1) NOT NULL DEFAULT b'0',
  `SupportReviewed` bit(1) NOT NULL DEFAULT b'0',
  `Notes` varchar(500) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `CreatedAt` datetime NOT NULL,
  PRIMARY KEY (`ID`),
  KEY `IX_ModernCutoverSmoke_Business_CreatedAt` (`BusinessID`,`CreatedAt`),
  KEY `IX_ModernCutoverSmoke_AdminUserID` (`AdminUserID`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `ModernCutoverSmoke`
--

LOCK TABLES `ModernCutoverSmoke` WRITE;
/*!40000 ALTER TABLE `ModernCutoverSmoke` DISABLE KEYS */;
/*!40000 ALTER TABLE `ModernCutoverSmoke` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `ModernPasswordResetToken`
--

DROP TABLE IF EXISTS `ModernPasswordResetToken`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `ModernPasswordResetToken` (
  `ID` bigint(20) NOT NULL AUTO_INCREMENT,
  `AccountType` varchar(20) COLLATE utf8mb4_unicode_ci NOT NULL,
  `AccountID` int(11) NOT NULL,
  `TokenHash` char(64) COLLATE utf8mb4_unicode_ci NOT NULL,
  `TokenSuffix` varchar(12) COLLATE utf8mb4_unicode_ci NOT NULL,
  `CreatedAt` datetime NOT NULL,
  `ExpiresAt` datetime NOT NULL,
  `UsedAt` datetime DEFAULT NULL,
  `RevokedAt` datetime DEFAULT NULL,
  PRIMARY KEY (`ID`),
  UNIQUE KEY `UX_ModernPasswordResetToken_TokenHash_AccountType` (`TokenHash`,`AccountType`),
  KEY `IX_ModernPasswordResetToken_Account` (`AccountType`,`AccountID`,`UsedAt`,`RevokedAt`,`ExpiresAt`),
  KEY `IX_ModernPasswordResetToken_TokenSuffix` (`TokenSuffix`)
) ENGINE=InnoDB AUTO_INCREMENT=2 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `ModernPasswordResetToken`
--

LOCK TABLES `ModernPasswordResetToken` WRITE;
/*!40000 ALTER TABLE `ModernPasswordResetToken` DISABLE KEYS */;
/*!40000 ALTER TABLE `ModernPasswordResetToken` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `ModernPilotBusiness`
--

DROP TABLE IF EXISTS `ModernPilotBusiness`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `ModernPilotBusiness` (
  `BusinessID` int(11) NOT NULL,
  `IsEnabled` tinyint(1) NOT NULL DEFAULT '0',
  `ActivationStatus` varchar(32) COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT 'LegacyOnly',
  `Notes` varchar(500) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `CreatedAt` datetime(6) NOT NULL,
  `UpdatedAt` datetime(6) NOT NULL,
  `UpdatedByAdminUserID` int(11) NOT NULL,
  PRIMARY KEY (`BusinessID`),
  KEY `IX_ModernPilotBusiness_IsEnabled` (`IsEnabled`),
  KEY `IX_ModernPilotBusiness_UpdatedAt` (`UpdatedAt`),
  KEY `IX_ModernPilotBusiness_ActivationStatus` (`ActivationStatus`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `ModernPilotBusiness`
--

LOCK TABLES `ModernPilotBusiness` WRITE;
/*!40000 ALTER TABLE `ModernPilotBusiness` DISABLE KEYS */;
INSERT INTO `ModernPilotBusiness` (`BusinessID`, `IsEnabled`, `ActivationStatus`, `Notes`, `CreatedAt`, `UpdatedAt`, `UpdatedByAdminUserID`) VALUES (40,0,'Inactive',NULL,'2026-05-14 22:28:04.245271','2026-05-26 21:13:07.876745',1);
/*!40000 ALTER TABLE `ModernPilotBusiness` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `ModernPilotClient`
--

DROP TABLE IF EXISTS `ModernPilotClient`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `ModernPilotClient` (
  `UserID` int(11) NOT NULL,
  `IsEnabled` tinyint(1) NOT NULL DEFAULT '0',
  `Notes` varchar(500) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `CreatedAt` datetime(6) NOT NULL,
  `UpdatedAt` datetime(6) NOT NULL,
  `UpdatedByAdminUserID` int(11) NOT NULL,
  PRIMARY KEY (`UserID`),
  KEY `IX_ModernPilotClient_IsEnabled` (`IsEnabled`),
  KEY `IX_ModernPilotClient_UpdatedAt` (`UpdatedAt`),
  KEY `IX_ModernPilotClient_UpdatedByAdminUserID` (`UpdatedByAdminUserID`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `ModernPilotClient`
--

LOCK TABLES `ModernPilotClient` WRITE;
/*!40000 ALTER TABLE `ModernPilotClient` DISABLE KEYS */;
/*!40000 ALTER TABLE `ModernPilotClient` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `PasswordResetToken`
--

DROP TABLE IF EXISTS `PasswordResetToken`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `PasswordResetToken` (
  `UserEmail` varchar(256) COLLATE utf8_unicode_ci NOT NULL,
  `Token` varchar(256) COLLATE utf8_unicode_ci NOT NULL,
  `Expiration` datetime NOT NULL,
  PRIMARY KEY (`Token`),
  KEY `UserEmail` (`UserEmail`),
  CONSTRAINT `PasswordResetToken_ibfk_1` FOREIGN KEY (`UserEmail`) REFERENCES `UserClient` (`UserEmail`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `PasswordResetToken`
--

LOCK TABLES `PasswordResetToken` WRITE;
/*!40000 ALTER TABLE `PasswordResetToken` DISABLE KEYS */;
INSERT INTO `PasswordResetToken` (`UserEmail`, `Token`, `Expiration`) VALUES ('guillenmorenoe58@gmail.com','456a8375-996e-4d83-b068-274d9c752e28','2026-05-08 19:50:33'),('ingelabmx@gmail.com','5b6f2731-7371-4ab3-95af-0da81c47e3bd','2025-04-16 21:33:18');
/*!40000 ALTER TABLE `PasswordResetToken` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `RewardRedemption`
--

DROP TABLE IF EXISTS `RewardRedemption`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `RewardRedemption` (
  `ID` bigint(20) NOT NULL AUTO_INCREMENT,
  `CardID` int(11) NOT NULL,
  `BusinessID` int(11) NOT NULL,
  `UserID` int(11) NOT NULL,
  `ActorBusinessID` int(11) DEFAULT NULL,
  `StampGoal` int(11) NOT NULL,
  `RedeemedCheckQTY` int(11) NOT NULL,
  `HistoricCheckQTY` int(11) NOT NULL,
  `RewardText` varchar(280) COLLATE utf8mb4_unicode_ci NOT NULL,
  `GoogleWalletAttempted` tinyint(1) NOT NULL DEFAULT '0',
  `GoogleWalletSucceeded` tinyint(1) NOT NULL DEFAULT '0',
  `AppleWalletAttempted` tinyint(1) NOT NULL DEFAULT '0',
  `AppleWalletSucceeded` tinyint(1) NOT NULL DEFAULT '0',
  `ErrorSummary` varchar(255) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `RedeemedAt` datetime(6) NOT NULL,
  `CreatedAt` datetime(6) NOT NULL,
  PRIMARY KEY (`ID`),
  KEY `IX_RewardRedemption_CardID_RedeemedAt` (`CardID`,`RedeemedAt`),
  KEY `IX_RewardRedemption_BusinessID_RedeemedAt` (`BusinessID`,`RedeemedAt`),
  KEY `IX_RewardRedemption_UserID_RedeemedAt` (`UserID`,`RedeemedAt`)
) ENGINE=InnoDB AUTO_INCREMENT=4 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `RewardRedemption`
--

LOCK TABLES `RewardRedemption` WRITE;
/*!40000 ALTER TABLE `RewardRedemption` DISABLE KEYS */;
INSERT INTO `RewardRedemption` (`ID`, `CardID`, `BusinessID`, `UserID`, `ActorBusinessID`, `StampGoal`, `RedeemedCheckQTY`, `HistoricCheckQTY`, `RewardText`, `GoogleWalletAttempted`, `GoogleWalletSucceeded`, `AppleWalletAttempted`, `AppleWalletSucceeded`, `ErrorSummary`, `RedeemedAt`, `CreatedAt`) VALUES (1,73,40,19,40,10,10,37,'Cafe gratis',1,1,1,1,NULL,'2026-05-15 18:39:12.515585','2026-05-15 18:39:16.411135'),(2,73,40,19,40,10,10,47,'Cafe gratis',1,1,1,1,NULL,'2026-05-15 19:37:36.641886','2026-05-15 19:37:39.938245'),(3,73,40,19,40,10,10,57,'Cafe gratis',1,0,0,0,'GoogleApiException','2026-05-23 04:54:14.726487','2026-05-23 04:54:18.651790');
/*!40000 ALTER TABLE `RewardRedemption` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `Role`
--

DROP TABLE IF EXISTS `Role`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `Role` (
  `RoleID` int(11) NOT NULL AUTO_INCREMENT,
  `RoleDescription` varchar(10) COLLATE utf8_unicode_ci DEFAULT NULL,
  PRIMARY KEY (`RoleID`)
) ENGINE=InnoDB AUTO_INCREMENT=3 DEFAULT CHARSET=utf8 COLLATE=utf8_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `Role`
--

LOCK TABLES `Role` WRITE;
/*!40000 ALTER TABLE `Role` DISABLE KEYS */;
INSERT INTO `Role` (`RoleID`, `RoleDescription`) VALUES (1,'ADMIN'),(2,'CLIENT');
/*!40000 ALTER TABLE `Role` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `StampLedger`
--

DROP TABLE IF EXISTS `StampLedger`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `StampLedger` (
  `ID` int(11) NOT NULL AUTO_INCREMENT,
  `CardID` int(11) NOT NULL,
  `BusinessID` int(11) NOT NULL,
  `UserID` int(11) NOT NULL,
  `Source` varchar(32) COLLATE utf8mb4_unicode_ci NOT NULL,
  `ActorBusinessID` int(11) DEFAULT NULL,
  `PreviousCheckQTY` int(11) NOT NULL,
  `NewCheckQTY` int(11) NOT NULL,
  `PreviousHistoricCheckQTY` int(11) NOT NULL,
  `NewHistoricCheckQTY` int(11) NOT NULL,
  `ObservedLastCheck` datetime(6) NOT NULL,
  `GoogleWalletAttempted` tinyint(1) NOT NULL DEFAULT '0',
  `GoogleWalletSucceeded` tinyint(1) NOT NULL DEFAULT '0',
  `AppleWalletAttempted` tinyint(1) NOT NULL DEFAULT '0',
  `AppleWalletSucceeded` tinyint(1) NOT NULL DEFAULT '0',
  `ErrorSummary` varchar(255) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `CreatedAt` datetime(6) NOT NULL,
  PRIMARY KEY (`ID`),
  KEY `IX_StampLedger_CardID_CreatedAt` (`CardID`,`CreatedAt`),
  KEY `IX_StampLedger_BusinessID_CreatedAt` (`BusinessID`,`CreatedAt`),
  KEY `IX_StampLedger_Source_CreatedAt` (`Source`,`CreatedAt`)
) ENGINE=InnoDB AUTO_INCREMENT=339 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `StampLedger`
--

LOCK TABLES `StampLedger` WRITE;
/*!40000 ALTER TABLE `StampLedger` DISABLE KEYS */;
INSERT INTO `StampLedger` (`ID`, `CardID`, `BusinessID`, `UserID`, `Source`, `ActorBusinessID`, `PreviousCheckQTY`, `NewCheckQTY`, `PreviousHistoricCheckQTY`, `NewHistoricCheckQTY`, `ObservedLastCheck`, `GoogleWalletAttempted`, `GoogleWalletSucceeded`, `AppleWalletAttempted`, `AppleWalletSucceeded`, `ErrorSummary`, `CreatedAt`) VALUES (2,73,40,19,'LegacySync',NULL,6,6,13,13,'2026-05-12 19:15:51.000000',1,1,1,1,NULL,'2026-05-12 21:35:59.101256'),(4,73,40,19,'LegacySync',NULL,6,6,13,13,'2026-05-12 19:15:51.000000',1,1,1,1,NULL,'2026-05-12 22:06:07.166451'),(6,73,40,19,'LegacySync',NULL,6,6,13,13,'2026-05-12 19:15:51.000000',1,1,1,1,NULL,'2026-05-12 22:12:46.198125'),(8,73,40,19,'LegacySync',NULL,6,6,13,13,'2026-05-12 19:15:51.000000',1,1,1,1,NULL,'2026-05-12 22:51:20.716196'),(10,73,40,19,'LegacySync',NULL,6,6,13,13,'2026-05-12 19:15:51.000000',1,1,1,1,NULL,'2026-05-13 13:12:36.841743'),(11,73,40,19,'LegacySync',NULL,6,6,13,13,'2026-05-12 19:15:51.000000',1,1,1,1,NULL,'2026-05-13 13:48:45.406839'),(12,73,40,19,'LegacySync',NULL,6,6,13,13,'2026-05-12 19:15:51.000000',1,1,1,1,NULL,'2026-05-13 16:50:55.241057'),(13,73,40,19,'LegacySync',NULL,6,6,13,13,'2026-05-12 19:15:51.000000',1,1,1,1,NULL,'2026-05-13 18:48:18.340444'),(14,73,40,19,'ModernBusiness',40,6,7,13,14,'2026-05-13 18:52:42.341304',1,1,1,1,NULL,'2026-05-13 18:52:45.402492'),(15,73,40,19,'LegacySync',NULL,7,7,14,14,'2026-05-13 18:52:42.000000',1,1,1,1,NULL,'2026-05-13 18:53:21.303262'),(16,65,40,20,'ModernBusiness',40,2,3,12,13,'2026-05-13 18:54:01.799013',1,1,1,1,NULL,'2026-05-13 18:54:04.483133'),(17,65,40,20,'LegacySync',NULL,3,3,13,13,'2026-05-13 18:54:02.000000',1,1,0,0,NULL,'2026-05-13 18:54:20.290402'),(18,73,40,19,'LegacySync',NULL,7,7,14,14,'2026-05-13 18:52:42.000000',1,1,1,1,NULL,'2026-05-13 20:20:20.331496'),(19,65,40,20,'LegacySync',NULL,3,3,13,13,'2026-05-13 18:54:02.000000',1,1,0,0,NULL,'2026-05-13 20:20:22.468714'),(20,73,40,19,'LegacySync',NULL,7,7,14,14,'2026-05-13 18:52:42.000000',1,1,1,1,NULL,'2026-05-13 21:31:23.689562'),(21,65,40,20,'LegacySync',NULL,3,3,13,13,'2026-05-13 18:54:02.000000',1,1,0,0,NULL,'2026-05-13 21:31:24.652634'),(22,73,40,19,'ModernBusiness',40,7,8,14,15,'2026-05-13 21:34:25.136932',1,1,1,1,NULL,'2026-05-13 21:34:28.275860'),(23,73,40,19,'LegacySync',NULL,8,8,15,15,'2026-05-13 21:34:25.000000',1,1,1,1,NULL,'2026-05-13 21:34:28.395104'),(24,65,40,20,'LegacySync',NULL,3,3,13,13,'2026-05-13 18:54:02.000000',1,1,0,0,NULL,'2026-05-13 23:17:09.563729'),(25,73,40,19,'LegacySync',NULL,8,8,15,15,'2026-05-13 21:34:25.000000',1,1,1,1,NULL,'2026-05-13 23:17:15.327793'),(26,65,40,20,'LegacySync',NULL,3,3,13,13,'2026-05-13 18:54:02.000000',1,1,0,0,NULL,'2026-05-14 14:03:10.755572'),(27,73,40,19,'LegacySync',NULL,8,8,15,15,'2026-05-13 21:34:25.000000',1,1,1,1,NULL,'2026-05-14 14:03:13.419668'),(28,65,40,20,'LegacySync',NULL,3,3,13,13,'2026-05-13 18:54:02.000000',1,1,0,0,NULL,'2026-05-14 16:19:36.148732'),(29,73,40,19,'LegacySync',NULL,8,8,15,15,'2026-05-13 21:34:25.000000',1,1,1,1,NULL,'2026-05-14 16:19:38.459804'),(30,73,40,19,'ModernBusiness',40,8,9,15,16,'2026-05-14 16:21:41.047649',1,1,1,1,NULL,'2026-05-14 16:21:50.676886'),(31,73,40,19,'LegacySync',NULL,9,9,16,16,'2026-05-14 16:21:41.000000',1,1,1,1,NULL,'2026-05-14 16:22:41.359952'),(32,73,40,19,'ModernBusiness',40,9,0,16,17,'2026-05-14 16:24:25.106343',1,1,1,1,NULL,'2026-05-14 16:24:27.650390'),(33,73,40,19,'LegacySync',NULL,0,0,17,17,'2026-05-14 16:24:25.000000',1,1,1,1,NULL,'2026-05-14 16:24:40.936790'),(34,65,40,20,'LegacySync',NULL,3,3,13,13,'2026-05-13 18:54:02.000000',1,1,0,0,NULL,'2026-05-14 17:58:01.275462'),(35,73,40,19,'LegacySync',NULL,0,0,17,17,'2026-05-14 16:24:25.000000',1,1,1,1,NULL,'2026-05-14 17:58:03.540134'),(36,73,40,19,'BrandingRefresh',40,0,0,17,17,'2026-05-14 16:24:25.000000',1,1,1,1,NULL,'2026-05-14 18:10:12.905653'),(37,65,40,20,'BrandingRefresh',40,3,3,13,13,'2026-05-13 18:54:02.000000',1,1,0,0,NULL,'2026-05-14 18:10:14.380780'),(42,65,40,20,'LegacySync',NULL,3,3,13,13,'2026-05-13 18:54:02.000000',1,1,0,0,NULL,'2026-05-14 18:21:22.733377'),(43,73,40,19,'LegacySync',NULL,0,0,17,17,'2026-05-14 16:24:25.000000',1,1,1,1,NULL,'2026-05-14 18:21:25.767822'),(44,73,40,19,'ModernBusiness',40,0,1,17,18,'2026-05-14 18:24:32.883350',1,1,1,1,NULL,'2026-05-14 18:24:35.578267'),(45,73,40,19,'LegacySync',NULL,1,1,18,18,'2026-05-14 18:24:33.000000',1,1,1,1,NULL,'2026-05-14 18:25:28.351784'),(46,73,40,19,'LegacySync',NULL,1,1,18,18,'2026-05-14 18:24:33.000000',1,1,1,1,NULL,'2026-05-14 19:36:32.167587'),(47,73,40,19,'BrandingRefresh',40,1,1,18,18,'2026-05-14 18:24:33.000000',1,1,1,1,NULL,'2026-05-14 19:41:33.293199'),(48,65,40,20,'BrandingRefresh',40,3,3,13,13,'2026-05-13 18:54:02.000000',1,1,0,0,NULL,'2026-05-14 19:41:34.893421'),(53,73,40,19,'ModernBusiness',40,1,2,18,19,'2026-05-14 19:42:03.234059',1,1,1,1,NULL,'2026-05-14 19:42:05.993430'),(54,73,40,19,'LegacySync',NULL,2,2,19,19,'2026-05-14 19:42:03.000000',1,1,1,1,NULL,'2026-05-14 19:42:34.809073'),(55,73,40,19,'BrandingRefresh',40,2,2,19,19,'2026-05-14 19:42:03.000000',1,1,1,1,NULL,'2026-05-14 19:45:03.148387'),(56,65,40,20,'BrandingRefresh',40,3,3,13,13,'2026-05-13 18:54:02.000000',1,1,0,0,NULL,'2026-05-14 19:45:04.598439'),(61,73,40,19,'BrandingRefresh',40,2,2,19,19,'2026-05-14 19:42:03.000000',1,1,1,1,NULL,'2026-05-14 19:46:10.855330'),(62,65,40,20,'BrandingRefresh',40,3,3,13,13,'2026-05-13 18:54:02.000000',1,1,0,0,NULL,'2026-05-14 19:46:12.327404'),(67,73,40,19,'ModernBusiness',40,2,3,19,20,'2026-05-14 19:51:07.638449',1,1,1,1,NULL,'2026-05-14 19:51:11.002597'),(68,73,40,19,'LegacySync',NULL,3,3,20,20,'2026-05-14 19:51:08.000000',1,1,1,1,NULL,'2026-05-14 19:51:34.583195'),(69,73,40,19,'LegacySync',NULL,3,3,20,20,'2026-05-14 19:51:08.000000',1,1,1,1,NULL,'2026-05-14 21:40:23.786636'),(70,73,40,19,'ModernBusiness',40,3,4,20,21,'2026-05-14 21:42:14.982386',1,1,1,1,NULL,'2026-05-14 21:42:18.563131'),(71,73,40,19,'LegacySync',NULL,4,4,21,21,'2026-05-14 21:42:15.000000',1,1,1,1,NULL,'2026-05-14 21:42:26.588776'),(72,73,40,19,'BrandingRefresh',40,4,4,21,21,'2026-05-14 21:42:15.000000',1,1,1,1,NULL,'2026-05-14 21:44:29.071969'),(73,65,40,20,'BrandingRefresh',40,3,3,13,13,'2026-05-13 18:54:02.000000',1,1,0,0,NULL,'2026-05-14 21:44:30.822055'),(78,73,40,19,'BrandingRefresh',40,4,4,21,21,'2026-05-14 21:42:15.000000',1,1,1,1,NULL,'2026-05-14 21:45:40.187098'),(79,65,40,20,'BrandingRefresh',40,3,3,13,13,'2026-05-13 18:54:02.000000',1,1,0,0,NULL,'2026-05-14 21:45:41.868911'),(84,73,40,19,'LegacySync',NULL,4,4,21,21,'2026-05-14 21:42:15.000000',1,1,1,1,NULL,'2026-05-14 22:04:35.254689'),(85,73,40,19,'ModernBusiness',40,4,5,21,22,'2026-05-14 22:05:06.325214',1,1,1,1,NULL,'2026-05-14 22:05:09.312556'),(86,73,40,19,'LegacySync',NULL,5,5,22,22,'2026-05-14 22:05:06.000000',1,1,1,1,NULL,'2026-05-14 22:05:38.061862'),(87,73,40,19,'BrandingRefresh',40,5,5,22,22,'2026-05-14 22:05:06.000000',1,1,1,1,NULL,'2026-05-14 22:07:39.682739'),(88,65,40,20,'BrandingRefresh',40,3,3,13,13,'2026-05-13 18:54:02.000000',1,1,0,0,NULL,'2026-05-14 22:07:41.152949'),(93,73,40,19,'BrandingRefresh',40,5,5,22,22,'2026-05-14 22:05:06.000000',1,1,1,1,NULL,'2026-05-14 22:12:14.719417'),(94,65,40,20,'BrandingRefresh',40,3,3,13,13,'2026-05-13 18:54:02.000000',1,1,0,0,NULL,'2026-05-14 22:12:16.247590'),(99,73,40,19,'BrandingRefresh',40,5,5,22,22,'2026-05-14 22:05:06.000000',1,1,1,1,NULL,'2026-05-14 22:15:24.807559'),(100,65,40,20,'BrandingRefresh',40,3,3,13,13,'2026-05-13 18:54:02.000000',1,1,0,0,NULL,'2026-05-14 22:15:26.320397'),(105,73,40,19,'LegacySync',NULL,5,5,22,22,'2026-05-14 22:05:06.000000',1,1,1,1,NULL,'2026-05-14 22:22:26.500788'),(106,73,40,19,'LegacySync',NULL,5,5,22,22,'2026-05-14 22:05:06.000000',1,1,1,1,NULL,'2026-05-14 22:42:58.991090'),(107,73,40,19,'BrandingRefresh',40,5,5,22,22,'2026-05-14 22:05:06.000000',1,1,1,1,NULL,'2026-05-14 22:45:29.970137'),(108,65,40,20,'BrandingRefresh',40,3,3,13,13,'2026-05-13 18:54:02.000000',1,1,0,0,NULL,'2026-05-14 22:45:31.838726'),(113,73,40,19,'LegacySync',NULL,5,5,22,22,'2026-05-14 22:05:06.000000',1,1,1,1,NULL,'2026-05-14 22:54:29.054467'),(114,73,40,19,'LegacySync',NULL,5,5,22,22,'2026-05-14 22:05:06.000000',1,1,1,1,NULL,'2026-05-15 13:10:47.949114'),(115,73,40,19,'BrandingRefresh',40,5,5,22,22,'2026-05-14 22:05:06.000000',1,1,1,1,NULL,'2026-05-15 13:15:11.161376'),(116,65,40,20,'BrandingRefresh',40,3,3,13,13,'2026-05-13 18:54:02.000000',1,1,0,0,NULL,'2026-05-15 13:15:14.098284'),(121,73,40,19,'BrandingRefresh',40,5,5,22,22,'2026-05-14 22:05:06.000000',1,1,1,1,NULL,'2026-05-15 13:15:41.728410'),(122,65,40,20,'BrandingRefresh',40,3,3,13,13,'2026-05-13 18:54:02.000000',1,1,0,0,NULL,'2026-05-15 13:15:44.437222'),(127,73,40,19,'BrandingRefresh',40,5,5,22,22,'2026-05-14 22:05:06.000000',1,1,1,1,NULL,'2026-05-15 13:16:43.609255'),(128,65,40,20,'BrandingRefresh',40,3,3,13,13,'2026-05-13 18:54:02.000000',1,1,0,0,NULL,'2026-05-15 13:16:45.658708'),(133,73,40,19,'BrandingRefresh',40,5,5,22,22,'2026-05-14 22:05:06.000000',1,1,1,1,NULL,'2026-05-15 13:17:58.694832'),(134,65,40,20,'BrandingRefresh',40,3,3,13,13,'2026-05-13 18:54:02.000000',1,1,0,0,NULL,'2026-05-15 13:18:01.238708'),(139,73,40,19,'BrandingRefresh',40,5,5,22,22,'2026-05-14 22:05:06.000000',1,1,1,1,NULL,'2026-05-15 13:18:34.297803'),(140,65,40,20,'BrandingRefresh',40,3,3,13,13,'2026-05-13 18:54:02.000000',1,1,0,0,NULL,'2026-05-15 13:18:36.250273'),(145,73,40,19,'BrandingRefresh',40,5,5,22,22,'2026-05-14 22:05:06.000000',1,1,1,1,NULL,'2026-05-15 13:27:34.683360'),(146,65,40,20,'BrandingRefresh',40,3,3,13,13,'2026-05-13 18:54:02.000000',1,1,0,0,NULL,'2026-05-15 13:27:36.900245'),(151,73,40,19,'LegacySync',NULL,5,5,22,22,'2026-05-14 22:05:06.000000',1,1,1,1,NULL,'2026-05-15 13:59:23.600190'),(152,73,40,19,'BrandingRefresh',40,5,5,22,22,'2026-05-14 22:05:06.000000',1,1,1,1,NULL,'2026-05-15 14:10:40.446032'),(153,65,40,20,'BrandingRefresh',40,3,3,13,13,'2026-05-13 18:54:02.000000',1,1,0,0,NULL,'2026-05-15 14:10:42.698199'),(158,73,40,19,'LegacySync',NULL,5,5,22,22,'2026-05-14 22:05:06.000000',1,1,1,1,NULL,'2026-05-15 14:42:03.222310'),(159,73,40,19,'BrandingRefresh',40,5,5,22,22,'2026-05-14 22:05:06.000000',1,1,1,1,NULL,'2026-05-15 14:43:06.062280'),(160,65,40,20,'BrandingRefresh',40,3,3,13,13,'2026-05-13 18:54:02.000000',1,1,0,0,NULL,'2026-05-15 14:43:08.634715'),(165,73,40,19,'BrandingRefresh',40,5,5,22,22,'2026-05-14 22:05:06.000000',1,1,1,1,NULL,'2026-05-15 14:57:40.576449'),(166,65,40,20,'BrandingRefresh',40,3,3,13,13,'2026-05-13 18:54:02.000000',1,1,0,0,NULL,'2026-05-15 14:57:42.419671'),(167,73,40,19,'LegacySync',NULL,5,5,22,22,'2026-05-14 22:05:06.000000',1,1,1,1,NULL,'2026-05-15 15:23:14.260882'),(168,73,40,19,'ModernBusiness',40,5,6,22,23,'2026-05-15 15:28:28.148339',1,1,1,1,NULL,'2026-05-15 15:28:31.295332'),(169,73,40,19,'LegacySync',NULL,6,6,23,23,'2026-05-15 15:28:28.000000',1,1,1,1,NULL,'2026-05-15 15:29:17.464290'),(170,73,40,19,'LegacySync',NULL,6,6,23,23,'2026-05-15 15:28:28.000000',1,1,1,1,NULL,'2026-05-15 16:42:36.896847'),(171,73,40,19,'BrandingRefresh',40,6,6,23,23,'2026-05-15 15:28:28.000000',1,1,1,1,NULL,'2026-05-15 16:48:06.961305'),(172,65,40,20,'BrandingRefresh',40,3,3,13,13,'2026-05-13 18:54:02.000000',1,1,0,0,NULL,'2026-05-15 16:48:09.052315'),(173,73,40,19,'ModernBusiness',40,6,7,23,24,'2026-05-15 16:49:04.085592',1,1,1,1,NULL,'2026-05-15 16:49:07.164502'),(174,73,40,19,'LegacySync',NULL,7,7,24,24,'2026-05-15 16:49:04.000000',1,1,1,1,NULL,'2026-05-15 16:49:40.402264'),(175,73,40,19,'BrandingRefresh',40,7,7,24,24,'2026-05-15 16:49:04.000000',1,1,1,1,NULL,'2026-05-15 16:54:22.494561'),(176,65,40,20,'BrandingRefresh',40,3,3,13,13,'2026-05-13 18:54:02.000000',1,1,0,0,NULL,'2026-05-15 16:54:24.832189'),(177,73,40,19,'ModernBusiness',40,7,8,24,25,'2026-05-15 16:55:11.852142',1,1,1,1,NULL,'2026-05-15 16:55:15.562823'),(178,73,40,19,'LegacySync',NULL,8,8,25,25,'2026-05-15 16:55:12.000000',1,1,1,1,NULL,'2026-05-15 16:55:40.636087'),(179,73,40,19,'LegacySync',NULL,8,8,25,25,'2026-05-15 16:55:12.000000',1,1,1,1,NULL,'2026-05-15 17:12:39.561277'),(180,73,40,19,'ModernBusiness',40,8,9,25,26,'2026-05-15 17:15:00.470645',1,1,1,1,NULL,'2026-05-15 17:15:04.261497'),(181,73,40,19,'LegacySync',NULL,9,9,26,26,'2026-05-15 17:15:00.000000',1,1,1,1,NULL,'2026-05-15 17:15:42.476013'),(182,73,40,19,'ModernBusiness',40,9,10,26,27,'2026-05-15 17:18:34.468266',1,1,1,1,NULL,'2026-05-15 17:18:37.798077'),(183,73,40,19,'LegacySync',NULL,10,10,27,27,'2026-05-15 17:18:34.000000',1,1,1,1,NULL,'2026-05-15 17:18:43.645071'),(184,73,40,19,'LegacySync',NULL,0,0,27,27,'2026-05-15 17:19:02.000000',1,1,1,1,NULL,'2026-05-15 17:19:43.606304'),(185,73,40,19,'ModernBusiness',40,0,1,27,28,'2026-05-15 17:19:47.287882',1,1,1,1,NULL,'2026-05-15 17:19:50.532467'),(186,73,40,19,'LegacySync',NULL,1,1,28,28,'2026-05-15 17:19:47.000000',1,1,1,1,NULL,'2026-05-15 17:20:43.129433'),(187,73,40,19,'LegacySync',NULL,1,1,28,28,'2026-05-15 17:19:47.000000',1,1,1,1,NULL,'2026-05-15 18:03:10.367400'),(188,73,40,19,'ModernBusiness',40,1,2,28,29,'2026-05-15 18:08:22.208313',1,1,1,1,NULL,'2026-05-15 18:08:26.029719'),(189,73,40,19,'ModernBusiness',40,2,3,29,30,'2026-05-15 18:08:40.762068',1,1,1,1,NULL,'2026-05-15 18:08:43.627004'),(190,73,40,19,'ModernBusiness',40,3,4,30,31,'2026-05-15 18:08:59.195798',1,1,1,1,NULL,'2026-05-15 18:09:02.058809'),(191,73,40,19,'LegacySync',NULL,4,4,31,31,'2026-05-15 18:08:59.000000',1,1,1,1,NULL,'2026-05-15 18:09:14.288221'),(192,73,40,19,'ModernBusiness',40,4,5,31,32,'2026-05-15 18:09:13.487664',1,1,1,1,NULL,'2026-05-15 18:09:16.315780'),(193,73,40,19,'ModernBusiness',40,5,6,32,33,'2026-05-15 18:09:29.362665',1,1,1,1,NULL,'2026-05-15 18:09:32.034216'),(194,73,40,19,'ModernBusiness',40,6,7,33,34,'2026-05-15 18:09:40.461614',1,1,1,1,NULL,'2026-05-15 18:09:43.490646'),(195,73,40,19,'ModernBusiness',40,7,8,34,35,'2026-05-15 18:09:56.216062',1,1,1,1,NULL,'2026-05-15 18:09:58.756387'),(196,73,40,19,'ModernBusiness',40,8,9,35,36,'2026-05-15 18:10:08.240550',1,1,1,1,NULL,'2026-05-15 18:10:10.940772'),(197,73,40,19,'LegacySync',NULL,9,9,36,36,'2026-05-15 18:10:08.000000',1,1,1,1,NULL,'2026-05-15 18:10:13.539330'),(198,73,40,19,'ModernBusiness',40,9,10,36,37,'2026-05-15 18:10:25.102052',1,1,1,1,NULL,'2026-05-15 18:10:28.129792'),(199,73,40,19,'LegacySync',NULL,10,10,37,37,'2026-05-15 18:10:25.000000',1,1,1,1,NULL,'2026-05-15 18:11:13.252056'),(200,73,40,19,'LegacySync',NULL,10,10,37,37,'2026-05-15 18:10:25.000000',1,1,1,1,NULL,'2026-05-15 18:35:03.805274'),(201,73,40,19,'RewardRedeemed',40,10,0,37,37,'2026-05-15 18:39:12.515585',1,1,1,1,NULL,'2026-05-15 18:39:16.769676'),(202,73,40,19,'LegacySync',NULL,0,0,37,37,'2026-05-15 18:39:13.000000',1,1,1,1,NULL,'2026-05-15 18:40:06.783355'),(203,73,40,19,'ModernBusiness',40,0,1,37,38,'2026-05-15 18:41:32.805883',1,1,1,1,NULL,'2026-05-15 18:41:35.451941'),(204,73,40,19,'LegacySync',NULL,1,1,38,38,'2026-05-15 18:41:33.000000',1,1,1,1,NULL,'2026-05-15 18:42:07.248293'),(205,73,40,19,'LegacySync',NULL,1,1,38,38,'2026-05-15 18:41:33.000000',1,1,1,1,NULL,'2026-05-15 19:16:49.338892'),(206,73,40,19,'ModernBusiness',40,1,2,38,39,'2026-05-15 19:31:46.593670',1,1,1,1,NULL,'2026-05-15 19:31:50.178069'),(207,73,40,19,'LegacySync',NULL,2,2,39,39,'2026-05-15 19:31:47.000000',1,1,1,1,NULL,'2026-05-15 19:31:53.012546'),(208,73,40,19,'ModernBusiness',40,2,3,39,40,'2026-05-15 19:31:58.180073',1,1,1,1,NULL,'2026-05-15 19:32:01.756578'),(209,73,40,19,'ModernBusiness',40,3,4,40,41,'2026-05-15 19:32:15.550515',1,1,1,1,NULL,'2026-05-15 19:32:18.299778'),(210,73,40,19,'ModernBusiness',40,4,5,41,42,'2026-05-15 19:32:30.172495',1,1,1,1,NULL,'2026-05-15 19:32:33.007509'),(211,73,40,19,'ModernBusiness',40,5,6,42,43,'2026-05-15 19:32:46.745860',1,1,1,1,NULL,'2026-05-15 19:32:49.990360'),(212,73,40,19,'LegacySync',NULL,6,6,43,43,'2026-05-15 19:32:47.000000',1,1,1,1,NULL,'2026-05-15 19:32:52.860550'),(213,73,40,19,'ModernBusiness',40,6,7,43,44,'2026-05-15 19:33:14.363684',1,1,1,1,NULL,'2026-05-15 19:33:17.346943'),(214,73,40,19,'LegacySync',NULL,7,7,44,44,'2026-05-15 19:33:14.000000',1,1,1,1,NULL,'2026-05-15 19:33:52.201159'),(215,73,40,19,'ModernBusiness',40,7,8,44,45,'2026-05-15 19:34:25.912035',1,1,1,1,NULL,'2026-05-15 19:34:29.079555'),(216,73,40,19,'LegacySync',NULL,8,8,45,45,'2026-05-15 19:34:26.000000',1,1,1,1,NULL,'2026-05-15 19:34:52.257159'),(217,73,40,19,'ModernBusiness',40,8,9,45,46,'2026-05-15 19:36:52.422846',1,1,1,1,NULL,'2026-05-15 19:36:55.310845'),(218,73,40,19,'ModernBusiness',40,9,10,46,47,'2026-05-15 19:37:19.653605',1,1,1,1,NULL,'2026-05-15 19:37:22.386697'),(219,73,40,19,'RewardRedeemed',40,10,0,47,47,'2026-05-15 19:37:37.000000',1,1,1,1,NULL,'2026-05-15 19:37:40.301649'),(220,73,40,19,'LegacySync',NULL,0,0,47,47,'2026-05-15 19:37:37.000000',1,1,1,1,NULL,'2026-05-15 19:37:52.721068'),(221,73,40,19,'LegacySync',NULL,0,0,47,47,'2026-05-15 19:37:37.000000',1,1,1,1,NULL,'2026-05-15 19:52:47.726442'),(222,73,40,19,'LegacySync',NULL,0,0,47,47,'2026-05-15 19:37:37.000000',1,1,1,1,NULL,'2026-05-15 19:59:15.682391'),(223,73,40,19,'LegacySync',NULL,0,0,47,47,'2026-05-15 19:37:37.000000',1,1,1,1,NULL,'2026-05-15 20:04:15.685913'),(224,73,40,19,'BrandingRefresh',40,0,0,47,47,'2026-05-15 19:37:37.000000',1,1,1,1,NULL,'2026-05-15 20:05:40.592195'),(225,65,40,20,'BrandingRefresh',40,3,3,13,13,'2026-05-13 18:54:02.000000',1,1,0,0,NULL,'2026-05-15 20:05:43.530878'),(226,73,40,19,'LegacySync',NULL,0,0,47,47,'2026-05-15 19:37:37.000000',1,1,1,1,NULL,'2026-05-15 20:14:54.631117'),(227,73,40,19,'LegacySync',NULL,0,0,47,47,'2026-05-15 19:37:37.000000',1,1,1,1,NULL,'2026-05-15 20:21:38.802882'),(228,73,40,19,'LegacySync',NULL,0,0,47,47,'2026-05-15 19:37:37.000000',1,0,0,0,'GoogleApiException','2026-05-15 20:31:54.495243'),(229,73,40,19,'LegacySync',NULL,0,0,47,47,'2026-05-15 19:37:37.000000',1,0,0,0,'GoogleApiException','2026-05-15 20:32:55.977929'),(230,73,40,19,'LegacySync',NULL,0,0,47,47,'2026-05-15 19:37:37.000000',1,0,0,0,'GoogleApiException','2026-05-15 20:33:55.935141'),(231,73,40,19,'LegacySync',NULL,0,0,47,47,'2026-05-15 19:37:37.000000',1,0,0,0,'GoogleApiException','2026-05-15 20:34:55.862278'),(232,73,40,19,'LegacySync',NULL,0,0,47,47,'2026-05-15 19:37:37.000000',1,0,0,0,'GoogleApiException','2026-05-15 20:35:56.107650'),(233,73,40,19,'LegacySync',NULL,0,0,47,47,'2026-05-15 19:37:37.000000',1,0,0,0,'GoogleApiException','2026-05-15 20:43:13.288920'),(234,73,40,19,'LegacySync',NULL,0,0,47,47,'2026-05-15 19:37:37.000000',1,0,0,0,'GoogleApiException','2026-05-15 20:44:14.884416'),(235,73,40,19,'LegacySync',NULL,0,0,47,47,'2026-05-15 19:37:37.000000',1,0,0,0,'GoogleApiException','2026-05-15 20:45:14.698407'),(236,73,40,19,'LegacySync',NULL,0,0,47,47,'2026-05-15 19:37:37.000000',1,1,1,1,NULL,'2026-05-15 20:55:27.652232'),(237,73,40,19,'ModernBusiness',40,0,1,47,48,'2026-05-15 20:55:57.094856',1,1,1,1,NULL,'2026-05-15 20:55:59.900623'),(238,73,40,19,'LegacySync',NULL,1,1,48,48,'2026-05-15 20:55:57.000000',1,1,1,1,NULL,'2026-05-15 20:56:30.754334'),(239,73,40,19,'ModernBusiness',40,1,2,48,49,'2026-05-18 16:50:11.130081',1,1,1,1,NULL,'2026-05-18 16:50:19.589946'),(240,73,40,19,'ModernBusiness',40,2,3,49,50,'2026-05-18 16:50:38.848237',1,1,1,1,NULL,'2026-05-18 16:50:43.605576'),(241,73,40,19,'LegacySync',NULL,3,3,50,50,'2026-05-18 16:50:39.000000',1,1,1,1,NULL,'2026-05-18 16:50:51.813960'),(242,73,40,19,'ModernBusiness',40,3,4,50,51,'2026-05-18 16:52:19.981688',1,1,1,1,NULL,'2026-05-18 16:52:26.159546'),(243,73,40,19,'LegacySync',NULL,4,4,51,51,'2026-05-18 16:52:20.000000',1,1,1,1,NULL,'2026-05-18 16:52:55.244465'),(244,73,40,19,'LegacySync',NULL,4,4,51,51,'2026-05-18 16:52:20.000000',1,1,1,1,NULL,'2026-05-18 17:15:25.373643'),(245,73,40,19,'LegacySync',NULL,4,4,51,51,'2026-05-18 16:52:20.000000',1,1,1,1,NULL,'2026-05-18 17:18:47.848860'),(246,73,40,19,'ModernBusiness',40,4,5,51,52,'2026-05-18 17:21:04.428380',1,1,1,1,NULL,'2026-05-18 17:21:08.511847'),(247,73,40,19,'ModernBusiness',40,5,6,52,53,'2026-05-18 17:21:26.690843',1,1,1,1,NULL,'2026-05-18 17:21:32.042022'),(248,73,40,19,'LegacySync',NULL,6,6,53,53,'2026-05-18 17:21:27.000000',1,1,1,1,NULL,'2026-05-18 17:21:52.809571'),(249,73,40,19,'LegacySync',NULL,6,6,53,53,'2026-05-18 17:21:27.000000',1,1,1,1,NULL,'2026-05-18 18:37:53.283040'),(250,73,40,19,'LegacySync',NULL,6,6,53,53,'2026-05-18 17:21:27.000000',1,1,1,1,NULL,'2026-05-18 22:07:58.019900'),(251,73,40,19,'LegacySync',NULL,6,6,53,53,'2026-05-18 17:21:27.000000',1,1,1,1,NULL,'2026-05-18 22:16:08.791395'),(252,73,40,19,'LegacySync',NULL,6,6,53,53,'2026-05-18 17:21:27.000000',1,0,0,0,'InvalidOperationException','2026-05-19 04:00:34.852468'),(253,73,40,19,'LegacySync',NULL,6,6,53,53,'2026-05-18 17:21:27.000000',1,0,0,0,'InvalidOperationException','2026-05-19 04:01:35.755973'),(254,73,40,19,'LegacySync',NULL,6,6,53,53,'2026-05-18 17:21:27.000000',1,0,0,0,'InvalidOperationException','2026-05-19 04:01:55.340644'),(255,73,40,19,'LegacySync',NULL,6,6,53,53,'2026-05-18 17:21:27.000000',1,0,0,0,'InvalidOperationException','2026-05-19 04:02:56.238713'),(256,73,40,19,'LegacySync',NULL,6,6,53,53,'2026-05-18 17:21:27.000000',1,1,1,1,NULL,'2026-05-19 15:16:48.566409'),(257,73,40,19,'ModernBusiness',40,6,7,53,54,'2026-05-19 15:20:15.484487',1,1,1,1,NULL,'2026-05-19 15:20:19.787615'),(258,73,40,19,'LegacySync',NULL,7,7,54,54,'2026-05-19 15:20:15.000000',1,1,1,1,NULL,'2026-05-19 15:20:52.955847'),(259,73,40,19,'ModernBusiness',40,7,8,54,55,'2026-05-21 03:57:31.378635',1,0,0,0,'InvalidOperationException','2026-05-21 03:57:31.932885'),(260,73,40,19,'LegacySync',NULL,8,8,55,55,'2026-05-21 03:57:31.000000',1,0,0,0,'InvalidOperationException','2026-05-21 03:58:18.819953'),(261,73,40,19,'LegacySync',NULL,8,8,55,55,'2026-05-21 03:57:31.000000',1,0,0,0,'InvalidOperationException','2026-05-21 03:58:28.419416'),(262,73,40,19,'LegacySync',NULL,8,8,55,55,'2026-05-21 03:57:31.000000',1,0,0,0,'InvalidOperationException','2026-05-21 03:59:18.987708'),(263,73,40,19,'LegacySync',NULL,8,8,55,55,'2026-05-21 03:57:31.000000',1,0,0,0,'InvalidOperationException','2026-05-21 03:59:28.266048'),(264,73,40,19,'LegacySync',NULL,8,8,55,55,'2026-05-21 03:57:31.000000',1,0,0,0,'InvalidOperationException','2026-05-21 04:00:18.903517'),(265,73,40,19,'LegacySync',NULL,8,8,55,55,'2026-05-21 03:57:31.000000',1,0,0,0,'InvalidOperationException','2026-05-21 04:00:28.312758'),(266,73,40,19,'ModernBusiness',40,8,9,55,56,'2026-05-21 04:00:29.142184',1,0,0,0,'InvalidOperationException','2026-05-21 04:00:29.513723'),(267,73,40,19,'LegacySync',NULL,9,9,56,56,'2026-05-21 04:00:29.000000',1,0,0,0,'InvalidOperationException','2026-05-21 04:01:18.816556'),(268,73,40,19,'LegacySync',NULL,9,9,56,56,'2026-05-21 04:00:29.000000',1,0,0,0,'InvalidOperationException','2026-05-21 04:02:18.916119'),(269,73,40,19,'LegacySync',NULL,9,9,56,56,'2026-05-21 04:00:29.000000',1,0,0,0,'InvalidOperationException','2026-05-21 04:03:18.900400'),(270,73,40,19,'LegacySync',NULL,9,9,56,56,'2026-05-21 04:00:29.000000',1,1,1,1,NULL,'2026-05-21 04:03:35.419257'),(271,73,40,19,'ModernBusiness',40,9,10,56,57,'2026-05-21 04:03:40.632195',1,0,0,0,'InvalidOperationException','2026-05-21 04:03:41.019493'),(272,73,40,19,'LegacySync',NULL,10,10,57,57,'2026-05-21 04:03:41.000000',1,0,0,0,'InvalidOperationException','2026-05-21 04:04:18.836980'),(273,73,40,19,'LegacySync',NULL,10,10,57,57,'2026-05-21 04:03:41.000000',1,1,1,1,NULL,'2026-05-21 04:04:40.373528'),(274,73,40,19,'LegacySync',NULL,10,10,57,57,'2026-05-21 04:03:41.000000',1,0,0,0,'InvalidOperationException','2026-05-21 04:05:18.913366'),(275,73,40,19,'LegacySync',NULL,10,10,57,57,'2026-05-21 04:03:41.000000',1,0,0,0,'InvalidOperationException','2026-05-21 04:06:01.160141'),(276,73,40,19,'LegacySync',NULL,10,10,57,57,'2026-05-21 04:03:41.000000',1,0,0,0,'InvalidOperationException','2026-05-21 04:07:02.016260'),(277,73,40,19,'LegacySync',NULL,10,10,57,57,'2026-05-21 04:03:41.000000',1,0,0,0,'InvalidOperationException','2026-05-21 04:08:02.055104'),(278,73,40,19,'LegacySync',NULL,10,10,57,57,'2026-05-21 04:03:41.000000',1,0,0,0,'InvalidOperationException','2026-05-21 04:09:02.060409'),(279,73,40,19,'LegacySync',NULL,10,10,57,57,'2026-05-21 04:03:41.000000',1,0,0,0,'InvalidOperationException','2026-05-21 04:10:02.066845'),(280,73,40,19,'LegacySync',NULL,10,10,57,57,'2026-05-21 04:03:41.000000',1,0,0,0,'InvalidOperationException','2026-05-21 04:11:02.066852'),(281,73,40,19,'LegacySync',NULL,10,10,57,57,'2026-05-21 04:03:41.000000',1,0,0,0,'InvalidOperationException','2026-05-21 04:12:02.092993'),(282,73,40,19,'LegacySync',NULL,10,10,57,57,'2026-05-21 04:03:41.000000',1,0,0,0,'InvalidOperationException','2026-05-21 04:13:02.087128'),(283,73,40,19,'LegacySync',NULL,10,10,57,57,'2026-05-21 04:03:41.000000',1,0,0,0,'InvalidOperationException','2026-05-21 04:14:02.098168'),(284,73,40,19,'LegacySync',NULL,10,10,57,57,'2026-05-21 04:03:41.000000',1,0,0,0,'InvalidOperationException','2026-05-21 04:15:02.108626'),(285,73,40,19,'LegacySync',NULL,10,10,57,57,'2026-05-21 04:03:41.000000',1,0,0,0,'InvalidOperationException','2026-05-21 04:16:02.017991'),(286,73,40,19,'LegacySync',NULL,10,10,57,57,'2026-05-21 04:03:41.000000',1,0,0,0,'InvalidOperationException','2026-05-21 04:17:02.013056'),(287,73,40,19,'LegacySync',NULL,10,10,57,57,'2026-05-21 04:03:41.000000',1,0,0,0,'InvalidOperationException','2026-05-21 04:18:02.040312'),(288,73,40,19,'LegacySync',NULL,10,10,57,57,'2026-05-21 04:03:41.000000',1,0,0,0,'InvalidOperationException','2026-05-21 04:19:02.150391'),(289,73,40,19,'LegacySync',NULL,10,10,57,57,'2026-05-21 04:03:41.000000',1,0,0,0,'InvalidOperationException','2026-05-21 04:20:02.267260'),(290,73,40,19,'LegacySync',NULL,10,10,57,57,'2026-05-21 04:03:41.000000',1,0,0,0,'InvalidOperationException','2026-05-21 04:21:02.046400'),(291,73,40,19,'LegacySync',NULL,10,10,57,57,'2026-05-21 04:03:41.000000',1,0,0,0,'InvalidOperationException','2026-05-21 04:22:02.071433'),(292,73,40,19,'LegacySync',NULL,10,10,57,57,'2026-05-21 04:03:41.000000',1,0,0,0,'InvalidOperationException','2026-05-21 04:23:02.055257'),(293,73,40,19,'LegacySync',NULL,10,10,57,57,'2026-05-21 04:03:41.000000',1,0,0,0,'InvalidOperationException','2026-05-21 04:24:02.254399'),(294,73,40,19,'LegacySync',NULL,10,10,57,57,'2026-05-21 04:03:41.000000',1,0,0,0,'InvalidOperationException','2026-05-21 04:25:02.492380'),(295,73,40,19,'LegacySync',NULL,10,10,57,57,'2026-05-21 04:03:41.000000',1,0,0,0,'InvalidOperationException','2026-05-21 04:26:55.021279'),(296,73,40,19,'LegacySync',NULL,10,10,57,57,'2026-05-21 04:03:41.000000',1,0,0,0,'InvalidOperationException','2026-05-21 04:27:59.891365'),(297,73,40,19,'LegacySync',NULL,10,10,57,57,'2026-05-21 04:03:41.000000',1,0,0,0,'InvalidOperationException','2026-05-21 04:28:56.181548'),(298,73,40,19,'LegacySync',NULL,10,10,57,57,'2026-05-21 04:03:41.000000',1,0,0,0,'InvalidOperationException','2026-05-21 04:29:56.192957'),(299,73,40,19,'LegacySync',NULL,10,10,57,57,'2026-05-21 04:03:41.000000',1,0,0,0,'InvalidOperationException','2026-05-21 04:30:56.048238'),(300,73,40,19,'LegacySync',NULL,10,10,57,57,'2026-05-21 04:03:41.000000',1,0,0,0,'InvalidOperationException','2026-05-21 04:31:33.246534'),(301,73,40,19,'LegacySync',NULL,10,10,57,57,'2026-05-21 04:03:41.000000',1,0,0,0,'InvalidOperationException','2026-05-21 04:33:01.331448'),(302,73,40,19,'LegacySync',NULL,10,10,57,57,'2026-05-21 04:03:41.000000',1,0,0,0,'InvalidOperationException','2026-05-21 04:34:02.396137'),(303,73,40,19,'LegacySync',NULL,10,10,57,57,'2026-05-21 04:03:41.000000',1,0,0,0,'InvalidOperationException','2026-05-21 04:35:02.402958'),(304,73,40,19,'LegacySync',NULL,10,10,57,57,'2026-05-21 04:03:41.000000',1,0,0,0,'InvalidOperationException','2026-05-21 04:36:02.404570'),(305,73,40,19,'LegacySync',NULL,10,10,57,57,'2026-05-21 04:03:41.000000',1,0,0,0,'InvalidOperationException','2026-05-21 04:37:02.526189'),(306,73,40,19,'RewardRedeemed',40,10,0,57,57,'2026-05-23 04:54:15.000000',1,0,0,0,'GoogleApiException','2026-05-23 04:54:19.016215'),(307,73,40,19,'LegacySync',NULL,0,0,57,57,'2026-05-23 04:54:15.000000',1,0,0,0,'GoogleApiException','2026-05-23 04:54:26.084811'),(308,73,40,19,'LegacySync',NULL,0,0,57,57,'2026-05-23 04:54:15.000000',1,0,0,0,'GoogleApiException','2026-05-23 04:58:28.574293'),(309,73,40,19,'LegacySync',NULL,0,0,57,57,'2026-05-23 04:54:15.000000',1,0,0,0,'GoogleApiException','2026-05-23 04:59:30.765126'),(310,73,40,19,'LegacySync',NULL,0,0,57,57,'2026-05-23 04:54:15.000000',1,0,0,0,'GoogleApiException','2026-05-23 05:00:30.502571'),(311,73,40,19,'LegacySync',NULL,0,0,57,57,'2026-05-23 04:54:15.000000',1,0,0,0,'GoogleApiException','2026-05-23 05:01:30.578059'),(312,73,40,19,'LegacySync',NULL,0,0,57,57,'2026-05-23 04:54:15.000000',1,0,0,0,'GoogleApiException','2026-05-23 05:02:30.924902'),(313,73,40,19,'LegacySync',NULL,0,0,57,57,'2026-05-23 04:54:15.000000',1,0,0,0,'GoogleApiException','2026-05-23 05:03:31.505715'),(314,73,40,19,'LegacySync',NULL,0,0,57,57,'2026-05-23 04:54:15.000000',1,0,0,0,'GoogleApiException','2026-05-23 05:56:39.856689'),(315,73,40,19,'LegacySync',NULL,0,0,57,57,'2026-05-23 04:54:15.000000',1,0,0,0,'GoogleApiException','2026-05-23 05:57:41.968298'),(316,73,40,19,'LegacySync',NULL,0,0,57,57,'2026-05-23 04:54:15.000000',1,0,0,0,'GoogleApiException','2026-05-23 05:58:41.885838'),(317,73,40,19,'LegacySync',NULL,0,0,57,57,'2026-05-23 04:54:15.000000',1,0,0,0,'GoogleApiException','2026-05-23 06:01:39.152292'),(318,73,40,19,'LegacySync',NULL,0,0,57,57,'2026-05-23 04:54:15.000000',1,0,0,0,'GoogleApiException','2026-05-23 06:02:41.166527'),(319,73,40,19,'LegacySync',NULL,0,0,57,57,'2026-05-23 04:54:15.000000',1,0,0,0,'GoogleApiException','2026-05-23 06:03:41.582394'),(320,73,40,19,'LegacySync',NULL,0,0,57,57,'2026-05-23 04:54:15.000000',1,0,0,0,'GoogleApiException','2026-05-23 06:06:19.553700'),(321,73,40,19,'LegacySync',NULL,0,0,57,57,'2026-05-23 04:54:15.000000',1,0,0,0,'GoogleApiException','2026-05-23 06:07:22.065489'),(322,73,40,19,'LegacySync',NULL,0,0,57,57,'2026-05-23 04:54:15.000000',1,0,0,0,'GoogleApiException','2026-05-23 06:08:29.760341'),(323,73,40,19,'LegacySync',NULL,0,0,57,57,'2026-05-23 04:54:15.000000',1,0,0,0,'GoogleApiException','2026-05-23 06:09:31.985920'),(324,73,40,19,'LegacySync',NULL,0,0,57,57,'2026-05-23 04:54:15.000000',1,0,0,0,'GoogleApiException','2026-05-23 20:11:32.362971'),(325,73,40,19,'LegacySync',NULL,0,0,57,57,'2026-05-23 04:54:15.000000',1,0,0,0,'GoogleApiException','2026-05-23 20:12:35.078633'),(326,73,40,19,'LegacySync',NULL,0,0,57,57,'2026-05-23 04:54:15.000000',1,0,0,0,'GoogleApiException','2026-05-23 20:13:34.844155'),(327,73,40,19,'LegacySync',NULL,0,0,57,57,'2026-05-23 04:54:15.000000',1,0,0,0,'GoogleApiException','2026-05-23 20:14:34.538402'),(328,73,40,19,'LegacySync',NULL,0,0,57,57,'2026-05-23 04:54:15.000000',1,0,0,0,'GoogleApiException','2026-05-23 20:15:34.594014'),(329,73,40,19,'LegacySync',NULL,0,0,57,57,'2026-05-23 04:54:15.000000',1,0,0,0,'GoogleApiException','2026-05-23 20:16:34.530043'),(330,73,40,19,'LegacySync',NULL,0,0,57,57,'2026-05-23 04:54:15.000000',1,0,0,0,'GoogleApiException','2026-05-23 20:17:34.793913'),(331,73,40,19,'LegacySync',NULL,0,0,57,57,'2026-05-23 04:54:15.000000',1,0,0,0,'GoogleApiException','2026-05-23 20:18:34.808623'),(332,73,40,19,'LegacySync',NULL,0,0,57,57,'2026-05-23 04:54:15.000000',1,0,0,0,'GoogleApiException','2026-05-23 20:19:34.517347'),(333,73,40,19,'LegacySync',NULL,0,0,57,57,'2026-05-23 04:54:15.000000',1,0,0,0,'GoogleApiException','2026-05-23 20:20:34.787096'),(334,73,40,19,'LegacySync',NULL,0,0,57,57,'2026-05-23 04:54:15.000000',1,0,0,0,'GoogleApiException','2026-05-23 20:21:34.839835'),(335,73,40,19,'LegacySync',NULL,0,0,57,57,'2026-05-23 04:54:15.000000',1,0,0,0,'GoogleApiException','2026-05-23 20:22:34.787913'),(336,73,40,19,'LegacySync',NULL,0,0,57,57,'2026-05-23 04:54:15.000000',1,0,0,0,'GoogleApiException','2026-05-23 20:23:34.906764'),(337,73,40,19,'LegacySync',NULL,0,0,57,57,'2026-05-23 04:54:15.000000',1,0,0,0,'GoogleApiException','2026-05-23 20:24:34.802434'),(338,73,40,19,'LegacySync',NULL,0,0,57,57,'2026-05-23 04:54:15.000000',1,0,0,0,'GoogleApiException','2026-05-23 20:42:55.627780');
/*!40000 ALTER TABLE `StampLedger` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `UserClient`
--

DROP TABLE IF EXISTS `UserClient`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `UserClient` (
  `UserID` int(11) NOT NULL AUTO_INCREMENT,
  `UserName` varchar(15) COLLATE utf8_unicode_ci DEFAULT NULL,
  `UserPassword` varchar(25) COLLATE utf8_unicode_ci DEFAULT NULL,
  `FirstName` varchar(30) COLLATE utf8_unicode_ci DEFAULT NULL,
  `Lastname` varchar(30) COLLATE utf8_unicode_ci DEFAULT NULL,
  `UserEmail` varchar(30) COLLATE utf8_unicode_ci DEFAULT NULL,
  `RoleID` int(11) DEFAULT NULL,
  PRIMARY KEY (`UserID`),
  UNIQUE KEY `UserName` (`UserName`),
  UNIQUE KEY `UserEmail` (`UserEmail`),
  KEY `RoleID` (`RoleID`),
  CONSTRAINT `UserClient_ibfk_1` FOREIGN KEY (`RoleID`) REFERENCES `Role` (`RoleID`)
) ENGINE=InnoDB AUTO_INCREMENT=34 DEFAULT CHARSET=utf8 COLLATE=utf8_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `UserClient`
--

LOCK TABLES `UserClient` WRITE;
/*!40000 ALTER TABLE `UserClient` DISABLE KEYS */;
INSERT INTO `UserClient` (`UserID`, `UserName`, `UserPassword`, `FirstName`, `Lastname`, `UserEmail`, `RoleID`) VALUES (1,'DCAdmin','9f86d081884c7d659a2feaa0c','IngeLab','Admin','ingelabmx@gmail.com',1),(19,'ericguillen','9f86d081884c7d659a2feaa0c','Eric','Guillen','guillenmorenoe58@gmail.com',2),(20,'Javier.1','cbe34ab81efe4b9196590cef7','Javier','Gomez ','Xtej24@gmail.com',2),(33,'ericg','937e8d5fbb48bd4949536cd65','Eric','Guillen','athleticsonfire01@gmail.com',2);
/*!40000 ALTER TABLE `UserClient` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `WalletLinkToken`
--

DROP TABLE IF EXISTS `WalletLinkToken`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `WalletLinkToken` (
  `ID` int(11) NOT NULL AUTO_INCREMENT,
  `TokenHash` char(64) COLLATE utf8mb4_unicode_ci NOT NULL,
  `TokenSuffix` varchar(16) COLLATE utf8mb4_unicode_ci NOT NULL,
  `CardID` int(11) NOT NULL,
  `Purpose` varchar(50) COLLATE utf8mb4_unicode_ci NOT NULL,
  `CreatedAt` datetime(6) NOT NULL,
  `LastUsedAt` datetime(6) DEFAULT NULL,
  `RevokedAt` datetime(6) DEFAULT NULL,
  PRIMARY KEY (`ID`),
  UNIQUE KEY `UX_WalletLinkToken_TokenHash_Purpose` (`TokenHash`,`Purpose`),
  KEY `IX_WalletLinkToken_CardID_Purpose_RevokedAt` (`CardID`,`Purpose`,`RevokedAt`),
  KEY `IX_WalletLinkToken_TokenSuffix` (`TokenSuffix`)
) ENGINE=InnoDB AUTO_INCREMENT=99 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `WalletLinkToken`
--

LOCK TABLES `WalletLinkToken` WRITE;
/*!40000 ALTER TABLE `WalletLinkToken` DISABLE KEYS */;
INSERT INTO `WalletLinkToken` (`ID`, `TokenHash`, `TokenSuffix`, `CardID`, `Purpose`, `CreatedAt`, `LastUsedAt`, `RevokedAt`) VALUES (1,'08359ca18885e10d166891f45bf78123e811cbed5e2f249b484d3b876d75cc2b','OqCRo5L8',73,'WalletSelect','2026-05-12 20:22:30.389363','2026-05-12 22:13:41.568930',NULL),(2,'16354beb2f5ddce426b7e3b9faab276916595e11680b130d92e5dabeb096b9df','hhgcl51w',73,'WalletSelect','2026-05-12 22:13:19.479193','2026-05-12 22:13:47.538665',NULL),(3,'f67f48e36a5afd984e29a6d7606c2429b4170ed30b5f06de0970cf7b5279a786','WWp8gbUg',73,'WalletSelect','2026-05-13 13:49:07.872312','2026-05-14 14:13:06.365757',NULL),(5,'33364053ccfc87450f287543381087898959890834d221a27bdda5ef0cbeece4','849iG3ZQ',65,'WalletSelect','2026-05-13 18:55:06.770350','2026-05-13 18:56:40.904262',NULL),(6,'ca6515d9eda522e4ab7dffec8bd84ccca8e681a3fb3e3272c55a2a9bf5ac7739','t2O6Tn3E',65,'WalletSelect','2026-05-13 18:57:15.427937','2026-05-13 18:57:35.030404',NULL),(7,'922965c3728f51eb30bd29d8e8f011818f0d454c480397ddc193189563155525','ZILiLSdc',73,'WalletSelect','2026-05-13 21:35:52.142049',NULL,NULL),(9,'21172b8427c3298519cde67419e365ff42efc41c0e3aa39b68423733e1d01da6','T5j3L1_0',73,'WalletSelect','2026-05-13 21:36:31.042878','2026-05-13 21:36:59.553531',NULL),(11,'682a70ed113ef4666fc8fae47a86202974af54e962fa99843eef68ff508e6bc0','39yg2pGE',73,'WalletSelect','2026-05-13 21:37:44.461764',NULL,NULL),(13,'4e74a0e983951b6e542b042de3bbd08d23d5e59cc8e1238a52ead79f6e7ff34f','P37ox9Xo',73,'WalletSelect','2026-05-13 21:39:28.682937','2026-05-13 21:39:35.717192',NULL),(15,'136557f9e9a6d47b0ea0cc985459dd44259bcd19a86e4b839a9c65cceb6f8db6','mAlY1DKs',73,'WalletSelect','2026-05-14 14:11:25.921120',NULL,NULL),(17,'cf2f8bc932b66a66dcc8b363645013b52c5d47c3f0cd3f2b3d397a0f5ab6d21d','_etRWRiI',73,'WalletSelect','2026-05-14 14:12:41.931622','2026-05-14 16:23:31.653798',NULL),(18,'cb867810042813b7a5075dbcf1727a0820abde847a4cf0fa3481e4a922ede079','uFFyvBiY',73,'WalletSelect','2026-05-14 16:23:01.676780','2026-05-14 22:05:58.674896',NULL),(19,'3cf19ca3445f82a5f3f21ca5bdd5742fb9f6bd207da5493ad655f928b8ac0081','aWgB5w8w',73,'WalletSelect','2026-05-14 16:48:44.975791',NULL,NULL),(21,'3b499f760ef505749f34dc6b850e5980fc7944e280381dbd95d72e5d4eb4a5e7','mYs-jSz4',73,'WalletSelect','2026-05-14 16:49:23.526849',NULL,NULL),(23,'1f7e45644d46c55c13dbe32a0fe504a69a05c9fcc5e106b762c34006e5fc6e0f','YJUfhxug',73,'WalletSelect','2026-05-14 16:49:35.333293',NULL,NULL),(25,'d5a76bd1aad0325fb4f78e4503e832e511835ea9454962b94bc65e55e9565036','2J0Dhm18',73,'WalletSelect','2026-05-14 16:50:19.253479',NULL,NULL),(27,'72fbc90a9735cbbf8710b4a0ff5e6a0c6a2c72925ac25a491c27f70eddf8237d','ffvW6ZNY',73,'WalletSelect','2026-05-14 18:11:25.139009',NULL,NULL),(29,'07fc9b9f4f101ce78f09b640326f0e96d7cfde29b71054630dacf45e42a6637f','VMjmasDY',73,'WalletSelect','2026-05-14 18:12:27.516557',NULL,NULL),(31,'cc18bb3ec4403f8c7d1cbb20e60bd2dd0fe5adb11df82f957870ab45427bdb4e','jDKgSccc',73,'WalletSelect','2026-05-14 18:12:41.462472',NULL,NULL),(33,'8f56387fd7a45220f238b1ca8cf44772cc8467a5a06252df19ce981267a0460e','jgCAzoIo',73,'WalletSelect','2026-05-14 18:14:36.903672',NULL,NULL),(35,'bf83d8aa2c1c8c260cfb0d38a9eed743ed6fdc3708254cc51fcd47927975076a','PujOqYBY',73,'WalletSelect','2026-05-14 19:37:12.389338',NULL,NULL),(36,'95f954b9e5c0039d54f7839a9e513c2749825deaae1eac6e6a19818bd039c2bb','CN_pQTC4',73,'WalletSelect','2026-05-14 19:37:24.805404',NULL,NULL),(37,'59f1dd1310147b8accd34135d04fb5471664cdb412defc17fb10dcfaafd7b0b6','6uVomnjA',73,'WalletSelect','2026-05-14 19:37:40.957946',NULL,NULL),(38,'84215f27e2ccf3f33a05919d23ad0d84b7385f056e3e559b1b7ad31d28cef3cf','hMepg9mA',73,'WalletSelect','2026-05-14 19:54:31.254061',NULL,NULL),(39,'441ed23df5e587009b7bd3191f0429d39c51d5c34626b5a100aa91a786ad9e75','D-fbv_wo',73,'WalletSelect','2026-05-14 19:54:54.553399',NULL,NULL),(40,'5dd93863a8cb45b659105a000e1cafef008bb540c78615cd2084a95f9f585192','Ql_FakK0',73,'WalletSelect','2026-05-14 21:52:02.023636',NULL,NULL),(41,'ba8093788f83842691ef9a2f97beb72a0d49696bed49e268f11ed676b22fd7c6','SItfZdcQ',73,'WalletSelect','2026-05-14 22:05:42.607852','2026-05-14 22:06:12.715746',NULL),(42,'26054273f6a7107b16add4350460806a23ea62a7375324e744087577171ceb5b','njL6105E',73,'WalletSelect','2026-05-14 22:22:35.963658',NULL,NULL),(43,'9bd5fa02b1bc001e6309c3aadf957115ac908324e41f388397415c87b3d7c4a2','P4otqh4c',73,'WalletSelect','2026-05-14 22:24:59.314487',NULL,NULL),(44,'ffdd4e17094d66e8d0b08aa930121b2b71606e053fc3c676b8d8d31412f2e89b','xOnx5r8k',73,'WalletSelect','2026-05-14 22:44:11.483689','2026-05-15 14:55:23.807561',NULL),(45,'20ea53fd7e7b165c7448961c2611be00ceb797882e81608f524be83eb9081ac4','_VK3-Y4U',73,'WalletSelect','2026-05-14 22:56:06.284357',NULL,NULL),(46,'b639b5523de90a55a6493f42ab63e1d42e92ea31ba6db09a5966bcc1860cd88e','rCnYmcyM',73,'WalletSelect','2026-05-14 22:56:22.180640',NULL,NULL),(47,'df6faf49826c2e3ed57b938dd5f9ee2c69dd2db6f9b44dbd9128bb11fe6c9b14','2tRaCZ1g',73,'WalletSelect','2026-05-15 14:55:07.853683','2026-05-15 19:54:39.145051',NULL),(48,'8935bdba727156ffa230246fbe20b761cd147f984ef76c9b630be7e44392d781','p1vuVEAs',73,'WalletSelect','2026-05-15 15:25:03.587570',NULL,NULL),(49,'766d31941ef939ae9b61f2f86f508ca1879cc2bac24a3e309c1ceb16d8358e22','Ks2_482w',73,'WalletSelect','2026-05-15 15:25:13.004006',NULL,NULL),(50,'e64a113ef4c15c2af532fdb89486d9346031c6bbac59255b46a626635da7ac37','3QrwYsdU',73,'WalletSelect','2026-05-15 15:25:23.770710',NULL,NULL),(51,'688e8be97d1b0dd1a0675a0184b07069a4117f9ac298c5594fb8267083b710b7','afyDZ-Fs',73,'WalletSelect','2026-05-15 16:52:21.651310',NULL,NULL),(52,'b0b21873a06ee963460aa9a77ddea347ea6755ec29c4fd826dbd16ebc89a1c95','XA0rR9T0',73,'WalletSelect','2026-05-15 18:06:44.606496',NULL,NULL),(53,'2706b7a541de4566d94d0271ff753c976a557346b5a3653001e2aef4326d6da3','jJ_FBdxI',73,'WalletSelect','2026-05-15 18:06:54.740066',NULL,NULL),(54,'a178ffecd4c05737f1cd4a3b0dd48f1890e3fab0fcebdb4204bbe368ddf47ae1','cz7BrJas',73,'WalletSelect','2026-05-15 18:43:14.052293','2026-05-15 18:43:26.711360',NULL),(55,'9e39cf73f4ff818c038068785b2932b4e071fa2a369b4866c6b8f160dd4e66a7','UPISIB8Q',73,'WalletSelect','2026-05-15 18:45:05.066126',NULL,NULL),(56,'43fd05dcf1c851ff32888cfd88e1c10ed18bca7e98822dff3a6c144fea4eccc4','Tb0rdkDI',73,'WalletSelect','2026-05-15 19:16:46.548003','2026-05-15 19:16:50.319778',NULL),(57,'51f56fbcec398a1cefd91b0469535756fff41a84510b4fcada5edf257f29b54a','PR8GOPEQ',73,'WalletSelect','2026-05-15 19:29:15.709102',NULL,NULL),(58,'580628b3b0beb38fb046c461f8c5a0d39fb4b109427fdab9a0a8b0eaaf455ff3','gjjEJ3mA',73,'WalletSelect','2026-05-15 19:29:21.037421',NULL,NULL),(59,'65a3aab6b8c3ae911e5a8964d970de7ce5b1b29c059a5468b7a912ef7c8fd2b6','vM_I9Gtc',73,'WalletSelect','2026-05-15 19:52:55.723717','2026-05-15 20:15:45.914312',NULL),(60,'725cfa6945bc8bf8cf0b15eb4ac357be3ec6ae7b4ab6530f099a5a8c2b46c9d1','yEFcmDMo',73,'WalletSelect','2026-05-15 19:56:01.925096','2026-05-15 19:56:06.005578',NULL),(61,'291a53a0d1d9fde0cc321644be8d945e517ff8d1c8e4558d4473ad00aebcc65d','ioys1KNw',73,'WalletSelect','2026-05-15 20:15:00.372657','2026-05-15 20:15:16.490301',NULL),(62,'282b94191e7bc3bc8e635e64ae5eb8cb85b0f047c4416343df5e94790fcabe89','x1S7jmRo',73,'WalletSelect','2026-05-15 20:16:04.933007',NULL,NULL),(63,'f816045c0969dd7604aa701ff03ef58bd3cac3af794a4e6448e0aaea29191ce4','3kqFu_AI',73,'WalletSelect','2026-05-15 20:22:23.194928','2026-05-15 20:33:08.282454',NULL),(64,'1b2a6ae841a2304eaa9b90e27f5f99201dc19784ba7ea65e0c13aed0893b64c5','6eQO_7II',73,'WalletSelect','2026-05-15 20:32:14.534999',NULL,NULL),(65,'a7811d50129470aa4445bdbe69957b843808ab0c9e3814ba439520ddf784c24f','pk4JSjao',73,'WalletSelect','2026-05-15 20:45:01.150647','2026-05-15 20:45:34.628128',NULL),(66,'5f7b6265612ae90424ed419eb1720b41d2b703f503076f24f8613f81d5374a32','kw1El-Nw',73,'WalletSelect','2026-05-22 18:11:32.283812',NULL,NULL),(67,'78569271a7694bfbfd17dd039c371d69aaf33ad1bec4e8697b91103c606aa185','gdaw58PA',73,'WalletSelect','2026-05-22 18:38:07.477543',NULL,NULL),(68,'ee589ea643e860e487e4414b3d70458d54d92c778dad22be66dd915e76754f14','4hwbmotg',73,'WalletSelect','2026-05-22 18:38:21.927518',NULL,NULL),(69,'e878d18a732b4dfb15367309b364c93bdad908a85fc6c2503e66b5e4dd128645','drnP2FA4',73,'WalletSelect','2026-05-22 18:38:36.548297',NULL,NULL),(70,'abbdb732facea66a360dc4169fde61546ee307f8e13ccc7eb1b58e257be25506','SgOPllKs',73,'WalletSelect','2026-05-22 18:38:47.373381',NULL,NULL),(71,'c1b7211cb843fdcc1ce54ab935c38972956cc165f3c9916df60ff5d85ad62d57','DadAIm-c',73,'WalletSelect','2026-05-22 20:05:32.741612',NULL,NULL),(72,'ae827e57b80265255fe62bb42b6e9107e7b1fa40c8c70c1efc35b0978d90130a','IDAaXJAs',73,'WalletSelect','2026-05-22 20:05:41.842499',NULL,NULL),(73,'5c685db43dd523e31b142c742d87d3a66507e4e60c6a1e97f8a0825e8f4117f9','qzLl0v6E',73,'WalletSelect','2026-05-22 20:19:20.949412',NULL,NULL),(74,'85fe5e53012af74405dc8a5696d4cec8c3c5efb0cede05ad6f8ce47cb7c6dd53','z5Zq7Mro',73,'WalletSelect','2026-05-22 20:19:34.133819','2026-05-22 20:19:40.498921',NULL),(75,'2105591757f26ee153e77ce186ac7817c223464feaab1f942d01c22edd020c5a','ZuIo6jPg',73,'WalletSelect','2026-05-22 20:19:51.163126',NULL,NULL),(76,'ce2ca50951ae407d801aa85de36ec1884ecf0404390979334353f92fda6238ef','v4d3dR_c',73,'WalletSelect','2026-05-22 23:23:45.110251',NULL,NULL),(77,'bc16de46536527640fe556c2dd9ee2bd26737fd327d4b45d4b2e8041b080573a','3QaD5ZPM',73,'WalletSelect','2026-05-22 23:23:53.124757',NULL,NULL),(78,'1e607bdbf89fb8912e4d34a9578fa1614d2335e2189e11d475db3afe40fed0b4','8IhxgJms',73,'WalletSelect','2026-05-22 23:24:02.773139',NULL,NULL),(79,'a0369a067a3b84d5e30d17c4bf8d5d216036480290726bdd29748f7ad51c3503','6iFZijn0',73,'WalletSelect','2026-05-23 01:59:24.613566',NULL,NULL),(80,'20836fc3f6964b08909745f7acc286f91051bd6dca5bb4bc7006cba381afea28','jtWJ7Fmc',73,'WalletSelect','2026-05-23 01:59:35.342349',NULL,NULL),(81,'82d50734b6e3c12e8252a649c85e59b2bbaddd7d320b9df5b7828e5133edd5d9','ZZnBnKUM',73,'WalletSelect','2026-05-23 02:00:03.958197',NULL,NULL),(82,'bc08cbf3e7360098542f450ac06bc14264d3ec5c4dcdc25c89da296c09f79b9b','nr-mqI_c',73,'WalletSelect','2026-05-23 02:08:43.228740',NULL,NULL),(83,'fa14ea46403d7774ae3fde2fa028636d2d46ff13c0cf65ab5fde34086f74096f','kZ46NKt8',73,'WalletSelect','2026-05-23 02:09:54.149991',NULL,NULL),(84,'f938f72fe623001b6b6198599c7185a23a67b8cf7802580ad9d217e7d5d7e48f','TZlAqVLA',73,'WalletSelect','2026-05-23 02:10:42.755304',NULL,NULL),(85,'c4e986b9586a6ac3da15bb468848b31dad4cf4218756e20a3dfc06ea6228ac09','rUv6LCBE',73,'WalletSelect','2026-05-23 02:55:55.758826',NULL,NULL),(86,'ffd8b23c347825329c73faf7a1b882a4affe7711eea573f27057045bf4e86cd7','HQtc1STo',73,'WalletSelect','2026-05-23 02:56:02.717860',NULL,NULL),(87,'83948e48b224f9d7e1397fa616aca7559ed99173b6a7fd97e6fdafb11e748c0c','ObPn28xE',73,'WalletSelect','2026-05-23 03:01:19.854636',NULL,NULL),(88,'38cf8d967b592e6d0257004af9997aa9cbf7a7bad602ba92bcb7d32e1f842852','0XAkxvXc',73,'WalletSelect','2026-05-23 03:01:25.843492',NULL,NULL),(89,'4305b7537c3061fc31c01a9bfa56839c91e375bccdf5e47409c58e57063015a7','4ldmu93M',73,'WalletSelect','2026-05-23 04:49:23.131609','2026-05-23 04:49:32.348718',NULL),(90,'9bbb141c769e27500c69dd8c832c14ecca33871ae0a20b54f07c37a7bcc83aed','izXJijPI',73,'WalletSelect','2026-05-23 05:58:04.618742',NULL,NULL),(91,'1e45e2e920b255c317b3d62df822f6053e3d5c5e4d8de2f733ddfbb30dc1240a','87FqOWYw',73,'WalletSelect','2026-05-23 05:58:11.631824',NULL,NULL),(92,'df7e1b30529d8ee3ec44b742b241bc67d7b8ede460f5c27e3cf284b259126a36','QMsTKbVw',73,'WalletSelect','2026-05-23 05:58:20.083069',NULL,NULL),(93,'1ff394d2bbe74a984bd0df3f99ebe23c699da87e4982cb8f6fa956a9fc4edbe1','CwioKDmw',73,'WalletSelect','2026-05-23 06:01:38.815115',NULL,NULL),(94,'cbc5b644bbc61954917ea1b04c29e06a231379ce22ef29cf663f6292e84ea2ad','vRMVxctQ',73,'WalletSelect','2026-05-23 06:01:45.151644',NULL,NULL),(95,'5fd387e06595f29c732570b2a19ef68ddd0a62967448ad4a30a99b0b54b2edc3','5S43Wn7k',73,'WalletSelect','2026-05-23 06:02:02.647244',NULL,NULL),(96,'12aaa1a99176a6fb0c69b201d547c0d8a4f18b1875bdca8f6d81793a8ad869a3','d8LCz_cg',73,'WalletSelect','2026-05-23 06:07:09.295683',NULL,NULL),(97,'48cdadfe39db8b378242f09fadc3b9f5bcece4406f5cf44a7666d4a3abd59d7e','X9AOtZHo',73,'WalletSelect','2026-05-23 06:08:38.856557',NULL,NULL),(98,'f041b59c55cc3a0e91993175370a12b2f671b9d5ccf96a51ff607cafbd573181','DkF9MkKA',75,'WalletSelect','2026-05-23 20:12:38.237939','2026-05-23 20:12:58.062978',NULL);
/*!40000 ALTER TABLE `WalletLinkToken` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Dumping events for database 'alltrac1_dcards'
--

--
-- Dumping routines for database 'alltrac1_dcards'
--
/*!50003 DROP PROCEDURE IF EXISTS `GetBusinessDetailsByUserID` */;
/*!50003 SET @saved_cs_client      = @@character_set_client */ ;
/*!50003 SET @saved_cs_results     = @@character_set_results */ ;
/*!50003 SET @saved_col_connection = @@collation_connection */ ;
/*!50003 SET character_set_client  = utf8mb4 */ ;
/*!50003 SET character_set_results = utf8mb4 */ ;
/*!50003 SET collation_connection  = utf8mb4_general_ci */ ;
/*!50003 SET @saved_sql_mode       = @@sql_mode */ ;
/*!50003 SET sql_mode              = '' */ ;
DELIMITER ;;
CREATE DEFINER=`root`@`localhost` PROCEDURE `GetBusinessDetailsByUserID`(IN `p_UserID` INT)
SELECT 
        b.BusinessName,   
        cc.CreationDate,  
        cc.CheckQTY       
    FROM 
        ClientCard cc
    INNER JOIN 
        Business b
    ON 
        cc.BusinessID = b.BusinessID  
    WHERE 
        cc.UserID = p_UserID ;;
DELIMITER ;
/*!50003 SET sql_mode              = @saved_sql_mode */ ;
/*!50003 SET character_set_client  = @saved_cs_client */ ;
/*!50003 SET character_set_results = @saved_cs_results */ ;
/*!50003 SET collation_connection  = @saved_col_connection */ ;
/*!50003 DROP PROCEDURE IF EXISTS `spCheckIfEmailExist` */;
/*!50003 SET @saved_cs_client      = @@character_set_client */ ;
/*!50003 SET @saved_cs_results     = @@character_set_results */ ;
/*!50003 SET @saved_col_connection = @@collation_connection */ ;
/*!50003 SET character_set_client  = utf8mb4 */ ;
/*!50003 SET character_set_results = utf8mb4 */ ;
/*!50003 SET collation_connection  = utf8mb4_general_ci */ ;
/*!50003 SET @saved_sql_mode       = @@sql_mode */ ;
/*!50003 SET sql_mode              = '' */ ;
DELIMITER ;;
CREATE DEFINER=`root`@`localhost` PROCEDURE `spCheckIfEmailExist`(IN `User_Email` INT)
SELECT COUNT(*) 
FROM UserClient 
WHERE UserEmail = User_Email
AND RoleID = 2 ;;
DELIMITER ;
/*!50003 SET sql_mode              = @saved_sql_mode */ ;
/*!50003 SET character_set_client  = @saved_cs_client */ ;
/*!50003 SET character_set_results = @saved_cs_results */ ;
/*!50003 SET collation_connection  = @saved_col_connection */ ;
/*!50003 DROP PROCEDURE IF EXISTS `spDeleteBusinessData` */;
/*!50003 SET @saved_cs_client      = @@character_set_client */ ;
/*!50003 SET @saved_cs_results     = @@character_set_results */ ;
/*!50003 SET @saved_col_connection = @@collation_connection */ ;
/*!50003 SET character_set_client  = utf8mb4 */ ;
/*!50003 SET character_set_results = utf8mb4 */ ;
/*!50003 SET collation_connection  = utf8mb4_general_ci */ ;
/*!50003 SET @saved_sql_mode       = @@sql_mode */ ;
/*!50003 SET sql_mode              = '' */ ;
DELIMITER ;;
CREATE DEFINER=`root`@`localhost` PROCEDURE `spDeleteBusinessData`(IN `Business_ID` INT, IN `Business_Name` VARCHAR(30), IN `Business_Email` VARCHAR(30))
DELETE FROM Business
WHERE BusinessID = Business_ID
AND BusinessName = Business_Name
AND BusinessEmail = Business_Email ;;
DELIMITER ;
/*!50003 SET sql_mode              = @saved_sql_mode */ ;
/*!50003 SET character_set_client  = @saved_cs_client */ ;
/*!50003 SET character_set_results = @saved_cs_results */ ;
/*!50003 SET collation_connection  = @saved_col_connection */ ;
/*!50003 DROP PROCEDURE IF EXISTS `spDeletePasswordResetToken` */;
/*!50003 SET @saved_cs_client      = @@character_set_client */ ;
/*!50003 SET @saved_cs_results     = @@character_set_results */ ;
/*!50003 SET @saved_col_connection = @@collation_connection */ ;
/*!50003 SET character_set_client  = utf8mb4 */ ;
/*!50003 SET character_set_results = utf8mb4 */ ;
/*!50003 SET collation_connection  = utf8mb4_general_ci */ ;
/*!50003 SET @saved_sql_mode       = @@sql_mode */ ;
/*!50003 SET sql_mode              = '' */ ;
DELIMITER ;;
CREATE DEFINER=`root`@`localhost` PROCEDURE `spDeletePasswordResetToken`(IN `User_Email` VARCHAR(30))
DELETE FROM PasswordResetToken 
WHERE UserEmail = User_Email ;;
DELIMITER ;
/*!50003 SET sql_mode              = @saved_sql_mode */ ;
/*!50003 SET character_set_client  = @saved_cs_client */ ;
/*!50003 SET character_set_results = @saved_cs_results */ ;
/*!50003 SET collation_connection  = @saved_col_connection */ ;
/*!50003 DROP PROCEDURE IF EXISTS `spGetBusinessData` */;
/*!50003 SET @saved_cs_client      = @@character_set_client */ ;
/*!50003 SET @saved_cs_results     = @@character_set_results */ ;
/*!50003 SET @saved_col_connection = @@collation_connection */ ;
/*!50003 SET character_set_client  = utf8mb4 */ ;
/*!50003 SET character_set_results = utf8mb4 */ ;
/*!50003 SET collation_connection  = utf8mb4_general_ci */ ;
/*!50003 SET @saved_sql_mode       = @@sql_mode */ ;
/*!50003 SET sql_mode              = '' */ ;
DELIMITER ;;
CREATE DEFINER=`root`@`localhost` PROCEDURE `spGetBusinessData`()
SELECT BusinessID, BusinessName AS 'Nombre del negocio', BusinessEmail AS 'Email del negocio', BusinessLogo AS 'Logo del negocio'
FROM Business ;;
DELIMITER ;
/*!50003 SET sql_mode              = @saved_sql_mode */ ;
/*!50003 SET character_set_client  = @saved_cs_client */ ;
/*!50003 SET character_set_results = @saved_cs_results */ ;
/*!50003 SET collation_connection  = @saved_col_connection */ ;
/*!50003 DROP PROCEDURE IF EXISTS `spGetBusinessDetails` */;
/*!50003 SET @saved_cs_client      = @@character_set_client */ ;
/*!50003 SET @saved_cs_results     = @@character_set_results */ ;
/*!50003 SET @saved_col_connection = @@collation_connection */ ;
/*!50003 SET character_set_client  = utf8mb4 */ ;
/*!50003 SET character_set_results = utf8mb4 */ ;
/*!50003 SET collation_connection  = utf8mb4_general_ci */ ;
/*!50003 SET @saved_sql_mode       = @@sql_mode */ ;
/*!50003 SET sql_mode              = '' */ ;
DELIMITER ;;
CREATE DEFINER=`root`@`localhost` PROCEDURE `spGetBusinessDetails`(IN `Business_ID` INT)
SELECT * FROM Business
WHERE BusinessID = Business_ID ;;
DELIMITER ;
/*!50003 SET sql_mode              = @saved_sql_mode */ ;
/*!50003 SET character_set_client  = @saved_cs_client */ ;
/*!50003 SET character_set_results = @saved_cs_results */ ;
/*!50003 SET collation_connection  = @saved_col_connection */ ;
/*!50003 DROP PROCEDURE IF EXISTS `spGetCardCreatedTime` */;
/*!50003 SET @saved_cs_client      = @@character_set_client */ ;
/*!50003 SET @saved_cs_results     = @@character_set_results */ ;
/*!50003 SET @saved_col_connection = @@collation_connection */ ;
/*!50003 SET character_set_client  = utf8mb4 */ ;
/*!50003 SET character_set_results = utf8mb4 */ ;
/*!50003 SET collation_connection  = utf8mb4_general_ci */ ;
/*!50003 SET @saved_sql_mode       = @@sql_mode */ ;
/*!50003 SET sql_mode              = '' */ ;
DELIMITER ;;
CREATE DEFINER=`root`@`localhost` PROCEDURE `spGetCardCreatedTime`(IN `User_ID` INT, IN `Business_ID` INT)
SELECT CreationDate
FROM ClientCard
WHERE UserID = User_ID and BusinessID = Business_ID ;;
DELIMITER ;
/*!50003 SET sql_mode              = @saved_sql_mode */ ;
/*!50003 SET character_set_client  = @saved_cs_client */ ;
/*!50003 SET character_set_results = @saved_cs_results */ ;
/*!50003 SET collation_connection  = @saved_col_connection */ ;
/*!50003 DROP PROCEDURE IF EXISTS `spGetCardDataBusiness` */;
/*!50003 SET @saved_cs_client      = @@character_set_client */ ;
/*!50003 SET @saved_cs_results     = @@character_set_results */ ;
/*!50003 SET @saved_col_connection = @@collation_connection */ ;
/*!50003 SET character_set_client  = utf8mb4 */ ;
/*!50003 SET character_set_results = utf8mb4 */ ;
/*!50003 SET collation_connection  = utf8mb4_general_ci */ ;
/*!50003 SET @saved_sql_mode       = @@sql_mode */ ;
/*!50003 SET sql_mode              = '' */ ;
DELIMITER ;;
CREATE DEFINER=`root`@`localhost` PROCEDURE `spGetCardDataBusiness`(IN `Business_ID` INT)
SELECT 
    uc.UserName AS 'Nombre de usuario',
    uc.FirstName As 'Nombre(s)',
    uc.LastName AS 'Apellido(s)',
    cc.CreationDate AS 'Fecha de creación',
    cc.CheckQTY AS 'Checadas actuales',
    cc.HistoricCheckQTY AS 'Checadas totales',
    cc.LastCheck AS 'Última checada'
FROM 
    ClientCard cc
JOIN 
    UserClient uc ON cc.UserID = uc.UserID
JOIN 
    Business b ON cc.BusinessID = b.BusinessID
WHERE 
    cc.BusinessID = Business_ID ;;
DELIMITER ;
/*!50003 SET sql_mode              = @saved_sql_mode */ ;
/*!50003 SET character_set_client  = @saved_cs_client */ ;
/*!50003 SET character_set_results = @saved_cs_results */ ;
/*!50003 SET collation_connection  = @saved_col_connection */ ;
/*!50003 DROP PROCEDURE IF EXISTS `spGetCardDataChecks` */;
/*!50003 SET @saved_cs_client      = @@character_set_client */ ;
/*!50003 SET @saved_cs_results     = @@character_set_results */ ;
/*!50003 SET @saved_col_connection = @@collation_connection */ ;
/*!50003 SET character_set_client  = utf8mb4 */ ;
/*!50003 SET character_set_results = utf8mb4 */ ;
/*!50003 SET collation_connection  = utf8mb4_general_ci */ ;
/*!50003 SET @saved_sql_mode       = @@sql_mode */ ;
/*!50003 SET sql_mode              = '' */ ;
DELIMITER ;;
CREATE DEFINER=`root`@`localhost` PROCEDURE `spGetCardDataChecks`(IN `User_ID` INT, IN `Business_ID` INT)
SELECT CheckQTY, HistoricCheckQTY, CardIDGoogle
FROM ClientCard
WHERE UserID = User_ID AND BusinessID = Business_ID ;;
DELIMITER ;
/*!50003 SET sql_mode              = @saved_sql_mode */ ;
/*!50003 SET character_set_client  = @saved_cs_client */ ;
/*!50003 SET character_set_results = @saved_cs_results */ ;
/*!50003 SET collation_connection  = @saved_col_connection */ ;
/*!50003 DROP PROCEDURE IF EXISTS `spGetCardDataClient` */;
/*!50003 SET @saved_cs_client      = @@character_set_client */ ;
/*!50003 SET @saved_cs_results     = @@character_set_results */ ;
/*!50003 SET @saved_col_connection = @@collation_connection */ ;
/*!50003 SET character_set_client  = utf8mb4 */ ;
/*!50003 SET character_set_results = utf8mb4 */ ;
/*!50003 SET collation_connection  = utf8mb4_general_ci */ ;
/*!50003 SET @saved_sql_mode       = @@sql_mode */ ;
/*!50003 SET sql_mode              = '' */ ;
DELIMITER ;;
CREATE DEFINER=`root`@`localhost` PROCEDURE `spGetCardDataClient`(IN `User_ID` INT)
SELECT * FROM ClientCard
WHERE UserID = User_ID ;;
DELIMITER ;
/*!50003 SET sql_mode              = @saved_sql_mode */ ;
/*!50003 SET character_set_client  = @saved_cs_client */ ;
/*!50003 SET character_set_results = @saved_cs_results */ ;
/*!50003 SET collation_connection  = @saved_col_connection */ ;
/*!50003 DROP PROCEDURE IF EXISTS `spGetClientCards` */;
/*!50003 SET @saved_cs_client      = @@character_set_client */ ;
/*!50003 SET @saved_cs_results     = @@character_set_results */ ;
/*!50003 SET @saved_col_connection = @@collation_connection */ ;
/*!50003 SET character_set_client  = utf8mb4 */ ;
/*!50003 SET character_set_results = utf8mb4 */ ;
/*!50003 SET collation_connection  = utf8mb4_general_ci */ ;
/*!50003 SET @saved_sql_mode       = @@sql_mode */ ;
/*!50003 SET sql_mode              = '' */ ;
DELIMITER ;;
CREATE DEFINER=`root`@`localhost` PROCEDURE `spGetClientCards`(IN `UserID` INT)
SELECT 
        Business.BusinessName, 
        ClientCard.CreationDate, 
        ClientCard.CheckQTY
    FROM 
        ClientCard
    INNER JOIN 
        Business ON ClientCard.BusinessID = Business.BusinessID
    WHERE 
        ClientCard.UserID = UserID ;;
DELIMITER ;
/*!50003 SET sql_mode              = @saved_sql_mode */ ;
/*!50003 SET character_set_client  = @saved_cs_client */ ;
/*!50003 SET character_set_results = @saved_cs_results */ ;
/*!50003 SET collation_connection  = @saved_col_connection */ ;
/*!50003 DROP PROCEDURE IF EXISTS `spGetEmailByToken` */;
/*!50003 SET @saved_cs_client      = @@character_set_client */ ;
/*!50003 SET @saved_cs_results     = @@character_set_results */ ;
/*!50003 SET @saved_col_connection = @@collation_connection */ ;
/*!50003 SET character_set_client  = utf8mb4 */ ;
/*!50003 SET character_set_results = utf8mb4 */ ;
/*!50003 SET collation_connection  = utf8mb4_general_ci */ ;
/*!50003 SET @saved_sql_mode       = @@sql_mode */ ;
/*!50003 SET sql_mode              = '' */ ;
DELIMITER ;;
CREATE DEFINER=`root`@`localhost` PROCEDURE `spGetEmailByToken`(IN `Token_Value` VARCHAR(255))
SELECT UserEmail 
    FROM PasswordResetToken 
    WHERE Token = Token_Value AND Expiration > NOW() ;;
DELIMITER ;
/*!50003 SET sql_mode              = @saved_sql_mode */ ;
/*!50003 SET character_set_client  = @saved_cs_client */ ;
/*!50003 SET character_set_results = @saved_cs_results */ ;
/*!50003 SET collation_connection  = @saved_col_connection */ ;
/*!50003 DROP PROCEDURE IF EXISTS `spGetLast5Checks` */;
/*!50003 SET @saved_cs_client      = @@character_set_client */ ;
/*!50003 SET @saved_cs_results     = @@character_set_results */ ;
/*!50003 SET @saved_col_connection = @@collation_connection */ ;
/*!50003 SET character_set_client  = utf8mb4 */ ;
/*!50003 SET character_set_results = utf8mb4 */ ;
/*!50003 SET collation_connection  = utf8mb4_general_ci */ ;
/*!50003 SET @saved_sql_mode       = @@sql_mode */ ;
/*!50003 SET sql_mode              = '' */ ;
DELIMITER ;;
CREATE DEFINER=`root`@`localhost` PROCEDURE `spGetLast5Checks`(IN `Business_ID` INT)
SELECT
    uc.UserName AS 'Nombre de usuario',
    uc.FirstName AS 'Nombre(s)',
    uc.LastName AS 'Apellido(s)',
    cc.LastCheck AS 'Última checada',
    cc.CheckQTY AS 'Checadas actuales',
    cc.HistoricCheckQTY AS 'Checadas totales'
FROM 
    ClientCard cc
JOIN 
    UserClient uc ON cc.UserID = uc.UserID
JOIN 
    Business b ON cc.BusinessID = b.BusinessID
WHERE 
    cc.BusinessID = Business_ID
ORDER BY 
    cc.LastCheck DESC
LIMIT 5 ;;
DELIMITER ;
/*!50003 SET sql_mode              = @saved_sql_mode */ ;
/*!50003 SET character_set_client  = @saved_cs_client */ ;
/*!50003 SET character_set_results = @saved_cs_results */ ;
/*!50003 SET collation_connection  = @saved_col_connection */ ;
/*!50003 DROP PROCEDURE IF EXISTS `spGetUserClientID` */;
/*!50003 SET @saved_cs_client      = @@character_set_client */ ;
/*!50003 SET @saved_cs_results     = @@character_set_results */ ;
/*!50003 SET @saved_col_connection = @@collation_connection */ ;
/*!50003 SET character_set_client  = utf8mb4 */ ;
/*!50003 SET character_set_results = utf8mb4 */ ;
/*!50003 SET collation_connection  = utf8mb4_general_ci */ ;
/*!50003 SET @saved_sql_mode       = @@sql_mode */ ;
/*!50003 SET sql_mode              = '' */ ;
DELIMITER ;;
CREATE DEFINER=`root`@`localhost` PROCEDURE `spGetUserClientID`(IN `User_Name` VARCHAR(50))
SELECT UserID
FROM UserClient
WHERE UserName = User_Name OR UserEmail = User_Name ;;
DELIMITER ;
/*!50003 SET sql_mode              = @saved_sql_mode */ ;
/*!50003 SET character_set_client  = @saved_cs_client */ ;
/*!50003 SET character_set_results = @saved_cs_results */ ;
/*!50003 SET collation_connection  = @saved_col_connection */ ;
/*!50003 DROP PROCEDURE IF EXISTS `spGetUserClientInfo` */;
/*!50003 SET @saved_cs_client      = @@character_set_client */ ;
/*!50003 SET @saved_cs_results     = @@character_set_results */ ;
/*!50003 SET @saved_col_connection = @@collation_connection */ ;
/*!50003 SET character_set_client  = utf8mb4 */ ;
/*!50003 SET character_set_results = utf8mb4 */ ;
/*!50003 SET collation_connection  = utf8mb4_general_ci */ ;
/*!50003 SET @saved_sql_mode       = @@sql_mode */ ;
/*!50003 SET sql_mode              = '' */ ;
DELIMITER ;;
CREATE DEFINER=`root`@`localhost` PROCEDURE `spGetUserClientInfo`(IN `User_Name` VARCHAR(50))
SELECT UserID, FirstName, LastName, UserEmail
FROM UserClient
WHERE UserName = User_Name OR UserEmail = User_Name ;;
DELIMITER ;
/*!50003 SET sql_mode              = @saved_sql_mode */ ;
/*!50003 SET character_set_client  = @saved_cs_client */ ;
/*!50003 SET character_set_results = @saved_cs_results */ ;
/*!50003 SET collation_connection  = @saved_col_connection */ ;
/*!50003 DROP PROCEDURE IF EXISTS `spGetUserNamesAutocomplete` */;
/*!50003 SET @saved_cs_client      = @@character_set_client */ ;
/*!50003 SET @saved_cs_results     = @@character_set_results */ ;
/*!50003 SET @saved_col_connection = @@collation_connection */ ;
/*!50003 SET character_set_client  = utf8mb4 */ ;
/*!50003 SET character_set_results = utf8mb4 */ ;
/*!50003 SET collation_connection  = utf8mb4_general_ci */ ;
/*!50003 SET @saved_sql_mode       = @@sql_mode */ ;
/*!50003 SET sql_mode              = '' */ ;
DELIMITER ;;
CREATE DEFINER=`root`@`localhost` PROCEDURE `spGetUserNamesAutocomplete`(IN `User_Name` VARCHAR(15))
SELECT UserName AS 'UserName'
FROM UserClient
WHERE UserName LIKE CONCAT(User_Name, '%')
ORDER BY UserName ;;
DELIMITER ;
/*!50003 SET sql_mode              = @saved_sql_mode */ ;
/*!50003 SET character_set_client  = @saved_cs_client */ ;
/*!50003 SET character_set_results = @saved_cs_results */ ;
/*!50003 SET collation_connection  = @saved_col_connection */ ;
/*!50003 DROP PROCEDURE IF EXISTS `spGetYearData` */;
/*!50003 SET @saved_cs_client      = @@character_set_client */ ;
/*!50003 SET @saved_cs_results     = @@character_set_results */ ;
/*!50003 SET @saved_col_connection = @@collation_connection */ ;
/*!50003 SET character_set_client  = utf8mb4 */ ;
/*!50003 SET character_set_results = utf8mb4 */ ;
/*!50003 SET collation_connection  = utf8mb4_general_ci */ ;
/*!50003 SET @saved_sql_mode       = @@sql_mode */ ;
/*!50003 SET sql_mode              = '' */ ;
DELIMITER ;;
CREATE DEFINER=`root`@`localhost` PROCEDURE `spGetYearData`(IN `Business_ID` INT, IN `Current_Year` INT)
SELECT 
        MONTH(CreationDate) AS Month, 
        COUNT(CardID) AS CardCount
    FROM 
        ClientCard
    WHERE 
        YEAR(CreationDate) = Current_Year
        AND BusinessID = Business_ID
    GROUP BY 
        MONTH(CreationDate)
    ORDER BY 
        Month ;;
DELIMITER ;
/*!50003 SET sql_mode              = @saved_sql_mode */ ;
/*!50003 SET character_set_client  = @saved_cs_client */ ;
/*!50003 SET character_set_results = @saved_cs_results */ ;
/*!50003 SET collation_connection  = @saved_col_connection */ ;
/*!50003 DROP PROCEDURE IF EXISTS `spIncreaseCheckQTY` */;
/*!50003 SET @saved_cs_client      = @@character_set_client */ ;
/*!50003 SET @saved_cs_results     = @@character_set_results */ ;
/*!50003 SET @saved_col_connection = @@collation_connection */ ;
/*!50003 SET character_set_client  = utf8mb4 */ ;
/*!50003 SET character_set_results = utf8mb4 */ ;
/*!50003 SET collation_connection  = utf8mb4_general_ci */ ;
/*!50003 SET @saved_sql_mode       = @@sql_mode */ ;
/*!50003 SET sql_mode              = '' */ ;
DELIMITER ;;
CREATE DEFINER=`root`@`localhost` PROCEDURE `spIncreaseCheckQTY`(IN `User_ID` INT, IN `Business_ID` INT)
UPDATE ClientCard
SET 
    CheckQTY = CASE
                 WHEN CheckQTY > 8 THEN 0
                 ELSE CheckQTY + 1
               END,
    LastCheck = NOW(),
    HistoricCheckQTY = HistoricCheckQTY + 1
WHERE 
    UserID = User_ID 
    AND BusinessID = Business_ID ;;
DELIMITER ;
/*!50003 SET sql_mode              = @saved_sql_mode */ ;
/*!50003 SET character_set_client  = @saved_cs_client */ ;
/*!50003 SET character_set_results = @saved_cs_results */ ;
/*!50003 SET collation_connection  = @saved_col_connection */ ;
/*!50003 DROP PROCEDURE IF EXISTS `spInsertBusinessData` */;
/*!50003 SET @saved_cs_client      = @@character_set_client */ ;
/*!50003 SET @saved_cs_results     = @@character_set_results */ ;
/*!50003 SET @saved_col_connection = @@collation_connection */ ;
/*!50003 SET character_set_client  = utf8mb4 */ ;
/*!50003 SET character_set_results = utf8mb4 */ ;
/*!50003 SET collation_connection  = utf8mb4_general_ci */ ;
/*!50003 SET @saved_sql_mode       = @@sql_mode */ ;
/*!50003 SET sql_mode              = '' */ ;
DELIMITER ;;
CREATE DEFINER=`root`@`localhost` PROCEDURE `spInsertBusinessData`(IN `Business_Name` VARCHAR(30), IN `Business_Password` VARCHAR(25), IN `Business_Email` VARCHAR(30), IN `Business_Logo` VARCHAR(100))
INSERT INTO Business (BusinessName, BusinessPassword, BusinessEmail, BusinessLogo)
        VALUES (Business_Name, Business_Password, Business_Email, Business_Logo) ;;
DELIMITER ;
/*!50003 SET sql_mode              = @saved_sql_mode */ ;
/*!50003 SET character_set_client  = @saved_cs_client */ ;
/*!50003 SET character_set_results = @saved_cs_results */ ;
/*!50003 SET collation_connection  = @saved_col_connection */ ;
/*!50003 DROP PROCEDURE IF EXISTS `spInsertCardData` */;
/*!50003 SET @saved_cs_client      = @@character_set_client */ ;
/*!50003 SET @saved_cs_results     = @@character_set_results */ ;
/*!50003 SET @saved_col_connection = @@collation_connection */ ;
/*!50003 SET character_set_client  = utf8mb4 */ ;
/*!50003 SET character_set_results = utf8mb4 */ ;
/*!50003 SET collation_connection  = utf8mb4_general_ci */ ;
/*!50003 SET @saved_sql_mode       = @@sql_mode */ ;
/*!50003 SET sql_mode              = '' */ ;
DELIMITER ;;
CREATE DEFINER=`root`@`localhost` PROCEDURE `spInsertCardData`(IN `User_ID` INT, IN `Business_ID` INT, IN `Card_IDGoogle` TEXT)
INSERT INTO ClientCard (CreationDate, CheckQTY, CardIDGoogle, LastCheck, UserID, BusinessID, HistoricCheckQTY)
SELECT NOW(), 1, Card_IDGoogle, NOW(), User_ID, Business_ID, 1
FROM DUAL
WHERE NOT EXISTS (
    SELECT 1
    FROM ClientCard
    WHERE UserID = User_ID AND BusinessID = Business_ID
)
  AND User_ID IN (
      SELECT UserID
      FROM UserClient
      WHERE RoleID != 1
  ) ;;
DELIMITER ;
/*!50003 SET sql_mode              = @saved_sql_mode */ ;
/*!50003 SET character_set_client  = @saved_cs_client */ ;
/*!50003 SET character_set_results = @saved_cs_results */ ;
/*!50003 SET collation_connection  = @saved_col_connection */ ;
/*!50003 DROP PROCEDURE IF EXISTS `spInsertPassData` */;
/*!50003 SET @saved_cs_client      = @@character_set_client */ ;
/*!50003 SET @saved_cs_results     = @@character_set_results */ ;
/*!50003 SET @saved_col_connection = @@collation_connection */ ;
/*!50003 SET character_set_client  = utf8mb4 */ ;
/*!50003 SET character_set_results = utf8mb4 */ ;
/*!50003 SET collation_connection  = utf8mb4_general_ci */ ;
/*!50003 SET @saved_sql_mode       = @@sql_mode */ ;
/*!50003 SET sql_mode              = '' */ ;
DELIMITER ;;
CREATE DEFINER=`root`@`localhost` PROCEDURE `spInsertPassData`(IN `serialNumber` VARCHAR(40), IN `authToken` VARCHAR(40), IN `pushToken` VARCHAR(40), IN `creationDate` DATETIME, IN `checkQTY` INT, IN `lastCheckIn` DATETIME, IN `iDUser` INT, IN `iDBusiness` INT)
INSERT INTO ApplePass (SerialNumber, AuthToken, PushToken, CreationDate, CheckQTY, LastCheckIn, UserID, BusinessID) VALUES (serialNumber, authToken, "-", NOW(), 0, Now(), 9, 1) ;;
DELIMITER ;
/*!50003 SET sql_mode              = @saved_sql_mode */ ;
/*!50003 SET character_set_client  = @saved_cs_client */ ;
/*!50003 SET character_set_results = @saved_cs_results */ ;
/*!50003 SET collation_connection  = @saved_col_connection */ ;
/*!50003 DROP PROCEDURE IF EXISTS `spInsertUserClientData` */;
/*!50003 SET @saved_cs_client      = @@character_set_client */ ;
/*!50003 SET @saved_cs_results     = @@character_set_results */ ;
/*!50003 SET @saved_col_connection = @@collation_connection */ ;
/*!50003 SET character_set_client  = utf8mb4 */ ;
/*!50003 SET character_set_results = utf8mb4 */ ;
/*!50003 SET collation_connection  = utf8mb4_general_ci */ ;
/*!50003 SET @saved_sql_mode       = @@sql_mode */ ;
/*!50003 SET sql_mode              = '' */ ;
DELIMITER ;;
CREATE DEFINER=`root`@`localhost` PROCEDURE `spInsertUserClientData`(IN `User_Name` VARCHAR(15), IN `User_Password` VARCHAR(25), IN `First_Name` VARCHAR(30), IN `Last_Name` VARCHAR(30), IN `User_Email` VARCHAR(30))
INSERT INTO UserClient (UserName, UserPassword, FirstName, LastName, UserEmail, RoleID)
VALUES (User_Name, User_Password, First_Name, Last_Name, User_Email, 2) ;;
DELIMITER ;
/*!50003 SET sql_mode              = @saved_sql_mode */ ;
/*!50003 SET character_set_client  = @saved_cs_client */ ;
/*!50003 SET character_set_results = @saved_cs_results */ ;
/*!50003 SET collation_connection  = @saved_col_connection */ ;
/*!50003 DROP PROCEDURE IF EXISTS `spModifyBusinessData` */;
/*!50003 SET @saved_cs_client      = @@character_set_client */ ;
/*!50003 SET @saved_cs_results     = @@character_set_results */ ;
/*!50003 SET @saved_col_connection = @@collation_connection */ ;
/*!50003 SET character_set_client  = utf8mb4 */ ;
/*!50003 SET character_set_results = utf8mb4 */ ;
/*!50003 SET collation_connection  = utf8mb4_general_ci */ ;
/*!50003 SET @saved_sql_mode       = @@sql_mode */ ;
/*!50003 SET sql_mode              = '' */ ;
DELIMITER ;;
CREATE DEFINER=`root`@`localhost` PROCEDURE `spModifyBusinessData`(IN `Business_ID` INT, IN `Business_Name` VARCHAR(100), IN `Business_Password` VARCHAR(30), IN `Business_Email` VARCHAR(30), IN `Business_Logo` VARCHAR(100))
UPDATE Business
SET 
    BusinessName = Business_Name,
    BusinessPassword = Business_Password,
    BusinessEmail = Business_Email,
    BusinessLogo = COALESCE(Business_Logo, BusinessLogo)
WHERE 
    BusinessID = Business_ID ;;
DELIMITER ;
/*!50003 SET sql_mode              = @saved_sql_mode */ ;
/*!50003 SET character_set_client  = @saved_cs_client */ ;
/*!50003 SET character_set_results = @saved_cs_results */ ;
/*!50003 SET collation_connection  = @saved_col_connection */ ;
/*!50003 DROP PROCEDURE IF EXISTS `spSelectBusinessData` */;
/*!50003 SET @saved_cs_client      = @@character_set_client */ ;
/*!50003 SET @saved_cs_results     = @@character_set_results */ ;
/*!50003 SET @saved_col_connection = @@collation_connection */ ;
/*!50003 SET character_set_client  = utf8mb4 */ ;
/*!50003 SET character_set_results = utf8mb4 */ ;
/*!50003 SET collation_connection  = utf8mb4_general_ci */ ;
/*!50003 SET @saved_sql_mode       = @@sql_mode */ ;
/*!50003 SET sql_mode              = '' */ ;
DELIMITER ;;
CREATE DEFINER=`root`@`localhost` PROCEDURE `spSelectBusinessData`(IN `Business_Email` VARCHAR(50), IN `Business_Password` VARCHAR(25))
SELECT * FROM Business
WHERE BusinessEmail = Business_Email AND BusinessPassword = Business_Password ;;
DELIMITER ;
/*!50003 SET sql_mode              = @saved_sql_mode */ ;
/*!50003 SET character_set_client  = @saved_cs_client */ ;
/*!50003 SET character_set_results = @saved_cs_results */ ;
/*!50003 SET collation_connection  = @saved_col_connection */ ;
/*!50003 DROP PROCEDURE IF EXISTS `spSelectUserClientData` */;
/*!50003 SET @saved_cs_client      = @@character_set_client */ ;
/*!50003 SET @saved_cs_results     = @@character_set_results */ ;
/*!50003 SET @saved_col_connection = @@collation_connection */ ;
/*!50003 SET character_set_client  = utf8mb4 */ ;
/*!50003 SET character_set_results = utf8mb4 */ ;
/*!50003 SET collation_connection  = utf8mb4_general_ci */ ;
/*!50003 SET @saved_sql_mode       = @@sql_mode */ ;
/*!50003 SET sql_mode              = '' */ ;
DELIMITER ;;
CREATE DEFINER=`root`@`localhost` PROCEDURE `spSelectUserClientData`(IN `User_Identifier` VARCHAR(50), IN `User_Password` VARCHAR(25))
SELECT 
        uc.UserID, 
        uc.UserName, 
        uc.FirstName, 
        uc.LastName, 
        uc.UserEmail, 
        r.RoleID
    FROM UserClient uc
    JOIN Role r ON uc.RoleID = r.RoleID
    WHERE (uc.UserName = User_Identifier OR uc.UserEmail = User_Identifier) 
          AND uc.UserPassword = User_Password ;;
DELIMITER ;
/*!50003 SET sql_mode              = @saved_sql_mode */ ;
/*!50003 SET character_set_client  = @saved_cs_client */ ;
/*!50003 SET character_set_results = @saved_cs_results */ ;
/*!50003 SET collation_connection  = @saved_col_connection */ ;
/*!50003 DROP PROCEDURE IF EXISTS `spStorePasswordResetToken` */;
/*!50003 SET @saved_cs_client      = @@character_set_client */ ;
/*!50003 SET @saved_cs_results     = @@character_set_results */ ;
/*!50003 SET @saved_col_connection = @@collation_connection */ ;
/*!50003 SET character_set_client  = utf8mb4 */ ;
/*!50003 SET character_set_results = utf8mb4 */ ;
/*!50003 SET collation_connection  = utf8mb4_general_ci */ ;
/*!50003 SET @saved_sql_mode       = @@sql_mode */ ;
/*!50003 SET sql_mode              = '' */ ;
DELIMITER ;;
CREATE DEFINER=`root`@`localhost` PROCEDURE `spStorePasswordResetToken`(IN `User_Email` VARCHAR(30), IN `Token_Value` VARCHAR(255), IN `Expiration_Time` DATETIME)
INSERT INTO PasswordResetToken (UserEmail, Token, Expiration)
VALUES (User_Email, Token_Value, Expiration_Time) ;;
DELIMITER ;
/*!50003 SET sql_mode              = @saved_sql_mode */ ;
/*!50003 SET character_set_client  = @saved_cs_client */ ;
/*!50003 SET character_set_results = @saved_cs_results */ ;
/*!50003 SET collation_connection  = @saved_col_connection */ ;
/*!50003 DROP PROCEDURE IF EXISTS `spUpdateClientPassword` */;
/*!50003 SET @saved_cs_client      = @@character_set_client */ ;
/*!50003 SET @saved_cs_results     = @@character_set_results */ ;
/*!50003 SET @saved_col_connection = @@collation_connection */ ;
/*!50003 SET character_set_client  = utf8mb4 */ ;
/*!50003 SET character_set_results = utf8mb4 */ ;
/*!50003 SET collation_connection  = utf8mb4_general_ci */ ;
/*!50003 SET @saved_sql_mode       = @@sql_mode */ ;
/*!50003 SET sql_mode              = '' */ ;
DELIMITER ;;
CREATE DEFINER=`root`@`localhost` PROCEDURE `spUpdateClientPassword`(IN `User_Email` VARCHAR(30), IN `User_Password` VARCHAR(25))
UPDATE UserClient
SET UserPassword = User_Password
WHERE UserEmail = User_Email
AND RoleID = 2 ;;
DELIMITER ;
/*!50003 SET sql_mode              = @saved_sql_mode */ ;
/*!50003 SET character_set_client  = @saved_cs_client */ ;
/*!50003 SET character_set_results = @saved_cs_results */ ;
/*!50003 SET collation_connection  = @saved_col_connection */ ;
/*!50003 DROP PROCEDURE IF EXISTS `spValidateResetToken` */;
/*!50003 SET @saved_cs_client      = @@character_set_client */ ;
/*!50003 SET @saved_cs_results     = @@character_set_results */ ;
/*!50003 SET @saved_col_connection = @@collation_connection */ ;
/*!50003 SET character_set_client  = utf8mb4 */ ;
/*!50003 SET character_set_results = utf8mb4 */ ;
/*!50003 SET collation_connection  = utf8mb4_general_ci */ ;
/*!50003 SET @saved_sql_mode       = @@sql_mode */ ;
/*!50003 SET sql_mode              = '' */ ;
DELIMITER ;;
CREATE DEFINER=`root`@`localhost` PROCEDURE `spValidateResetToken`(IN `Token_Value` VARCHAR(255))
SELECT COUNT(*) 
    FROM PasswordResetToken 
    WHERE Token = Token_Value AND Expiration > NOW() ;;
DELIMITER ;
/*!50003 SET sql_mode              = @saved_sql_mode */ ;
/*!50003 SET character_set_client  = @saved_cs_client */ ;
/*!50003 SET character_set_results = @saved_cs_results */ ;
/*!50003 SET collation_connection  = @saved_col_connection */ ;
/*!50112 SET @disable_bulk_load = IF (@is_rocksdb_supported, 'SET SESSION rocksdb_bulk_load = @old_rocksdb_bulk_load', 'SET @dummy_rocksdb_bulk_load = 0') */;
/*!50112 PREPARE s FROM @disable_bulk_load */;
/*!50112 EXECUTE s */;
/*!50112 DEALLOCATE PREPARE s */;
/*!40103 SET TIME_ZONE=@OLD_TIME_ZONE */;

/*!40101 SET SQL_MODE=@OLD_SQL_MODE */;
/*!40014 SET FOREIGN_KEY_CHECKS=@OLD_FOREIGN_KEY_CHECKS */;
/*!40014 SET UNIQUE_CHECKS=@OLD_UNIQUE_CHECKS */;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
/*!40111 SET SQL_NOTES=@OLD_SQL_NOTES */;

-- Dump completed on 2026-05-26 15:48:47
