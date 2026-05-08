-- MySQL dump 10.13  Distrib 5.7.23-23, for Linux (x86_64)
--
-- Host: localhost    Database: alltrac1_dcards
-- ------------------------------------------------------
-- Server version	5.7.23-23

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
) ENGINE=InnoDB AUTO_INCREMENT=42 DEFAULT CHARSET=utf8 COLLATE=utf8_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `Business`
--

LOCK TABLES `Business` WRITE;
/*!40000 ALTER TABLE `Business` DISABLE KEYS */;
INSERT INTO `Business` (`BusinessID`, `BusinessName`, `BusinessPassword`, `BusinessEmail`, `BusinessLogo`) VALUES (1,'test','9f86d081884c7d659a2feaa0c','test','~/Logos/BWG logo.png'),(30,'testpresentacion','9f86d081884c7d659a2feaa0c','testpresentacion@gmail.com','~/Logos/logo empresa.jpg'),(36,'testo','9f86d081884c7d659a2feaa0c','aaa@gmail.com','~/Logos/logo.jpg'),(37,'testoto','03ac674216f3e15c761ee1a5e','alfredo.24david@gmail.com','~/Logos/Logo.png'),(39,'test60','9f86d081884c7d659a2feaa0c','test60@gmail.com','~/Logos/BWG_logo_20250415073730.png'),(40,'Balboa Water','9f86d081884c7d659a2feaa0c','ingelabmx@gmail.com','~/Logos/Coast_20250415145706.png'),(41,'test92','9f86d081884c7d659a2feaa0c','test92@gmail.com','~/Logos/BWG_logo_20250416070443.png');
/*!40000 ALTER TABLE `Business` ENABLE KEYS */;
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
) ENGINE=InnoDB AUTO_INCREMENT=66 DEFAULT CHARSET=utf8 COLLATE=utf8_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `ClientCard`
--

LOCK TABLES `ClientCard` WRITE;
/*!40000 ALTER TABLE `ClientCard` DISABLE KEYS */;
INSERT INTO `ClientCard` (`CardID`, `CardIDGoogle`, `CreationDate`, `CheckQTY`, `LastCheck`, `UserID`, `BusinessID`, `HistoricCheckQTY`) VALUES (7,NULL,'2024-12-04 09:57:42',2,'2024-12-28 00:07:26',7,1,0),(8,NULL,'2025-01-03 23:00:39',9,'2025-04-10 09:14:07',8,1,15),(57,'E7fB2tZv0s','2025-01-18 16:46:44',1,'2025-01-18 16:46:44',11,36,0),(58,'ATo4eSo94z','2025-02-28 15:47:57',1,'2025-02-28 15:47:57',17,1,0),(59,'mCRkRBZjES','2025-04-08 15:00:48',2,'2025-04-16 08:02:37',8,30,2),(60,'dC9DNBDHel','2025-04-08 15:14:29',2,'2025-04-16 08:09:47',20,30,1),(61,'fNEcdZzcPx','2025-04-08 15:14:46',1,'2025-04-08 15:14:46',19,30,0),(62,'UUbLRZQsFP','2025-04-09 08:38:14',1,'2025-04-09 08:38:14',21,1,1),(63,'d0Y1kI0GqN','2025-04-10 01:20:22',1,'2025-04-25 19:44:58',11,37,11),(64,'69sqWoiNjF','2025-04-15 08:38:45',1,'2025-04-15 08:38:45',8,39,1),(65,'GhUY6Uuoeo','2025-04-15 16:01:19',2,'2025-04-16 14:51:33',20,40,12);
/*!40000 ALTER TABLE `ClientCard` ENABLE KEYS */;
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
INSERT INTO `PasswordResetToken` (`UserEmail`, `Token`, `Expiration`) VALUES ('ingelabmx@gmail.com','5b6f2731-7371-4ab3-95af-0da81c47e3bd','2025-04-16 21:33:18'),('alfredo60fil@gmail.com','b8809904-f6f4-4ccb-a7a8-6e77ba569503','2025-01-14 04:30:42');
/*!40000 ALTER TABLE `PasswordResetToken` ENABLE KEYS */;
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
) ENGINE=InnoDB AUTO_INCREMENT=22 DEFAULT CHARSET=utf8 COLLATE=utf8_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `UserClient`
--

LOCK TABLES `UserClient` WRITE;
/*!40000 ALTER TABLE `UserClient` DISABLE KEYS */;
INSERT INTO `UserClient` (`UserID`, `UserName`, `UserPassword`, `FirstName`, `Lastname`, `UserEmail`, `RoleID`) VALUES (1,'DCAdmin','9f86d081884c7d659a2feaa0c','IngeLabs','Admin','ingelabmx@gmail.com',1),(7,'test50','9f86d081884c7d659a2feaa0c','test','test','test50',2),(8,'EloyTest','9f86d081884c7d659a2feaa0c','test','test','eloycobianmar@gmail.com',2),(9,'Ejim984','03ac674216f3e15c761ee1a5e','Emmanuel','Jimenez','ejim984@gmail.com',2),(11,'alfredo60fil','03ac674216f3e15c761ee1a5e','David','Avila','alfredo60fil@gmail.com',2),(13,'MannyTest','9f86d081884c7d659a2feaa0c','Manuel','Flores','MannyTest',2),(14,'OtroTest','9f86d081884c7d659a2feaa0c','Test','Test','Otro@gmail.com',2),(15,'Mantest','03ac674216f3e15c761ee1a5e','Manuel','Flores','Mantest',2),(16,'Hola123','03ac674216f3e15c761ee1a5e','Hola','123','hola123@gmail.com',2),(17,'Veloraptor17','71e9d7499aa68afc045a39e89','Eduardo ','Cobián Márquez ','ecobianmarquez@gmail.com',2),(18,'Manuel Flores','03ac674216f3e15c761ee1a5e','Jose Manuel','Flores','jmanuel.flores147@gmail.com',2),(19,'Dang','9f86d081884c7d659a2feaa0c','Eric','Guillen','guillenmorenoe58@gmail.com',2),(20,'Javier.1','cbe34ab81efe4b9196590cef7','Javier','Gomez ','Xtej24@gmail.com',2),(21,'test25','9f86d081884c7d659a2feaa0c','test','test','test25@gmail.com',2);
/*!40000 ALTER TABLE `UserClient` ENABLE KEYS */;
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
INSERT INTO ApplePass (SerialNumber, AuthToken, PushToken, CreationDate, CheckQTY, LastCheck, UserID, BusinessID) VALUES (serialNumber, authToken, "-", NOW(), 0, Now(), 9, 1) ;;
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

-- Dump completed on 2025-11-07 14:23:44
