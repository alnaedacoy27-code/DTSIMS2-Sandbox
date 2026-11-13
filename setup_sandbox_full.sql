-- =====================================================================
-- DTSIMS2 SANDBOX - one-shot database setup
-- Creates schema `dtsims2_sandbox` and loads a COPY of dtsims2_db into it.
-- The original dtsims2_db is never touched.
-- HOW: MySQL Workbench > File > Open SQL Script > this file > run all (lightning bolt)
-- Safe to re-run: it rebuilds the sandbox tables from scratch each time.
-- =====================================================================

CREATE DATABASE IF NOT EXISTS `dtsims2_sandbox`
  DEFAULT CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci;
USE `dtsims2_sandbox`;

-- Only needed if the app says "access denied" for darwin_server on the new schema.
-- First check the login exists:  SELECT user, host FROM mysql.user;
-- then uncomment the matching line(s):
-- GRANT ALL PRIVILEGES ON `dtsims2_sandbox`.* TO 'darwin_server'@'localhost';
-- GRANT ALL PRIVILEGES ON `dtsims2_sandbox`.* TO 'darwin_server'@'%';

-- ---------------------- copy of dtsims2_db.sql below ------------------
-- MySQL dump 10.13  Distrib 8.4.11, for Win64 (x86_64)
--
-- Host: localhost    Database: dtsims2_db
-- ------------------------------------------------------
-- Server version	8.4.11

/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!50503 SET NAMES utf8mb4 */;
/*!40103 SET @OLD_TIME_ZONE=@@TIME_ZONE */;
/*!40103 SET TIME_ZONE='+00:00' */;
/*!40014 SET @OLD_UNIQUE_CHECKS=@@UNIQUE_CHECKS, UNIQUE_CHECKS=0 */;
/*!40014 SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0 */;
/*!40101 SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='NO_AUTO_VALUE_ON_ZERO' */;
/*!40111 SET @OLD_SQL_NOTES=@@SQL_NOTES, SQL_NOTES=0 */;

--
-- Table structure for table `audit_trail`
--

DROP TABLE IF EXISTS `audit_trail`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `audit_trail` (
  `id` bigint NOT NULL AUTO_INCREMENT,
  `user_id` int DEFAULT NULL,
  `username` varchar(50) DEFAULT NULL,
  `action` varchar(30) NOT NULL,
  `module` varchar(60) NOT NULL,
  `description` varchar(500) DEFAULT NULL,
  `device_name` varchar(100) DEFAULT NULL,
  `created_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  KEY `idx_audit_date` (`created_at`),
  KEY `idx_audit_user` (`user_id`)
) ENGINE=InnoDB AUTO_INCREMENT=394 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `audit_trail`
--

LOCK TABLES `audit_trail` WRITE;
/*!40000 ALTER TABLE `audit_trail` DISABLE KEYS */;
INSERT INTO `audit_trail` VALUES (1,NULL,'naed','CREATE','Setup','Superadmin account \'naed\' was created','NAED','2026-10-18 22:33:22'),(2,1,'naed','LOGIN','Login','\'naed\' logged in','NAED','2026-10-18 22:33:51'),(3,NULL,'naed','CREATE','Setup','Superadmin account \'naed\' was created','NAED','2026-10-18 22:40:20'),(4,2,'naed','LOGIN','Login','\'naed\' logged in','NAED','2026-10-18 22:40:44'),(5,NULL,'naed','CREATE','Setup','Superadmin account \'naed\' was created','NAED','2026-10-18 22:45:31'),(6,3,'naed','LOGIN_FAILED','Login','Wrong password for \'naed\'','NAED','2026-10-18 22:46:12'),(7,3,'naed','LOGIN','Login','\'naed\' logged in','NAED','2026-10-18 22:46:21'),(8,3,'naed','LOGIN','Login','\'naed\' logged in','NAED','2026-10-18 22:49:21'),(9,NULL,'naed','CREATE','Setup','Superadmin account \'naed\' was created','NAED','2026-10-18 22:53:04'),(10,4,'naed','LOGIN','Login','\'naed\' logged in','NAED','2026-10-18 22:53:23'),(11,4,'naed','LOGIN','Login','\'naed\' logged in','NAED','2026-10-18 22:54:32'),(12,4,'naed','LOGIN','Login','\'naed\' logged in','NAED','2026-10-19 09:17:32'),(13,NULL,'naed','CREATE','Setup','Superadmin account \'naed\' was created','NAED','2026-10-19 09:18:40'),(14,5,'naed','LOGIN','Login','\'naed\' logged in','NAED','2026-10-19 09:18:47'),(15,5,'naed','LOGIN','Login','\'naed\' logged in','NAED','2026-10-19 14:21:38'),(16,6,'ana','LOGIN_FAILED','Login','Wrong password for \'ana\'','NAED','2026-10-19 14:28:52'),(17,6,'ana','LOGIN_FAILED','Login','Wrong password for \'ana\'','NAED','2026-10-19 14:29:28'),(18,5,'naed','LOGIN','Login','\'naed\' logged in','NAED','2026-10-19 14:31:35'),(19,6,'ana','LOGIN_FAILED','Login','Wrong password for \'ana\'','NAED','2026-10-19 14:32:44'),(20,6,'ana','LOGIN_FAILED','Login','Wrong password for \'ana\'','NAED','2026-10-19 14:32:48'),(21,6,'ana','LOGIN_FAILED','Login','Wrong password for \'ana\'','NAED','2026-10-19 14:32:49'),(22,5,'naed','LOGIN','Login','\'naed\' logged in','NAED','2026-10-19 14:33:05'),(23,6,'ana','LOGIN_FAILED','Login','Wrong password for \'ana\'','NAED','2026-10-19 14:50:36'),(24,6,'ana','LOGIN_FAILED','Login','Wrong password for \'ana\'','NAED','2026-10-19 14:50:43'),(25,6,'ana','LOGIN_FAILED','Login','Wrong password for \'ana\'','NAED','2026-10-19 14:52:49'),(26,6,'ana','LOGIN_FAILED','Login','Wrong password for \'ana\'','NAED','2026-10-19 14:54:20'),(27,6,'ana','LOGIN_FAILED','Login','Wrong password for \'ana\'','NAED','2026-10-19 14:54:23'),(28,7,'ben','LOGIN_FAILED','Login','Wrong password for \'ben\'','NAED','2026-10-19 14:55:47'),(29,7,'ben','LOGIN_FAILED','Login','Wrong password for \'ben\'','NAED','2026-10-19 14:56:06'),(30,5,'naed','LOGIN','Login','\'naed\' logged in','NAED','2026-10-19 14:57:41'),(31,7,'ben','LOGIN','Login','\'ben\' logged in','NAED','2026-10-19 15:05:19'),(32,5,'naed','LOGIN','Login','\'naed\' logged in','NAED','2026-10-19 19:02:29'),(33,5,'naed','LOGIN','Login','\'naed\' logged in','NAED','2026-10-19 19:10:25'),(34,5,'naed','EDIT','Users','Edited Superadmin \'naed\'','NAED','2026-10-19 19:11:03'),(35,5,'naed','RESET_PASSWORD','Users','Password reset for \'ana\'','NAED','2026-10-19 19:11:56'),(36,5,'naed','DISABLE','Users','Disabled account \'ben\'','NAED','2026-10-19 19:12:28'),(37,5,'naed','LOGIN','Login','\'naed\' logged in','NAED','2026-10-19 19:12:51'),(38,5,'naed','ENABLE','Users','Enabled account \'ben\'','NAED','2026-10-19 19:13:02'),(39,5,'naed','ADD','Users','Added Cashier \'naedd\'','NAED','2026-10-19 19:14:53'),(40,5,'naed','EDIT','Users','Edited Admin \'naedd\'','NAED','2026-10-19 19:15:28'),(41,9,'naedd','LOGIN','Login','\'naedd\' logged in','NAED','2026-10-19 19:15:40'),(42,5,'naed','LOGIN','Login','\'naed\' logged in','NAED','2026-10-19 19:29:21'),(43,5,'naed','EDIT','Users','Edited Admin \'ana\'','NAED','2026-10-19 19:30:06'),(44,5,'naed','EDIT','Users','Edited Admin \'ana\'','NAED','2026-10-19 19:30:17'),(45,5,'naed','EDIT','Users','Edited Superadmin \'naed\'','NAED','2026-10-19 19:36:23'),(46,5,'naed','DELETE','Users','Deleted Admin \'ana\'','NAED','2026-10-19 19:36:44'),(47,5,'naed','LOGIN','Login','\'naed\' logged in','NAED','2026-10-20 00:09:44'),(48,5,'naed','ADD','Users','Added Cashier \'carl\'','NAED','2026-10-20 00:11:59'),(49,5,'naed','EDIT','Users','Edited Admin \'carl\'','NAED','2026-10-20 00:12:09'),(50,5,'naed','ADD','Users','Added Cashier \'red\'','NAED','2026-10-20 00:26:25'),(51,5,'naed','EDIT','Users','Edited Admin \'ben\'','NAED','2026-10-20 00:26:41'),(52,5,'naed','EDIT','Users','Edited Admin \'red\'','NAED','2026-10-20 00:26:47'),(53,5,'naed','ADD','Users','Added Cashier \'muksin\'','NAED','2026-10-20 00:29:48'),(54,5,'naed','EDIT','Users','Edited Admin \'muksin\'','NAED','2026-10-20 00:29:57'),(55,5,'naed','EDIT','Users','Edited Admin \'muksin\'','NAED','2026-10-20 00:29:59'),(56,5,'naed','ADD','Users','Added Cashier \'francis\'','NAED','2026-10-20 00:30:55'),(57,13,'francis','LOGIN','Login','\'francis\' logged in','NAED','2026-10-20 00:32:00'),(58,5,'naed','LOGIN','Login','\'naed\' logged in','NAED','2026-10-20 00:33:34'),(59,5,'naed','LOGIN','Login','\'naed\' logged in','NAED','2026-10-20 07:22:46'),(60,5,'naed','DELETE','Users','Deleted Admin \'ben\'','NAED','2026-10-20 07:24:39'),(61,5,'naed','LOGIN','Login','\'naed\' logged in','NAED','2026-10-20 07:28:32'),(62,5,'naed','ADD','Users','Added Admin \'beanch\'','NAED','2026-10-20 07:31:11'),(63,13,'francis','LOGIN','Login','\'francis\' logged in','NAED','2026-10-20 07:31:33'),(64,5,'naed','LOGIN','Login','\'naed\' logged in','NAED','2026-10-20 07:31:42'),(65,5,'naed','LOGIN','Login','\'naed\' logged in','NAED','2026-10-21 21:16:51'),(66,5,'naed','LOGIN','Login','\'naed\' logged in','NAED','2026-10-21 21:22:45'),(67,5,'naed','LOGIN','Login','\'naed\' logged in','NAED','2026-10-22 06:14:45'),(68,5,'naed','OPEN','User Monitoring','Opened User Monitoring','NAED','2026-10-22 06:14:51'),(69,5,'naed','OPEN','User Monitoring','Opened User Monitoring','NAED','2026-10-22 06:15:34'),(70,5,'naed','OPEN','User Monitoring','Opened User Monitoring','NAED','2026-10-22 06:18:24'),(71,5,'naed','OPEN','Users','Opened Users','NAED','2026-10-22 06:18:56'),(72,5,'naed','OPEN','User Monitoring','Opened User Monitoring','NAED','2026-10-22 06:19:01'),(73,5,'naed','OPEN','Users','Opened Users','NAED','2026-10-22 06:19:45'),(74,5,'naed','LOGOUT','Login','\'naed\' logged out','NAED','2026-10-22 06:20:11'),(75,13,'francis','LOGIN','Login','\'francis\' logged in','NAED','2026-10-22 06:20:17'),(76,13,'francis','LOGOUT','Login','\'francis\' logged out','NAED','2026-10-22 06:20:55'),(77,11,'red','LOGIN_FAILED','Login','Wrong password for \'red\'','NAED','2026-10-22 06:21:01'),(78,11,'red','LOGIN_FAILED','Login','Wrong password for \'red\'','NAED','2026-10-22 06:21:06'),(79,11,'red','LOGIN','Login','\'red\' logged in','NAED','2026-10-22 06:21:12'),(80,11,'red','OPEN','Categories','Opened Categories','NAED','2026-10-22 06:21:47'),(81,11,'red','LOGOUT','Login','\'red\' logged out','NAED','2026-10-22 06:21:53'),(82,5,'naed','LOGIN','Login','\'naed\' logged in','NAED','2026-10-22 06:21:58'),(83,5,'naed','OPEN','User Monitoring','Opened User Monitoring','NAED','2026-10-22 06:22:11'),(84,5,'naed','OPEN','User Monitoring','Opened User Monitoring','NAED','2026-10-22 06:47:01'),(85,5,'naed','OPEN','Users','Opened Users','NAED','2026-10-22 07:34:44'),(86,5,'naed','OPEN','User Monitoring','Opened User Monitoring','NAED','2026-10-22 07:34:52'),(87,5,'naed','OPEN','Inventory Report','Opened Inventory Report','NAED','2026-10-22 07:35:25'),(88,5,'naed','OPEN','User Monitoring','Opened User Monitoring','NAED','2026-10-22 07:35:29'),(89,5,'naed','LOGOUT','Login','\'naed\' logged out','NAED','2026-10-22 07:39:08'),(90,5,'naed','LOGIN','Login','\'naed\' logged in','NAED','2026-10-22 11:33:17'),(91,5,'naed','OPEN','Users','Opened Users','NAED','2026-10-22 11:33:22'),(92,5,'naed','OPEN','User Monitoring','Opened User Monitoring','NAED','2026-10-22 11:33:41'),(93,5,'naed','LOGOUT','Login','\'naed\' logged out','NAED','2026-10-22 11:34:12'),(94,5,'naed','LOGIN','Login','\'naed\' logged in','NAED','2026-10-22 11:50:20'),(95,5,'naed','LOGOUT','Login','\'naed\' logged out','NAED','2026-10-22 11:53:27'),(96,13,'francis','LOGIN','Login','\'francis\' logged in','LUCKY','2026-10-22 12:00:58'),(97,5,'naed','LOGIN_FAILED','Login','Wrong password for \'naed\'','NAED','2026-10-22 12:01:11'),(98,5,'naed','LOGIN','Login','\'naed\' logged in','NAED','2026-10-22 12:01:17'),(99,5,'naed','OPEN','User Monitoring','Opened User Monitoring','NAED','2026-10-22 12:01:23'),(100,13,'francis','OPEN','Sales Report','Opened Sales Report','LUCKY','2026-10-22 12:01:27'),(101,13,'francis','OPEN','POS','Opened POS','LUCKY','2026-10-22 12:02:00'),(102,5,'naed','OPEN','Users','Opened Users','NAED','2026-10-22 12:02:35'),(103,5,'naed','OPEN','User Monitoring','Opened User Monitoring','NAED','2026-10-22 12:02:56'),(104,13,'francis','LOGOUT','Login','\'francis\' logged out','LUCKY','2026-10-22 12:03:02'),(105,5,'naed','LOGIN','Login','\'naed\' logged in','LUCKY','2026-10-22 12:03:20'),(106,5,'naed','OPEN','User Monitoring','Opened User Monitoring','LUCKY','2026-10-22 12:03:29'),(107,5,'naed','OPEN','VAT','Opened VAT','LUCKY','2026-10-22 12:03:42'),(108,5,'naed','OPEN','User Monitoring','Opened User Monitoring','LUCKY','2026-10-22 12:05:08'),(109,5,'naed','LOGOUT','Login','\'naed\' logged out','NAED','2026-10-22 12:05:17'),(110,5,'naed','OPEN','Users','Opened Users','LUCKY','2026-10-22 12:05:42'),(111,5,'naed','OPEN','User Monitoring','Opened User Monitoring','LUCKY','2026-10-22 12:06:32'),(112,5,'naed','LOGIN','Login','\'naed\' logged in','NAED','2026-10-22 12:07:08'),(113,5,'naed','OPEN','Users','Opened Users','NAED','2026-10-22 12:07:19'),(114,5,'naed','LOGOUT','Login','\'naed\' logged out','LUCKY','2026-10-22 12:08:34'),(115,5,'naed','OPEN','User Monitoring','Opened User Monitoring','NAED','2026-10-22 12:09:00'),(116,5,'naed','LOGOUT','Login','\'naed\' logged out','NAED','2026-10-22 12:09:38'),(117,5,'naed','LOGIN_FAILED','Login','Wrong password for \'naed\'','NAED','2026-10-22 17:08:52'),(118,5,'naed','LOGIN','Login','\'naed\' logged in','NAED','2026-10-22 17:09:03'),(119,5,'naed','OPEN','Users','Opened Users','NAED','2026-10-22 17:09:05'),(120,5,'naed','OPEN','User Monitoring','Opened User Monitoring','NAED','2026-10-22 17:09:16'),(121,5,'naed','LOGOUT','Login','\'naed\' logged out','NAED','2026-10-22 17:09:53'),(122,5,'naed','LOGIN','Login','\'naed\' logged in','NAED','2026-10-22 22:56:29'),(123,5,'naed','OPEN','Suppliers','Opened Suppliers','NAED','2026-10-22 22:56:31'),(124,5,'naed','ADD','Suppliers','Added supplier \'jasper metal corp\' (taguig)','NAED','2026-10-22 22:57:40'),(125,5,'naed','ADD','Suppliers','Added supplier \'carl\' (makati)','NAED','2026-10-22 22:58:54'),(126,5,'naed','EDIT','Suppliers','Edited supplier \'jasper metal corp\' (taguig)','NAED','2026-10-22 22:59:25'),(127,5,'naed','OPEN','Products','Opened Products','NAED','2026-10-22 22:59:39'),(128,5,'naed','ADD','Products','Added category \'Alloy\'','NAED','2026-10-22 22:59:55'),(129,5,'naed','EDIT','Products','Edited category \'Alloy\'','NAED','2026-10-22 23:00:12'),(130,5,'naed','ADD','Products','Added category \'stainless\'','NAED','2026-10-22 23:00:57'),(131,5,'naed','ADD','Products','Added product \'L bar 2mx2mx2m\'','NAED','2026-10-22 23:01:47'),(132,5,'naed','ADD','Products','Added product \'L bar 3mx3mx3m\'','NAED','2026-10-22 23:02:23'),(133,5,'naed','OPEN','Delivery Management','Opened Delivery Management','NAED','2026-10-22 23:03:02'),(134,5,'naed','OPEN','Suppliers','Opened Suppliers','NAED','2026-10-22 23:03:06'),(135,5,'naed','ADD','Suppliers','Added supplier \'carl\' (pasig)','NAED','2026-10-22 23:03:39'),(136,5,'naed','OPEN','Users','Opened Users','NAED','2026-10-22 23:04:44'),(137,5,'naed','OPEN','Products','Opened Products','NAED','2026-10-22 23:04:48'),(138,5,'naed','OPEN','Users','Opened Users','NAED','2026-10-22 23:04:52'),(139,5,'naed','OPEN','Products','Opened Products','NAED','2026-10-22 23:05:07'),(140,5,'naed','OPEN','Products','Opened Products','NAED','2026-10-22 23:05:16'),(141,5,'naed','OPEN','Suppliers','Opened Suppliers','NAED','2026-10-22 23:05:25'),(142,5,'naed','OPEN','Users','Opened Users','NAED','2026-10-22 23:05:36'),(143,5,'naed','OPEN','Suppliers','Opened Suppliers','NAED','2026-10-22 23:05:40'),(144,5,'naed','OPEN','Products','Opened Products','NAED','2026-10-22 23:05:50'),(145,5,'naed','ADD','Products','Added category \'0k\'','NAED','2026-10-22 23:06:58'),(146,5,'naed','EDIT','Products','Edited product \'L bar 2mx2mx2m\'','NAED','2026-10-22 23:07:24'),(147,5,'naed','OPEN','Suppliers','Opened Suppliers','NAED','2026-10-22 23:10:00'),(148,5,'naed','EDIT','Suppliers','Edited supplier \'carlksdfksdfksdfnkosdlksdfkldfnsdfsdfnsdklfnsdkfnskldfnklsdfnklsdnfklsdfnklsdfnsdlfnsdfnfnsdklfnsdklfnsdklfnsdklfnsdklfnsdklfnsdklfnsdklfnsdlkfnsdlkfn\' (makatijasdjasdbkasdbjkasdjasdbjasdbjasbsadbasbdhjabdbjasbdbhjasbdhjassbdhjbasjhbdsabdhjasbdjhasbdjhasbdhjasbdjhasbdjhasbasfsoaidjapsodoasnpoaspasdnpiasdassadbsa)','NAED','2026-10-22 23:10:47'),(149,5,'naed','OPEN','Products','Opened Products','NAED','2026-10-22 23:11:32'),(150,5,'naed','OPEN','User Monitoring','Opened User Monitoring','NAED','2026-10-22 23:12:09'),(151,5,'naed','LOGOUT','Login','\'naed\' logged out','NAED','2026-10-22 23:12:52'),(152,5,'naed','LOGIN','Login','\'naed\' logged in','NAED','2026-10-23 04:06:05'),(153,5,'naed','OPEN','Suppliers','Opened Suppliers','NAED','2026-10-23 04:06:07'),(154,5,'naed','OPEN','Products','Opened Products','NAED','2026-10-23 04:06:22'),(155,5,'naed','LOGIN','Login','\'naed\' logged in','NAED','2026-10-24 09:56:21'),(156,5,'naed','OPEN','Inventory','Opened Inventory','NAED','2026-10-24 09:56:24'),(157,5,'naed','OPEN','Delivery Management','Opened Delivery Management','NAED','2026-10-24 09:57:13'),(158,5,'naed','ADD','Delivery','Started delivery batch (id 1)','NAED','2026-10-24 09:57:56'),(159,5,'naed','EDIT','Delivery','Changed details of batch DEL-2026-0001','NAED','2026-10-24 10:01:25'),(160,5,'naed','SEND','Delivery','Sent batch DEL-2026-0001 to inventory: 2 items (120 pcs)    |    Total cost â‚±14,200.00    |    Expected sales â‚±21,040.00    |    Expected profit â‚±6,840.00  (32.5% margin)','NAED','2026-10-24 10:02:00'),(161,5,'naed','OPEN','Inventory','Opened Inventory','NAED','2026-10-24 10:02:16'),(162,5,'naed','OPEN','Delivery Management','Opened Delivery Management','NAED','2026-10-24 10:02:39'),(163,5,'naed','ADD','Delivery','Started delivery batch (id 2)','NAED','2026-10-24 10:02:56'),(164,5,'naed','OPEN','Products','Opened Products','NAED','2026-10-24 10:04:04'),(165,5,'naed','OPEN','Inventory','Opened Inventory','NAED','2026-10-24 10:04:30'),(166,5,'naed','OPEN','Delivery Management','Opened Delivery Management','NAED','2026-10-24 10:04:50'),(167,5,'naed','SEND','Delivery','Sent batch DEL-2026-0002 to inventory: 2 items (221 pcs)    |    Total cost â‚±35,410.00    |    Expected sales â‚±51,542.00    |    Expected profit â‚±16,132.00  (31.3% margin)','NAED','2026-10-24 10:04:56'),(168,5,'naed','OPEN','Inventory','Opened Inventory','NAED','2026-10-24 10:05:07'),(169,5,'naed','OPEN','Stock Threshold','Opened Stock Threshold','NAED','2026-10-24 10:05:45'),(170,5,'naed','EDIT','Stock Threshold','Set the low-stock warning of L bar Alloy   |   3mx3mx3m to 210','NAED','2026-10-24 10:06:05'),(171,5,'naed','EDIT','Stock Threshold','Set the low-stock warning of L bar stainless   |   2mx2mx2m to 200','NAED','2026-10-24 10:06:25'),(172,5,'naed','OPEN','Stock Threshold','Opened Stock Threshold','NAED','2026-10-24 10:06:58'),(173,5,'naed','EDIT','Stock Threshold','Set the low-stock warning of L bar stainless   |   2mx2mx2m to 50','NAED','2026-10-24 10:07:21'),(174,5,'naed','EDIT','Stock Threshold','Set the low-stock warning of L bar Alloy   |   3mx3mx3m to 50','NAED','2026-10-24 10:07:31'),(175,5,'naed','OPEN','Inventory','Opened Inventory','NAED','2026-10-24 10:07:45'),(176,5,'naed','OPEN','Delivery Management','Opened Delivery Management','NAED','2026-10-24 10:08:46'),(177,5,'naed','OPEN','Products','Opened Products','NAED','2026-10-24 10:09:15'),(178,5,'naed','ADD','Products','Added product \'hotdog\'','NAED','2026-10-24 10:09:34'),(179,5,'naed','ADD','Products','Added product \'cheesdog\'','NAED','2026-10-24 10:09:52'),(180,5,'naed','OPEN','Delivery Management','Opened Delivery Management','NAED','2026-10-24 10:10:02'),(181,5,'naed','ADD','Delivery','Started delivery batch (id 3)','NAED','2026-10-24 10:10:10'),(182,5,'naed','ADD','Products','Added product \'burger 44x44x44mm\'','NAED','2026-10-24 10:11:12'),(183,5,'naed','SEND','Delivery','Sent batch DEL-2026-0003 to inventory: 5 items (290 pcs)    |    Total cost â‚±55,500.00    |    Expected sales â‚±68,350.00    |    Expected profit â‚±12,850.00  (18.8% margin)','NAED','2026-10-24 10:13:37'),(184,5,'naed','OPEN','Inventory','Opened Inventory','NAED','2026-10-24 10:14:19'),(185,5,'naed','OPEN','Products','Opened Products','NAED','2026-10-24 10:14:44'),(186,5,'naed','OPEN','User Monitoring','Opened User Monitoring','NAED','2026-10-24 10:15:13'),(187,5,'naed','OPEN','Suppliers','Opened Suppliers','NAED','2026-10-24 10:15:36'),(188,5,'naed','ADD','Suppliers','Added supplier \'qwerty\' (qwerty)','NAED','2026-10-24 10:16:53'),(189,5,'naed','EDIT','Suppliers','Edited supplier \'qwerty\' (manila)','NAED','2026-10-24 10:17:14'),(190,5,'naed','OPEN','Delivery Management','Opened Delivery Management','NAED','2026-10-24 10:17:41'),(191,5,'naed','ADD','Delivery','Started delivery batch (id 4)','NAED','2026-10-24 10:18:02'),(192,5,'naed','ADD','Products','Added product \'siopao\'','NAED','2026-10-24 10:18:17'),(193,5,'naed','ADD','Products','Added product \'burger\'','NAED','2026-10-24 10:20:13'),(194,5,'naed','OPEN','Products','Opened Products','NAED','2026-10-24 10:20:37'),(195,5,'naed','ADD','Products','Added product \'burger\'','NAED','2026-10-24 10:20:47'),(196,5,'naed','ADD','Products','Added product \'burger\'','NAED','2026-10-24 10:22:20'),(197,5,'naed','OPEN','VAT','Opened VAT','NAED','2026-10-24 10:25:00'),(198,5,'naed','OPEN','User Monitoring','Opened User Monitoring','NAED','2026-10-24 10:25:04'),(199,5,'naed','OPEN','Delivery Management','Opened Delivery Management','NAED','2026-10-24 10:25:09'),(200,5,'naed','SEND','Delivery','Sent batch DEL-2026-0004 to inventory: 5 items (80 pcs)    |    Total cost â‚±9,500.00    |    Expected sales â‚±11,930.00    |    Expected profit â‚±2,430.00  (20.4% margin)','NAED','2026-10-24 10:27:14'),(201,5,'naed','OPEN','Inventory','Opened Inventory','NAED','2026-10-24 10:27:20'),(202,5,'naed','OPEN','Stock Threshold','Opened Stock Threshold','NAED','2026-10-24 10:27:46'),(203,5,'naed','EDIT','Stock Threshold','Set the low-stock warning of burger 0k to 10 (all sizes)','NAED','2026-10-24 10:28:16'),(204,5,'naed','EDIT','Stock Threshold','Set the low-stock warning of burger 0k to 5 (all sizes)','NAED','2026-10-24 10:28:25'),(205,5,'naed','EDIT','Stock Threshold','Set the low-stock warning of cheesdog 0k to 5 (all sizes)','NAED','2026-10-24 10:29:29'),(206,5,'naed','EDIT','Stock Threshold','Set the low-stock warning of hotdog 0k to 5 (all sizes)','NAED','2026-10-24 10:29:34'),(207,5,'naed','EDIT','Stock Threshold','Set the low-stock warning of siopao 0k to 5 (all sizes)','NAED','2026-10-24 10:29:40'),(208,5,'naed','EDIT','Stock Threshold','Set the low-stock warning of burger Alloy to 5 (all sizes)','NAED','2026-10-24 10:29:46'),(209,5,'naed','EDIT','Stock Threshold','Set the low-stock warning of burger stainless to 5 (all sizes)','NAED','2026-10-24 10:29:51'),(210,5,'naed','OPEN','Delivery Management','Opened Delivery Management','NAED','2026-10-24 10:30:11'),(211,5,'naed','ADD','Delivery','Started delivery batch (id 5)','NAED','2026-10-24 10:30:18'),(212,5,'naed','SEND','Delivery','Sent batch DEL-2026-0005 to inventory: 2 items (222 pcs)    |    Total cost â‚±22,200.00    |    Expected sales â‚±26,640.00    |    Expected profit â‚±4,440.00  (16.7% margin)','NAED','2026-10-24 10:30:59'),(213,5,'naed','OPEN','Inventory Report','Opened Inventory Report','NAED','2026-10-24 10:31:04'),(214,5,'naed','OPEN','Inventory','Opened Inventory','NAED','2026-10-24 10:31:10'),(215,5,'naed','OPEN','Stock Threshold','Opened Stock Threshold','NAED','2026-10-24 10:31:23'),(216,5,'naed','OPEN','Products','Opened Products','NAED','2026-10-24 10:31:57'),(217,5,'naed','OPEN','Delivery Management','Opened Delivery Management','NAED','2026-10-24 10:32:24'),(218,5,'naed','ADD','Delivery','Started delivery batch (id 6)','NAED','2026-10-24 10:32:28'),(219,5,'naed','ADD','Products','Added product \'sss\'','NAED','2026-10-24 10:32:39'),(220,5,'naed','DELETE','Delivery','Deleted draft batch DEL-2026-0006','NAED','2026-10-24 10:33:49'),(221,5,'naed','OPEN','VAT','Opened VAT','NAED','2026-10-24 10:34:50'),(222,5,'naed','OPEN','Inventory Report','Opened Inventory Report','NAED','2026-10-24 10:34:56'),(223,5,'naed','OPEN','Inventory','Opened Inventory','NAED','2026-10-24 10:34:58'),(224,5,'naed','OPEN','Stock Threshold','Opened Stock Threshold','NAED','2026-10-24 10:35:20'),(225,5,'naed','EDIT','Stock Threshold','Set the low-stock warning of sss 0k to 5','NAED','2026-10-24 10:35:24'),(226,5,'naed','EDIT','Stock Threshold','Set the low-stock warning of sss 0k to 5 (all sizes)','NAED','2026-10-24 10:35:31'),(227,5,'naed','OPEN','Inventory','Opened Inventory','NAED','2026-10-24 10:35:39'),(228,5,'naed','OPEN','User Monitoring','Opened User Monitoring','NAED','2026-10-24 10:48:04'),(229,5,'naed','LOGOUT','Login','\'naed\' logged out','NAED','2026-10-24 10:48:20'),(230,5,'naed','LOGIN','Login','\'naed\' logged in','NAED','2026-10-29 21:51:38'),(231,5,'naed','OPEN','Inventory','Opened Inventory','NAED','2026-10-29 21:51:52'),(232,5,'naed','OPEN','User Monitoring','Opened User Monitoring','NAED','2026-10-29 21:52:01'),(233,5,'naed','OPEN','Stock Threshold','Opened Stock Threshold','NAED','2026-10-29 21:54:55'),(234,5,'naed','LOGOUT','Login','\'naed\' logged out','NAED','2026-10-29 21:55:46'),(235,5,'naed','LOGIN','Login','\'naed\' logged in','NAED','2026-11-04 22:47:47'),(236,5,'naed','OPEN','Inventory','Opened Inventory','NAED','2026-11-04 22:47:54'),(237,5,'naed','OPEN','VAT','Opened VAT','NAED','2026-11-04 22:55:37'),(238,5,'naed','EDIT','VAT','VAT rate changed from 12% to 100%','NAED','2026-11-04 22:55:57'),(239,5,'naed','OPEN','Inventory','Opened Inventory','NAED','2026-11-04 22:56:02'),(240,5,'naed','OPEN','VAT','Opened VAT','NAED','2026-11-04 22:56:45'),(241,5,'naed','EDIT','VAT','VAT rate changed from 100% to 15%','NAED','2026-11-04 22:56:52'),(242,5,'naed','OPEN','Inventory','Opened Inventory','NAED','2026-11-04 22:56:59'),(243,5,'naed','OPEN','User Monitoring','Opened User Monitoring','NAED','2026-11-04 23:22:52'),(244,5,'naed','OPEN','Suppliers','Opened Suppliers','NAED','2026-11-04 23:23:38'),(245,5,'naed','OPEN','Products','Opened Products','NAED','2026-11-04 23:24:07'),(246,5,'naed','ADD','Products','Added product \'cheese large\'','NAED','2026-11-04 23:25:12'),(247,5,'naed','OPEN','Delivery Management','Opened Delivery Management','NAED','2026-11-04 23:25:39'),(248,5,'naed','ADD','Delivery','Started delivery batch (id 7)','NAED','2026-11-04 23:26:19'),(249,5,'naed','OPEN','Inventory','Opened Inventory','NAED','2026-11-04 23:34:14'),(250,5,'naed','OPEN','POS','Opened POS','NAED','2026-11-04 23:38:48'),(251,5,'naed','OPEN','Discounts','Opened Discounts','NAED','2026-11-04 23:38:51'),(252,5,'naed','ADD','Discounts','Added discount \'pwd\' (20%)','NAED','2026-11-04 23:39:17'),(253,5,'naed','DEACTIVATE','Discounts','Deactivated discount \'pwd\'','NAED','2026-11-04 23:39:25'),(254,5,'naed','ACTIVATE','Discounts','Activated discount \'pwd\'','NAED','2026-11-04 23:39:27'),(255,5,'naed','DEACTIVATE','Discounts','Deactivated discount \'pwd\'','NAED','2026-11-04 23:39:28'),(256,5,'naed','ACTIVATE','Discounts','Activated discount \'pwd\'','NAED','2026-11-04 23:39:29'),(257,5,'naed','DEACTIVATE','Discounts','Deactivated discount \'pwd\'','NAED','2026-11-04 23:39:30'),(258,5,'naed','ACTIVATE','Discounts','Activated discount \'pwd\'','NAED','2026-11-04 23:39:31'),(259,5,'naed','OPEN','Stock Threshold','Opened Stock Threshold','NAED','2026-11-04 23:39:42'),(260,5,'naed','EDIT','Stock Threshold','Set the low-stock warning of cheese 0k   |   large to 0','NAED','2026-11-04 23:39:50'),(261,5,'naed','EDIT','Stock Threshold','Set the low-stock warning of cheese 0k   |   large to 60','NAED','2026-11-04 23:39:55'),(262,5,'naed','OPEN','Delivery Management','Opened Delivery Management','NAED','2026-11-04 23:40:01'),(263,5,'naed','ADD','Delivery','Started delivery batch (id 8)','NAED','2026-11-04 23:40:19'),(264,5,'naed','ADD','Products','Added product \'mm ii\'','NAED','2026-11-04 23:41:32'),(265,5,'naed','SEND','Delivery','Sent batch DEL-2026-0006 to inventory: 3 items (201 pcs)    |    Total cost â‚±162,900.00    |    Expected sales â‚±196,400.00    |    Expected profit â‚±33,500.00  (17.1% margin)','NAED','2026-11-04 23:42:54'),(266,5,'naed','ADD','Delivery','Started delivery batch (id 9)','NAED','2026-11-04 23:44:01'),(267,5,'naed','SEND','Delivery','Sent batch DEL-2026-0008 to inventory: 1 item (100 pcs)    |    Total cost â‚±9,900.00    |    Expected sales â‚±12,500.00    |    Expected profit â‚±2,600.00  (20.8% margin)','NAED','2026-11-04 23:46:03'),(268,5,'naed','OPEN','Inventory','Opened Inventory','NAED','2026-11-04 23:46:09'),(269,5,'naed','OPEN','Delivery Management','Opened Delivery Management','NAED','2026-11-04 23:47:21'),(270,5,'naed','SEND','Delivery','Sent batch DEL-2026-0007 to inventory: 1 item (100 pcs)    |    Total cost â‚±90,000.00    |    Expected sales â‚±120,000.00    |    Expected profit â‚±30,000.00  (25.0% margin)','NAED','2026-11-04 23:47:49'),(271,5,'naed','OPEN','Inventory Report','Opened Inventory Report','NAED','2026-11-04 23:47:55'),(272,5,'naed','OPEN','Inventory','Opened Inventory','NAED','2026-11-04 23:48:03'),(273,5,'naed','OPEN','Delivery Management','Opened Delivery Management','NAED','2026-11-04 23:49:20'),(274,5,'naed','ADD','Delivery','Started delivery batch (id 10)','NAED','2026-11-04 23:49:31'),(275,5,'naed','SEND','Delivery','Sent batch DEL-2026-0009 to inventory: 1 item (20 pcs)    |    Total cost â‚±600.00    |    Expected sales â‚±2,500.00    |    Expected profit â‚±1,900.00  (76.0% margin)','NAED','2026-11-04 23:49:55'),(276,5,'naed','OPEN','Inventory','Opened Inventory','NAED','2026-11-04 23:50:00'),(277,5,'naed','OPEN','Users','Opened Users','NAED','2026-11-04 23:51:08'),(278,5,'naed','OPEN','Suppliers','Opened Suppliers','NAED','2026-11-04 23:51:18'),(279,5,'naed','OPEN','Products','Opened Products','NAED','2026-11-04 23:51:21'),(280,5,'naed','OPEN','Inventory','Opened Inventory','NAED','2026-11-04 23:51:28'),(281,5,'naed','OPEN','Delivery Management','Opened Delivery Management','NAED','2026-11-04 23:53:39'),(282,5,'naed','OPEN','Suppliers','Opened Suppliers','NAED','2026-11-04 23:54:01'),(283,5,'naed','EDIT','Suppliers','Edited supplier \'carl\' (pasig)','NAED','2026-11-04 23:54:24'),(284,5,'naed','OPEN','Inventory Report','Opened Inventory Report','NAED','2026-11-04 23:54:40'),(285,5,'naed','OPEN','Delivery Management','Opened Delivery Management','NAED','2026-11-04 23:54:48'),(286,5,'naed','OPEN','Discounts','Opened Discounts','NAED','2026-11-04 23:54:51'),(287,5,'naed','OPEN','VAT','Opened VAT','NAED','2026-11-04 23:54:52'),(288,5,'naed','OPEN','Stock Threshold','Opened Stock Threshold','NAED','2026-11-04 23:55:01'),(289,5,'naed','EDIT','Stock Threshold','Set the low-stock warning of cheesdog 0k to 30','NAED','2026-11-04 23:55:25'),(290,5,'naed','EDIT','Stock Threshold','Set the low-stock warning of cheesdog 0k to 20','NAED','2026-11-04 23:55:32'),(291,5,'naed','EDIT','Stock Threshold','Set the low-stock warning of cheesdog 0k to 90','NAED','2026-11-04 23:55:39'),(292,5,'naed','EDIT','Stock Threshold','Set the low-stock warning of cheesdog 0k to 20','NAED','2026-11-04 23:56:21'),(293,5,'naed','OPEN','User Monitoring','Opened User Monitoring','NAED','2026-11-04 23:56:51'),(294,5,'naed','OPEN','Users','Opened Users','NAED','2026-11-05 00:02:00'),(295,5,'naed','DISABLE','Users','Disabled account \'naedd\'','NAED','2026-11-05 00:02:20'),(296,5,'naed','OPEN','VAT','Opened VAT','NAED','2026-11-05 00:02:48'),(297,5,'naed','EDIT','VAT','VAT rate changed from 15% to 12%','NAED','2026-11-05 00:02:55'),(298,5,'naed','OPEN','User Monitoring','Opened User Monitoring','NAED','2026-11-05 00:25:14'),(299,5,'naed','LOGOUT','Login','\'naed\' logged out','NAED','2026-11-05 00:26:09'),(300,5,'naed','LOGIN','Login','\'naed\' logged in','NAED','2026-11-06 17:12:08'),(301,5,'naed','OPEN','Users','Opened Users','NAED','2026-11-06 17:12:14'),(302,5,'naed','LOGOUT','Login','\'naed\' logged out','NAED','2026-11-06 17:12:51'),(303,5,'naed','LOGIN_FAILED','Login','Wrong password for \'naed\'','NAED','2026-11-06 17:12:55'),(304,5,'naed','LOGIN_FAILED','Login','Wrong password for \'naed\'','NAED','2026-11-06 17:12:57'),(305,5,'naed','LOGIN_FAILED','Login','Wrong password for \'naed\'','NAED','2026-11-06 17:12:59'),(306,5,'naed','LOGIN_FAILED','Login','Wrong password for \'naed\'','NAED','2026-11-06 17:13:01'),(307,5,'naed','LOGIN_FAILED','Login','Wrong password for \'naed\'','NAED','2026-11-06 17:13:02'),(308,5,'naed','LOGIN','Login','\'naed\' logged in','NAED','2026-11-08 21:52:19'),(309,5,'naed','OPEN','POS','Opened POS','NAED','2026-11-08 21:52:23'),(310,5,'naed','OPEN','POS','Opened POS','NAED','2026-11-08 21:54:42'),(311,5,'naed','SALE','POS','Sale OR-2026-000001: 12 item(s), total â‚±9,038.00','NAED','2026-11-08 21:56:20'),(312,5,'naed','SALE','POS','Sale OR-2026-000002: 6 item(s), total â‚±149,336.00','NAED','2026-11-08 21:59:53'),(313,5,'naed','SALE','POS','Sale OR-2026-000003: 1 item(s), total â‚±2.00','NAED','2026-11-08 22:01:14'),(314,5,'naed','SPECIAL_PRICE','POS','Special price on OR-2026-000003: â‚±998.00 off, approved by naed acoy (cashier naed acoy)','NAED','2026-11-08 22:01:14'),(315,5,'naed','OPEN','Inventory','Opened Inventory','NAED','2026-11-08 22:02:33'),(316,5,'naed','OPEN','Suppliers','Opened Suppliers','NAED','2026-11-08 22:02:48'),(317,5,'naed','OPEN','POS','Opened POS','NAED','2026-11-08 22:03:05'),(318,5,'naed','OPEN','Sales Report','Opened Sales Report','NAED','2026-11-08 22:03:16'),(319,5,'naed','OPEN','POS','Opened POS','NAED','2026-11-08 22:03:23'),(320,5,'naed','LOGOUT','Login','\'naed\' logged out','NAED','2026-11-08 22:04:35'),(321,5,'naed','LOGIN','Login','\'naed\' logged in','NAED','2026-11-08 23:32:53'),(322,5,'naed','OPEN','Products','Opened Products','NAED','2026-11-08 23:32:59'),(323,5,'naed','OPEN','POS','Opened POS','NAED','2026-11-08 23:33:18'),(324,5,'naed','SALE','POS','Sale OR-2026-000004: 1 item(s), total â‚±220.00','NAED','2026-11-08 23:33:51'),(325,5,'naed','OPEN','POS','Opened POS','NAED','2026-11-08 23:35:44'),(326,5,'naed','OPEN','POS','Opened POS','NAED','2026-11-08 23:35:47'),(327,5,'naed','OPEN','POS','Opened POS','NAED','2026-11-08 23:35:48'),(328,5,'naed','OPEN','POS','Opened POS','NAED','2026-11-08 23:35:49'),(329,5,'naed','OPEN','POS','Opened POS','NAED','2026-11-08 23:35:50'),(330,5,'naed','OPEN','POS','Opened POS','NAED','2026-11-08 23:35:50'),(331,5,'naed','OPEN','Delivery Management','Opened Delivery Management','NAED','2026-11-08 23:37:42'),(332,5,'naed','OPEN','Inventory','Opened Inventory','NAED','2026-11-08 23:37:47'),(333,5,'naed','OPEN','Products','Opened Products','NAED','2026-11-08 23:37:56'),(334,5,'naed','ADD','Products','Added category \'Shampoo\'','NAED','2026-11-08 23:39:32'),(335,5,'naed','ADD','Products','Added product \'head and shoulder cool mentol 12ml4902430333597\'','NAED','2026-11-08 23:40:28'),(336,5,'naed','EDIT','Products','Edited product \'head and shoulder cool mentol 12ml\'','NAED','2026-11-08 23:40:46'),(337,5,'naed','OPEN','POS','Opened POS','NAED','2026-11-08 23:40:56'),(338,5,'naed','OPEN','Delivery Management','Opened Delivery Management','NAED','2026-11-08 23:41:13'),(339,5,'naed','ADD','Delivery','Started delivery batch (id 11)','NAED','2026-11-08 23:41:22'),(340,5,'naed','SEND','Delivery','Sent batch DEL-2026-0010 to inventory: 1 item (100 pcs)    |    Total cost â‚±800.00    |    Expected sales â‚±1,000.00    |    Expected profit â‚±200.00  (20.0% margin)','NAED','2026-11-08 23:42:36'),(341,5,'naed','OPEN','POS','Opened POS','NAED','2026-11-08 23:42:40'),(342,5,'naed','SALE','POS','Sale OR-2026-000005: 1 item(s), total â‚±350.00','NAED','2026-11-08 23:44:44'),(343,5,'naed','OPEN','Users','Opened Users','NAED','2026-11-08 23:47:32'),(344,5,'naed','OPEN','Suppliers','Opened Suppliers','NAED','2026-11-08 23:47:36'),(345,5,'naed','OPEN','Products','Opened Products','NAED','2026-11-08 23:47:39'),(346,5,'naed','OPEN','Inventory','Opened Inventory','NAED','2026-11-08 23:47:41'),(347,5,'naed','OPEN','Inventory Report','Opened Inventory Report','NAED','2026-11-08 23:47:58'),(348,5,'naed','OPEN','POS','Opened POS','NAED','2026-11-08 23:48:02'),(349,5,'naed','OPEN','Delivery Management','Opened Delivery Management','NAED','2026-11-08 23:48:23'),(350,5,'naed','OPEN','VAT','Opened VAT','NAED','2026-11-08 23:48:50'),(351,5,'naed','OPEN','Discounts','Opened Discounts','NAED','2026-11-08 23:48:54'),(352,5,'naed','OPEN','Stock Threshold','Opened Stock Threshold','NAED','2026-11-08 23:49:01'),(353,5,'naed','EDIT','Stock Threshold','Set the low-stock warning of mm 0k   |   ii to 20','NAED','2026-11-08 23:49:09'),(354,5,'naed','EDIT','Stock Threshold','Set the low-stock warning of mm 0k   |   ii to 20 (all sizes)','NAED','2026-11-08 23:49:15'),(355,5,'naed','EDIT','Stock Threshold','Set the low-stock warning of head and shoulder cool mentol Shampoo   |   12ml to 20','NAED','2026-11-08 23:49:22'),(356,5,'naed','EDIT','Stock Threshold','Set the low-stock warning of head and shoulder cool mentol Shampoo   |   12ml to 20 (all sizes)','NAED','2026-11-08 23:49:27'),(357,5,'naed','OPEN','Database Settings','Opened Database Settings','NAED','2026-11-08 23:49:48'),(358,5,'naed','OPEN','User Monitoring','Opened User Monitoring','NAED','2026-11-08 23:49:51'),(359,5,'naed','OPEN','POS','Opened POS','NAED','2026-11-08 23:51:01'),(360,5,'naed','SALE','POS','Sale OR-2026-000006: 12 item(s), total â‚±84,475.00','NAED','2026-11-08 23:53:06'),(361,5,'naed','OPEN','Inventory','Opened Inventory','NAED','2026-11-08 23:54:08'),(362,5,'naed','OPEN','User Monitoring','Opened User Monitoring','NAED','2026-11-09 00:41:55'),(363,5,'naed','LOGOUT','Login','\'naed\' logged out','NAED','2026-11-09 00:42:33'),(364,5,'naed','LOGIN','Login','\'naed\' logged in','NAED','2026-11-09 02:22:08'),(365,5,'naed','OPEN','Users','Opened Users','NAED','2026-11-09 02:22:10'),(366,5,'naed','OPEN','Suppliers','Opened Suppliers','NAED','2026-11-09 02:22:19'),(367,5,'naed','OPEN','Users','Opened Users','NAED','2026-11-09 02:22:35'),(368,5,'naed','OPEN','Products','Opened Products','NAED','2026-11-09 02:23:16'),(369,5,'naed','EDIT','Products','Edited product \'burger 10mm\'','NAED','2026-11-09 02:27:49'),(370,5,'naed','OPEN','POS','Opened POS','NAED','2026-11-09 02:28:05'),(371,5,'naed','OPEN','Stock Threshold','Opened Stock Threshold','NAED','2026-11-09 02:28:50'),(372,5,'naed','OPEN','VAT','Opened VAT','NAED','2026-11-09 02:28:59'),(373,5,'naed','OPEN','Delivery Management','Opened Delivery Management','NAED','2026-11-09 02:29:08'),(374,5,'naed','OPEN','Products','Opened Products','NAED','2026-11-09 02:29:34'),(375,5,'naed','OPEN','Users','Opened Users','NAED','2026-11-09 02:30:51'),(376,5,'naed','ADD','Users','Added Cashier \'jasljsadjbsadsakjdbsajdbsajdbs\'','NAED','2026-11-09 02:32:06'),(377,5,'naed','OPEN','Suppliers','Opened Suppliers','NAED','2026-11-09 02:32:36'),(378,5,'naed','OPEN','Products','Opened Products','NAED','2026-11-09 02:32:49'),(379,5,'naed','OPEN','Inventory','Opened Inventory','NAED','2026-11-09 02:33:06'),(380,5,'naed','OPEN','Inventory Report','Opened Inventory Report','NAED','2026-11-09 02:33:37'),(381,5,'naed','OPEN','POS','Opened POS','NAED','2026-11-09 02:33:40'),(382,5,'naed','SALE','POS','Sale OR-2026-000007: 11 item(s), total â‚±2,000.00','NAED','2026-11-09 02:34:21'),(383,5,'naed','SPECIAL_PRICE','POS','Special price on OR-2026-000007: â‚±329.00 off, approved by naed acoy (cashier naed acoy)','NAED','2026-11-09 02:34:21'),(384,5,'naed','OPEN','User Monitoring','Opened User Monitoring','NAED','2026-11-09 02:34:50'),(385,5,'naed','OPEN','Database Settings','Opened Database Settings','NAED','2026-11-09 02:35:18'),(386,5,'naed','OPEN','Discounts','Opened Discounts','NAED','2026-11-09 02:35:22'),(387,5,'naed','ADD','Discounts','Added discount \'123\' (100%)','NAED','2026-11-09 02:36:12'),(388,5,'naed','OPEN','VAT','Opened VAT','NAED','2026-11-09 02:37:12'),(389,5,'naed','OPEN','Customer Accounts','Opened Customer Accounts','NAED','2026-11-09 02:38:39'),(390,5,'naed','LOGOUT','Login','\'naed\' logged out','NAED','2026-11-09 02:47:50'),(391,5,'naed','LOGIN','Login','\'naed\' logged in','NAED','2026-11-11 02:13:15'),(392,5,'naed','OPEN','POS','Opened POS','NAED','2026-11-11 02:13:23'),(393,5,'naed','LOGOUT','Login','\'naed\' logged out','NAED','2026-11-11 02:13:49');
/*!40000 ALTER TABLE `audit_trail` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `backup_log`
--

DROP TABLE IF EXISTS `backup_log`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `backup_log` (
  `id` int NOT NULL AUTO_INCREMENT,
  `file_name` varchar(200) NOT NULL,
  `file_path` varchar(500) NOT NULL,
  `file_size` bigint NOT NULL DEFAULT '0',
  `backup_type` enum('manual','auto','pre_restore') NOT NULL DEFAULT 'manual',
  `created_by` int DEFAULT NULL,
  `note` varchar(200) DEFAULT NULL,
  `created_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  KEY `idx_backup_date` (`created_at`),
  KEY `fk_backup_user` (`created_by`),
  CONSTRAINT `fk_backup_user` FOREIGN KEY (`created_by`) REFERENCES `users` (`id`) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `backup_log`
--

LOCK TABLES `backup_log` WRITE;
/*!40000 ALTER TABLE `backup_log` DISABLE KEYS */;
/*!40000 ALTER TABLE `backup_log` ENABLE KEYS */;
UNLOCK TABLES;
/*!50003 SET @saved_cs_client      = @@character_set_client */ ;
/*!50003 SET @saved_cs_results     = @@character_set_results */ ;
/*!50003 SET @saved_col_connection = @@collation_connection */ ;
/*!50003 SET character_set_client  = utf8mb4 */ ;
/*!50003 SET character_set_results = utf8mb4 */ ;
/*!50003 SET collation_connection  = utf8mb4_0900_ai_ci */ ;
/*!50003 SET @saved_sql_mode       = @@sql_mode */ ;
/*!50003 SET sql_mode              = 'ONLY_FULL_GROUP_BY,STRICT_TRANS_TABLES,NO_ZERO_IN_DATE,NO_ZERO_DATE,ERROR_FOR_DIVISION_BY_ZERO,NO_ENGINE_SUBSTITUTION' */ ;
DELIMITER ;;
/*!50003 CREATE*/ /*!50017 DEFINER=`root`@`localhost`*/ /*!50003 TRIGGER `trg_backup_log_ai` AFTER INSERT ON `backup_log` FOR EACH ROW INSERT INTO change_log (module) VALUES ('backup_log') */;;
DELIMITER ;
/*!50003 SET sql_mode              = @saved_sql_mode */ ;
/*!50003 SET character_set_client  = @saved_cs_client */ ;
/*!50003 SET character_set_results = @saved_cs_results */ ;
/*!50003 SET collation_connection  = @saved_col_connection */ ;
/*!50003 SET @saved_cs_client      = @@character_set_client */ ;
/*!50003 SET @saved_cs_results     = @@character_set_results */ ;
/*!50003 SET @saved_col_connection = @@collation_connection */ ;
/*!50003 SET character_set_client  = utf8mb4 */ ;
/*!50003 SET character_set_results = utf8mb4 */ ;
/*!50003 SET collation_connection  = utf8mb4_0900_ai_ci */ ;
/*!50003 SET @saved_sql_mode       = @@sql_mode */ ;
/*!50003 SET sql_mode              = 'ONLY_FULL_GROUP_BY,STRICT_TRANS_TABLES,NO_ZERO_IN_DATE,NO_ZERO_DATE,ERROR_FOR_DIVISION_BY_ZERO,NO_ENGINE_SUBSTITUTION' */ ;
DELIMITER ;;
/*!50003 CREATE*/ /*!50017 DEFINER=`root`@`localhost`*/ /*!50003 TRIGGER `trg_backup_log_au` AFTER UPDATE ON `backup_log` FOR EACH ROW INSERT INTO change_log (module) VALUES ('backup_log') */;;
DELIMITER ;
/*!50003 SET sql_mode              = @saved_sql_mode */ ;
/*!50003 SET character_set_client  = @saved_cs_client */ ;
/*!50003 SET character_set_results = @saved_cs_results */ ;
/*!50003 SET collation_connection  = @saved_col_connection */ ;
/*!50003 SET @saved_cs_client      = @@character_set_client */ ;
/*!50003 SET @saved_cs_results     = @@character_set_results */ ;
/*!50003 SET @saved_col_connection = @@collation_connection */ ;
/*!50003 SET character_set_client  = utf8mb4 */ ;
/*!50003 SET character_set_results = utf8mb4 */ ;
/*!50003 SET collation_connection  = utf8mb4_0900_ai_ci */ ;
/*!50003 SET @saved_sql_mode       = @@sql_mode */ ;
/*!50003 SET sql_mode              = 'ONLY_FULL_GROUP_BY,STRICT_TRANS_TABLES,NO_ZERO_IN_DATE,NO_ZERO_DATE,ERROR_FOR_DIVISION_BY_ZERO,NO_ENGINE_SUBSTITUTION' */ ;
DELIMITER ;;
/*!50003 CREATE*/ /*!50017 DEFINER=`root`@`localhost`*/ /*!50003 TRIGGER `trg_backup_log_ad` AFTER DELETE ON `backup_log` FOR EACH ROW INSERT INTO change_log (module) VALUES ('backup_log') */;;
DELIMITER ;
/*!50003 SET sql_mode              = @saved_sql_mode */ ;
/*!50003 SET character_set_client  = @saved_cs_client */ ;
/*!50003 SET character_set_results = @saved_cs_results */ ;
/*!50003 SET collation_connection  = @saved_col_connection */ ;

--
-- Table structure for table `categories`
--

DROP TABLE IF EXISTS `categories`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `categories` (
  `id` int NOT NULL AUTO_INCREMENT,
  `name` varchar(100) NOT NULL,
  `description` varchar(255) DEFAULT NULL,
  `created_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `updated_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  UNIQUE KEY `uq_categories_name` (`name`)
) ENGINE=InnoDB AUTO_INCREMENT=6 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `categories`
--

LOCK TABLES `categories` WRITE;
/*!40000 ALTER TABLE `categories` DISABLE KEYS */;
INSERT INTO `categories` VALUES (1,'Alloy','Alloy products','2026-10-22 22:59:55','2026-10-22 23:00:12'),(2,'stainless','stainless products','2026-10-22 23:00:57','2026-10-22 23:00:57'),(4,'0k',NULL,'2026-10-22 23:06:58','2026-10-22 23:06:58'),(5,'Shampoo','hair detergent','2026-11-08 23:39:32','2026-11-08 23:39:32');
/*!40000 ALTER TABLE `categories` ENABLE KEYS */;
UNLOCK TABLES;
/*!50003 SET @saved_cs_client      = @@character_set_client */ ;
/*!50003 SET @saved_cs_results     = @@character_set_results */ ;
/*!50003 SET @saved_col_connection = @@collation_connection */ ;
/*!50003 SET character_set_client  = utf8mb4 */ ;
/*!50003 SET character_set_results = utf8mb4 */ ;
/*!50003 SET collation_connection  = utf8mb4_0900_ai_ci */ ;
/*!50003 SET @saved_sql_mode       = @@sql_mode */ ;
/*!50003 SET sql_mode              = 'ONLY_FULL_GROUP_BY,STRICT_TRANS_TABLES,NO_ZERO_IN_DATE,NO_ZERO_DATE,ERROR_FOR_DIVISION_BY_ZERO,NO_ENGINE_SUBSTITUTION' */ ;
DELIMITER ;;
/*!50003 CREATE*/ /*!50017 DEFINER=`root`@`localhost`*/ /*!50003 TRIGGER `trg_categories_ai` AFTER INSERT ON `categories` FOR EACH ROW INSERT INTO change_log (module) VALUES ('categories') */;;
DELIMITER ;
/*!50003 SET sql_mode              = @saved_sql_mode */ ;
/*!50003 SET character_set_client  = @saved_cs_client */ ;
/*!50003 SET character_set_results = @saved_cs_results */ ;
/*!50003 SET collation_connection  = @saved_col_connection */ ;
/*!50003 SET @saved_cs_client      = @@character_set_client */ ;
/*!50003 SET @saved_cs_results     = @@character_set_results */ ;
/*!50003 SET @saved_col_connection = @@collation_connection */ ;
/*!50003 SET character_set_client  = utf8mb4 */ ;
/*!50003 SET character_set_results = utf8mb4 */ ;
/*!50003 SET collation_connection  = utf8mb4_0900_ai_ci */ ;
/*!50003 SET @saved_sql_mode       = @@sql_mode */ ;
/*!50003 SET sql_mode              = 'ONLY_FULL_GROUP_BY,STRICT_TRANS_TABLES,NO_ZERO_IN_DATE,NO_ZERO_DATE,ERROR_FOR_DIVISION_BY_ZERO,NO_ENGINE_SUBSTITUTION' */ ;
DELIMITER ;;
/*!50003 CREATE*/ /*!50017 DEFINER=`root`@`localhost`*/ /*!50003 TRIGGER `trg_categories_au` AFTER UPDATE ON `categories` FOR EACH ROW INSERT INTO change_log (module) VALUES ('categories') */;;
DELIMITER ;
/*!50003 SET sql_mode              = @saved_sql_mode */ ;
/*!50003 SET character_set_client  = @saved_cs_client */ ;
/*!50003 SET character_set_results = @saved_cs_results */ ;
/*!50003 SET collation_connection  = @saved_col_connection */ ;
/*!50003 SET @saved_cs_client      = @@character_set_client */ ;
/*!50003 SET @saved_cs_results     = @@character_set_results */ ;
/*!50003 SET @saved_col_connection = @@collation_connection */ ;
/*!50003 SET character_set_client  = utf8mb4 */ ;
/*!50003 SET character_set_results = utf8mb4 */ ;
/*!50003 SET collation_connection  = utf8mb4_0900_ai_ci */ ;
/*!50003 SET @saved_sql_mode       = @@sql_mode */ ;
/*!50003 SET sql_mode              = 'ONLY_FULL_GROUP_BY,STRICT_TRANS_TABLES,NO_ZERO_IN_DATE,NO_ZERO_DATE,ERROR_FOR_DIVISION_BY_ZERO,NO_ENGINE_SUBSTITUTION' */ ;
DELIMITER ;;
/*!50003 CREATE*/ /*!50017 DEFINER=`root`@`localhost`*/ /*!50003 TRIGGER `trg_categories_ad` AFTER DELETE ON `categories` FOR EACH ROW INSERT INTO change_log (module) VALUES ('categories') */;;
DELIMITER ;
/*!50003 SET sql_mode              = @saved_sql_mode */ ;
/*!50003 SET character_set_client  = @saved_cs_client */ ;
/*!50003 SET character_set_results = @saved_cs_results */ ;
/*!50003 SET collation_connection  = @saved_col_connection */ ;

--
-- Table structure for table `change_log`
--

DROP TABLE IF EXISTS `change_log`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `change_log` (
  `id` bigint NOT NULL AUTO_INCREMENT,
  `module` varchar(40) NOT NULL,
  `changed_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  KEY `idx_changelog_time` (`changed_at`)
) ENGINE=InnoDB AUTO_INCREMENT=548 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `change_log`
--

LOCK TABLES `change_log` WRITE;
/*!40000 ALTER TABLE `change_log` DISABLE KEYS */;
INSERT INTO `change_log` VALUES (547,'users','2026-11-11 02:13:15');
/*!40000 ALTER TABLE `change_log` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `customers`
--

DROP TABLE IF EXISTS `customers`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `customers` (
  `id` int NOT NULL AUTO_INCREMENT,
  `card_number` varchar(30) NOT NULL,
  `name` varchar(150) NOT NULL,
  `contact_number` varchar(30) DEFAULT NULL,
  `address` varchar(255) DEFAULT NULL,
  `points_balance` decimal(12,2) NOT NULL DEFAULT '0.00',
  `last_purchase_at` datetime DEFAULT NULL,
  `created_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `updated_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  UNIQUE KEY `uq_customers_card` (`card_number`),
  KEY `idx_customers_name` (`name`),
  KEY `idx_customers_last_purchase` (`last_purchase_at`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `customers`
--

LOCK TABLES `customers` WRITE;
/*!40000 ALTER TABLE `customers` DISABLE KEYS */;
/*!40000 ALTER TABLE `customers` ENABLE KEYS */;
UNLOCK TABLES;
/*!50003 SET @saved_cs_client      = @@character_set_client */ ;
/*!50003 SET @saved_cs_results     = @@character_set_results */ ;
/*!50003 SET @saved_col_connection = @@collation_connection */ ;
/*!50003 SET character_set_client  = utf8mb4 */ ;
/*!50003 SET character_set_results = utf8mb4 */ ;
/*!50003 SET collation_connection  = utf8mb4_0900_ai_ci */ ;
/*!50003 SET @saved_sql_mode       = @@sql_mode */ ;
/*!50003 SET sql_mode              = 'ONLY_FULL_GROUP_BY,STRICT_TRANS_TABLES,NO_ZERO_IN_DATE,NO_ZERO_DATE,ERROR_FOR_DIVISION_BY_ZERO,NO_ENGINE_SUBSTITUTION' */ ;
DELIMITER ;;
/*!50003 CREATE*/ /*!50017 DEFINER=`root`@`localhost`*/ /*!50003 TRIGGER `trg_customers_ai` AFTER INSERT ON `customers` FOR EACH ROW INSERT INTO change_log (module) VALUES ('customers') */;;
DELIMITER ;
/*!50003 SET sql_mode              = @saved_sql_mode */ ;
/*!50003 SET character_set_client  = @saved_cs_client */ ;
/*!50003 SET character_set_results = @saved_cs_results */ ;
/*!50003 SET collation_connection  = @saved_col_connection */ ;
/*!50003 SET @saved_cs_client      = @@character_set_client */ ;
/*!50003 SET @saved_cs_results     = @@character_set_results */ ;
/*!50003 SET @saved_col_connection = @@collation_connection */ ;
/*!50003 SET character_set_client  = utf8mb4 */ ;
/*!50003 SET character_set_results = utf8mb4 */ ;
/*!50003 SET collation_connection  = utf8mb4_0900_ai_ci */ ;
/*!50003 SET @saved_sql_mode       = @@sql_mode */ ;
/*!50003 SET sql_mode              = 'ONLY_FULL_GROUP_BY,STRICT_TRANS_TABLES,NO_ZERO_IN_DATE,NO_ZERO_DATE,ERROR_FOR_DIVISION_BY_ZERO,NO_ENGINE_SUBSTITUTION' */ ;
DELIMITER ;;
/*!50003 CREATE*/ /*!50017 DEFINER=`root`@`localhost`*/ /*!50003 TRIGGER `trg_customers_au` AFTER UPDATE ON `customers` FOR EACH ROW INSERT INTO change_log (module) VALUES ('customers') */;;
DELIMITER ;
/*!50003 SET sql_mode              = @saved_sql_mode */ ;
/*!50003 SET character_set_client  = @saved_cs_client */ ;
/*!50003 SET character_set_results = @saved_cs_results */ ;
/*!50003 SET collation_connection  = @saved_col_connection */ ;
/*!50003 SET @saved_cs_client      = @@character_set_client */ ;
/*!50003 SET @saved_cs_results     = @@character_set_results */ ;
/*!50003 SET @saved_col_connection = @@collation_connection */ ;
/*!50003 SET character_set_client  = utf8mb4 */ ;
/*!50003 SET character_set_results = utf8mb4 */ ;
/*!50003 SET collation_connection  = utf8mb4_0900_ai_ci */ ;
/*!50003 SET @saved_sql_mode       = @@sql_mode */ ;
/*!50003 SET sql_mode              = 'ONLY_FULL_GROUP_BY,STRICT_TRANS_TABLES,NO_ZERO_IN_DATE,NO_ZERO_DATE,ERROR_FOR_DIVISION_BY_ZERO,NO_ENGINE_SUBSTITUTION' */ ;
DELIMITER ;;
/*!50003 CREATE*/ /*!50017 DEFINER=`root`@`localhost`*/ /*!50003 TRIGGER `trg_customers_ad` AFTER DELETE ON `customers` FOR EACH ROW INSERT INTO change_log (module) VALUES ('customers') */;;
DELIMITER ;
/*!50003 SET sql_mode              = @saved_sql_mode */ ;
/*!50003 SET character_set_client  = @saved_cs_client */ ;
/*!50003 SET character_set_results = @saved_cs_results */ ;
/*!50003 SET collation_connection  = @saved_col_connection */ ;

--
-- Table structure for table `deliveries`
--

DROP TABLE IF EXISTS `deliveries`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `deliveries` (
  `id` int NOT NULL AUTO_INCREMENT,
  `batch_no` varchar(30) NOT NULL,
  `supplier_id` int NOT NULL,
  `status` enum('draft','sent') NOT NULL DEFAULT 'draft',
  `delivery_date` date NOT NULL,
  `total_cost` decimal(14,2) NOT NULL DEFAULT '0.00',
  `expected_profit` decimal(14,2) NOT NULL DEFAULT '0.00',
  `created_by` int DEFAULT NULL,
  `created_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `updated_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  `sent_by` int DEFAULT NULL,
  `sent_at` datetime DEFAULT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `uq_deliveries_batch` (`batch_no`),
  KEY `idx_deliveries_status` (`status`,`delivery_date`),
  KEY `fk_deliveries_supplier` (`supplier_id`),
  KEY `fk_deliveries_creator` (`created_by`),
  KEY `fk_deliveries_sender` (`sent_by`),
  CONSTRAINT `fk_deliveries_creator` FOREIGN KEY (`created_by`) REFERENCES `users` (`id`) ON DELETE SET NULL,
  CONSTRAINT `fk_deliveries_sender` FOREIGN KEY (`sent_by`) REFERENCES `users` (`id`) ON DELETE SET NULL,
  CONSTRAINT `fk_deliveries_supplier` FOREIGN KEY (`supplier_id`) REFERENCES `suppliers` (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=12 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `deliveries`
--

LOCK TABLES `deliveries` WRITE;
/*!40000 ALTER TABLE `deliveries` DISABLE KEYS */;
INSERT INTO `deliveries` VALUES (1,'DEL-2026-0001',6,'sent','2026-10-24',14200.00,6840.00,5,'2026-10-24 09:57:56','2026-10-24 10:02:00',5,'2026-10-24 10:02:00'),(2,'DEL-2026-0002',5,'sent','2026-10-23',35410.00,16132.00,5,'2026-10-24 10:02:56','2026-10-24 10:04:56',5,'2026-10-24 10:04:56'),(3,'DEL-2026-0003',1,'sent','2026-10-24',55500.00,12850.00,5,'2026-10-24 10:10:10','2026-10-24 10:13:37',5,'2026-10-24 10:13:37'),(4,'DEL-2026-0004',6,'sent','2026-11-30',9500.00,2430.00,5,'2026-10-24 10:18:02','2026-10-24 10:27:14',5,'2026-10-24 10:27:14'),(5,'DEL-2026-0005',6,'sent','2026-10-24',22200.00,4440.00,5,'2026-10-24 10:30:18','2026-10-24 10:30:59',5,'2026-10-24 10:30:59'),(7,'DEL-2026-0006',7,'sent','2026-10-28',162900.00,33500.00,5,'2026-11-04 23:26:19','2026-11-04 23:42:54',5,'2026-11-04 23:42:54'),(8,'DEL-2026-0007',1,'sent','2026-11-04',90000.00,30000.00,5,'2026-11-04 23:40:19','2026-11-04 23:47:49',5,'2026-11-04 23:47:49'),(9,'DEL-2026-0008',7,'sent','2026-11-04',9900.00,2600.00,5,'2026-11-04 23:44:01','2026-11-04 23:46:03',5,'2026-11-04 23:46:03'),(10,'DEL-2026-0009',5,'sent','2026-11-04',600.00,1900.00,5,'2026-11-04 23:49:31','2026-11-04 23:49:55',5,'2026-11-04 23:49:55'),(11,'DEL-2026-0010',6,'sent','2026-11-08',800.00,200.00,5,'2026-11-08 23:41:22','2026-11-08 23:42:36',5,'2026-11-08 23:42:36');
/*!40000 ALTER TABLE `deliveries` ENABLE KEYS */;
UNLOCK TABLES;
/*!50003 SET @saved_cs_client      = @@character_set_client */ ;
/*!50003 SET @saved_cs_results     = @@character_set_results */ ;
/*!50003 SET @saved_col_connection = @@collation_connection */ ;
/*!50003 SET character_set_client  = utf8mb4 */ ;
/*!50003 SET character_set_results = utf8mb4 */ ;
/*!50003 SET collation_connection  = utf8mb4_0900_ai_ci */ ;
/*!50003 SET @saved_sql_mode       = @@sql_mode */ ;
/*!50003 SET sql_mode              = 'ONLY_FULL_GROUP_BY,STRICT_TRANS_TABLES,NO_ZERO_IN_DATE,NO_ZERO_DATE,ERROR_FOR_DIVISION_BY_ZERO,NO_ENGINE_SUBSTITUTION' */ ;
DELIMITER ;;
/*!50003 CREATE*/ /*!50017 DEFINER=`root`@`localhost`*/ /*!50003 TRIGGER `trg_deliveries_ai` AFTER INSERT ON `deliveries` FOR EACH ROW INSERT INTO change_log (module) VALUES ('deliveries') */;;
DELIMITER ;
/*!50003 SET sql_mode              = @saved_sql_mode */ ;
/*!50003 SET character_set_client  = @saved_cs_client */ ;
/*!50003 SET character_set_results = @saved_cs_results */ ;
/*!50003 SET collation_connection  = @saved_col_connection */ ;
/*!50003 SET @saved_cs_client      = @@character_set_client */ ;
/*!50003 SET @saved_cs_results     = @@character_set_results */ ;
/*!50003 SET @saved_col_connection = @@collation_connection */ ;
/*!50003 SET character_set_client  = utf8mb4 */ ;
/*!50003 SET character_set_results = utf8mb4 */ ;
/*!50003 SET collation_connection  = utf8mb4_0900_ai_ci */ ;
/*!50003 SET @saved_sql_mode       = @@sql_mode */ ;
/*!50003 SET sql_mode              = 'ONLY_FULL_GROUP_BY,STRICT_TRANS_TABLES,NO_ZERO_IN_DATE,NO_ZERO_DATE,ERROR_FOR_DIVISION_BY_ZERO,NO_ENGINE_SUBSTITUTION' */ ;
DELIMITER ;;
/*!50003 CREATE*/ /*!50017 DEFINER=`root`@`localhost`*/ /*!50003 TRIGGER `trg_deliveries_au` AFTER UPDATE ON `deliveries` FOR EACH ROW INSERT INTO change_log (module) VALUES ('deliveries') */;;
DELIMITER ;
/*!50003 SET sql_mode              = @saved_sql_mode */ ;
/*!50003 SET character_set_client  = @saved_cs_client */ ;
/*!50003 SET character_set_results = @saved_cs_results */ ;
/*!50003 SET collation_connection  = @saved_col_connection */ ;
/*!50003 SET @saved_cs_client      = @@character_set_client */ ;
/*!50003 SET @saved_cs_results     = @@character_set_results */ ;
/*!50003 SET @saved_col_connection = @@collation_connection */ ;
/*!50003 SET character_set_client  = utf8mb4 */ ;
/*!50003 SET character_set_results = utf8mb4 */ ;
/*!50003 SET collation_connection  = utf8mb4_0900_ai_ci */ ;
/*!50003 SET @saved_sql_mode       = @@sql_mode */ ;
/*!50003 SET sql_mode              = 'ONLY_FULL_GROUP_BY,STRICT_TRANS_TABLES,NO_ZERO_IN_DATE,NO_ZERO_DATE,ERROR_FOR_DIVISION_BY_ZERO,NO_ENGINE_SUBSTITUTION' */ ;
DELIMITER ;;
/*!50003 CREATE*/ /*!50017 DEFINER=`root`@`localhost`*/ /*!50003 TRIGGER `trg_deliveries_ad` AFTER DELETE ON `deliveries` FOR EACH ROW INSERT INTO change_log (module) VALUES ('deliveries') */;;
DELIMITER ;
/*!50003 SET sql_mode              = @saved_sql_mode */ ;
/*!50003 SET character_set_client  = @saved_cs_client */ ;
/*!50003 SET character_set_results = @saved_cs_results */ ;
/*!50003 SET collation_connection  = @saved_col_connection */ ;

--
-- Table structure for table `delivery_items`
--

DROP TABLE IF EXISTS `delivery_items`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `delivery_items` (
  `id` int NOT NULL AUTO_INCREMENT,
  `delivery_id` int NOT NULL,
  `product_id` int NOT NULL,
  `qty` int NOT NULL,
  `qty_remaining` int NOT NULL DEFAULT '0',
  `cost_price` decimal(14,4) NOT NULL,
  `cost_mode` enum('piece','pack') NOT NULL DEFAULT 'piece',
  `pack_size` int NOT NULL DEFAULT '1',
  `entered_cost` decimal(14,2) NOT NULL DEFAULT '0.00',
  `selling_price` decimal(12,2) NOT NULL,
  `line_cost` decimal(14,2) NOT NULL,
  `expected_profit` decimal(14,2) NOT NULL,
  PRIMARY KEY (`id`),
  KEY `idx_ditems_delivery` (`delivery_id`),
  KEY `idx_ditems_product` (`product_id`),
  CONSTRAINT `fk_ditems_delivery` FOREIGN KEY (`delivery_id`) REFERENCES `deliveries` (`id`) ON DELETE CASCADE,
  CONSTRAINT `fk_ditems_product` FOREIGN KEY (`product_id`) REFERENCES `products` (`id`),
  CONSTRAINT `chk_ditems_qty` CHECK ((`qty` > 0))
) ENGINE=InnoDB AUTO_INCREMENT=24 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `delivery_items`
--

LOCK TABLES `delivery_items` WRITE;
/*!40000 ALTER TABLE `delivery_items` DISABLE KEYS */;
INSERT INTO `delivery_items` VALUES (1,1,2,100,96,100.0000,'piece',1,100.00,150.00,10000.00,5000.00),(2,1,1,20,16,210.0000,'piece',1,210.00,302.00,4200.00,1840.00),(3,2,2,100,100,100.0000,'piece',1,100.00,150.00,10000.00,5000.00),(4,2,1,121,121,210.0000,'piece',1,210.00,302.00,25410.00,11132.00),(5,3,5,20,14,100.0000,'piece',1,100.00,120.00,2000.00,400.00),(6,3,4,10,4,10.0000,'piece',1,10.00,11.00,100.00,10.00),(7,3,3,20,15,210.0000,'piece',1,210.00,220.00,4200.00,200.00),(8,3,2,120,120,200.0000,'piece',1,200.00,210.00,24000.00,1200.00),(9,3,1,120,120,210.0000,'piece',1,210.00,302.00,25200.00,11040.00),(10,4,7,10,6,10.0000,'piece',1,10.00,11.00,100.00,10.00),(11,4,5,10,10,100.0000,'piece',1,100.00,120.00,1000.00,200.00),(12,4,4,20,20,10.0000,'piece',1,10.00,11.00,200.00,20.00),(13,4,3,20,20,210.0000,'piece',1,210.00,220.00,4200.00,200.00),(14,4,6,20,15,200.0000,'piece',1,200.00,300.00,4000.00,2000.00),(15,5,16,20,16,100.0000,'piece',1,100.00,120.00,2000.00,400.00),(16,5,18,202,198,100.0000,'piece',1,100.00,120.00,20200.00,4040.00),(17,7,20,22,0,900.0000,'piece',1,900.00,1200.00,19800.00,6600.00),(18,7,21,90,85,700.0000,'piece',1,700.00,900.00,63000.00,18000.00),(19,7,19,89,0,900.0000,'piece',1,900.00,1000.00,80100.00,8900.00),(20,9,5,100,100,99.0000,'piece',1,99.00,125.00,9900.00,2600.00),(21,8,20,100,0,900.0000,'piece',1,900.00,1200.00,90000.00,30000.00),(22,10,5,20,20,30.0000,'piece',1,30.00,125.00,600.00,1900.00),(23,11,22,100,62,8.0000,'piece',1,8.00,10.00,800.00,200.00);
/*!40000 ALTER TABLE `delivery_items` ENABLE KEYS */;
UNLOCK TABLES;
/*!50003 SET @saved_cs_client      = @@character_set_client */ ;
/*!50003 SET @saved_cs_results     = @@character_set_results */ ;
/*!50003 SET @saved_col_connection = @@collation_connection */ ;
/*!50003 SET character_set_client  = utf8mb4 */ ;
/*!50003 SET character_set_results = utf8mb4 */ ;
/*!50003 SET collation_connection  = utf8mb4_0900_ai_ci */ ;
/*!50003 SET @saved_sql_mode       = @@sql_mode */ ;
/*!50003 SET sql_mode              = 'ONLY_FULL_GROUP_BY,STRICT_TRANS_TABLES,NO_ZERO_IN_DATE,NO_ZERO_DATE,ERROR_FOR_DIVISION_BY_ZERO,NO_ENGINE_SUBSTITUTION' */ ;
DELIMITER ;;
/*!50003 CREATE*/ /*!50017 DEFINER=`root`@`localhost`*/ /*!50003 TRIGGER `trg_delivery_items_ai` AFTER INSERT ON `delivery_items` FOR EACH ROW INSERT INTO change_log (module) VALUES ('delivery_items') */;;
DELIMITER ;
/*!50003 SET sql_mode              = @saved_sql_mode */ ;
/*!50003 SET character_set_client  = @saved_cs_client */ ;
/*!50003 SET character_set_results = @saved_cs_results */ ;
/*!50003 SET collation_connection  = @saved_col_connection */ ;
/*!50003 SET @saved_cs_client      = @@character_set_client */ ;
/*!50003 SET @saved_cs_results     = @@character_set_results */ ;
/*!50003 SET @saved_col_connection = @@collation_connection */ ;
/*!50003 SET character_set_client  = utf8mb4 */ ;
/*!50003 SET character_set_results = utf8mb4 */ ;
/*!50003 SET collation_connection  = utf8mb4_0900_ai_ci */ ;
/*!50003 SET @saved_sql_mode       = @@sql_mode */ ;
/*!50003 SET sql_mode              = 'ONLY_FULL_GROUP_BY,STRICT_TRANS_TABLES,NO_ZERO_IN_DATE,NO_ZERO_DATE,ERROR_FOR_DIVISION_BY_ZERO,NO_ENGINE_SUBSTITUTION' */ ;
DELIMITER ;;
/*!50003 CREATE*/ /*!50017 DEFINER=`root`@`localhost`*/ /*!50003 TRIGGER `trg_delivery_items_au` AFTER UPDATE ON `delivery_items` FOR EACH ROW INSERT INTO change_log (module) VALUES ('delivery_items') */;;
DELIMITER ;
/*!50003 SET sql_mode              = @saved_sql_mode */ ;
/*!50003 SET character_set_client  = @saved_cs_client */ ;
/*!50003 SET character_set_results = @saved_cs_results */ ;
/*!50003 SET collation_connection  = @saved_col_connection */ ;
/*!50003 SET @saved_cs_client      = @@character_set_client */ ;
/*!50003 SET @saved_cs_results     = @@character_set_results */ ;
/*!50003 SET @saved_col_connection = @@collation_connection */ ;
/*!50003 SET character_set_client  = utf8mb4 */ ;
/*!50003 SET character_set_results = utf8mb4 */ ;
/*!50003 SET collation_connection  = utf8mb4_0900_ai_ci */ ;
/*!50003 SET @saved_sql_mode       = @@sql_mode */ ;
/*!50003 SET sql_mode              = 'ONLY_FULL_GROUP_BY,STRICT_TRANS_TABLES,NO_ZERO_IN_DATE,NO_ZERO_DATE,ERROR_FOR_DIVISION_BY_ZERO,NO_ENGINE_SUBSTITUTION' */ ;
DELIMITER ;;
/*!50003 CREATE*/ /*!50017 DEFINER=`root`@`localhost`*/ /*!50003 TRIGGER `trg_delivery_items_ad` AFTER DELETE ON `delivery_items` FOR EACH ROW INSERT INTO change_log (module) VALUES ('delivery_items') */;;
DELIMITER ;
/*!50003 SET sql_mode              = @saved_sql_mode */ ;
/*!50003 SET character_set_client  = @saved_cs_client */ ;
/*!50003 SET character_set_results = @saved_cs_results */ ;
/*!50003 SET collation_connection  = @saved_col_connection */ ;

--
-- Table structure for table `discounts`
--

DROP TABLE IF EXISTS `discounts`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `discounts` (
  `id` int NOT NULL AUTO_INCREMENT,
  `name` varchar(100) NOT NULL,
  `percentage` decimal(5,2) NOT NULL,
  `status` enum('active','disabled') NOT NULL DEFAULT 'active',
  `created_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `updated_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  UNIQUE KEY `uq_discounts_name` (`name`),
  CONSTRAINT `chk_discount_pct` CHECK (((`percentage` >= 0) and (`percentage` <= 100)))
) ENGINE=InnoDB AUTO_INCREMENT=3 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `discounts`
--

LOCK TABLES `discounts` WRITE;
/*!40000 ALTER TABLE `discounts` DISABLE KEYS */;
INSERT INTO `discounts` VALUES (1,'pwd',20.00,'active','2026-11-04 23:39:17','2026-11-04 23:39:31'),(2,'123',100.00,'active','2026-11-09 02:36:12','2026-11-09 02:36:12');
/*!40000 ALTER TABLE `discounts` ENABLE KEYS */;
UNLOCK TABLES;
/*!50003 SET @saved_cs_client      = @@character_set_client */ ;
/*!50003 SET @saved_cs_results     = @@character_set_results */ ;
/*!50003 SET @saved_col_connection = @@collation_connection */ ;
/*!50003 SET character_set_client  = utf8mb4 */ ;
/*!50003 SET character_set_results = utf8mb4 */ ;
/*!50003 SET collation_connection  = utf8mb4_0900_ai_ci */ ;
/*!50003 SET @saved_sql_mode       = @@sql_mode */ ;
/*!50003 SET sql_mode              = 'ONLY_FULL_GROUP_BY,STRICT_TRANS_TABLES,NO_ZERO_IN_DATE,NO_ZERO_DATE,ERROR_FOR_DIVISION_BY_ZERO,NO_ENGINE_SUBSTITUTION' */ ;
DELIMITER ;;
/*!50003 CREATE*/ /*!50017 DEFINER=`root`@`localhost`*/ /*!50003 TRIGGER `trg_discounts_ai` AFTER INSERT ON `discounts` FOR EACH ROW INSERT INTO change_log (module) VALUES ('discounts') */;;
DELIMITER ;
/*!50003 SET sql_mode              = @saved_sql_mode */ ;
/*!50003 SET character_set_client  = @saved_cs_client */ ;
/*!50003 SET character_set_results = @saved_cs_results */ ;
/*!50003 SET collation_connection  = @saved_col_connection */ ;
/*!50003 SET @saved_cs_client      = @@character_set_client */ ;
/*!50003 SET @saved_cs_results     = @@character_set_results */ ;
/*!50003 SET @saved_col_connection = @@collation_connection */ ;
/*!50003 SET character_set_client  = utf8mb4 */ ;
/*!50003 SET character_set_results = utf8mb4 */ ;
/*!50003 SET collation_connection  = utf8mb4_0900_ai_ci */ ;
/*!50003 SET @saved_sql_mode       = @@sql_mode */ ;
/*!50003 SET sql_mode              = 'ONLY_FULL_GROUP_BY,STRICT_TRANS_TABLES,NO_ZERO_IN_DATE,NO_ZERO_DATE,ERROR_FOR_DIVISION_BY_ZERO,NO_ENGINE_SUBSTITUTION' */ ;
DELIMITER ;;
/*!50003 CREATE*/ /*!50017 DEFINER=`root`@`localhost`*/ /*!50003 TRIGGER `trg_discounts_au` AFTER UPDATE ON `discounts` FOR EACH ROW INSERT INTO change_log (module) VALUES ('discounts') */;;
DELIMITER ;
/*!50003 SET sql_mode              = @saved_sql_mode */ ;
/*!50003 SET character_set_client  = @saved_cs_client */ ;
/*!50003 SET character_set_results = @saved_cs_results */ ;
/*!50003 SET collation_connection  = @saved_col_connection */ ;
/*!50003 SET @saved_cs_client      = @@character_set_client */ ;
/*!50003 SET @saved_cs_results     = @@character_set_results */ ;
/*!50003 SET @saved_col_connection = @@collation_connection */ ;
/*!50003 SET character_set_client  = utf8mb4 */ ;
/*!50003 SET character_set_results = utf8mb4 */ ;
/*!50003 SET collation_connection  = utf8mb4_0900_ai_ci */ ;
/*!50003 SET @saved_sql_mode       = @@sql_mode */ ;
/*!50003 SET sql_mode              = 'ONLY_FULL_GROUP_BY,STRICT_TRANS_TABLES,NO_ZERO_IN_DATE,NO_ZERO_DATE,ERROR_FOR_DIVISION_BY_ZERO,NO_ENGINE_SUBSTITUTION' */ ;
DELIMITER ;;
/*!50003 CREATE*/ /*!50017 DEFINER=`root`@`localhost`*/ /*!50003 TRIGGER `trg_discounts_ad` AFTER DELETE ON `discounts` FOR EACH ROW INSERT INTO change_log (module) VALUES ('discounts') */;;
DELIMITER ;
/*!50003 SET sql_mode              = @saved_sql_mode */ ;
/*!50003 SET character_set_client  = @saved_cs_client */ ;
/*!50003 SET character_set_results = @saved_cs_results */ ;
/*!50003 SET collation_connection  = @saved_col_connection */ ;

--
-- Table structure for table `inventory_movements`
--

DROP TABLE IF EXISTS `inventory_movements`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `inventory_movements` (
  `id` bigint NOT NULL AUTO_INCREMENT,
  `product_id` int NOT NULL,
  `movement_type` enum('delivery_in','sale_out','loss_out','adjustment') NOT NULL,
  `qty_change` int NOT NULL,
  `balance_after` int NOT NULL,
  `reference_type` varchar(30) DEFAULT NULL,
  `reference_id` int DEFAULT NULL,
  `user_id` int DEFAULT NULL,
  `created_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  KEY `idx_moves_product` (`product_id`,`created_at`),
  KEY `idx_moves_date` (`created_at`),
  CONSTRAINT `fk_moves_product` FOREIGN KEY (`product_id`) REFERENCES `products` (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=68 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `inventory_movements`
--

LOCK TABLES `inventory_movements` WRITE;
/*!40000 ALTER TABLE `inventory_movements` DISABLE KEYS */;
INSERT INTO `inventory_movements` VALUES (1,2,'delivery_in',100,100,'delivery',1,5,'2026-10-24 10:02:00'),(2,1,'delivery_in',20,20,'delivery',1,5,'2026-10-24 10:02:00'),(3,2,'delivery_in',100,200,'delivery',2,5,'2026-10-24 10:04:56'),(4,1,'delivery_in',121,141,'delivery',2,5,'2026-10-24 10:04:56'),(5,5,'delivery_in',20,20,'delivery',3,5,'2026-10-24 10:13:37'),(6,4,'delivery_in',10,10,'delivery',3,5,'2026-10-24 10:13:37'),(7,3,'delivery_in',20,20,'delivery',3,5,'2026-10-24 10:13:37'),(8,2,'delivery_in',120,320,'delivery',3,5,'2026-10-24 10:13:37'),(9,1,'delivery_in',120,261,'delivery',3,5,'2026-10-24 10:13:37'),(10,7,'delivery_in',10,10,'delivery',4,5,'2026-10-24 10:27:14'),(11,5,'delivery_in',10,30,'delivery',4,5,'2026-10-24 10:27:14'),(12,4,'delivery_in',20,30,'delivery',4,5,'2026-10-24 10:27:14'),(13,3,'delivery_in',20,40,'delivery',4,5,'2026-10-24 10:27:14'),(14,6,'delivery_in',20,20,'delivery',4,5,'2026-10-24 10:27:14'),(15,16,'delivery_in',20,20,'delivery',5,5,'2026-10-24 10:30:59'),(16,18,'delivery_in',202,202,'delivery',5,5,'2026-10-24 10:30:59'),(17,20,'delivery_in',22,22,'delivery',7,5,'2026-11-04 23:42:54'),(18,21,'delivery_in',90,90,'delivery',7,5,'2026-11-04 23:42:54'),(19,19,'delivery_in',89,89,'delivery',7,5,'2026-11-04 23:42:54'),(20,5,'delivery_in',100,130,'delivery',9,5,'2026-11-04 23:46:03'),(21,20,'delivery_in',100,122,'delivery',8,5,'2026-11-04 23:47:49'),(22,5,'delivery_in',20,150,'delivery',10,5,'2026-11-04 23:49:55'),(23,1,'sale_out',-2,259,'sale',1,5,'2026-11-08 21:56:20'),(24,2,'sale_out',-2,318,'sale',1,5,'2026-11-08 21:56:20'),(25,3,'sale_out',-2,38,'sale',1,5,'2026-11-08 21:56:20'),(26,4,'sale_out',-2,28,'sale',1,5,'2026-11-08 21:56:20'),(27,5,'sale_out',-2,148,'sale',1,5,'2026-11-08 21:56:20'),(28,6,'sale_out',-2,18,'sale',1,5,'2026-11-08 21:56:20'),(29,7,'sale_out',-2,8,'sale',1,5,'2026-11-08 21:56:20'),(30,16,'sale_out',-2,18,'sale',1,5,'2026-11-08 21:56:20'),(31,18,'sale_out',-2,200,'sale',1,5,'2026-11-08 21:56:20'),(32,19,'sale_out',-2,87,'sale',1,5,'2026-11-08 21:56:20'),(33,20,'sale_out',-2,120,'sale',1,5,'2026-11-08 21:56:20'),(34,21,'sale_out',-2,88,'sale',1,5,'2026-11-08 21:56:20'),(35,4,'sale_out',-1,27,'sale',2,5,'2026-11-08 21:59:53'),(36,5,'sale_out',-1,147,'sale',2,5,'2026-11-08 21:59:53'),(37,6,'sale_out',-1,17,'sale',2,5,'2026-11-08 21:59:53'),(38,19,'sale_out',-4,83,'sale',2,5,'2026-11-08 21:59:53'),(39,20,'sale_out',-120,0,'sale',2,5,'2026-11-08 21:59:53'),(40,21,'sale_out',-1,87,'sale',2,5,'2026-11-08 21:59:53'),(41,19,'sale_out',-1,82,'sale',3,5,'2026-11-08 22:01:14'),(42,3,'sale_out',-1,37,'sale',4,5,'2026-11-08 23:33:51'),(43,22,'delivery_in',100,100,'delivery',11,5,'2026-11-08 23:42:36'),(44,22,'sale_out',-35,65,'sale',5,5,'2026-11-08 23:44:44'),(45,1,'sale_out',-1,258,'sale',6,5,'2026-11-08 23:53:06'),(46,2,'sale_out',-1,317,'sale',6,5,'2026-11-08 23:53:06'),(47,3,'sale_out',-1,36,'sale',6,5,'2026-11-08 23:53:06'),(48,4,'sale_out',-2,25,'sale',6,5,'2026-11-08 23:53:06'),(49,5,'sale_out',-2,145,'sale',6,5,'2026-11-08 23:53:06'),(50,6,'sale_out',-1,16,'sale',6,5,'2026-11-08 23:53:06'),(51,7,'sale_out',-1,7,'sale',6,5,'2026-11-08 23:53:06'),(52,16,'sale_out',-1,17,'sale',6,5,'2026-11-08 23:53:06'),(53,18,'sale_out',-1,199,'sale',6,5,'2026-11-08 23:53:06'),(54,19,'sale_out',-82,0,'sale',6,5,'2026-11-08 23:53:06'),(55,21,'sale_out',-1,86,'sale',6,5,'2026-11-08 23:53:06'),(56,22,'sale_out',-2,63,'sale',6,5,'2026-11-08 23:53:06'),(57,1,'sale_out',-1,257,'sale',7,5,'2026-11-09 02:34:21'),(58,2,'sale_out',-1,316,'sale',7,5,'2026-11-09 02:34:21'),(59,3,'sale_out',-1,35,'sale',7,5,'2026-11-09 02:34:21'),(60,4,'sale_out',-1,24,'sale',7,5,'2026-11-09 02:34:21'),(61,5,'sale_out',-1,144,'sale',7,5,'2026-11-09 02:34:21'),(62,6,'sale_out',-1,15,'sale',7,5,'2026-11-09 02:34:21'),(63,7,'sale_out',-1,6,'sale',7,5,'2026-11-09 02:34:21'),(64,16,'sale_out',-1,16,'sale',7,5,'2026-11-09 02:34:21'),(65,18,'sale_out',-1,198,'sale',7,5,'2026-11-09 02:34:21'),(66,21,'sale_out',-1,85,'sale',7,5,'2026-11-09 02:34:21'),(67,22,'sale_out',-1,62,'sale',7,5,'2026-11-09 02:34:21');
/*!40000 ALTER TABLE `inventory_movements` ENABLE KEYS */;
UNLOCK TABLES;
/*!50003 SET @saved_cs_client      = @@character_set_client */ ;
/*!50003 SET @saved_cs_results     = @@character_set_results */ ;
/*!50003 SET @saved_col_connection = @@collation_connection */ ;
/*!50003 SET character_set_client  = utf8mb4 */ ;
/*!50003 SET character_set_results = utf8mb4 */ ;
/*!50003 SET collation_connection  = utf8mb4_0900_ai_ci */ ;
/*!50003 SET @saved_sql_mode       = @@sql_mode */ ;
/*!50003 SET sql_mode              = 'ONLY_FULL_GROUP_BY,STRICT_TRANS_TABLES,NO_ZERO_IN_DATE,NO_ZERO_DATE,ERROR_FOR_DIVISION_BY_ZERO,NO_ENGINE_SUBSTITUTION' */ ;
DELIMITER ;;
/*!50003 CREATE*/ /*!50017 DEFINER=`root`@`localhost`*/ /*!50003 TRIGGER `trg_inventory_movements_ai` AFTER INSERT ON `inventory_movements` FOR EACH ROW INSERT INTO change_log (module) VALUES ('inventory_movements') */;;
DELIMITER ;
/*!50003 SET sql_mode              = @saved_sql_mode */ ;
/*!50003 SET character_set_client  = @saved_cs_client */ ;
/*!50003 SET character_set_results = @saved_cs_results */ ;
/*!50003 SET collation_connection  = @saved_col_connection */ ;
/*!50003 SET @saved_cs_client      = @@character_set_client */ ;
/*!50003 SET @saved_cs_results     = @@character_set_results */ ;
/*!50003 SET @saved_col_connection = @@collation_connection */ ;
/*!50003 SET character_set_client  = utf8mb4 */ ;
/*!50003 SET character_set_results = utf8mb4 */ ;
/*!50003 SET collation_connection  = utf8mb4_0900_ai_ci */ ;
/*!50003 SET @saved_sql_mode       = @@sql_mode */ ;
/*!50003 SET sql_mode              = 'ONLY_FULL_GROUP_BY,STRICT_TRANS_TABLES,NO_ZERO_IN_DATE,NO_ZERO_DATE,ERROR_FOR_DIVISION_BY_ZERO,NO_ENGINE_SUBSTITUTION' */ ;
DELIMITER ;;
/*!50003 CREATE*/ /*!50017 DEFINER=`root`@`localhost`*/ /*!50003 TRIGGER `trg_inventory_movements_au` AFTER UPDATE ON `inventory_movements` FOR EACH ROW INSERT INTO change_log (module) VALUES ('inventory_movements') */;;
DELIMITER ;
/*!50003 SET sql_mode              = @saved_sql_mode */ ;
/*!50003 SET character_set_client  = @saved_cs_client */ ;
/*!50003 SET character_set_results = @saved_cs_results */ ;
/*!50003 SET collation_connection  = @saved_col_connection */ ;
/*!50003 SET @saved_cs_client      = @@character_set_client */ ;
/*!50003 SET @saved_cs_results     = @@character_set_results */ ;
/*!50003 SET @saved_col_connection = @@collation_connection */ ;
/*!50003 SET character_set_client  = utf8mb4 */ ;
/*!50003 SET character_set_results = utf8mb4 */ ;
/*!50003 SET collation_connection  = utf8mb4_0900_ai_ci */ ;
/*!50003 SET @saved_sql_mode       = @@sql_mode */ ;
/*!50003 SET sql_mode              = 'ONLY_FULL_GROUP_BY,STRICT_TRANS_TABLES,NO_ZERO_IN_DATE,NO_ZERO_DATE,ERROR_FOR_DIVISION_BY_ZERO,NO_ENGINE_SUBSTITUTION' */ ;
DELIMITER ;;
/*!50003 CREATE*/ /*!50017 DEFINER=`root`@`localhost`*/ /*!50003 TRIGGER `trg_inventory_movements_ad` AFTER DELETE ON `inventory_movements` FOR EACH ROW INSERT INTO change_log (module) VALUES ('inventory_movements') */;;
DELIMITER ;
/*!50003 SET sql_mode              = @saved_sql_mode */ ;
/*!50003 SET character_set_client  = @saved_cs_client */ ;
/*!50003 SET character_set_results = @saved_cs_results */ ;
/*!50003 SET collation_connection  = @saved_col_connection */ ;

--
-- Table structure for table `loss_records`
--

DROP TABLE IF EXISTS `loss_records`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `loss_records` (
  `id` int NOT NULL AUTO_INCREMENT,
  `product_id` int NOT NULL,
  `qty` int NOT NULL,
  `reason` enum('damaged','rusted','lost','stolen','cutting_waste','other') NOT NULL DEFAULT 'other',
  `cost_value` decimal(14,2) NOT NULL DEFAULT '0.00',
  `note` varchar(255) DEFAULT NULL,
  `recorded_by` int DEFAULT NULL,
  `created_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  KEY `idx_loss_date` (`created_at`),
  KEY `fk_loss_product` (`product_id`),
  KEY `fk_loss_user` (`recorded_by`),
  CONSTRAINT `fk_loss_product` FOREIGN KEY (`product_id`) REFERENCES `products` (`id`),
  CONSTRAINT `fk_loss_user` FOREIGN KEY (`recorded_by`) REFERENCES `users` (`id`) ON DELETE SET NULL,
  CONSTRAINT `chk_loss_qty` CHECK ((`qty` > 0))
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `loss_records`
--

LOCK TABLES `loss_records` WRITE;
/*!40000 ALTER TABLE `loss_records` DISABLE KEYS */;
/*!40000 ALTER TABLE `loss_records` ENABLE KEYS */;
UNLOCK TABLES;
/*!50003 SET @saved_cs_client      = @@character_set_client */ ;
/*!50003 SET @saved_cs_results     = @@character_set_results */ ;
/*!50003 SET @saved_col_connection = @@collation_connection */ ;
/*!50003 SET character_set_client  = utf8mb4 */ ;
/*!50003 SET character_set_results = utf8mb4 */ ;
/*!50003 SET collation_connection  = utf8mb4_0900_ai_ci */ ;
/*!50003 SET @saved_sql_mode       = @@sql_mode */ ;
/*!50003 SET sql_mode              = 'ONLY_FULL_GROUP_BY,STRICT_TRANS_TABLES,NO_ZERO_IN_DATE,NO_ZERO_DATE,ERROR_FOR_DIVISION_BY_ZERO,NO_ENGINE_SUBSTITUTION' */ ;
DELIMITER ;;
/*!50003 CREATE*/ /*!50017 DEFINER=`root`@`localhost`*/ /*!50003 TRIGGER `trg_loss_records_ai` AFTER INSERT ON `loss_records` FOR EACH ROW INSERT INTO change_log (module) VALUES ('loss_records') */;;
DELIMITER ;
/*!50003 SET sql_mode              = @saved_sql_mode */ ;
/*!50003 SET character_set_client  = @saved_cs_client */ ;
/*!50003 SET character_set_results = @saved_cs_results */ ;
/*!50003 SET collation_connection  = @saved_col_connection */ ;
/*!50003 SET @saved_cs_client      = @@character_set_client */ ;
/*!50003 SET @saved_cs_results     = @@character_set_results */ ;
/*!50003 SET @saved_col_connection = @@collation_connection */ ;
/*!50003 SET character_set_client  = utf8mb4 */ ;
/*!50003 SET character_set_results = utf8mb4 */ ;
/*!50003 SET collation_connection  = utf8mb4_0900_ai_ci */ ;
/*!50003 SET @saved_sql_mode       = @@sql_mode */ ;
/*!50003 SET sql_mode              = 'ONLY_FULL_GROUP_BY,STRICT_TRANS_TABLES,NO_ZERO_IN_DATE,NO_ZERO_DATE,ERROR_FOR_DIVISION_BY_ZERO,NO_ENGINE_SUBSTITUTION' */ ;
DELIMITER ;;
/*!50003 CREATE*/ /*!50017 DEFINER=`root`@`localhost`*/ /*!50003 TRIGGER `trg_loss_records_au` AFTER UPDATE ON `loss_records` FOR EACH ROW INSERT INTO change_log (module) VALUES ('loss_records') */;;
DELIMITER ;
/*!50003 SET sql_mode              = @saved_sql_mode */ ;
/*!50003 SET character_set_client  = @saved_cs_client */ ;
/*!50003 SET character_set_results = @saved_cs_results */ ;
/*!50003 SET collation_connection  = @saved_col_connection */ ;
/*!50003 SET @saved_cs_client      = @@character_set_client */ ;
/*!50003 SET @saved_cs_results     = @@character_set_results */ ;
/*!50003 SET @saved_col_connection = @@collation_connection */ ;
/*!50003 SET character_set_client  = utf8mb4 */ ;
/*!50003 SET character_set_results = utf8mb4 */ ;
/*!50003 SET collation_connection  = utf8mb4_0900_ai_ci */ ;
/*!50003 SET @saved_sql_mode       = @@sql_mode */ ;
/*!50003 SET sql_mode              = 'ONLY_FULL_GROUP_BY,STRICT_TRANS_TABLES,NO_ZERO_IN_DATE,NO_ZERO_DATE,ERROR_FOR_DIVISION_BY_ZERO,NO_ENGINE_SUBSTITUTION' */ ;
DELIMITER ;;
/*!50003 CREATE*/ /*!50017 DEFINER=`root`@`localhost`*/ /*!50003 TRIGGER `trg_loss_records_ad` AFTER DELETE ON `loss_records` FOR EACH ROW INSERT INTO change_log (module) VALUES ('loss_records') */;;
DELIMITER ;
/*!50003 SET sql_mode              = @saved_sql_mode */ ;
/*!50003 SET character_set_client  = @saved_cs_client */ ;
/*!50003 SET character_set_results = @saved_cs_results */ ;
/*!50003 SET collation_connection  = @saved_col_connection */ ;

--
-- Table structure for table `points_ledger`
--

DROP TABLE IF EXISTS `points_ledger`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `points_ledger` (
  `id` bigint NOT NULL AUTO_INCREMENT,
  `customer_id` int NOT NULL,
  `sale_id` int DEFAULT NULL,
  `entry_type` enum('earned','redeemed','adjustment') NOT NULL,
  `points` decimal(12,2) NOT NULL,
  `balance_after` decimal(12,2) NOT NULL,
  `note` varchar(200) DEFAULT NULL,
  `created_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  KEY `idx_points_customer` (`customer_id`,`created_at`),
  CONSTRAINT `fk_points_customer` FOREIGN KEY (`customer_id`) REFERENCES `customers` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `points_ledger`
--

LOCK TABLES `points_ledger` WRITE;
/*!40000 ALTER TABLE `points_ledger` DISABLE KEYS */;
/*!40000 ALTER TABLE `points_ledger` ENABLE KEYS */;
UNLOCK TABLES;
/*!50003 SET @saved_cs_client      = @@character_set_client */ ;
/*!50003 SET @saved_cs_results     = @@character_set_results */ ;
/*!50003 SET @saved_col_connection = @@collation_connection */ ;
/*!50003 SET character_set_client  = utf8mb4 */ ;
/*!50003 SET character_set_results = utf8mb4 */ ;
/*!50003 SET collation_connection  = utf8mb4_0900_ai_ci */ ;
/*!50003 SET @saved_sql_mode       = @@sql_mode */ ;
/*!50003 SET sql_mode              = 'ONLY_FULL_GROUP_BY,STRICT_TRANS_TABLES,NO_ZERO_IN_DATE,NO_ZERO_DATE,ERROR_FOR_DIVISION_BY_ZERO,NO_ENGINE_SUBSTITUTION' */ ;
DELIMITER ;;
/*!50003 CREATE*/ /*!50017 DEFINER=`root`@`localhost`*/ /*!50003 TRIGGER `trg_points_ledger_ai` AFTER INSERT ON `points_ledger` FOR EACH ROW INSERT INTO change_log (module) VALUES ('points_ledger') */;;
DELIMITER ;
/*!50003 SET sql_mode              = @saved_sql_mode */ ;
/*!50003 SET character_set_client  = @saved_cs_client */ ;
/*!50003 SET character_set_results = @saved_cs_results */ ;
/*!50003 SET collation_connection  = @saved_col_connection */ ;
/*!50003 SET @saved_cs_client      = @@character_set_client */ ;
/*!50003 SET @saved_cs_results     = @@character_set_results */ ;
/*!50003 SET @saved_col_connection = @@collation_connection */ ;
/*!50003 SET character_set_client  = utf8mb4 */ ;
/*!50003 SET character_set_results = utf8mb4 */ ;
/*!50003 SET collation_connection  = utf8mb4_0900_ai_ci */ ;
/*!50003 SET @saved_sql_mode       = @@sql_mode */ ;
/*!50003 SET sql_mode              = 'ONLY_FULL_GROUP_BY,STRICT_TRANS_TABLES,NO_ZERO_IN_DATE,NO_ZERO_DATE,ERROR_FOR_DIVISION_BY_ZERO,NO_ENGINE_SUBSTITUTION' */ ;
DELIMITER ;;
/*!50003 CREATE*/ /*!50017 DEFINER=`root`@`localhost`*/ /*!50003 TRIGGER `trg_points_ledger_au` AFTER UPDATE ON `points_ledger` FOR EACH ROW INSERT INTO change_log (module) VALUES ('points_ledger') */;;
DELIMITER ;
/*!50003 SET sql_mode              = @saved_sql_mode */ ;
/*!50003 SET character_set_client  = @saved_cs_client */ ;
/*!50003 SET character_set_results = @saved_cs_results */ ;
/*!50003 SET collation_connection  = @saved_col_connection */ ;
/*!50003 SET @saved_cs_client      = @@character_set_client */ ;
/*!50003 SET @saved_cs_results     = @@character_set_results */ ;
/*!50003 SET @saved_col_connection = @@collation_connection */ ;
/*!50003 SET character_set_client  = utf8mb4 */ ;
/*!50003 SET character_set_results = utf8mb4 */ ;
/*!50003 SET collation_connection  = utf8mb4_0900_ai_ci */ ;
/*!50003 SET @saved_sql_mode       = @@sql_mode */ ;
/*!50003 SET sql_mode              = 'ONLY_FULL_GROUP_BY,STRICT_TRANS_TABLES,NO_ZERO_IN_DATE,NO_ZERO_DATE,ERROR_FOR_DIVISION_BY_ZERO,NO_ENGINE_SUBSTITUTION' */ ;
DELIMITER ;;
/*!50003 CREATE*/ /*!50017 DEFINER=`root`@`localhost`*/ /*!50003 TRIGGER `trg_points_ledger_ad` AFTER DELETE ON `points_ledger` FOR EACH ROW INSERT INTO change_log (module) VALUES ('points_ledger') */;;
DELIMITER ;
/*!50003 SET sql_mode              = @saved_sql_mode */ ;
/*!50003 SET character_set_client  = @saved_cs_client */ ;
/*!50003 SET character_set_results = @saved_cs_results */ ;
/*!50003 SET collation_connection  = @saved_col_connection */ ;

--
-- Table structure for table `products`
--

DROP TABLE IF EXISTS `products`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `products` (
  `id` int NOT NULL AUTO_INCREMENT,
  `category_id` int NOT NULL,
  `name` varchar(150) NOT NULL,
  `measurement` varchar(100) NOT NULL DEFAULT '',
  `barcode` varchar(30) NOT NULL,
  `cost_price` decimal(14,4) NOT NULL DEFAULT '0.0000',
  `selling_price` decimal(12,2) NOT NULL DEFAULT '0.00',
  `qty_on_hand` int NOT NULL DEFAULT '0',
  `low_stock_threshold` int NOT NULL DEFAULT '0',
  `created_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `updated_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  UNIQUE KEY `uq_products_barcode` (`barcode`),
  UNIQUE KEY `uq_products_identity` (`category_id`,`name`,`measurement`),
  KEY `idx_products_name` (`name`),
  CONSTRAINT `fk_products_category` FOREIGN KEY (`category_id`) REFERENCES `categories` (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=23 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `products`
--

LOCK TABLES `products` WRITE;
/*!40000 ALTER TABLE `products` DISABLE KEYS */;
INSERT INTO `products` VALUES (1,2,'L bar','2mx2mx2m','2000000000015',210.0000,302.00,257,50,'2026-10-22 23:01:47','2026-11-09 02:34:21'),(2,1,'L bar','3mx3mx3m','2000000000022',200.0000,210.00,316,50,'2026-10-22 23:02:23','2026-11-09 02:34:21'),(3,4,'hotdog','','2000000000039',210.0000,220.00,35,5,'2026-10-24 10:09:34','2026-11-09 02:34:21'),(4,4,'cheesdog','','2000000000046',10.0000,11.00,24,20,'2026-10-24 10:09:52','2026-11-09 02:34:21'),(5,4,'burger','44x44x44mm','2000000000053',30.0000,125.00,144,5,'2026-10-24 10:11:12','2026-11-09 02:34:21'),(6,4,'siopao','','2000000000060',200.0000,300.00,15,5,'2026-10-24 10:18:17','2026-11-09 02:34:21'),(7,4,'burger','','2000000000077',10.0000,11.00,6,5,'2026-10-24 10:20:13','2026-11-09 02:34:21'),(16,1,'burger','10mm','2000000000169',100.0000,120.00,16,5,'2026-10-24 10:20:47','2026-11-09 02:34:21'),(18,2,'burger','','2000000000183',100.0000,120.00,198,5,'2026-10-24 10:22:20','2026-11-09 02:34:21'),(19,4,'sss','','2000000000190',900.0000,1000.00,0,5,'2026-10-24 10:32:38','2026-11-08 23:53:06'),(20,4,'cheese','large','2000000000206',900.0000,1200.00,0,60,'2026-11-04 23:25:12','2026-11-08 21:59:53'),(21,4,'mm','ii','2000000000213',700.0000,900.00,85,20,'2026-11-04 23:41:32','2026-11-09 02:34:21'),(22,5,'head and shoulder cool mentol','12ml','4902430333597',8.0000,10.00,62,20,'2026-11-08 23:40:28','2026-11-09 02:34:21');
/*!40000 ALTER TABLE `products` ENABLE KEYS */;
UNLOCK TABLES;
/*!50003 SET @saved_cs_client      = @@character_set_client */ ;
/*!50003 SET @saved_cs_results     = @@character_set_results */ ;
/*!50003 SET @saved_col_connection = @@collation_connection */ ;
/*!50003 SET character_set_client  = utf8mb4 */ ;
/*!50003 SET character_set_results = utf8mb4 */ ;
/*!50003 SET collation_connection  = utf8mb4_0900_ai_ci */ ;
/*!50003 SET @saved_sql_mode       = @@sql_mode */ ;
/*!50003 SET sql_mode              = 'ONLY_FULL_GROUP_BY,STRICT_TRANS_TABLES,NO_ZERO_IN_DATE,NO_ZERO_DATE,ERROR_FOR_DIVISION_BY_ZERO,NO_ENGINE_SUBSTITUTION' */ ;
DELIMITER ;;
/*!50003 CREATE*/ /*!50017 DEFINER=`root`@`localhost`*/ /*!50003 TRIGGER `trg_products_ai` AFTER INSERT ON `products` FOR EACH ROW INSERT INTO change_log (module) VALUES ('products') */;;
DELIMITER ;
/*!50003 SET sql_mode              = @saved_sql_mode */ ;
/*!50003 SET character_set_client  = @saved_cs_client */ ;
/*!50003 SET character_set_results = @saved_cs_results */ ;
/*!50003 SET collation_connection  = @saved_col_connection */ ;
/*!50003 SET @saved_cs_client      = @@character_set_client */ ;
/*!50003 SET @saved_cs_results     = @@character_set_results */ ;
/*!50003 SET @saved_col_connection = @@collation_connection */ ;
/*!50003 SET character_set_client  = utf8mb4 */ ;
/*!50003 SET character_set_results = utf8mb4 */ ;
/*!50003 SET collation_connection  = utf8mb4_0900_ai_ci */ ;
/*!50003 SET @saved_sql_mode       = @@sql_mode */ ;
/*!50003 SET sql_mode              = 'ONLY_FULL_GROUP_BY,STRICT_TRANS_TABLES,NO_ZERO_IN_DATE,NO_ZERO_DATE,ERROR_FOR_DIVISION_BY_ZERO,NO_ENGINE_SUBSTITUTION' */ ;
DELIMITER ;;
/*!50003 CREATE*/ /*!50017 DEFINER=`root`@`localhost`*/ /*!50003 TRIGGER `trg_products_au` AFTER UPDATE ON `products` FOR EACH ROW INSERT INTO change_log (module) VALUES ('products') */;;
DELIMITER ;
/*!50003 SET sql_mode              = @saved_sql_mode */ ;
/*!50003 SET character_set_client  = @saved_cs_client */ ;
/*!50003 SET character_set_results = @saved_cs_results */ ;
/*!50003 SET collation_connection  = @saved_col_connection */ ;
/*!50003 SET @saved_cs_client      = @@character_set_client */ ;
/*!50003 SET @saved_cs_results     = @@character_set_results */ ;
/*!50003 SET @saved_col_connection = @@collation_connection */ ;
/*!50003 SET character_set_client  = utf8mb4 */ ;
/*!50003 SET character_set_results = utf8mb4 */ ;
/*!50003 SET collation_connection  = utf8mb4_0900_ai_ci */ ;
/*!50003 SET @saved_sql_mode       = @@sql_mode */ ;
/*!50003 SET sql_mode              = 'ONLY_FULL_GROUP_BY,STRICT_TRANS_TABLES,NO_ZERO_IN_DATE,NO_ZERO_DATE,ERROR_FOR_DIVISION_BY_ZERO,NO_ENGINE_SUBSTITUTION' */ ;
DELIMITER ;;
/*!50003 CREATE*/ /*!50017 DEFINER=`root`@`localhost`*/ /*!50003 TRIGGER `trg_products_ad` AFTER DELETE ON `products` FOR EACH ROW INSERT INTO change_log (module) VALUES ('products') */;;
DELIMITER ;
/*!50003 SET sql_mode              = @saved_sql_mode */ ;
/*!50003 SET character_set_client  = @saved_cs_client */ ;
/*!50003 SET character_set_results = @saved_cs_results */ ;
/*!50003 SET collation_connection  = @saved_col_connection */ ;

--
-- Table structure for table `role_permissions`
--

DROP TABLE IF EXISTS `role_permissions`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `role_permissions` (
  `role` enum('superadmin','admin','cashier') NOT NULL,
  `module_key` varchar(40) NOT NULL,
  `is_allowed` tinyint(1) NOT NULL DEFAULT '0',
  PRIMARY KEY (`role`,`module_key`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `role_permissions`
--

LOCK TABLES `role_permissions` WRITE;
/*!40000 ALTER TABLE `role_permissions` DISABLE KEYS */;
INSERT INTO `role_permissions` VALUES ('superadmin','audit_trail',1),('superadmin','categories',1),('superadmin','customers',1),('superadmin','dashboard',1),('superadmin','database_settings',1),('superadmin','delivery',1),('superadmin','discounts',1),('superadmin','inventory',1),('superadmin','inventory_report',1),('superadmin','loss_report',1),('superadmin','pos',1),('superadmin','products',1),('superadmin','sales_report',1),('superadmin','stock_threshold',1),('superadmin','suppliers',1),('superadmin','user_monitoring',1),('superadmin','users',1),('superadmin','vat',1),('admin','audit_trail',1),('admin','categories',1),('admin','customers',1),('admin','dashboard',1),('admin','database_settings',0),('admin','delivery',1),('admin','discounts',0),('admin','inventory',1),('admin','inventory_report',1),('admin','loss_report',1),('admin','pos',1),('admin','products',1),('admin','sales_report',1),('admin','stock_threshold',0),('admin','suppliers',1),('admin','user_monitoring',0),('admin','users',1),('admin','vat',0),('cashier','audit_trail',0),('cashier','categories',0),('cashier','customers',0),('cashier','dashboard',0),('cashier','database_settings',0),('cashier','delivery',0),('cashier','discounts',0),('cashier','inventory',0),('cashier','inventory_report',0),('cashier','loss_report',0),('cashier','pos',1),('cashier','products',0),('cashier','sales_report',1),('cashier','stock_threshold',0),('cashier','suppliers',0),('cashier','user_monitoring',0),('cashier','users',0),('cashier','vat',0);
/*!40000 ALTER TABLE `role_permissions` ENABLE KEYS */;
UNLOCK TABLES;
/*!50003 SET @saved_cs_client      = @@character_set_client */ ;
/*!50003 SET @saved_cs_results     = @@character_set_results */ ;
/*!50003 SET @saved_col_connection = @@collation_connection */ ;
/*!50003 SET character_set_client  = utf8mb4 */ ;
/*!50003 SET character_set_results = utf8mb4 */ ;
/*!50003 SET collation_connection  = utf8mb4_0900_ai_ci */ ;
/*!50003 SET @saved_sql_mode       = @@sql_mode */ ;
/*!50003 SET sql_mode              = 'ONLY_FULL_GROUP_BY,STRICT_TRANS_TABLES,NO_ZERO_IN_DATE,NO_ZERO_DATE,ERROR_FOR_DIVISION_BY_ZERO,NO_ENGINE_SUBSTITUTION' */ ;
DELIMITER ;;
/*!50003 CREATE*/ /*!50017 DEFINER=`root`@`localhost`*/ /*!50003 TRIGGER `trg_role_permissions_ai` AFTER INSERT ON `role_permissions` FOR EACH ROW INSERT INTO change_log (module) VALUES ('role_permissions') */;;
DELIMITER ;
/*!50003 SET sql_mode              = @saved_sql_mode */ ;
/*!50003 SET character_set_client  = @saved_cs_client */ ;
/*!50003 SET character_set_results = @saved_cs_results */ ;
/*!50003 SET collation_connection  = @saved_col_connection */ ;
/*!50003 SET @saved_cs_client      = @@character_set_client */ ;
/*!50003 SET @saved_cs_results     = @@character_set_results */ ;
/*!50003 SET @saved_col_connection = @@collation_connection */ ;
/*!50003 SET character_set_client  = utf8mb4 */ ;
/*!50003 SET character_set_results = utf8mb4 */ ;
/*!50003 SET collation_connection  = utf8mb4_0900_ai_ci */ ;
/*!50003 SET @saved_sql_mode       = @@sql_mode */ ;
/*!50003 SET sql_mode              = 'ONLY_FULL_GROUP_BY,STRICT_TRANS_TABLES,NO_ZERO_IN_DATE,NO_ZERO_DATE,ERROR_FOR_DIVISION_BY_ZERO,NO_ENGINE_SUBSTITUTION' */ ;
DELIMITER ;;
/*!50003 CREATE*/ /*!50017 DEFINER=`root`@`localhost`*/ /*!50003 TRIGGER `trg_role_permissions_au` AFTER UPDATE ON `role_permissions` FOR EACH ROW INSERT INTO change_log (module) VALUES ('role_permissions') */;;
DELIMITER ;
/*!50003 SET sql_mode              = @saved_sql_mode */ ;
/*!50003 SET character_set_client  = @saved_cs_client */ ;
/*!50003 SET character_set_results = @saved_cs_results */ ;
/*!50003 SET collation_connection  = @saved_col_connection */ ;
/*!50003 SET @saved_cs_client      = @@character_set_client */ ;
/*!50003 SET @saved_cs_results     = @@character_set_results */ ;
/*!50003 SET @saved_col_connection = @@collation_connection */ ;
/*!50003 SET character_set_client  = utf8mb4 */ ;
/*!50003 SET character_set_results = utf8mb4 */ ;
/*!50003 SET collation_connection  = utf8mb4_0900_ai_ci */ ;
/*!50003 SET @saved_sql_mode       = @@sql_mode */ ;
/*!50003 SET sql_mode              = 'ONLY_FULL_GROUP_BY,STRICT_TRANS_TABLES,NO_ZERO_IN_DATE,NO_ZERO_DATE,ERROR_FOR_DIVISION_BY_ZERO,NO_ENGINE_SUBSTITUTION' */ ;
DELIMITER ;;
/*!50003 CREATE*/ /*!50017 DEFINER=`root`@`localhost`*/ /*!50003 TRIGGER `trg_role_permissions_ad` AFTER DELETE ON `role_permissions` FOR EACH ROW INSERT INTO change_log (module) VALUES ('role_permissions') */;;
DELIMITER ;
/*!50003 SET sql_mode              = @saved_sql_mode */ ;
/*!50003 SET character_set_client  = @saved_cs_client */ ;
/*!50003 SET character_set_results = @saved_cs_results */ ;
/*!50003 SET collation_connection  = @saved_col_connection */ ;

--
-- Table structure for table `sale_items`
--

DROP TABLE IF EXISTS `sale_items`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `sale_items` (
  `id` int NOT NULL AUTO_INCREMENT,
  `sale_id` int NOT NULL,
  `product_id` int NOT NULL,
  `product_name` varchar(150) NOT NULL,
  `measurement` varchar(100) NOT NULL DEFAULT '',
  `qty` int NOT NULL,
  `unit_price` decimal(12,2) NOT NULL,
  `cost_price` decimal(14,4) NOT NULL DEFAULT '0.0000',
  `line_total` decimal(14,2) NOT NULL,
  PRIMARY KEY (`id`),
  KEY `idx_sitems_sale` (`sale_id`),
  KEY `idx_sitems_product` (`product_id`),
  CONSTRAINT `fk_sitems_product` FOREIGN KEY (`product_id`) REFERENCES `products` (`id`),
  CONSTRAINT `fk_sitems_sale` FOREIGN KEY (`sale_id`) REFERENCES `sales` (`id`) ON DELETE CASCADE,
  CONSTRAINT `chk_sitems_qty` CHECK ((`qty` > 0))
) ENGINE=InnoDB AUTO_INCREMENT=45 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `sale_items`
--

LOCK TABLES `sale_items` WRITE;
/*!40000 ALTER TABLE `sale_items` DISABLE KEYS */;
INSERT INTO `sale_items` VALUES (1,1,1,'L bar','2mx2mx2m',2,302.00,210.0000,604.00),(2,1,2,'L bar','3mx3mx3m',2,210.00,200.0000,420.00),(3,1,3,'hotdog','',2,220.00,210.0000,440.00),(4,1,4,'cheesdog','',2,11.00,10.0000,22.00),(5,1,5,'burger','44x44x44mm',2,125.00,30.0000,250.00),(6,1,6,'siopao','',2,300.00,200.0000,600.00),(7,1,7,'burger','',2,11.00,10.0000,22.00),(8,1,16,'burger','',2,120.00,100.0000,240.00),(9,1,18,'burger','',2,120.00,100.0000,240.00),(10,1,19,'sss','',2,1000.00,900.0000,2000.00),(11,1,20,'cheese','large',2,1200.00,900.0000,2400.00),(12,1,21,'mm','ii',2,900.00,700.0000,1800.00),(13,2,4,'cheesdog','',1,11.00,10.0000,11.00),(14,2,5,'burger','44x44x44mm',1,125.00,30.0000,125.00),(15,2,6,'siopao','',1,300.00,200.0000,300.00),(16,2,19,'sss','',4,1000.00,900.0000,4000.00),(17,2,20,'cheese','large',120,1200.00,900.0000,144000.00),(18,2,21,'mm','ii',1,900.00,700.0000,900.00),(19,3,19,'sss','',1,1000.00,900.0000,1000.00),(20,4,3,'hotdog','',1,220.00,210.0000,220.00),(21,5,22,'head and shoulder cool mentol','12ml',35,10.00,8.0000,350.00),(22,6,1,'L bar','2mx2mx2m',1,302.00,210.0000,302.00),(23,6,2,'L bar','3mx3mx3m',1,210.00,200.0000,210.00),(24,6,3,'hotdog','',1,220.00,210.0000,220.00),(25,6,4,'cheesdog','',2,11.00,10.0000,22.00),(26,6,5,'burger','44x44x44mm',2,125.00,30.0000,250.00),(27,6,6,'siopao','',1,300.00,200.0000,300.00),(28,6,7,'burger','',1,11.00,10.0000,11.00),(29,6,16,'burger','',1,120.00,100.0000,120.00),(30,6,18,'burger','',1,120.00,100.0000,120.00),(31,6,19,'sss','',82,1000.00,900.0000,82000.00),(32,6,21,'mm','ii',1,900.00,700.0000,900.00),(33,6,22,'head and shoulder cool mentol','12ml',2,10.00,8.0000,20.00),(34,7,1,'L bar','2mx2mx2m',1,302.00,210.0000,302.00),(35,7,2,'L bar','3mx3mx3m',1,210.00,200.0000,210.00),(36,7,3,'hotdog','',1,220.00,210.0000,220.00),(37,7,4,'cheesdog','',1,11.00,10.0000,11.00),(38,7,5,'burger','44x44x44mm',1,125.00,30.0000,125.00),(39,7,6,'siopao','',1,300.00,200.0000,300.00),(40,7,7,'burger','',1,11.00,10.0000,11.00),(41,7,16,'burger','10mm',1,120.00,100.0000,120.00),(42,7,18,'burger','',1,120.00,100.0000,120.00),(43,7,21,'mm','ii',1,900.00,700.0000,900.00),(44,7,22,'head and shoulder cool mentol','12ml',1,10.00,8.0000,10.00);
/*!40000 ALTER TABLE `sale_items` ENABLE KEYS */;
UNLOCK TABLES;
/*!50003 SET @saved_cs_client      = @@character_set_client */ ;
/*!50003 SET @saved_cs_results     = @@character_set_results */ ;
/*!50003 SET @saved_col_connection = @@collation_connection */ ;
/*!50003 SET character_set_client  = utf8mb4 */ ;
/*!50003 SET character_set_results = utf8mb4 */ ;
/*!50003 SET collation_connection  = utf8mb4_0900_ai_ci */ ;
/*!50003 SET @saved_sql_mode       = @@sql_mode */ ;
/*!50003 SET sql_mode              = 'ONLY_FULL_GROUP_BY,STRICT_TRANS_TABLES,NO_ZERO_IN_DATE,NO_ZERO_DATE,ERROR_FOR_DIVISION_BY_ZERO,NO_ENGINE_SUBSTITUTION' */ ;
DELIMITER ;;
/*!50003 CREATE*/ /*!50017 DEFINER=`root`@`localhost`*/ /*!50003 TRIGGER `trg_sale_items_ai` AFTER INSERT ON `sale_items` FOR EACH ROW INSERT INTO change_log (module) VALUES ('sale_items') */;;
DELIMITER ;
/*!50003 SET sql_mode              = @saved_sql_mode */ ;
/*!50003 SET character_set_client  = @saved_cs_client */ ;
/*!50003 SET character_set_results = @saved_cs_results */ ;
/*!50003 SET collation_connection  = @saved_col_connection */ ;
/*!50003 SET @saved_cs_client      = @@character_set_client */ ;
/*!50003 SET @saved_cs_results     = @@character_set_results */ ;
/*!50003 SET @saved_col_connection = @@collation_connection */ ;
/*!50003 SET character_set_client  = utf8mb4 */ ;
/*!50003 SET character_set_results = utf8mb4 */ ;
/*!50003 SET collation_connection  = utf8mb4_0900_ai_ci */ ;
/*!50003 SET @saved_sql_mode       = @@sql_mode */ ;
/*!50003 SET sql_mode              = 'ONLY_FULL_GROUP_BY,STRICT_TRANS_TABLES,NO_ZERO_IN_DATE,NO_ZERO_DATE,ERROR_FOR_DIVISION_BY_ZERO,NO_ENGINE_SUBSTITUTION' */ ;
DELIMITER ;;
/*!50003 CREATE*/ /*!50017 DEFINER=`root`@`localhost`*/ /*!50003 TRIGGER `trg_sale_items_au` AFTER UPDATE ON `sale_items` FOR EACH ROW INSERT INTO change_log (module) VALUES ('sale_items') */;;
DELIMITER ;
/*!50003 SET sql_mode              = @saved_sql_mode */ ;
/*!50003 SET character_set_client  = @saved_cs_client */ ;
/*!50003 SET character_set_results = @saved_cs_results */ ;
/*!50003 SET collation_connection  = @saved_col_connection */ ;
/*!50003 SET @saved_cs_client      = @@character_set_client */ ;
/*!50003 SET @saved_cs_results     = @@character_set_results */ ;
/*!50003 SET @saved_col_connection = @@collation_connection */ ;
/*!50003 SET character_set_client  = utf8mb4 */ ;
/*!50003 SET character_set_results = utf8mb4 */ ;
/*!50003 SET collation_connection  = utf8mb4_0900_ai_ci */ ;
/*!50003 SET @saved_sql_mode       = @@sql_mode */ ;
/*!50003 SET sql_mode              = 'ONLY_FULL_GROUP_BY,STRICT_TRANS_TABLES,NO_ZERO_IN_DATE,NO_ZERO_DATE,ERROR_FOR_DIVISION_BY_ZERO,NO_ENGINE_SUBSTITUTION' */ ;
DELIMITER ;;
/*!50003 CREATE*/ /*!50017 DEFINER=`root`@`localhost`*/ /*!50003 TRIGGER `trg_sale_items_ad` AFTER DELETE ON `sale_items` FOR EACH ROW INSERT INTO change_log (module) VALUES ('sale_items') */;;
DELIMITER ;
/*!50003 SET sql_mode              = @saved_sql_mode */ ;
/*!50003 SET character_set_client  = @saved_cs_client */ ;
/*!50003 SET character_set_results = @saved_cs_results */ ;
/*!50003 SET collation_connection  = @saved_col_connection */ ;

--
-- Table structure for table `sales`
--

DROP TABLE IF EXISTS `sales`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `sales` (
  `id` int NOT NULL AUTO_INCREMENT,
  `receipt_no` varchar(30) NOT NULL,
  `customer_id` int DEFAULT NULL,
  `customer_name` varchar(150) DEFAULT NULL,
  `cashier_id` int DEFAULT NULL,
  `cashier_name` varchar(100) DEFAULT NULL,
  `subtotal` decimal(14,2) NOT NULL DEFAULT '0.00',
  `discount_id` int DEFAULT NULL,
  `discount_name` varchar(100) DEFAULT NULL,
  `discount_amount` decimal(14,2) NOT NULL DEFAULT '0.00',
  `special_discount_percent` decimal(5,2) NOT NULL DEFAULT '0.00',
  `special_discount_amount` decimal(14,2) NOT NULL DEFAULT '0.00',
  `special_approved_by` int DEFAULT NULL,
  `special_approved_name` varchar(100) DEFAULT NULL,
  `vat_name` varchar(50) DEFAULT NULL,
  `vat_percent` decimal(5,2) NOT NULL DEFAULT '0.00',
  `vat_amount` decimal(14,2) NOT NULL DEFAULT '0.00',
  `total` decimal(14,2) NOT NULL DEFAULT '0.00',
  `points_used` decimal(12,2) NOT NULL DEFAULT '0.00',
  `points_value` decimal(14,2) NOT NULL DEFAULT '0.00',
  `amount_paid` decimal(14,2) NOT NULL DEFAULT '0.00',
  `change_amount` decimal(14,2) NOT NULL DEFAULT '0.00',
  `points_earned` decimal(12,2) NOT NULL DEFAULT '0.00',
  `created_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  UNIQUE KEY `uq_sales_receipt` (`receipt_no`),
  KEY `idx_sales_date` (`created_at`),
  KEY `idx_sales_cashier` (`cashier_id`,`created_at`),
  KEY `fk_sales_customer` (`customer_id`),
  KEY `fk_sales_discount` (`discount_id`),
  KEY `fk_sales_approver` (`special_approved_by`),
  CONSTRAINT `fk_sales_approver` FOREIGN KEY (`special_approved_by`) REFERENCES `users` (`id`) ON DELETE SET NULL,
  CONSTRAINT `fk_sales_cashier` FOREIGN KEY (`cashier_id`) REFERENCES `users` (`id`) ON DELETE SET NULL,
  CONSTRAINT `fk_sales_customer` FOREIGN KEY (`customer_id`) REFERENCES `customers` (`id`) ON DELETE SET NULL,
  CONSTRAINT `fk_sales_discount` FOREIGN KEY (`discount_id`) REFERENCES `discounts` (`id`) ON DELETE SET NULL
) ENGINE=InnoDB AUTO_INCREMENT=8 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `sales`
--

LOCK TABLES `sales` WRITE;
/*!40000 ALTER TABLE `sales` DISABLE KEYS */;
INSERT INTO `sales` VALUES (1,'OR-2026-000001',NULL,NULL,5,'naed acoy',9038.00,NULL,NULL,0.00,0.00,0.00,NULL,NULL,'VAT',12.00,968.36,9038.00,0.00,0.00,10000.00,962.00,0.00,'2026-11-08 21:56:20'),(2,'OR-2026-000002',NULL,NULL,5,'naed acoy',149336.00,NULL,NULL,0.00,0.00,0.00,NULL,NULL,'VAT',12.00,16000.29,149336.00,0.00,0.00,149436.00,100.00,0.00,'2026-11-08 21:59:53'),(3,'OR-2026-000003',NULL,NULL,5,'naed acoy',1000.00,NULL,NULL,0.00,0.00,998.00,5,'naed acoy','VAT',12.00,0.21,2.00,0.00,0.00,100.00,98.00,0.00,'2026-11-08 22:01:14'),(4,'OR-2026-000004',NULL,NULL,5,'naed acoy',220.00,NULL,NULL,0.00,0.00,0.00,NULL,NULL,'VAT',12.00,23.57,220.00,0.00,0.00,220.00,0.00,0.00,'2026-11-08 23:33:51'),(5,'OR-2026-000005',NULL,NULL,5,'naed acoy',350.00,NULL,NULL,0.00,0.00,0.00,NULL,NULL,'VAT',12.00,37.50,350.00,0.00,0.00,350.00,0.00,0.00,'2026-11-08 23:44:44'),(6,'OR-2026-000006',NULL,NULL,5,'naed acoy',84475.00,NULL,NULL,0.00,0.00,0.00,NULL,NULL,'VAT',12.00,9050.89,84475.00,0.00,0.00,84575.00,100.00,0.00,'2026-11-08 23:53:06'),(7,'OR-2026-000007',NULL,NULL,5,'naed acoy',2329.00,NULL,NULL,0.00,0.00,329.00,5,'naed acoy','VAT',12.00,214.29,2000.00,0.00,0.00,2000.00,0.00,0.00,'2026-11-09 02:34:21');
/*!40000 ALTER TABLE `sales` ENABLE KEYS */;
UNLOCK TABLES;
/*!50003 SET @saved_cs_client      = @@character_set_client */ ;
/*!50003 SET @saved_cs_results     = @@character_set_results */ ;
/*!50003 SET @saved_col_connection = @@collation_connection */ ;
/*!50003 SET character_set_client  = utf8mb4 */ ;
/*!50003 SET character_set_results = utf8mb4 */ ;
/*!50003 SET collation_connection  = utf8mb4_0900_ai_ci */ ;
/*!50003 SET @saved_sql_mode       = @@sql_mode */ ;
/*!50003 SET sql_mode              = 'ONLY_FULL_GROUP_BY,STRICT_TRANS_TABLES,NO_ZERO_IN_DATE,NO_ZERO_DATE,ERROR_FOR_DIVISION_BY_ZERO,NO_ENGINE_SUBSTITUTION' */ ;
DELIMITER ;;
/*!50003 CREATE*/ /*!50017 DEFINER=`root`@`localhost`*/ /*!50003 TRIGGER `trg_sales_ai` AFTER INSERT ON `sales` FOR EACH ROW INSERT INTO change_log (module) VALUES ('sales') */;;
DELIMITER ;
/*!50003 SET sql_mode              = @saved_sql_mode */ ;
/*!50003 SET character_set_client  = @saved_cs_client */ ;
/*!50003 SET character_set_results = @saved_cs_results */ ;
/*!50003 SET collation_connection  = @saved_col_connection */ ;
/*!50003 SET @saved_cs_client      = @@character_set_client */ ;
/*!50003 SET @saved_cs_results     = @@character_set_results */ ;
/*!50003 SET @saved_col_connection = @@collation_connection */ ;
/*!50003 SET character_set_client  = utf8mb4 */ ;
/*!50003 SET character_set_results = utf8mb4 */ ;
/*!50003 SET collation_connection  = utf8mb4_0900_ai_ci */ ;
/*!50003 SET @saved_sql_mode       = @@sql_mode */ ;
/*!50003 SET sql_mode              = 'ONLY_FULL_GROUP_BY,STRICT_TRANS_TABLES,NO_ZERO_IN_DATE,NO_ZERO_DATE,ERROR_FOR_DIVISION_BY_ZERO,NO_ENGINE_SUBSTITUTION' */ ;
DELIMITER ;;
/*!50003 CREATE*/ /*!50017 DEFINER=`root`@`localhost`*/ /*!50003 TRIGGER `trg_sales_au` AFTER UPDATE ON `sales` FOR EACH ROW INSERT INTO change_log (module) VALUES ('sales') */;;
DELIMITER ;
/*!50003 SET sql_mode              = @saved_sql_mode */ ;
/*!50003 SET character_set_client  = @saved_cs_client */ ;
/*!50003 SET character_set_results = @saved_cs_results */ ;
/*!50003 SET collation_connection  = @saved_col_connection */ ;
/*!50003 SET @saved_cs_client      = @@character_set_client */ ;
/*!50003 SET @saved_cs_results     = @@character_set_results */ ;
/*!50003 SET @saved_col_connection = @@collation_connection */ ;
/*!50003 SET character_set_client  = utf8mb4 */ ;
/*!50003 SET character_set_results = utf8mb4 */ ;
/*!50003 SET collation_connection  = utf8mb4_0900_ai_ci */ ;
/*!50003 SET @saved_sql_mode       = @@sql_mode */ ;
/*!50003 SET sql_mode              = 'ONLY_FULL_GROUP_BY,STRICT_TRANS_TABLES,NO_ZERO_IN_DATE,NO_ZERO_DATE,ERROR_FOR_DIVISION_BY_ZERO,NO_ENGINE_SUBSTITUTION' */ ;
DELIMITER ;;
/*!50003 CREATE*/ /*!50017 DEFINER=`root`@`localhost`*/ /*!50003 TRIGGER `trg_sales_ad` AFTER DELETE ON `sales` FOR EACH ROW INSERT INTO change_log (module) VALUES ('sales') */;;
DELIMITER ;
/*!50003 SET sql_mode              = @saved_sql_mode */ ;
/*!50003 SET character_set_client  = @saved_cs_client */ ;
/*!50003 SET character_set_results = @saved_cs_results */ ;
/*!50003 SET collation_connection  = @saved_col_connection */ ;

--
-- Table structure for table `schema_version`
--

DROP TABLE IF EXISTS `schema_version`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `schema_version` (
  `version` int NOT NULL,
  `description` varchar(200) NOT NULL,
  `applied_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`version`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `schema_version`
--

LOCK TABLES `schema_version` WRITE;
/*!40000 ALTER TABLE `schema_version` DISABLE KEYS */;
INSERT INTO `schema_version` VALUES (1,'Initial schema','2026-10-18 12:08:17'),(2,'Session tracking: current screen','2026-10-20 07:13:53'),(3,'Suppliers: only the address is unique','2026-10-22 22:54:24'),(4,'POS: who approved a special price','2026-11-06 13:06:46'),(5,'Delivery: cost per piece or per pack','2026-11-09 19:18:09');
/*!40000 ALTER TABLE `schema_version` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `settings`
--

DROP TABLE IF EXISTS `settings`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `settings` (
  `setting_key` varchar(60) NOT NULL,
  `setting_value` varchar(500) DEFAULT NULL,
  `updated_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`setting_key`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `settings`
--

LOCK TABLES `settings` WRITE;
/*!40000 ALTER TABLE `settings` DISABLE KEYS */;
INSERT INTO `settings` VALUES ('backup_folder','C:/DTSIMS2/Backups','2026-10-18 12:08:09'),('point_peso_value','1','2026-10-18 12:08:09'),('points_spend_per_point','100','2026-10-18 12:08:09');
/*!40000 ALTER TABLE `settings` ENABLE KEYS */;
UNLOCK TABLES;
/*!50003 SET @saved_cs_client      = @@character_set_client */ ;
/*!50003 SET @saved_cs_results     = @@character_set_results */ ;
/*!50003 SET @saved_col_connection = @@collation_connection */ ;
/*!50003 SET character_set_client  = utf8mb4 */ ;
/*!50003 SET character_set_results = utf8mb4 */ ;
/*!50003 SET collation_connection  = utf8mb4_0900_ai_ci */ ;
/*!50003 SET @saved_sql_mode       = @@sql_mode */ ;
/*!50003 SET sql_mode              = 'ONLY_FULL_GROUP_BY,STRICT_TRANS_TABLES,NO_ZERO_IN_DATE,NO_ZERO_DATE,ERROR_FOR_DIVISION_BY_ZERO,NO_ENGINE_SUBSTITUTION' */ ;
DELIMITER ;;
/*!50003 CREATE*/ /*!50017 DEFINER=`root`@`localhost`*/ /*!50003 TRIGGER `trg_settings_ai` AFTER INSERT ON `settings` FOR EACH ROW INSERT INTO change_log (module) VALUES ('settings') */;;
DELIMITER ;
/*!50003 SET sql_mode              = @saved_sql_mode */ ;
/*!50003 SET character_set_client  = @saved_cs_client */ ;
/*!50003 SET character_set_results = @saved_cs_results */ ;
/*!50003 SET collation_connection  = @saved_col_connection */ ;
/*!50003 SET @saved_cs_client      = @@character_set_client */ ;
/*!50003 SET @saved_cs_results     = @@character_set_results */ ;
/*!50003 SET @saved_col_connection = @@collation_connection */ ;
/*!50003 SET character_set_client  = utf8mb4 */ ;
/*!50003 SET character_set_results = utf8mb4 */ ;
/*!50003 SET collation_connection  = utf8mb4_0900_ai_ci */ ;
/*!50003 SET @saved_sql_mode       = @@sql_mode */ ;
/*!50003 SET sql_mode              = 'ONLY_FULL_GROUP_BY,STRICT_TRANS_TABLES,NO_ZERO_IN_DATE,NO_ZERO_DATE,ERROR_FOR_DIVISION_BY_ZERO,NO_ENGINE_SUBSTITUTION' */ ;
DELIMITER ;;
/*!50003 CREATE*/ /*!50017 DEFINER=`root`@`localhost`*/ /*!50003 TRIGGER `trg_settings_au` AFTER UPDATE ON `settings` FOR EACH ROW INSERT INTO change_log (module) VALUES ('settings') */;;
DELIMITER ;
/*!50003 SET sql_mode              = @saved_sql_mode */ ;
/*!50003 SET character_set_client  = @saved_cs_client */ ;
/*!50003 SET character_set_results = @saved_cs_results */ ;
/*!50003 SET collation_connection  = @saved_col_connection */ ;
/*!50003 SET @saved_cs_client      = @@character_set_client */ ;
/*!50003 SET @saved_cs_results     = @@character_set_results */ ;
/*!50003 SET @saved_col_connection = @@collation_connection */ ;
/*!50003 SET character_set_client  = utf8mb4 */ ;
/*!50003 SET character_set_results = utf8mb4 */ ;
/*!50003 SET collation_connection  = utf8mb4_0900_ai_ci */ ;
/*!50003 SET @saved_sql_mode       = @@sql_mode */ ;
/*!50003 SET sql_mode              = 'ONLY_FULL_GROUP_BY,STRICT_TRANS_TABLES,NO_ZERO_IN_DATE,NO_ZERO_DATE,ERROR_FOR_DIVISION_BY_ZERO,NO_ENGINE_SUBSTITUTION' */ ;
DELIMITER ;;
/*!50003 CREATE*/ /*!50017 DEFINER=`root`@`localhost`*/ /*!50003 TRIGGER `trg_settings_ad` AFTER DELETE ON `settings` FOR EACH ROW INSERT INTO change_log (module) VALUES ('settings') */;;
DELIMITER ;
/*!50003 SET sql_mode              = @saved_sql_mode */ ;
/*!50003 SET character_set_client  = @saved_cs_client */ ;
/*!50003 SET character_set_results = @saved_cs_results */ ;
/*!50003 SET collation_connection  = @saved_col_connection */ ;

--
-- Table structure for table `suppliers`
--

DROP TABLE IF EXISTS `suppliers`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `suppliers` (
  `id` int NOT NULL AUTO_INCREMENT,
  `name` varchar(100) NOT NULL,
  `company_name` varchar(150) NOT NULL,
  `contact_number` varchar(30) DEFAULT NULL,
  `company_address` varchar(255) NOT NULL,
  `email` varchar(100) DEFAULT NULL,
  `created_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `updated_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  UNIQUE KEY `uq_suppliers_address` (`company_address`)
) ENGINE=InnoDB AUTO_INCREMENT=8 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `suppliers`
--

LOCK TABLES `suppliers` WRITE;
/*!40000 ALTER TABLE `suppliers` DISABLE KEYS */;
INSERT INTO `suppliers` VALUES (1,'Jasper','jasper metal corp','09123123123','taguig','jasper@gmail.com','2026-10-22 22:57:40','2026-10-22 22:59:25'),(5,'Jaspersldknflksdnfklsdnfklsdfnsdlfnsdfnsdfnsdklfnsdklfnsdlkfnsdklfnsdklfnsfsklfklsdfnlsdfnsdlfnsdfnk','carlksdfksdfksdfnkosdlksdfkldfnsdfsdfnsdklfnsdkfnskldfnklsdfnklsdnfklsdfnklsdfnsdlfnsdfnfnsdklfnsdklfnsdklfnsdklfnsdklfnsdklfnsdklfnsdklfnsdlkfnsdlkfn','0912312312323','makatijasdjasdbkasdbjkasdjasdbjasdbjasbsadbasbdhjabdbjasbdbhjasbdhjassbdhjbasjhbdsabdhjasbdjhasbdjhasbdhjasbdjhasbdjhasbasfsoaidjapsodoasnpoaspasdnpiasdassadbsa','jasper@gmail.comjhahjadbasbdsabsahjasdhjasdbhsahjbashjdsabhdhbsjdsadsadsadsassadsabhjdasdsadasdsadas','2026-10-22 22:58:54','2026-10-22 23:10:47'),(6,'Jasper','carl','09212577107','pasig','jasper@gmail.com','2026-10-22 23:03:39','2026-11-04 23:54:24'),(7,'Qwerty','qwerty','09787712345','manila',NULL,'2026-10-24 10:16:53','2026-10-24 10:17:14');
/*!40000 ALTER TABLE `suppliers` ENABLE KEYS */;
UNLOCK TABLES;
/*!50003 SET @saved_cs_client      = @@character_set_client */ ;
/*!50003 SET @saved_cs_results     = @@character_set_results */ ;
/*!50003 SET @saved_col_connection = @@collation_connection */ ;
/*!50003 SET character_set_client  = utf8mb4 */ ;
/*!50003 SET character_set_results = utf8mb4 */ ;
/*!50003 SET collation_connection  = utf8mb4_0900_ai_ci */ ;
/*!50003 SET @saved_sql_mode       = @@sql_mode */ ;
/*!50003 SET sql_mode              = 'ONLY_FULL_GROUP_BY,STRICT_TRANS_TABLES,NO_ZERO_IN_DATE,NO_ZERO_DATE,ERROR_FOR_DIVISION_BY_ZERO,NO_ENGINE_SUBSTITUTION' */ ;
DELIMITER ;;
/*!50003 CREATE*/ /*!50017 DEFINER=`root`@`localhost`*/ /*!50003 TRIGGER `trg_suppliers_ai` AFTER INSERT ON `suppliers` FOR EACH ROW INSERT INTO change_log (module) VALUES ('suppliers') */;;
DELIMITER ;
/*!50003 SET sql_mode              = @saved_sql_mode */ ;
/*!50003 SET character_set_client  = @saved_cs_client */ ;
/*!50003 SET character_set_results = @saved_cs_results */ ;
/*!50003 SET collation_connection  = @saved_col_connection */ ;
/*!50003 SET @saved_cs_client      = @@character_set_client */ ;
/*!50003 SET @saved_cs_results     = @@character_set_results */ ;
/*!50003 SET @saved_col_connection = @@collation_connection */ ;
/*!50003 SET character_set_client  = utf8mb4 */ ;
/*!50003 SET character_set_results = utf8mb4 */ ;
/*!50003 SET collation_connection  = utf8mb4_0900_ai_ci */ ;
/*!50003 SET @saved_sql_mode       = @@sql_mode */ ;
/*!50003 SET sql_mode              = 'ONLY_FULL_GROUP_BY,STRICT_TRANS_TABLES,NO_ZERO_IN_DATE,NO_ZERO_DATE,ERROR_FOR_DIVISION_BY_ZERO,NO_ENGINE_SUBSTITUTION' */ ;
DELIMITER ;;
/*!50003 CREATE*/ /*!50017 DEFINER=`root`@`localhost`*/ /*!50003 TRIGGER `trg_suppliers_au` AFTER UPDATE ON `suppliers` FOR EACH ROW INSERT INTO change_log (module) VALUES ('suppliers') */;;
DELIMITER ;
/*!50003 SET sql_mode              = @saved_sql_mode */ ;
/*!50003 SET character_set_client  = @saved_cs_client */ ;
/*!50003 SET character_set_results = @saved_cs_results */ ;
/*!50003 SET collation_connection  = @saved_col_connection */ ;
/*!50003 SET @saved_cs_client      = @@character_set_client */ ;
/*!50003 SET @saved_cs_results     = @@character_set_results */ ;
/*!50003 SET @saved_col_connection = @@collation_connection */ ;
/*!50003 SET character_set_client  = utf8mb4 */ ;
/*!50003 SET character_set_results = utf8mb4 */ ;
/*!50003 SET collation_connection  = utf8mb4_0900_ai_ci */ ;
/*!50003 SET @saved_sql_mode       = @@sql_mode */ ;
/*!50003 SET sql_mode              = 'ONLY_FULL_GROUP_BY,STRICT_TRANS_TABLES,NO_ZERO_IN_DATE,NO_ZERO_DATE,ERROR_FOR_DIVISION_BY_ZERO,NO_ENGINE_SUBSTITUTION' */ ;
DELIMITER ;;
/*!50003 CREATE*/ /*!50017 DEFINER=`root`@`localhost`*/ /*!50003 TRIGGER `trg_suppliers_ad` AFTER DELETE ON `suppliers` FOR EACH ROW INSERT INTO change_log (module) VALUES ('suppliers') */;;
DELIMITER ;
/*!50003 SET sql_mode              = @saved_sql_mode */ ;
/*!50003 SET character_set_client  = @saved_cs_client */ ;
/*!50003 SET character_set_results = @saved_cs_results */ ;
/*!50003 SET collation_connection  = @saved_col_connection */ ;

--
-- Table structure for table `user_sessions`
--

DROP TABLE IF EXISTS `user_sessions`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `user_sessions` (
  `id` int NOT NULL AUTO_INCREMENT,
  `session_id` varchar(64) NOT NULL,
  `user_id` int NOT NULL,
  `username` varchar(50) DEFAULT NULL,
  `device_name` varchar(100) DEFAULT NULL,
  `ip_address` varchar(45) DEFAULT NULL,
  `current_module` varchar(60) DEFAULT NULL,
  `module_since` datetime DEFAULT NULL,
  `login_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `last_seen` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `logout_at` datetime DEFAULT NULL,
  `is_active` tinyint(1) NOT NULL DEFAULT '1',
  PRIMARY KEY (`id`),
  UNIQUE KEY `uq_sessions_sid` (`session_id`),
  KEY `idx_sessions_user` (`user_id`,`is_active`,`last_seen`),
  CONSTRAINT `fk_sessions_user` FOREIGN KEY (`user_id`) REFERENCES `users` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=28 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `user_sessions`
--

LOCK TABLES `user_sessions` WRITE;
/*!40000 ALTER TABLE `user_sessions` DISABLE KEYS */;
INSERT INTO `user_sessions` VALUES (1,'a2acae69804e4d54a53d981e28f53f2c',5,'naed','NAED','100.103.54.107','Users','2026-10-20 07:22:48','2026-10-20 07:22:46','2026-10-20 07:24:48','2026-10-20 07:24:48',0),(2,'46d0462e124c4b5f8fde31a3a7011e2a',5,'naed','NAED','100.103.54.107','Users','2026-10-20 07:28:37','2026-10-20 07:28:32','2026-10-20 07:31:24','2026-10-20 07:31:26',0),(3,'54ef8509eb124c68bd92e4457446d219',13,'francis','NAED','100.103.54.107','Home','2026-10-20 07:31:33','2026-10-20 07:31:33','2026-10-20 07:31:34','2026-10-20 07:31:38',0),(4,'9a91e093d7ae441e9a2a24a5bd7d97d0',5,'naed','NAED','100.103.54.107','Home','2026-10-20 07:34:08','2026-10-20 07:31:42','2026-10-20 07:34:44','2026-10-20 07:34:45',0),(5,'e905d22d46fc425cb6057465702259c9',5,'naed','NAED','100.103.54.107','Users','2026-10-21 21:16:58','2026-10-21 21:16:51','2026-10-21 21:17:57','2026-10-21 21:17:58',0),(6,'bdb873a98101442db746a4168e492dc3',5,'naed','NAED','100.103.54.107','Home','2026-10-21 21:22:45','2026-10-21 21:22:45','2026-10-21 21:23:06','2026-10-21 21:23:08',0),(7,'bc22a77ed83646ba8052e8c9845e8c84',5,'naed','NAED','100.103.54.107','Users','2026-10-22 06:19:45','2026-10-22 06:14:45','2026-10-22 06:20:09','2026-10-22 06:20:11',0),(8,'e35fb565619f478cb5cf1663b774001c',13,'francis','NAED','100.103.54.107','Home','2026-10-22 06:20:47','2026-10-22 06:20:17','2026-10-22 06:20:53','2026-10-22 06:20:55',0),(9,'3f8a842a0a03460e81da62930c69bc49',11,'red','NAED','100.103.54.107','Categories','2026-10-22 06:21:47','2026-10-22 06:21:12','2026-10-22 06:21:48','2026-10-22 06:21:53',0),(10,'27ad501e9339453aacb20d7c042fe9e8',5,'naed','NAED','100.103.54.107','User Monitoring','2026-10-22 07:35:29','2026-10-22 06:21:59','2026-10-22 07:39:05','2026-10-22 07:39:08',0),(11,'e938e33f2db9428891d3316b84b97bc9',5,'naed','NAED','100.103.54.107','User Monitoring','2026-10-22 11:33:41','2026-10-22 11:33:17','2026-10-22 11:34:09','2026-10-22 11:34:12',0),(12,'68e6ba83cc2241abb67ef616cd3a020b',5,'naed','NAED','100.103.54.107','Home','2026-10-22 11:50:20','2026-10-22 11:50:20','2026-10-22 11:53:23','2026-10-22 11:53:27',0),(13,'5dd3a1c8a93f493ea6b2e4346c922a1b',13,'francis','LUCKY','10.199.96.99','Home','2026-10-22 12:02:29','2026-10-22 12:00:58','2026-10-22 12:02:59','2026-10-22 12:03:02',0),(14,'a71bc9c7c0ef4eed8e02ad8965a1fe58',5,'naed','NAED','100.103.54.107','User Monitoring','2026-10-22 12:02:56','2026-10-22 12:01:17','2026-10-22 12:05:15','2026-10-22 12:05:17',0),(15,'a7528d85d6ab496cb42a41662822b163',5,'naed','LUCKY','10.199.96.99','Home','2026-10-22 12:08:26','2026-10-22 12:03:20','2026-10-22 12:08:30','2026-10-22 12:08:34',0),(16,'551a44a1c8a344d59941e624c7b5e928',5,'naed','NAED','100.103.54.107','User Monitoring','2026-10-22 12:09:00','2026-10-22 12:07:08','2026-10-22 12:09:36','2026-10-22 12:09:38',0),(17,'89d680c073aa4aa6be4d88fb7f798009',5,'naed','NAED','100.103.54.107','User Monitoring','2026-10-22 17:09:16','2026-10-22 17:09:03','2026-10-22 17:09:49','2026-10-22 17:09:53',0),(18,'b2a0566ec51b4eaba25d4d1cb59c03d6',5,'naed','NAED','100.103.54.107','User Monitoring','2026-10-22 23:12:09','2026-10-22 22:56:29','2026-10-22 23:12:49','2026-10-22 23:12:52',0),(19,'36dee08531ed4fa7b2c30f4873040384',5,'naed','NAED','100.103.54.107','Products','2026-10-23 04:06:22','2026-10-23 04:06:05','2026-10-23 04:06:36','2026-10-23 04:06:36',0),(20,'64acafeac0f747b5ae13f313d83f66f9',5,'naed','NAED','100.103.54.107','User Monitoring','2026-10-24 10:48:04','2026-10-24 09:56:21','2026-10-24 10:48:16','2026-10-24 10:48:20',0),(21,'458572c6f76343b18cae89f66955b24f',5,'naed','NAED','169.254.83.107','Stock Threshold','2026-10-29 21:54:55','2026-10-29 21:51:39','2026-10-29 21:55:42','2026-10-29 21:55:46',0),(22,'d1d1a655f1d74df8ba9a90c9bef471a2',5,'naed','NAED','100.103.54.107','User Monitoring','2026-11-05 00:25:14','2026-11-04 22:47:47','2026-11-05 00:26:04','2026-11-05 00:26:09',0),(23,'783c9dbe1711463b90cd5762ba9218cf',5,'naed','NAED','100.103.54.107','Users','2026-11-06 17:12:14','2026-11-06 17:12:08','2026-11-06 17:12:49','2026-11-06 17:12:51',0),(24,'938a5d4a70964ccc8dc07cda15422325',5,'naed','NAED','100.103.54.107','Home','2026-11-08 22:04:20','2026-11-08 21:52:19','2026-11-08 22:04:34','2026-11-08 22:04:35',0),(25,'b295bcec103c40518febe8eb3a5c1dda',5,'naed','NAED','100.103.54.107','User Monitoring','2026-11-09 00:41:55','2026-11-08 23:32:54','2026-11-09 00:42:30','2026-11-09 00:42:33',0),(26,'58a0e2d4952e4de88c0b04617100639d',5,'naed','NAED','100.103.54.107','Customer Accounts','2026-11-09 02:38:39','2026-11-09 02:22:08','2026-11-09 02:47:49','2026-11-09 02:47:50',0),(27,'aa7709e4dad246c7ad2b58f20473b75e',5,'naed','NAED','100.103.54.107','Home','2026-11-11 02:13:45','2026-11-11 02:13:15','2026-11-11 02:13:46','2026-11-11 02:13:49',0);
/*!40000 ALTER TABLE `user_sessions` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `users`
--

DROP TABLE IF EXISTS `users`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `users` (
  `id` int NOT NULL AUTO_INCREMENT,
  `username` varchar(50) NOT NULL,
  `password_hash` varchar(255) NOT NULL,
  `full_name` varchar(100) NOT NULL,
  `contact_number` varchar(30) DEFAULT NULL,
  `address` varchar(255) DEFAULT NULL,
  `photo` mediumblob,
  `role` enum('superadmin','admin','cashier') NOT NULL DEFAULT 'cashier',
  `is_active` tinyint(1) NOT NULL DEFAULT '1',
  `is_locked` tinyint(1) NOT NULL DEFAULT '0',
  `failed_attempts` int NOT NULL DEFAULT '0',
  `locked_until` datetime DEFAULT NULL,
  `last_login` datetime DEFAULT NULL,
  `created_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `updated_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  UNIQUE KEY `uq_users_username` (`username`)
) ENGINE=InnoDB AUTO_INCREMENT=16 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `users`
--

LOCK TABLES `users` WRITE;
/*!40000 ALTER TABLE `users` DISABLE KEYS */;
INSERT INTO `users` VALUES (5,'naed','PBKDF2-SHA256$120000$v1OOuJVetbzAFEaF49q1dw==$88jgc40A94VKjMrZOLc9hX8wn8LpqOvRdTPJSlob3CY=','naed acoy','09212577107','taguig',_binary 'ÿ\Øÿ\à\0JFIF\0\0`\0`\0\0ÿ\Û\0C\0\n\n\n\r\rÿ\Û\0C		\r\rÿÀ\0\0\0\"\0ÿ\Ä\0\0\0\0\0\0\0\0\0\0\0	\nÿ\Ä\0µ\0\0\0}\0!1AQa\"q2‘¡#B±ÁR\Ñ\ð$3br‚	\n\Z%&\'()*456789:CDEFGHIJSTUVWXYZcdefghijstuvwxyzƒ„…†‡ˆ‰Š’“”•–—˜™š¢£¤¥¦§¨©ª²³´µ¶·¸¹º\Â\Ã\Ä\Å\Æ\Ç\È\É\Ê\Ò\Ó\Ô\Õ\Ö\×\Ø\Ù\Ú\á\â\ã\ä\å\æ\ç\è\é\ê\ñ\ò\ó\ô\õ\ö\÷øùúÿ\Ä\0\0\0\0\0\0\0\0	\nÿ\Ä\0µ\0\0w\0!1AQaq\"2B‘¡±Á	#3R\ðbr\Ñ\n$4\á%\ñ\Z&\'()*56789:CDEFGHIJSTUVWXYZcdefghijstuvwxyz‚ƒ„…†‡ˆ‰Š’“”•–—˜™š¢£¤¥¦§¨©ª²³´µ¶·¸¹º\Â\Ã\Ä\Å\Æ\Ç\È\É\Ê\Ò\Ó\Ô\Õ\Ö\×\Ø\Ù\Ú\â\ã\ä\å\æ\ç\è\é\ê\ò\ó\ô\õ\ö\÷øùúÿ\Ú\0\0\0?\0ùDÅ²e•ÿ\0Þšx’T?+²cU¾\Ó1?-³\ê\Ùþ‚¼\ï\÷‹G\ì‘\ëš\à\Ôû2y\çŸ‘Ý˜s³q¿Àû\Ó\âIz\Í\"1\ÞOoFžÿ\0Ë¥E\ä)û\âi=œ7\ò©b!FE¾e*‡\÷m\í\ì\ÏZc^c›P$\òÏœd\Æp§ û\æ”\\\Þ9$·–?Ú±ü…DrR\ê\0R0}½\é\ô§¬\ï\Z\ÑI\')\ØA>\Çüi…ƒuÌ’l7OŒe\ñÀÇ§¯?\ã[q™¬m\öù\Ò-\Í\Âüüc²ýOz\Ë\Óä·‰\ÚI\ÖG7`Cû\×\ì¿O\ð\á¨M#³´,]\Î\æi3\Éú\0i’Hº$K„r£\ÐSM\Ô\Ö\×PL%|±ŽOCþJ­\ö—“\ïJc\öŽü\ÍG2C,d3\ÜH\ÝF\í\Ø\ÏÓ¥U\É\ô/¥\Û\Ú\Ís—<>\õ\Üø\È<\ñø\æ‘\õ&•p¦y†s\ò\Ìâ¡ž\î9nm¦\Øû\Ì[eYùHü?\ÎjO·\Ä8;ÿ\0\ï†ÿ\0\nW*Ú’‹™¯AûXž6t®fQ\÷‡\ëT\îE\í¡U[‡U~U£|\Ç\'û§œ\ó\ïSø»o\ðP>¢ˆ®‰o,\Èÿ\0~&LFþþ\Ç\ÜSQ\ðmœ\Ë4Œ>\ò\Ë!$~\Å9$’\Å\ØÌƒ\Ê?t\ãø…Us(\ðùÀžŠT\ïŒýz0¦\Û\Ë\"\Ün˜LøB7•8<ŽÝºP\öZ¢\í¾f7e‰\Ï\'¯SÞ«\ÜL\èJ\Ãq2\íû\Ïæ’‹ù\õ5V9f\ØÁ¼\õNG–«\Ës\ëŽ\ãV¼ø\ì–6Ž¸Ÿ±(Dq\öA1\÷4®Or\å¬ª\ïžY”?!~\ö_\öW\ßùTyxYwDDI\÷#†_•}\ðq“\ïU Ô£\ç1\Ü+žY¤BK¨©¿´b\ï¿þøo\ð¤?A_St8‘\ç‹ý\õl~cŠXo\Þ8/oRfehÁ²	\êR?*i\Ô\"\Ïf=ÿ\0…T{…“MŠÜ‰7K7™8\Øx\ç\ÈS¸-5,Z´\ðZ\Å\È\á\Â\å²Ç©\äÓ\çl<ˆÀ\ä0nA\rV\"\Ü7\Èg‹ý\Í\ØüŽi¦\âDo–S \ô’ý1TM‹—\î\÷0ý§s®\ó\å\Ü c…~\Ì=¥eFnpûT»”\à\î9Ï¡üj\õ¾¦cŸl\ÖÅ¡yr\ì\É}p@<R¸*³’ES€\Û]\ÄH\ÚR\Ô_>\íz\ÈþWüj?·\\\Î0$’5\'·n-þ\ï¯Ö†&M¥£|\÷\n£Ýzr\ìw$¬¡y\Û\â==‡°üh(r\Ñ\" —–<+?\äŸ\â>\Ý*_´Lþ\öF\'«95T¨‘™\Ú\'\à)\\\í^Âš\ÑGŸ–\'S\êªG\ò¤+v-ýªb0e|½Q1r~û\÷X\åP\"}\Ó/Ñ“?Ò”M0<Å»\èÿ\0\Z	k¹ªd²\â\ìRc¤ûNšx{#zG±¿•]‹E´ˆ(ea\Ý\Îj\ìQ¬c¡¢ŒQcK³6m§\Çú6¡\Zú\É\å¯\õ©†j9\Ùy\ô\Ý\\–\â8‡\ïcÿ\0xâ«µüoþ©d›þ¹¡#\ó\éH|\Åhá±•X„¾ù+\Ñ\äg§\õµ±ue\ï\â\Ñ\Ó%žH®EÀ¶+eV`K ÿ\0\çOhÌ“\0·,‘¸Ê”-\èr}G\ê­;}}Ž\ÉF|»\ßû\ê:‰Žš†kº$ŒŸÊ¦m:\Æ\à\óúj\å¿N•<qGü¨‘(\î\0QO”9™F%²‘\ð¶Ú‘\Þù~¸«?b³ÿ\0žw¿\÷\Üt¦\ò\0\Ø‰\Ò0\\þ”ßµ9\'e´‡\ÞBSúQasyzzùû£¾J\î`3oZ”ZXœ—„‘\ó\ÇP’=F)&DX¦S*’~™\'\ðü\êŒ\Ây\ägŒ”)¸\ö8lR°ù‹’C¦\ÅÌ‹vƒÕ¤ŒT;\ô¿\á‡Q—þ¹\ì?¯J€\ß\Ù\Ûd¤J§\Ô\à\ñ¨d\Öd“ý\\l}\Âÿ\0S@¹\Ë\Ût\õšø\Çûoþµ¸\Ów\ò\ï\É?\íG\ê\õ”\×\Èr\Ê3\ê\íŸ\åH–P]º€Ñ”ýzE…\ÎÍ˜¤\Ò\ä8\Þ\õ\Ç2F*VŽ\Åyû6¢þ\Ê\ñŸ\ëX(\Ì\Ã*\ÊA\'u=\ÅL—“\Å\÷cý–~F‹;5„šXá¡¿Œÿ\0¶È¿Î¤Ž>Q•Ž\ñÿ\0Ý’3Y\ñ\ëÄ‹\÷_þµ?\íV—\r¹¡\0ÿ\0y@?¨¢\È\î]x¬#PLW\Ã,\0\ñ\õ\'šØ™dŒG|Y1»\ç<\Ôyr\ÞDi\Z8\ÃH\Ê\ÌH‡_©ü©\Ú{L\Ñ\É(H\ÜJ\æB	*\Ü\ô\çžØ¢È®fX\ÖC$\Ã|~D\Ï`¬wYjŠ?½\òúf¥ûIO¿«\î¸qúsúT©{ùVU\r\è\ß)ýi\Ø\\Ìª¦K\÷V\ï\'±’<þU!µ°\Ïú«\ßû\ê:³$+\"þ\ñ¯ûC\"«›	ùU¢>±9_þµf#Ca³&;ß¦ø\ê8\â°iŒ\"+\âø\Ü@h\ð¹¡dKDi\Ù\ÞUR6‡\ÆIþ~?—¥:\Æ9\íƒa\È\çs²?9\ôÁ¾´š3${N¢+\Ð=\Þ:Ž[KH\×\"\ÛP—þ¹´dÿ\0:³\ö\è‡$±{²~c\"¤Ž\â)?\ÕÈ­þ\éXžc\î´\è\Î\rFÿ\0MB-vžFB^£Fkz@$]­\È\ô5F\ãF´Ÿ¬A[û\Ë\Åfm\ÔÀ‚c· \íe\nY”úÿ\0…\'ÙŒŸ\ë&šA\é¿hü†*µž nU\È78\Û®~YGec\ë\èß…=§”žd·\0\á”/Ì§¸;»þHMR\ÚO\É©\õ\Ç?:K¨b8’tC\è[ŸÊ³\ä’\Ðýù$¸?\í1#\ò\àTMªAlv\ÇGø€!LE\ö»YA	Òƒ\Æv\í\ñ8ª\öª\ñ\æ\Õ\×gW‡\r“\êW?¨\÷Mµy¤Ï–\õ\Ô\Ôsr\Ì°T³TúŒTKS^97®\é®\ÊÁT\Â\õ\ê$³Oœ§š¼ÿ\07\ê\Õ^\íú\Ý.\Æ\èˆùeU8\Ãwü;bjª\Û!\ÆÈšS\ë‚ß© N\÷Ðºú\ØAˆa–þU\êwRºW\ò_\ñ4i±’«\Z\ï·øQ\å/\ñMŸh\Ç\õæ‹…™]\Þy%\ÆG#©\äs\ÔÕF$’\÷\Ì;¼¹\ãYpOCÀü)R°´\Íþ\Ñ-þ4¸cÞ«²00§®ÑžF;  v\ér\íŸw\î #Ü€¿Î¥\òI\Þ\ñ\Ç\õ9?Ò¤¶F¸b$r§°!†:Ž\Çò©…œIrƒi9BNN2r=1HZ\"´b\ê\á!..ç‘‚¤0¯.}\0š\ô?	~\Î?<a|Ë¤xSTšt\Í)©a8\É.A#<V†¼I©xY¶\Ö4+Ÿ\ìûø\Û\Ë¢)\Ê7¤A½“Eý­<Q\Äk³\Óg›•\Ç$2	8*\ã®GJÂ£ª¾th¹m\æyŠ>\rø£À\Éru\ê‘Ú±I¥{f_,½¹†WŽü\×$!‰¿\Õ\Ì\Ã\ë†Ÿÿ\0^½\÷Zý«¼_«i\Í„z~Ž³!V’\Ú\î\ë‚\äŒ\ó\×\âÛ¡’`PGû\"\'Q¯\Þ+\n\\ºX¤\Ð\Éü9\ÝlW’r\ð0?\ÞŸ\ÔVœ\ö±\ã\\¹X\ðs\Ó¹\õª\ó/”O—>@8,\Ýú¹>Â·\'FGmˆ,n\Ü1Ì¬!MÇ§\õúŸÊ£Gš&Ê>™Z¶m§\åž\"“¬y\÷%s€\n‚A\Õ\"\'ˆ7 ¡\Ç\éÿ\0Ö¤šµ\ÊU˜{€\ßýzu´“\å’5ú\Ð\Õ_%q\ò\Íÿ\0?Â‘\í¦î¬‹\ê­þ4\î+H¾“Ú±\Î\Z\ê™_\åS\æ:\Ç\ÃL\÷‘°\Üzg\à\rb<K\Ë#D}_\åZ2Dt\ëŒ1ûTÀœ“’«\Üÿ\0Aù\Ó\Z\ó\'w3Ý¬‘\Æ&··\ÈL¶7?v\éÏ¥X\Z€ÿ\0–‘K¾\Ý\Ã\ô\Í`£\Ë`\001:j³\Æp\è\Ä´¹ýE!sIu„•	\ô\Î\åD°,Ÿ\ë#V?\í-fV)\Æ×\Û þ†-±?+\Én\Ù%ú\ÕH.]û6ß¹$±{#œ~G\"‘M\Ð}‹4nq¸™S»1`Th\ó\ÂLÇœJwf#ª^^ý¤ýš\ß/6K\Zv\ìO¢ŽÃµ&4º±\ó\Ú}¡\ÔFƒ\Ì*w&0$\Æ9‡\Øÿ\0\õ\ê»\Ëý ªŽ\Ûn‡Ê“ùh?¸\Ùþ/Cø\Z\Ô{v„Fb]\ÊT3&y\\\Èüÿ\0q\Åe_À$z2N77Qœw\Ç\à{PBvÐªmÐ¾\×\Þ\Î:£H>„\n™-\äS„ƒh\õ8_þ½MžU›+ž~bIg#¿~µr;¤#Î¹fÂ‚ùü)1\Ù|†S‡š4>ƒ“þ\n<ˆ‰Û‰&c\ï\Ðbµ-l\ì\ÒËŒ\ÈG;þ]*H\Úç˜©Ø¤ŒaHt{R†ZZÌ¤\"[ùa\Æv’@`=G\Æ{Ô‚\ÞsM5\Ü187\0\ô\éþ5¡,‘™\ã`Ä…G\É\Úx\é\íQÌ™ƒ§ú$G\ó\ÍK}`¹Šf\Â\Ö\ò{1\Ýþ7“\í{Q†\r’ž;dŠ\ì4OX\ê>ƒX¿\ñZ8¹¼{{vt.:n`xžqU,¾jW>9O\\b\Öø;y²\æ\"\Æ˜½71Žj9¼\Ë\å‰Î¤€J\ï\ö»s»|ü`cÒ™XP(»¶8\É\Î\×>•\×i%¼øq\ái\ï³Á\æ³],[ƒ* e!Ix0\ï\Å%ß€\ívi7šn½©¢__¦œ\÷«F\ö\Ò1ç##‘\Û\Ô\Z|Â´N1â›r\Ý[ \'%@}¤ú\ô\àûŠ‘yš\òÝˆA!\óŽ:\ñ\í]ž¡\ð§U´\Òü_}	{´\ð\å\êZJ±@ß½V\Ûû\Ì\ä\ìû\éÁ\ÏS\Íu~Ì¾\"—\Å^!\Ð\Å\ì\Óhš!\Ön\ê2§„Vhv†$2\ó\ïÒ—=·ah\ö<\Ý\\\0n\í†7G\íøS·…t´Û¤ž\Ïz{×®7\ìÉ«/ƒü\âF\Ö }?\Ä7\Â\ÂO.Ù‰±‘¦\ò\Óq\Ý\ó†\Ú\ä—qT¼_\ð!ü+\âsF:\Ò\Ý\r.\ñ\í|\ájT\È6\Ð\Ç¦OJŸh»—\ZjN\ÑZÿ\0^g—B\ëj‚\ê\Ü\í\0gþª\ê®\ç\íV\çq\î¿\é_B_~\Ê^\Zƒ\ÂM\âK‹V3im3\ÚC3è³¨–\áT·”~\à\Ç•ƒcû.j:Ž\à\÷OiVú\÷Šl…æŸ¤]F\è\\\÷\Ì\èž§ 4{E\Ü\Î\Ñ]?¯¼\ñK€’°o¶@0¤mù\Î=½ª!n…Fn`,1\ó~\0 \àqÒ»¿\nü\×|I­\êºm\ÄiM¥†R\ÉJ© vM™RT~s\Ñj…|e¬X\ë\×Z¶«&ŽQfx\à\ó¾ffS\Ç^\n\ãZ®o1µžûH\Çü|[Ÿøÿ\0…2)’8V?´Û°Pv¿?¥v¿\æŸ\ÆZN‹ý©–\Z¬-qiª\ÛÇ½%Œ!oºH\Ã\0#=Á¬\Ô\ð-Ì¾\0¾\ñD3	V\Ê\é\àš\Ô&0‹€d\rŸV\àw\âŽo0´{\ãEn-=¿-ž\Ça\è*6µ„°)u\n}\Ï\ô\ÍwR|*œø\Ù\ô\õ(¼ˆ,“P¹\ÔgbA	@\ÌJ\ä\äŒ\às\Ï^1X~\"\Ñ\ôm1meÑ¼@º\Ü\în\Ð\Í	Áe<ml\ð}ºS\æ\óXÿ\0_\ð\æ–Ó˜‰Š\â;…,¨øº}\î\Ô\Ù,¦@^k`\à_qlù*\õºå»	!\Ï\æj\ÝÜˆ\Ö\Ó\"\î,P€žN>•i‘?u˜\Ô\ã\÷±ŸL\ä~G4¾KÑ¿³\Z\ßf£\r\ÃÑŸ\éU#²¶tù£*Å› a\Üâ™Ñ4gC¸{a©‰i\æ\È#\ó\É(\0I\íZ²\é\éæ°\ä\nü\Ï\\\ôª\êH±m,Jr@# \Î©Ö˜´#y\"\Ù\Ú\è\Änvi\Û\Ôú(\ì*ü\Ëk’rÛ¹”pN8_l\÷þf«\ØF¶\á\ò¡VV™±\Æ=qŸ\Ï\õ­E´e‚Y¥\0\Ë(U?^Þ´	–¿\à‡°x.4\åyb\ò¼Z1\ò\äQ\Ï\Ô{×œ\êQ\Þ-£\0ÀÐ±$f¾ˆ²™f\ÔcR­\ñÚ’Ñ¿\Þ_™y\ô#Ž\â¼o\Ç\ö[\ë\ö‹b5š\Îu^›˜œ‘éšˆ³iAnŒ¸!1Ø»¬\Ó.\ÝÁ@~¯-ŽF>\ÓsŒÿ\0\ÏJc\Ä ³š1\óvPOž®–8\õª3¶¥[3%¼R5\Äû™A8’ˆ­ZI¦Ssq„m«û\Ï\öAþ¦¬\Ùÿ\0Ç•¹ÿ\0a•$\÷·?\õ\Óÿ\0eZ\ÅY­ŠH\\\Ü\0cf?¼\ô#üjû\ÐKX‘«—¼\Ì;~\åÿ\0˜ªR¯\ôµ‡ÿ\0A¬\ämKvzN™\à½W\Æ\ß\ô›-*%v\Z\Í\ÃM,®!BŒ¥\É=@\È\àd\×Kuq¥\Ý\ëþ6\ÖÆ³›cg¦A\á\èu©t[‡S½\×o\Þ#\å¹\æ¼cO\Ónµ‰\r½²;©\å\Æ\ò#\í\Ûük²\Ñüe¦”’p.§\\©0½	¦±“H\í§‡W¦Ç¬xÁ—^%ø\Ã\á½_L)qc«øj\æxo\Ü2Cr-\Ô,Ž‡<\éÒ³¼Y\à}1~x_Ð \òü?y,¯sb2M¶¦ƒ*’>Iq…|\×h=/Ã¾Ÿ\Å6þ ¹²–›D\Ó$\Ô\äŠVp\Ò\Ä$[N\à’{\ó]6•\ðW]»\×|§–³¶“\Åv\í{g3v¨‘\îc2Œe„e\Ëý\à3\Åg\Ì\Þ\Ãt£JmNKO\'ýzLYkzÏ‰|5\át\Û_°ø\ÏM—Wš4\\	X$&,\×*ÿ\0~…yO‚\õƒâˆÿ\0u€\Û\Ò\ë\Ãú´ˆ\ÝG–®‘§\á¶5ü\ë\Ìü\ð\ÏS\ñg…¼O¯i¦\×\ìž„K0g=\Æ\Ð\ï¶>\înÀý\à\Íjx[\á¤^$\ð½Þ½y\âM\ÃZ5­\à\ÓMPÈª\ÎÑ£¹H[pN~\ï5–\Ì\É\Æ*-¹v\é\óý_øw\â»3Áÿ\0\nü/®\Û\Å/‡üCovÁŠ\î+\à\ÖÏ»°\ß\òû3/½y_\Å5#â·pÿ\0\Û\äz}Ú–\Ãà¦«u\ã\ÝSÂ——\Zn•si§I©\Ïp\Ï5¤–¹B$Œ®\Ö\Þ[ ÁF\È\Ï5•\ð\åuo\r@\ñV…®ƒ¦\Üê¨Y,\Æ\ò¹16y26A\'Ž½©4\Þ\Å\ÂT\éO››\ðf£¶\ï\Ùÿ\0O?â±œg\×ý«\Ô<6ƒqm\ðŠ\Â\ãK¶“\Æø^yü;«^³4\\¦1F[–\r\Ôl `šùþ/\n\Ü\Ë\à¨|e‰­§\ÕJKDn\Zf„8aü<\ä\'©\ãš\êuO\Ùÿ\0UÒ¬\õ\Z–‡{¯i–‰¨xn\Æ\á\Úú\Î½¹OBW;°»}³‘–‚¢„´r¶¬\è>\Í„~^j\Þ\'\ñ8\ð~§«x¤M5\ÕÕ»\Ê\÷?b5\Ä%c\è\Z_81\è\é\Í\"ø\Z<Y\â:f“¥GqªG¦jºg—(€2\Î\Î\îCŸ—I99\é^cu\à?K\ðn¢“Y¼^,ž\ÑG˜L¸Ž#\æ\Ãn2%z\ãœ\õ«2|–\×W\ñ]¾§®\é:•á»¨\ì/uË¿;È’W\nbŠ4y\á‡\ás\Æs\ÅÝ­H\å‹\÷¹—\Ü\Íø`žø›\ð\ÇÁ\Z>µk¯júm½\Ê\ê\r8gx™ü\ã Ž8 m\Î3^}\á]Lx3Àdj\ö¤iWž#¸\Ó/|\Õù|·‡’C”œv\Ív\ZWÁIo/¼Ugq\âO\ézw†E¼³\ê«#Ki\"\Ì£\Æñ‘·Œdr\Ø\÷<w‰|)¥\éÚ´\Ö0jzg‰m(È½\Ó\Ñþ\ÎÄ®\n\âP\0$v#ž\ÕJ]\Í#F¡?Ášº­´S|H\ñNæ¥½Æ­\á\ØmlšV\á\ÝU°¹\ï‘Ï¾\rp\Þ4ð½·‡<¤&¡¡[hž(’\ëk\î\ZYe\î‘ù nb8t\ÅR\ÕüŒ¦K;†“+œt\ÚÇ‘ŽÙ®V\æ	a\Öq Ÿ£y¤–ü\Ï5ªwÙ‘:ƒ\÷‚\Ý<Ä‘72\å\á\åù[šÀG®³\ÜnU,	“¸V\Ó\åi¿Þ„ÿ\0\ã\æµ.ú4\Ø?\òÍ¿•m’®\åE±\Þ›‹Ÿ_\õ”\È-<\È\Éi\î3½†|\ÏF U\ô?»N?„*ŠÕ€Gþz?þ„kDeb©´ÿ\0Hxüû€ªª\ß\ëOSŸðªº¬^Dq\âiœ6\àU\ß ü‡¤0o%\ô\Í?›Sg³[Û‹hX•Vg\Éx\ô¡‚Z™:,bk‰\0R\Î^0€rr[>¸\èúw‚|»\rJ}D~\ò=¬¶À\ð2\å¥`ü&H¥›V‘\ãV–8m\ÙŽW\÷¼\â½CP`\ÇY¶B¾{ª°\ÛUW`ù\n¼u?†j\èi«]œ­Í¶¯\à™E¸Ž]kFŒyª\ñü·ŠI©:s\ÕOq\\¿‹¢þÔ»°\Ö-.b¼\Ó‚\ÔI\Ë$eOD\ìH\î2N•\íW#n¡z@ÁK,\ä}dÿ\0\n\ñYAm\ã\ß*’5?dÜ¨0	!K{\æ¦.\åIr\ìS¸¸ŽX\å	¸î‘„nFüç§¥L³§$n?\ðÿ\0\n£5š$[‘\ç\\¸Q‰[\0Ç¯¥X\ZlaI\ó.8þ[µha­\ÇZH«m\n0u`Š1·JHe	$åƒ€\Ò?vÜŒje®¶\ð³Kq¸¢“û\ö\êGÖ„Ó‘\Ú`e¹$*?~\Ý0=ý\è\Zu\"¼\Ì\Ä>Á\ó\å·\\j\Ô\Ó|%.§s“¨·ƒ\'øŸ\äþoz¥{á²ºt·\r-\Ôqy•Zf\Ë\óŒ\õ\é^¹}¢\ßOq\ÖÖ†h\Z\Ò\ÙU’D\åˆ0XW5i\ò\Ø\ô0R“s\ÛþÅµ´†\Æ‚\Þ1K\ÑWùŸS\ïS`\Õ\ñ¡j§þa²\ß\è¿øºš\Û\ÂZ\í\é\Ä\ZL\Ò\ÉÄ±`}~z\ä\æ]Ï¡U`‘\×ü¸‰~*\é\Ú}\Æ\Z\ÓXµ»\Ò\çŒ\ôu’\"Á?\Z\÷}s\Å\Ú]Ï…<w©\ÚB°\ßx\r®´+	¾\ëKP«~.vÿ\0\Û:ù·N\ð‰­n!»KK›\áI\ö·1,±°\ä2°#\ÔVÕ§„µ{;k˜\ËR’;·Y®’[\ô)s\"±ey\ÌÃ°ff²rI¥\Ïç—‰¦ªÏ™?\Ì\öo‚ž\Z“Âžø}¦É­iZm¦®n¯\õ-*ú\èGu}\r\ÄFeŽ?\ãBž{\ôÉ¬\0xc\ÃV5ø\Ý/F™ŽWI’K9¼“«\ZDŽ\ìDxv9\Ãf¼üø{Z˜¤’é·³\ÍqE\ó_#\ËGþ©QÌ»!\åB‘ƒÒ.¯Ü¥\Ê\\Y\êwQ\ÝNn\îcžý$I\ç ©šE2\á¤Á#q\ç…\ñ\îqº5\ß}\÷={\ÃcTo\Ú\Ç6:®É¦øf\çO´Ñ­wä³\Êû<H\ÝO˜­\×\Õ\È\Ç\Ë\\\ç\Â{)´¿Œ\ï%\çƒn<k\'‡\ïf‡B›\Ï*cH\ãY\n¼À3n<p	\Å\Ão\â¨/\Íú.¸º—” ûpÕ¿\Ò<¡\Ò?3\ÎÝ°`|¹\Ç“\Ûø²\ò\õon»5\ò\Â\ö\Ëw.¯ºe…þüA\Ì\Ù\Ý\Ôp{\ôsÇ¹>Â§Km\æw\Ñx\Ã\Ò|\"\ðNµ§xj/\nh¶~9µ–\æ\Å.š\å@H·´¥\ØÂ•$v	]6“\à\Íc\Ã´\'‹|_©Z\É†- ¾ÔŸU|}žxe†=ˆ­œ1H+\Û`\õ\\ø—\ö¸–«jºe\ò\Ù,¾z\Ù}¶?³‰vl\ó<¯7n\íŸ.qœU‰l¼O6™›-¾­.•	=:MMZ\ÙéˆŒ»F;`v\Å\ñ\îW’½žþ»™\Øh\Þ\Ö|o\ð\ë\à|º.%\ò\és5†£\ä\àý‰\Ö\î)™\Ï\0,mÏºÿ\0xT/ª\ëw~$.Ÿ\á\ñïµz;]VÁ#Þ¢EX\ÝN\äu(ÿ\0>6ü«–R®R\Æ\×\Ä\ÚP¹[}[O[‘‰Å–¤°	†1\ó”gŽ2y\ÇV\ÇL\ñ‘(“L±\Ôt™D~O›§ß¥³ù\Ý%%cO\Ú.\å{	+¯»~\ç¢øSÀ\Ö~\Ôþ6øoK\ÑÆš]’\é\â»¸\rºO%™w!n d\ì\ç5\á~+Ó®´¿j]ø~H\òy\ñh\ó‰3m}\ÅS \Ë\òœ1\÷\ô®–\ß\Ã\Þ#°šy´Ñ¬iÏ:kQciˆ$ƒ!‚\ç,\Ç$\ç,}k3P\ðŠ\õ+ç¹º·¼\Ô\î\Ü*µ\Å\íúK!QÂ‚\Ï!8†q\Í\Ñ}Nº”\'\Í\'ù\ßüŽ_\Ë\õª\Z¦mª\Å\åÎŸ0,‹\÷“\éþ\Ð\\xs[µ•£ŸJ–7Œ\Ñ\ñ\\\ÔCB\Õ\Ï?Ù¯ÿ\0\âÿ\0\âê¹’\êz¤d¬\Ï/¹\Ð.\ôÛ©\âh\Úec\ãRw“!ÀÀ\ï\íW\åÐµF†E]*ø’¤\0-d\ôúWs¬é·¶\ñ$³ÀÖ»­a°•nYweJ“‚29®\"\î\ËRµ´Ž\è\êZ‹E*.\äùIÁæº©O™3À\ÄAFZÇ¡j«\ZÒ¯Á\n6²z})x{RErt\Û\ÐLŒGú43\ôª‘\Ü\Þ\Ë\Ô\õYA?\éo\×ZlR]\ÈeÎ£…€>\Öý8\÷®„\ÙÇ¡p\è\Z ¸g\Z]\îÒ€g\ì\Ï\Ô\íH4»û{\ËI¥±º†\ï’HUAF’8\äø\Õ\\\Ý}§o\ö…þÐ›€û\\s\õ©!3‹û${»\Écy\ÕZ9.]•¸=A<\ò&Ý†·E\á\Ý2\óU\Ôe[K\ÈV(pAžb’cO@F77\ìOÒ¶“«ø\Ö©¯Ìš6‘\n•³LùÓ’¤«9<œ\ã«s\èrŸ¬a¸\Õu4K+Ã§´‘ùœ„`Àn\0\ð	ü\ëÛµE/>´9´^\à2TIÙšB<\Ë]‚\í\Ê\Ý\êŒGcÿ\0F×xÁÁø”À\ñû\ÛE\Çü+\Òüo¼U\âu\Ó\îuQ.\à‘\Ò(\Î£q·ÔšÀø‹\ð³\ÄZ‹“[–\Þ;\Ý.[˜3sg–\ò\ö€¿:™s·=Ç½a\Z°\æ\åoSYE\ïn§Ÿ³¶Œw.Ÿú«NH‰\Ï_”ÿ\0*§€\Ö\Ö\ì¤Î‡#\ëV¤;`—¿\È\ßÊºŽK…¨ý\Ä?\î/\ò­\ï\n\è\öI\î\îW6\âvØ‡ø\È“\íüê¯†\ô\íFŸ\"\Ú%]ÿ\0\í—ük±\Ó\à(EÄ€\ÛÒ¦O¡´!{6W\Ö4\Ô\Ôn„.\í\Zµ¿U\ê0þ•\Ï^|=±‡\ì»%q¾uC\ò˜?\á]†\Ã&®Š³p¤\ï5\Õ\Ùøyt\ét©\îT5Ñ¼\\/UA±\Ï\âk>k\Z¸¦pR|·Múú\æy-ü›i%Ž1\Z\ï$!#>>´]|(Ò…´F\ò\"M«Eha\ãˆ\ÙÂ°\Ý×¦:\õ:C\ámfI*9²[\Ý\ry‹|Ugom‚,u3rF3½\Ãzžr–\ÆsŒc\Ð\í-~xGX\Ô5gN‰\'hcŽ§Ž0¨z’I%ÿ\0ª´\á<!–\å\Çý¿\ÉXZ\Å!mkq\õ¬“\Ýý¦I$xvª\Ø#Ž\Ü\Z\Ö_‹VŠ?\är\àiS\ïr2\Èý<\">\Ò\çþß¤©\ì\ï\áÿ\0-\ß\õý%Rmÿ\0}\Ïý\ö”‹¶¬?\är?\àkK\ß\îW\î\Ëÿ\0\ð\ÏÁý\óû–øgOcýsÿ\0\à|•Dü]´\éýŸsÿ\0}­\ð·l\Çü\Ã\îïµ¥j\Ã\÷e\ãû:øG·þIþ4\Óû:xH\òÓ·þIþ5L|[µoù‡\Üÿ\0\ßÅ¤­ÁÓ®?\ï\â\Ñi\÷Ý—\öu\ð‘\ó\Ûÿ\0\ä¦ÿ\0\Ã;xMý\ñÿ\0À\é*£|_¶\í¦\Ü¬‹L?-±ÿ\0 \éÿ\0\ï\ê\Ó\÷û‹\Ü-¿\ì\ï\á3‘\æ\óÿ\0_²U;ß¾\Ðä±¸X\"¿\Ý-´°Kr\ò†WV\0ž!NE\â\í²Žt\éÿ\0\ï\êÿ\0…d\ëÿ\0M\ä‰el\ö\×Ku«$Œ®\0\\±\ã”\×?q>Cœ\ð\÷\Â\í\'W’xD¥P¸@ûˆ–EÁ\É\êQx‡\á\í‚]\è0 UIt‘3ºB£sQÏ¯^µ·\ð›^“P\Ô\õ\ïZ1wp#x|´\Æ\äË·\'û\Øo\ÈcµuE\æ\ë~WEx\ßBue# 18ªri\ê(\Æ-#\È\î~\Ù[F²¤§r\Ëü³Q\ÕÀ®¶+‹Oû+~\ö5a\Þ>\ð\Çz\Ü\ñ‡NŸd\Ó\Û\Ö\Þde‡S\ï\ô\÷¬\ÃüC\ØÒ½ÑªŠOC\Ï/tWÓ­­fB^\ÞXÐ«ªvŽ\rf[º\ãþºŸ\ä+Ñ¬­\Ò\ãG¶ŠUÀ€¯üW\rw§É§\Þ\Ý\Â\Çr‰~W\Ä6©ü\ëd\îs\Î6³EUÿ\0¯+ÿ\0f©\âÿ\0M$gý.?\ëP\É\ò]\Ç\ïÿ\0Ð…NAûNž@\Éq\èT\ìJÜ—\áqÆ¿©¦:\é“þŒµ\íWc}Ö¤;µšŸ\ÒJ\ñ‡\×I¦øŠ\ê\â\ä46\ò\Ø\ÝF’²6\ÆcÀ\0\ã$\õ\ëo\â\"[‰˜\ê0…’\ÉPH\Ë\åþ^y”\÷6†\Â|@\Þ?³\ôN\ñ\Ïþ½{Ž¬ \r3ûB\ê¥|¹\á_\"\â\êú\ÒQ$ÿ\0b¸Š\Þ\âÅƒ••”%O#Ÿ­{7\Ã\Üx‹\Ã:C_\ê_\Ú\Zˆ\ÔP°•”\Ì\Ê,2£y\"¼š\ô\äŸ3;#.†\'\í\à\Ï\Ãq\ë\Ö\ÖIiª›øbi-þE”1l\ïQÁ<u\àûš\ð!®X\ÐeŒmÿ\0 žk\é\ÚJPŸ-ø\ÏüL\à8ú,‡úW‡Yh·¶\ó«¤/=\Êù(\ÂC„1ý\ß_\å]xZ–¥\ï>§-X7=¿¦\ÚEagŒ\" \ä\õ\'“\õ©\ôÏ•\'\Ï{‰Oþ<j.g1g\ìñ¤¦Ç’?»\í]o†ü3u§\êsÇ©Û›yc‘\æH\É²\Ç¬8 d{\ç·\ö‘n\×.\Ít,øoBû6µ\ö‹•ÿ\0Hû\"²)ÿ\0–`»~¼~¿ª®Û(\Ó\ßþÒ’’\ÝO\ö\ìùÿ\0ŸXÿ\0\ô9)uU\Í\æ•ÿ\0_-ÿ\0¢¤¤\÷C\'\â[cÀ:\È\õ„üxWx\ñDºþ£(e7sB7(¯g\ñ Ïƒ\õ€p\ÑÛ¯\á\\7´;kY\ï¯6nž\à^»o\ä)[¨T¸&´ƒ\ØÎ¢½\Ì\Í\áWü$–:E\ã^F’^	Ü—Œ\È\áQˆPI>†·À\ÇþBQ\ãþ½mxR\Ö\ÎoxY/-“tÀL@\Þ1\×\ë]0\Ó4/ù\çcÿ\0üj\\\ÚbP]Žþÿ\0\óL×  ~\Ï\è[Es\íhµ\ß\r+B\Çú«ûø¿\ãA\ÓtA\ò¬?\ï\âÿ\0.w\Ü|‘\ìpŸ\ðÏ‰\ÔjKÿ\0€‹AýŸ”ÿ\0\ÌMGý¹­wƒM\ðúžb°Yüi\ß\Ù~ˆ¬ý´_\ñ£\÷${\0ýž\Ãÿ\0\ÌL±­\ð\Ïi\ÏüL¹ÿ\0¯E®üi\Ú?»°ÿ\0¿‹þ4\ß\ì\ß\óû«ûø¿\ãG;\î‘\ìp?\ð\Ïj\æ&?\ði\áŸMqÿ\0^k]\ï\öw‡\ðw§ÿ\0\ß\Åÿ\0\ZhÓ¼?œùZýü_\ñ£™\÷${ø™ÿ\0—þJ-bx›\á$^\Z°\Õn\Ú\ð\É-¼H»b\ò\÷+\ÊU”\àŽ0+\Ö\r†€IýÞŸÿ\0ük˜\ñÕ¥”>\ñ0±\ò>\Ã7ÀŒ‰›®\r5&\'\Ûc”ø_b%\ñ”\Ï\Êù+ªŽù\Ê\ã\õ®\Õ¦·\áOûÎ¿”‰V<+£\ÚYk\÷WP®\Ùg³³/\Ï*IÀ\÷#5Yy\Öü\"}2\è~R-7«U¬lk„¶9=š#ÿ\0‘R°üK\áÕ³•®m\×3a\Ð>ž\ßÊ¶\õ\â‹r?\Ü?ùk]¡Y\åh\äP\è\Çk)\èEfºž=¥\È*\Ìÿ\0\ÓþUy¦® ÚŒgü\ådoC\å¯\é[‘i\ÇN°µL)£\Ý\ë’1\õ˜\'Ž\Ûÿ\01\ÑtÁf?»Z\ÙnF\é&p7Y†\ðS¹#|¯|‚8¯|\ð‡À\Í6\Ê\ãE¹\×$]bK\ÒP¥mˆ\÷¯\\ƒ\ë\ÇJ\ñ%±½Š\æ¢b\ðÈ²pyp\é_Z\é\æ[ø]º–Aú\Û1þ•É‹©(\År»^\â£\ä\îyGÇ‹tµ\ñŒ‘Æ±Dº~\ÔH\Ô*¨¶\0€9¯3ºC,*•\ÕÀcŒ\àƒÖ½\öŸ–Xu]3\ì\óºŽÁˆH\È/Ÿ=\0uþ#^]{ª¥¤l\Í¬xû\÷.?Eþ¢³¢›§n\÷g–Z\Úùºp\Û\×\÷±HOq\Üþu\Ö\ê^)\Ôt^%µ\ò\Ý\"‚Ü¨qµ•¼”\ÉaYú„pMpŽ3\'ž0À\r\ØÀ\ïIâ¨·k’©°X¢dŒ0\â\ï\Ô~u\é=^§]–‡Y©|c¾\ñn¬\Þ\\\Ço\Ë:´Ê³\á•Xœa\ñ\ój\ïü$‰ª\Ão\öI‘”J¦G¶”7?\ð!\Î;W›M¢´pLP\Üþµ±¤øZ\ëJ\Õl&œB<\Í\ê»\ÚOf$VnœR\Ð\Ö5g¬\ØKowb\ÞE‘w.v¶H%†sß©\ï^Ë®°MV\Ü0ÉŒœg\çN•\ónŸ\à\Ûy±Gz \r­+)n:a\Ãú\×A&µ\â=[W\ð¸\ño„¼+o+´rnÃ¨|°\Ãd#\ñ®O`\ÕE$\ôW:=¢q±\ì6m¿Zº>–\ñüzJMU±¤ÿ\0\×wÿ\0\ÑOE¡ÿ\0‰\Í\ö?\ç”?\ÎJnª\ÃûGG\ô\ÚSÿ\0š¶2e/ø£us\ß\È\Çþ<+–\ñó³žùŽû¯½\ì\"ºýcI›\Å:v£¤Z\Ü\Ú[\Ê`Gy.]ø\Ý\"ª¨H\ÑÙ‹\Ø\íYz\÷\ÃýKX–\Ò\ÊM[G³¹œIis\ö\Ä\ó\Z[”u\ÚZ\Øªœdš´\Òj\æS{øFKXtÆˆFmn6ùªgzúƒ]1¿\Ðûµ—ýú_\ð®cÁ:‡“\á\Ï\nLc”—\Ó\å;aB\äe\Ð\óŽ\Þ\õ\Õs= ¾ÿ\0Àgÿ\0\n†µ-lG\ö\íu6?ŒKþý±±ü\"_\ð©—]\\©¾\Ïý{?øQý¼?\ç\÷þ?øPQ\Ôt!üV_\÷\è… \Ôt :\Ùß‘þ(\×@ÿ\0–7\ßø\r\'øRÿ\0\Â@¸ÿ\0Sÿ\0€\Ò…+0¹Ô´\"3º\Ë?\õ\Ä… \Õ4B1º\Ï?\õ\È…=ü@?qÿ\0€\Ïþƒ\Ä@u·¿ÿ\0ÀY?ÂDDuû\Ö\÷\ä…/\öž‰·³ÿ\0¿#ü*S\âýEÿ\0þ\Éþ‡\ÄK\õ\ä×«ÿ\0…=B\ä-¨\è¸ûÖŸ\÷\è…s^5¹²º\ðÇ‰þ\Æ\Ñ]:-\ÞR\ãŸ9º\ñ]Rø„@‚øÛ³ÿ\0…s^6½[ÿ\0ø˜„6\éH1<LŸ\ò\Ô\ô\ÏZq½É–\ÆÏ‡À\í\èl¬þ:\õ’\Ý{Á\ãþœoWÿ\0\"\ð­-	‡\Û\Æ?\èfG¬¢3®x;þ½¯\×ÿ\05]Fú]MŸ¨:%\áÿ\0düxV\Ü<\\\ç¯\Íýk\Ägn…{ÿ\0\\ÿ\0¨­\Å;.?\à_Ö§¡}NA,—Q\ðÍ´jT\ÜDŒ\ñ¨#w\Ù\ô8¦-.\Ç\áªj\Çkow6¯µ\îdÀ‘\ÑK\02yÀ\n\óû·\Õ\Ó\ÅlºQ~l[\æi’°Kƒ\ó°8\ä/A\ë\\Æ§\áŸ\Z^O4f[HdE\Þ\â\Þ\áK\r\Ù?}›$\ðsŒSœ9´½µd’4\Å6\Ú\\Ú…\Í\ì‚Ey­\Þy\Äh¿»Áù\0\Ë}\î)º\ç\íª^\Ù\ÚZZ\ÞH\"¶@‘‹<Z¯	³%‰.r	\î:\×«i“\Ý^[\0¥Ð«1Á$\ì\õ\'\Ø\ÕtÆµY<Ø™¤C2´WŸLdŸ\ÌUª0z´fêµ¢6\á\ñ\îµ®Ò¼Q•µ\ó\Él¸a,1l’OÖ¹Y:‰¦d‘\ÈÉ‘\æ\ãš\ßÑ¢\ê\Ñ\ì‰G\öya±1Ÿ™$äš³e¥Z¨³>B3\ó¹þcœ/­l´zÍ·fÎ§\Ä1°y\Þ\Ò^YO\ô­Y\ãr—Q(ä À\0®R\óÄ¶Z¤‘®¥7kwR\ÄroË­E¡u\Íje £\Î\ÇN Š\ãpv±ÔšoB\ô(^I$d1\ä*€H9\Æ}>µÄƒ[\Ñ&äŽŸ\ì\ÓWSY>\ê+ÿ\0\×9‘¿¨¨$œ¾³¤L\ÑK0\ÎYÝ—€1\íœ\ô5š‹OSW±\í\ì-tOI{§Âº}èº‰<\ë\\\ÄH,r\\øŠ\ó;I\Þ\î\0<¬\ÒK%Ü¤³“û\õ®»â·Ž´\Ý[BK\Ö\ãu\Är³\íu+‚\Ùßqz5\Ì7\Ã\Èh\ÞdšB\è¬&e‘\ÔRÂ©*^\÷s*­{Ma³`5Gþ¹Áüž™©¶um}^sÿ\0ø\Ôvx\Z\æ¥þ\ìÿ\0o\ñ¥Ô˜m\è\Ã#þ^þCÐ‘\r•|­Xx{Æ¾%\Õ5\ë;kY\ì˜\Ë}q\äF\Íæ©¿’1\Çj\õOZ^_xž\Æ\ëR±´n`t”Lf\ã\ÏHý\ØÁm¥F\ë\É\Å|ù\âA¬i\Ú\íÕ­½Ý\Ë\Æ\î—\ÖQ\ÝBû26\×X\ä\Z»®üXø‚—Ù»½µYaŽ6\ó\'\Ó!ª\Ò~^prG¡\äT\ÊN\èÉ»]2ß‚ndƒ\Ã>d·{’t¦\Ê\ÆÊ¸ÉŒ\ç\æ\"º¡©\\c.\ãþþEÿ\0\Å\×/\á_6\r\Â\Ñ\Ú\Ç¥4²14…8\Ì|\ä)\æºE—S#þ=-\0ÿ\0¯¦ÿ\0\ãt\Þ\åÄ›ûFaÿ\00Ûœÿ\0\×H¿øºi\Ô\î?\èr\í¤_ü]5e\ÔOüº\Ú\àS\ñº<\ÝG\'ý\×ÿ\0›ÿ\0\Ò(y\Ô\îq¥\Üÿ\0\ßÈ¿øºi\Ôgt«œ\×X¿øºúÿ\0—;Oü\noþ7L3\êlq\öKAÿ\0oMÿ\0\Æ\é€\ó©\Ï\ÛJ¹?\ö\Ò/þ.šÚ„ý²\î?\ï\ä_ü]4Ë©ùu´ÿ\0À–ÿ\0\ãt¢MHŽ-¬ÿ\0\ð%ÿ\0ø\Ý ¸B\ç\ò¸ÿ\0¿±\ñu\Æx£\ã„î§†\÷H¾o\"T‰\ÌrFH,\àgž­vf]Hø\ö³?\ö\ò\ßün¾yø\Ë\æÇ­\ê\ò\ÉK7\Ú#VAû\ÔÁ‰y”s\ïŽ+ZqM\êsb*JœS‹=\Ë\Ã\Þ2‡\Ä\ÚL\Z•†›v\ö³‚Q£RpJžzƒ\\ÿ\0<Ud-5û+‚,\î\ä\Ó|ˆ\áwWg“\Ì\Îß”=q^\ð¯\Æ\ÓøcÀ›’rZGe2\Ü¨A*¡So<žy9ªšŸ‡­µˆ¥·–\Ý^þ;\'\Õ5K™%\óX²œ\"€Ty{™ù\0œ…\õ\\–d:²qVÝž•¦üO\ðÍ•\ÚK6© ±¶„œ¾›·/\á‘T#ø\áËx^\ÔjiÅ¯\Ú\áš9—kJ\Ä\Æ9ù²1\õ­=@¶ƒVº½“\Ã{\ìæ´´\òÈ°B2±þ\ð\ã¶O9\ï^g«]A¦.‰4jŒ4]\Íß·\Â\ìŠ\ÐÊšIŠSœmv¿¯™\ï>!ûP\ÏQj\í\Ýý½“y\×W\Ú\Æ_§‘P\é’j—ˆ—n‰©/\\@ÿ\0\È\×\ñ\ÇhzQ\à\â\ô\õÿ\0®OY%v‘\ÓR~\Î.]ŒM\×\Æ?4½=/ »‚E’XJ\È@Ie~™\Ç;\0ú\Z\è<E\ð\×H\Ñü\ë\Ø\Éý¤\÷È%\Ã2™|\0 €N\Õ\â¿\n5\Ë\nüSÒµ	¿K\"I¿2F]YAÀÝ“\ÏE5\ïŸ¼y¥\Üxetý*\è\Þ_Kp²\'•\0\ç\',\0=GÓ“Ú¹«ûEV1ŽÃ¡8N“œ–¨\ñ³¦Eq}x†1ãµ‹\Éˆ\ÚpÀcŸ\çV£\ð\åÌ³3\é\ÛQ‚€$\Ï8\Îz\íX\Z<\ê7sµ\Ý\ÍÀMŠQ¢ØŒË’bÇ§\\\Õfþ\Ö\ÚO•w+H\Çf\Ù.\â$\çŽy\ÎH<\Õ\Ð\â\ïk¦\Üyù4ÜšûO·ÓµÕ· ˆ\é2–\Ü\îã¯°­\íáž¡{g£\\IuoW6û\Ðü\Ìq±O<\n\áü5v\×V’­\Å\êK4v—yn\å\äd\Ø[wºŒ‘øU±\â-2†\'\×\æ¸X\Ë]°¹\Æ;\çªMh=$“ýC_\Õ¢¶\á­l\àužW\ó-\à±\à\ðH\í\íY^(‰d\ñ>¦O{™GSØWµ>\ò\ÆK4»µž\Ð\ÊÒ²}¢Œ°\ç¦\à3\Ôt\õª^ !üI~\Ý\Í\ßþšb­k±gRg\Ð\ìŒÑ¯XK\0X\ñƒ\ïëŠµ§iv°\ê+¸h\÷\Å*¶]€n;\ô<\õ­\Í	gW\n+P„3•9\'>†–\âI\ÛXƒthùÌ 99c·<\ã\éY¹\ë\Ê8\Ãiú‚4\È\ä_´k3Ü¿Qoky!\0{\ó“øU½7H·´\×|2ZYÏ™!’V|° g“\ÇŠ\ì~Á¨[\ê7\Z\ÚÁ\Ão‹6ŒÜ•b\Ò2`\ñ\È\Ícj\ÖR\éž>\ð\õ…\Æ\Ã5¥\ÄÑ³DÅ”œ†\È$üC·j\Â5y\ä\âo\ËÊ‘\ÝY•þ\Ù\Õ	=<Ÿý™©º¶µ£ã°¸?ø\â\Õ!%ßˆu˜\Ö\ç\ì\â!n~XÃ–%©\ö6¥c2¼[\ö3[‚±†·]§yU9\æ¥Õ„_+z”¡)+¤;\ÄÍŸ\ÞŒƒÿ\0Z\ã¾\"¹þÛ¸½½¨?øÿ\0\á]©\ö\È\ô\è\ï\ÕR\å%eR\ÒZ3ŒŽ^µ\ÌüB`u»Œÿ\0\ÏAÿ\0‘¥?Ò·ƒ\æI£)\èt>\Zy—M\ðø·xU†•Ïœ¥£\é‚+¡\ê8½±\ö\Éÿ\0øº\å\ô(\Þ[=Rá­¶\éJr¨­œ”\õº¶·,?\ä%\'\ÔAøP\Ð&[2\ê*8’\Èÿ\0\Û\'ÿ\0\â\è\ê-ÿ\0-,‡ý²þ.ª[GüL\äÿ\0¿ÿ\0…\rmt:jrßˆÿ\0Â•Š-´š‰ÿ\0–\Ö_\÷\åÿ\0øº<\ÍEú\ë#ÿ\0l_ÿ\0‹ªf®?\âg/ýøü)­or\ß\ó—þüGþX.[{­A™¥±UPX“\ð_ã¦£x‚TGKh\ÝC+\r>\\FAû\õB\ò\Ú\äY\ÜgR—Sÿ\0\ËùùOµv\öÚÅ®‰\ÃU\Ó\ÌqY+ª˜¾l„\Î\ï:p=ú\×-z²¥k%©pŠ•\îr¡¼A+H±\Ùm±\ña/\Ê\Ø\ç\ô#\ó¯ø\ÊnWU\Ö\î3\Â\ÜA½Ll˜ý\Ò\ã‚IW\ÔV7Ë²h\õ=9’\ì­\Ä\Å\Ó	TP‘…\îz\×\Ê´DÒŠZ¢4\ë:¸Œ³\Å\ÂE\Î2zz¬-yT©\Ê\Ò\Ø\ã\ÆÃ–’i\õ1¼9©¥Ÿ‡´˜å¸ŠŒ\î\ä´lûp\ìG\Ê\Í\Ð\ñ\ÇÖ·~xªK¹<Y¢\Ï=\íýƒ…¸H\ñ\Ê\r\ÛX`dž½Ey\÷Ù® \Òl\ç\ò\Ø\Âp\êA\Æ\ã\ßÓŒÆ½/\á\ö±a¥øWX\Ùä¹¼·{yc+·³dd\ç”ÛŽ\Ù<f½)lpR“r½\ì‘\Öj\ZUŽ‡\à\ë\Í]Q\Þ\æ+;˜ƒ¾TJÊ¼cû»»zW™\éz…ÇŠ´¯	]¦\Î\è¨D–‰#e8\ð¦­]]x\Ç]\Ò&\ÓPŠxŒ	\Ãmqµ\0\Ø\Øû`w\Í} ü:\ðŸ§[<\ZJ[\Ë=ºù¥\È.^0?PH?Z\ä«Z4>-_‘\ÓK\ï\ïÔ†\rUµ\ßA<Ks%Þ•\öƒ\å(;G–71\Éd\×\ñƒ[‚\æ\Ö\ËNh§Š\î¥w \Ú…ˆù#£^¤[øgB˜­\ËXiqZ\Å®Ù§H\Â\åËª’yT\'\è+Ê¿hv\Ò\ô\Æ\Ò.´ßžV\ó¤¸]\ò|ûP\Î\ï@p1XR¯\ÏUE-ª\ðµ6\Ï\ÓF<\\ü°\Ì\ÝQŠž­\ÜV\ïŒÍž™«¥“L\Ò\ídmÄ»&GrI8\0–œW=§L \ñkÉÊ’\îÇ¯\Ìx«\Z®ýf\ê(\Z@\ì\óŒž@nb;\çny¯M­ny*mSQ]Y¯e\Ì\ö‘\ËL‘\Û\Æ0\Î?xA¬\íz2\ñÛ¤2YÆ~¾Õ¹¥;H\Óî¿µ®²±\ÚF`/Î¤\ÏÂª\çž3\×\ô®?\Å:\Ä\Z›\Ä b\È@M§o›==ªbuV’K\Í\Ø\Ú\ð\Ük·©D\0$Ar\ãœlsýEX°Š7²°&4\ÜI\í<5R\ðü‚,o\÷\Ú\Úda\ï\ä:ÿ\05­\r,¥\é\ç \Ü\öj\å\Ów¦¿®Ç·þ\ÒÀ}§Ã \ò\Û\\äŒž[ÿ\0­\\`¶ŠA34Q3‰%ùŒ(O\ßn\äWUûEj	>½\á«fWŠfŽA\åIŒ‚’²·BG_Bk†\Ä6­q-²y’O\æÈ¥rI\Þ\Ù\Ç<×›I5J\'¥&¹™\ïþø]\á‰]$—G‚\çu¸–A%¼±\å\Ï)\Ëa\ð3ùWÏ—1µ8‚~hˆ$\ã9^k\Ú<S\ñº\Ñ\ô\áou²0Is\'\Í,„…RXr:d¯¥x„š5\ËO\"[\ê\Ïq\Z›x eE;†X0\í\Ç_jŠ<\ÎM\ÈRj:${\Ç\Âp\Ã:ˆ\ÆKj–\Ã\õŠ¸?ˆž$Ž\Ã\â\í\Ñ\òL\í§Þ±1«€[r¯8\Æ+J‡^µÒš\ËûNX‹\É\æHb¹q0#\r´`\ä(\Ç^¼\Ófð´—¸7¬\ÒoÞ¦€`q½\÷s\ÏsZS¤\ãQ\Éÿ\0[›’Iþ• .¥bš¾¾Š\öú’I\å€\Îß—\å\éSXA5œW\ðOu-Ë®ÏšY‡ú\õ\0ž8\ãÞ©\è\÷šÝ­…­´ÈD±x\Ün\n\0\Éùªo\ì\ív\âY\äi ·3‘¹QAu\Ïp+Ò¨Û¹ÐœRVF‡‰š{«\Ýb\Ò(ƒ\Ï\Ò\íû¢&#>•\Ãx\ßÍ½¿¼¹r-faÚ”‘¼\Õ\\°;À\Û\ÖF\ë\Óo½u\ï\á›\ë™e¸¹Ô¤\ód;\äq#Œœœ.@:ÔƒÂ†\ö\'‚{\Û\ç·q‡ÎªGÐ¶O\å]´ýÈ¨¾‡,\à\ä\Û34Õ\ìº\'›xÖ˜\Òc\ÆÉ‚g•\îz\ô­2\Ý\æ7?þ-lhúhD5Ž©j|¥ƒ\÷r\ãƒ¼\çZ\ës©\ð?·uCÿ\0m\Çÿ\0U\Í\ØJ›8\ï2ØŸù\rOŸkÅ¥2\Û\ôþÙœŸúü_\ð®\ÓûKR\Ä\ãP?\ö\ßÿ\0­Rij8\ÈÕµûþÂŽw\Ø~Íœ3½¶?\ä1?þ-!6\Øÿ\0\Äÿ\0øµ\Ý\rOS\Ç\Z¾¡ÿ\0ÿ\0ú\Ô\rKQ?\ó¿\çþ›ÿ\0\õ¨\ça\ì\ÙÁ\\‹g´¸\Û3œD\Ü}­}\rz%µÑµ\Ó\í™ü?¢Z‰\Õ\âÀÚ€\ó•\êy\Ç\Ð\Ö7ˆ\æ\×o´;ø,u›å½’!g¸!wvŽ‡§\ã^g£\é ‘\ÓS\Ôn\Ç\Ë*–\ö—ªy\à ‚\äq\×Ú¹\ê\Òu\Ò\Õ+	JT¥nV\ï\Ø\ö¥0<³·‡£xo]g”FV5(‹‡8\Â\ô\'\Ó\ò\÷\Ç\èM\ï\ÅK\è\"¶Kf(Šb.bŒq·‚I\ì;×ª¼^1h|´\ëžFÝž_\Û\áeÛŒczb¼\ã]øy\ãK\ßÉ©G¥\Ëpc9I%š6w\0©\\Œ€H\Ç=3\õ§†£\ì§\Í&¶9±r•Jj1‹½\ÌM\Ä\÷¾³]U†9´µû4€ \ro—\ÌV\çk`‘\ÛÚ«xkF¹[\á¦Ez|û¹\Ò)‘HWRAoQŒ±üik^\ñf³\nZ^[\Æ\Ë8\à(û\Ýz\"€\0\ã©&º½.\ï\Ç\öqG\Úƒù{\Ý\ð1\É]¼.\é%w¾½{™PŒ«E,Cj\ÛiÓª\ô\ìz%–›o¥i‹gk—hBŽ\çŽI\õ\'©5\è\Z7\ö±\Òt\ò\Ëh·‹’\\\ã`\÷¯º\ñWŒl-\å’}#b\"’Y\á`\0ÿ\0¾k¼\Ñ|}à¥±Ó–}FHeŠ\ÖU\ÍÀ¶\0\ÙÁÁ\ä~•\â¼5D¯%{\ö\Ô\÷j\âi;$\ínú¾£—¥8\Ó\î\Ò\Þ6H$Á\Ée`\àü¿­y\íK\Ú\éz$\×FÝ°.Q|¦rsµO;…t\öž2\ð´ú}»7ˆæ²¾)¶iVI··^s\Ó9\õ\ç?\õý/]\Ñt\õ\Óo\Úü\Å=\Ë1\ó$pˆb]¿{\Ü5t\á\é\Î5¢\Úq\æ\â*\ÂTd”—\Þy7‚\í#\Ô<Ym\Í\âY\Æ\È\×3€\Ã;sŸV\"¦\ñžtQ&‰\r´\êÌ¬±¶T0\ç\å\õ}«\Ú?†-olK\",’ŠU‹…\ç<žOE\è¼ûT\Ó\å\ÔuV\Ó\í§HŒe&…\çc±A22GŠ\ö[\å|\Í\èx\ô­RÐ„}\îýü¿\È\ê<¥Zx\ã\ÅVºl·‘iÖ¥\ØOu+(\Û\r\Ì\Ê[Œœ\àS^û€þ\é\Ê?\ÐSRuþ&i\îÿ\0\ß8Z\óø~\ç\Ã×·W\×Vr—ˆG‚bøù²O\Ì:\n\ì.uk[;Yî®®#Š\Þ2I#6B¨&¼ªØ¶¥\ËOT{ølq\ö••›ü\â7…\ôK=@\êz^žšdl¯\nù£Œ€\Ã\'¨\Ý\Æ{\Ô\Þ\ð†•©x7A\à’F–R.\æR\Ãtœ\ã<`\ëÿ\0´\Ï\ÇœwQ\ïc\åµ\È$\×ûÖŸ‚üC¦i\ZB\Ú]\ØL\ÆŸi…²ÄŽF	<\õ­èºœ¶©¸ª{Ó¥f¶­o.¤V’\ÒØ„,±†22¡Ý·\ï+<ƒÁŠž\×\ÃV0í‘ Sr	c*’¤±$“œûš\ÒŒCG˜+¥AGa¤ºŒ‡O&!&—\ç dœ\ÙÀ\É\'\ñ5lEÚ \0\â¢I”\ð3ŠS R03ž§=)\Ú\Å\Ù#­\Ó4h\âI%\"\å˜\×\åü=kf\Ú(\â\0$i\ÝP+†´Õ¦³\â)J¯\÷O#\ò­[H€bWªH¨qbº:\ñƒßZH£\n@\Ì1˜\äýsXxª\Ù\ñ¹dOÀ\Z³ˆl‹qrý\å\"³qennª=\é7ŒVb\ë6’\ôº³Š²—±>\0p~„R³\ËBœUw›z­´\öm»±øR	Yx/‘€Sœ\÷&– Z\07CK’§Ö«,ø74‚\ä†\ç‘E‚Å NsÒž1\ëU>\Ôø±J\'\Ï¨°‹g\Ø\Òm\ÍW8-GÚ€n´\ì,0ûTF|ž\r(\'½;d…I<\Ó\ÑB\öæ«´À“ƒŠˆ\ß\Åù\åEú°XW-M\ZJ\\d0*G¨=k\Î?\áE\èV\×\Ñ^[\Þj\Ï«\Æ\Í(m¥FŒ\àWm6³f™&\ê!\ôl\ÕŸÛ¨E\×\Ì;ye•½O\ÐÕ«­Œ\å\Ïtf\É\à)d\çûz\ðÿ\0½\Z5sZ\ïÀø5Û¦¹Ÿ]º\Üq• @*\äH®½üc_ø\÷}\Ýþa·\ðªsø\Æb•G\î~cVœŒ\Ý(IZH\á¥øujˆü«r¸\"\â\Î1ŽJ·<w®r…/§\Ìîšš<ù9¸Ed\ÝÛŒž•\è\×z½\Í\ïú\é\×û½\ä*¾\ðzƒWw\ÔQ\ÃÓƒ\æH\ãƒ\õDÀM]€ÿ\0}\ê\ßê—–\Ò\ÛÍ©y\ÐH6º3¶ü«¹\Â\õ¦3Ž‚—,o±½®­s\Ìføs¬Ì°\ÂgµX\âm\âXÜ‡\'¨\ÆWŠÞ²\Ñuk3­ÁŒmVb¹Àéœ¡\É\êsÔ“]y”´\Æa´¶@É¦Ò’³0…SøL(µv\è\ê­\ô\â¬ÇªDG\ÌZç‘Ži\æFøª±i¿·\ÚXÌŠw\â¥I\òŒ¬™\èwqZ\å\ÌÇŠy”ŽZ,>c©\Ü\Ý\ÎO°À§«\óœ\ó\\º^²\ôvJºŒ\ÈÖ°üiX|\ÇJ\')Þœ&\Ï9®y5iI\åÁú¨§ÿ\0lLD?…+˜\è<\Îs\Í\â°\×Y|}\Å?‰¥]`\õ1û5+˜ÞŽ\ñ\Ôp\ì>†¤\Zœ\ê8žOû\ì\×<5¥\ïÿ\0¾©\ÃXûùŠ,;\ê\×Cþ^e\ð3OþÚºÿ\0Ÿ™\ïª\ç¶#?\Âß¦Z<ý\Öü…/\ö\Ý\Ïk™3\õ¡u\Ë\Î\×2~uÍ®«\ê\òÿ\0\ë\ÐÚ¼C‹ ¹\Ñÿ\0mÞž·2~tƒZº\Ïü|\Ëÿ\0}W8º\Ì\ÝsøRÿ\0k\ÄO*ÿ\0¥¢¹Ðb\é\Éÿ\0I—\é¼\Ó´g9\r4‡\êÆ°¶#¿J?¶Œ\ìb~¢‹Ñ²n\\’Kø\ÓL™¬_\í‘ÿ\0<\Ïý\õH\Ú\Ë\Äc\ñjvÑ·\ç\ö\Í\"Ë·Ö°Z¶B ü\éW˜®r£þN\Â\æ7V\'“H[;pqƒ“\Ær+µY\Üq/\äDo¦n²¿\ài\Ø9‘Ñ—(\Üs\É~?*d·±§YU~¤W4\×ø$Ÿ­7y…\Ìt«BŸ\ò\Ówû š†Mn0>HÙûG†d\'šarsJÂ¹«6³3\ä\r©\ô5B[\É$û\î_Øšƒ~O\"‚;Ó°®D~\÷\âÙ¬ªIù‚cÁ¥}hG\Ë\ÇùA75\ñÞ‚\Ùšš²¿\ð0©§yÒ€¹s\Ð\óK»œšª5+\ï0ÿ\0€Ó…\ô?\Ö~`\Ó–7s\Å=I\íUE\ÜÞ¯\ãR˜³*þt‚\ä\ÆB´\ô—\Ô\Õ9\×\ó¥Þ½1@\îM\æf0\Ô\Çf.ÿ\0q@\îN$>´†L\÷¨KbÞ´\nä…ˆ\Ç4¾fGZ‹q&”:\Z\0‘f\"†”·Jb/\åI\r\0I\æ9À\Í;,GZˆžG4¬ÁG\Þ0$Y@\ëM2\Üt¦n\Ée7Î¼Š?\àB€¹7™\éMw~*3q\n\õ™?\ïªi½€\ËUü\è>v\Ò\î$\Õo¶A\×\Ìú\ZC¨@O\Ñh–rA\Í+·Eµ4^\áL:ºÿ\0\Ï6ü\Å¹}y­\r#Dº\ÖT\Z\Ý;\"	¥*\\ƒƒŒ)\ö\Îkm_\Ò/Íª›ø®\çOÕ­\ÄL–\ð>\Óq”Þ².q†^ø\õ\ôÁ¥g\ÐRšŠ»;K¯\\X\êY\Ý]YZM0\Ì~tŽ¨þÁ\ömÏ¶sK/†n`\Õ³&»±‚\ð€Q&yH\Ï\È\Å0\ß@jø©it§J‘~\Øü¡\ð}¢7\ÉÀ\Ø\ÙŽ?‹‘\ëS\Þøº>\Â-2{6¤Ky\íZPy#t„Žzs\ô\éQ¨sE\ì\Ï;U°Øª£¿§s¼;\ä\Ôÿ\0e\Ë\'ÿ\0¾Mgÿ\0\Ë\'ÿ\0¾MXX‚EVaŸ\Ë5 z\r¼¹ÿ\0Tÿ\0\÷É¥û4¿\ó\Éÿ\0\ïš\n\Ð@FiÛ©>\Ï)ÿ\0–Oÿ\0|Ò¤d-\ñþ\íd88?Z]\ôŸg—þy¿ý\òi<‰Oü³û\ä\Ð!Kn\ïA\â\"Qÿ\0,\ßþù4y3ùdÿ\0\÷É \Ô\âx$˜\ßþù4e#ýSÿ\0\ß4\0gŠ7q@†Qÿ\0,\ßþù4\å†Où\æÿ\0\÷É ƒž\æ\ç\ÔÔŸg—Dÿ\0\÷É¤\Òÿ\0\Ï\'ÿ\0¾hÌ1“ù\Ò´rj_³\Ì1?ý\ó@¶•¿\å“ÿ\0\ß4b~”¹\ãÞ¤û4£)ÿ\0*O\"_ù\äÿ\0\÷\Í\00Š	\Å?È—\ê\ßþù£\ì\òÿ\0\Ï\'ÿ\0¾M\03’iqJ`•[ýSÿ\0\ß4­¿\ó\Éÿ\0*3w½.h\òÿ\0\Ï7ü¨\ò%\ò\Íÿ\0#LcKŽ´SŒ\Ï\'ü\'\Ùe\ë\å>?Ý .7š¥b—«‡,}\Ö¥he\ò\Éÿ\0\ï“H-\å\'˜Ÿþù4\\–”•™\ÍA\æhw\É6eutv\ä;f¦¹Õ§\Õo§º™\É/€\0\'h8\Ø\ë[²Z\ÈF<¦#ýÚˆX¾\ã\Üÿ\0\ß\ïsG•\Ý=;ÿ\Ù','superadmin',1,0,0,NULL,'2026-11-11 02:13:15','2026-10-19 09:18:40','2026-11-11 02:13:15'),(9,'naedd','PBKDF2-SHA256$120000$zJBr/pJP9edulKa/wWipiQ==$9+hmyvz+Ce7DHm3WiILUb/xbXV4EX8IovRmCL0UtkXw=','naed acoy','09212577107','taguig',_binary 'ÿ\Øÿ\à\0JFIF\0\0`\0`\0\0ÿ\Û\0C\0\n\n\n\r\rÿ\Û\0C		\r\rÿÀ\0\0\0\"\0ÿ\Ä\0\0\0\0\0\0\0\0\0\0\0	\nÿ\Ä\0µ\0\0\0}\0!1AQa\"q2‘¡#B±ÁR\Ñ\ð$3br‚	\n\Z%&\'()*456789:CDEFGHIJSTUVWXYZcdefghijstuvwxyzƒ„…†‡ˆ‰Š’“”•–—˜™š¢£¤¥¦§¨©ª²³´µ¶·¸¹º\Â\Ã\Ä\Å\Æ\Ç\È\É\Ê\Ò\Ó\Ô\Õ\Ö\×\Ø\Ù\Ú\á\â\ã\ä\å\æ\ç\è\é\ê\ñ\ò\ó\ô\õ\ö\÷øùúÿ\Ä\0\0\0\0\0\0\0\0	\nÿ\Ä\0µ\0\0w\0!1AQaq\"2B‘¡±Á	#3R\ðbr\Ñ\n$4\á%\ñ\Z&\'()*56789:CDEFGHIJSTUVWXYZcdefghijstuvwxyz‚ƒ„…†‡ˆ‰Š’“”•–—˜™š¢£¤¥¦§¨©ª²³´µ¶·¸¹º\Â\Ã\Ä\Å\Æ\Ç\È\É\Ê\Ò\Ó\Ô\Õ\Ö\×\Ø\Ù\Ú\â\ã\ä\å\æ\ç\è\é\ê\ò\ó\ô\õ\ö\÷øùúÿ\Ú\0\0\0?\0\ñOü\0¹\Óü/6®\îL¨¦M\Ç>•\á¯•Y˜cpÁ\í_¦_t4ÿ\0^DT*˜ŽœW\æ\Ô\Û¥xƒ\ç1\n{\Ö\é>k\ZJ\Ê:5\Ù0ß°.WŽ0*t•\\…q¼u8\"µ5}=\r\Ælc+\×Ú¨Å¦\É(I\ÏÒ‰&¶8®Þ†\ï…|?q\âz\ÓM´%|y\0g’k\ôŸ\à§\ÂûOxr\Ö\Þ8”K´oaüF¼G\öHø%-µ¢ëš¾\Ù\ç\ÃF}\Õ\í_hhºX„*ª‚ª;W‡ˆ¨\ëOÙ­RŒU(s=Ëºe’\Ú\Ûy¯€ª2k\âoÛ‹\ã—o…mˆ”\ï¹+\Ð\'eüM}q\ñwÆ¶ž\ðm\í\íÌ¢Šv9\ëÀ¯ÉŸ\Zx¶\÷\Ç>+\Ôu™œ;\\\ÊYTž‹\Øc\é]\ô –\ç=Y¶Œ\0a™Jƒ\Ô\äJU´d¸–D\\œü\ã\"”\ÉlI2\â^	®¿\ÂzLl²\\YI<c¶pO\Ð\×m¯±\ÈÒ¶§K¨ip/š{gG‘m\Îÿ\0³\ÎA\Ï|¡\àŠ\ñØ­\âZ9|¶\Î\nž\Õ\î\Ð\õ8^\Ük²\èW‡.°\ß\ÂDl{®\ñ^I\ã\r<\è—\ïi!†I:¬\Ö\í¹{\Z.#kÉ¢“ao5A\Ç5Nkƒ.Fvûv¨Ý‰R+ƒŒâ™º6C\ó\ÞÕ\Ë\Ôi°sŽ8NbD™¯5bB\àc\éQ\"	“€ \ãši	”\ämÀ\Æ;šA¹˜\òJ\ç¥Xš\Ûo8A¦¬xb@\Ú)Ø’h-¶}\Ö\'=AQ™š&$u\ëI\æ\î\ÍDFC6H\ö¤\ÆjA|$8q‘Ž\õ\Ð\èw¼…‹Ðš\ãPž!I©£šH˜Àj\ÎJ\è´\ì\Ï^{7†™\îD\Ç5l?Â´4û–™¶e¦C\ÑOLv¯(¶\ñ\ÊY[z\ô\ë³\Ð<@„\0Ò¸S\Åg%dl\ö:]m\ã\îˆ\ZŒ\ê[üyž£¦\ÜMpY‡›8+¶\Ôue[Ü\ã\ä\nÂ½ŽFE–v`\\wÿ\0JqVD\É\\\ä®P\Û\r¡j8\È\äŸÆ©\ÍBxük¨û$Š®\ò(’0J\ò¥eKaou™#o%¿º{š´\ìÌš1$\È ©\ïKì©wJ½%•\ÌJr»\Ôú{\ÕF\å²\n0\àqZ^\æ~bF\ê¸úT–ƒw\áÁF!Ã¶\Ü0ýh¶+È›œoü\è°#\ö§\ãþ•\Þ	\Õrv•µcœ³_”\Ï¦¥6Ï˜A\0d\õ¯\ØŒZ/\ö—†o\ãl\r\Ð0\ç¦1_’>&µ[=Rˆ®6\Îø9\Æpx®Ö½\ã¦_	…\â4KWC4G$A\í^‹û>ü7›\âŠ\ã;Y\ôûw\r\"¸$\Ùs\\…·†®<A=¥œ*e’v\n>µúû:ü\'µ\ð†m¢X¿~F\çf–=k‹QÓ—QQ‡4¯\Ñ\á?E£i\ð\Û\ÃMª\0â»›¥»Lø\n£¡ª\Úeˆf\Ís?ü{m\ð\ëÀú…\ô\Ò*ù0–\0žY»ø×•BµgMI\Üø\ë\ö\íø—s®^\Å\á‹	XD£Í¸ \î®G\ç_µ½\ÌN|\È\ö\Î\á\Ð\×m¯x\Ö\ï^×¯u+»‡2™7Ì‡=€\ö•u«H\Ìkhf\\}\èøoN•\ë(«Yž{’f“»\Õ\â€1V‘†\'Ö¾²\ð\÷\Ã\Í!</77Zu\ÑL\ïÀ–>=A\é\Í|\í \êvm¨C0´\ÅÚ‘±X2+\×u‹z¦‡£¤\rg\ös·†p\'Úº ¹P­u©‡\ñ<;‰s,W±¨xO\÷^\Õ\áz\Íé¾¸gU\n£…\Ú0+´\×üE&½ –ø\à°<©Àü«„\Õ\Z7•\Öùy\É\Ïœµ-2Ž98cƒ\Ôg½@\ò\r\ì\0\ãÒ¤f1©\ÔUw`ž\Ã•¬;\è>KŒŒc \ñŠƒxq•\È šg%I\'Þ­\Ú\Û9 \àúSnÄ¥r!#‚B\çƒF\ÒX*Û·\Ñ\ÞU\ÆÜ’3Šj\é§\Ï\Ç=\rgÎ‹\öm+˜\ë\íû¹SÀ\ÍXû¥€G®š\ÓA/2.7(985¸\ÚŽ6r¸\à‚+9\ÔQf´\é9jy¸³ \ò>lsž”\Ï,¡!€#=\Íu\÷š12\0_~\Õ\èiqØ£…\ë×š#;™¸4s\Ñ[–Œ7š²þ|$G!GµF\ðIa>Ý§h\ê=kJ5\óa\Îr1\ÊV›‰hƒJÖ™I‘°\ö5\Ó\Û5½\ë‘•e\'Ž•\ÇM§7™¹OQ\Í[°½û;\í”\ô\ç=iI,\ê\î!lü§Œ!>µ“s¦\'›¸§^Iš#Ö›‡„´l[‘œ«}E^>\"[\Åÿ\0H‚9œ–Œ\à\â”JJ/©\Í\ÝYO-Ý¶Ê·j£5\à\í¸€azÖ»•K;\èÿ\0t\æã‰€JûÃŽ\È\ÅUf\0uS‘\ô¦ŸrCŽû\r\Ö\ólb9V¢\Ú\Ê\å.¡7®\õ\äsžj\õç‡Œ(C\éŽW³’\óO¹Œ?,ƒ\Üc5¥\ô\"\Ö?u~+X=ÎwJï‰—>\Ûk\ò;^\ÓYuBIr&a\æc$Œœ\Zý”ø…c$šD\áWp)\Ðý+\ò\Â\×À³k?5	Tˆ…\Ù\ÞkÑ«Ê•Ù¢\\\Ö=\öoøJº„k7‘\ç ‘‡\ë\ì\Í\Åm\âŽ4j\ã¥rŸü+¦hz%´q*FB<Š\ô\'Gr\èbbFy¾n¤½½K\ô:ÿ\0‡\Z–±‹Kg\ð¡Fy¯\ÏÛ“\â\ãx—\ÄI\áK+¢‘Z-\É‚Ý–¾\Ýøý\ã\ë_†ÿ\0µ=BIU$‚µIû\ÎxQù\×\ä½\â+\ïk\×wú©3\Ëu1•\åúú\Z\î¡¾fqÔ•ŒIln\í\Éo\õ¨rNþ”y_ip‘³\Å/\'PMoH\ñUPT“\ÃÎº‡~“_\ñ-°Žkx\Ú6’¾:ú]\ÒQµ\ÎT\õ:_\0|\r\ÅvI\äø¢\Ö\ÃT#+iG¿\ÔÞ®ø³\áÖ©\à$\âHXPyqKlû£#žy¯¢n4\rVM;=kÃºwˆ\àE\Ê\Ëf\ßg¸Qê§¦~˜¯ø¹©.\Âk=SW‚\ßn\ã¤\ëq\î+\îI\ÎkŠ_5º|¨ùû_¸ˆ\Üµvh\ä¿\\\Ö¤¯O®}*\Ý\æL¬\Ë\ÈcŸ¥P›¯\ÌIÏ¥n\îÙ›²Ð©$¤?s\ëQ\ì\Ü\Äþ4ÿ\0,¼¼sŽuž\ð\Ôr¬—²ªGž€\óS\'Ê®T\"\ä\ìaiº4\÷Ž(Yû\n\ï\ô?‡··\n¥­\Ù@\é]n©xO@†–A\÷°¿ÖºHþ)øn0B,\0\0\rœ\ç\ð¯>¥J¯\áG¯J/y\êri\à)\í\ãTú:V5×…o#²• ¸Œ\ö¯I“\â©G\Z\Ç!þ£ª\Z•ÕµÅ¨x\È9\ê¹ÿ\0>\Õ\Í4\õ:§Ihy\ö›*\Ç\'”B•\ë“Þº¨UmŽÒ¤vý3Y\ðij\×Ï†\'q\Ï\Ì>\íuVšKipc—®iÔž¦0´8k\Û?27b¤\Ë\ëX\â\èZ\ÌAAxŒ×¨^\è\ö\ñ¨E\\Ž3\Üb¼\ëÄš[Gp[wúdÖ´gs*\ô\ÒZzœhÀ95šˆlØ°”œqÒµ\Þ\ÊS’	Ç½T\ò\Ã\0­\Éû£<]<\Ög&š•*¸\'f}j[\Í,\Én&N2O¥BÐ•“n3\õÏ½lCn\òÚ²©qÀ§µn\Ý\ÌTlb\Ú”¬mùŠ·.jÁ¥k[€9\ò¤N\ÜV{\Â-¤Ý¸\àÕ¨V+\ëd”n\Z†m+¨\ÝYI\å± <7­hXøˆ\Æ\áã‘¡o\öz\Öe\î•udÅš2P\ò^*¯œ‡\÷r&\ÒGF3V\ãq]\Çc±]Yn\Æ\Ù9\ÝÀcüªt½\÷Q²u\Þ8<w¬h‰\0\ÛÏœº\æµt½F\ïN»…¤r\ãvzu©µ†¥Í¤\è+\ÄvB[W;?¥|^~Ac\â\íSP¸„,\Ï1 ý	\Å}Ã¯²[Ú³9\0\É\ÏÒ¾Tø¥\Éussqgq,\ò3§4c+Z’]Îœ>­—<?\áË™m\÷G!BX\í‘^™\áD—N¶w»v=\rq?u;©\"†\Ö\ò2\Ätr9\ÅMûL|A¶øi\ð\Îÿ\0TŽuK˜”?~CÀ\æÓŠâœšøˆÿ\0o\ïŒÿ\0\ð’ø\É|#a96–#Íº\Øxi\Ý_À:ù.\Â\ö`¡H\ÞxaœV§‰\ä½\×u[\íJ\âF¼»º\Í$™\É,NMcÀ\Þ|¾B\ó`\æ½\ØSå‘\æ¶\ïsZE\ÑX”0yšû\à7Áý\Ä5K}·Ž»\ÌÌ¼\äú\Z\ð\ï\Ù\ßÀ\Ç\Æ>.Š\êD2\ÛZ²\ö\êÝ«\ôO\Ñm Ó¢€\Ä\Ñ\ÔV¤\â¬mEsjy\Çü#þ \ðFV8\Ä:1\Î\Ñ~þ!\Ûµ\ó_\í¬\Újlµ–_1N$µ¼ˆ¤©Ï¡¯µn´û»¤–\Ò\ã(J8¯Šj\ß\Å}¨[\Û\\[¤w¡Ž%^»Gl\×(\óT:$ì™®mw\È\Ç5‡x„)\çÚºi\ä\ÞÎ¡y=*”ZP½¹H‘x8\àt¯K\áWf	]•t]%\ïd«ŸÂº”\ðý\ëE„œs\òÊ½+\Â>´°\ÓciT6zsš\ítM¹l®9Á\å^E\\ZM¤vŽ\r¸\êxA\ð>¥:\ïH±Ôž¾Õ•q\áû»RRDu`y¯²¬\ô(\0·VúX\Þ&\ð\r\í»8\Êwh¬!‰w6–+©\ò†•–’) \ç¦=+¦K©D<n\ç­u\Z¿…\Ò\é\×nT¶\Í]\Ñü\'\ö¦±džŠGoZ*VOV8P}\n¾±k›Ÿ\ß(\äc`\×}¨Y‹	€NœVÇ„þ]¡\ó#·bA\ãÖ¶|Aá™¢·h\Þ7Š\ðzf¼ùT\æw;#E$|\Ý\â­r\èN\Éel\ðA\ÆÂ¸§–þ[Œ“$’n\Ï\'5îš‡€\ãž\è´Ë…\È\àU\í\'\áÖœÀ(±\ÏS\Ø\×tk\Â(\à\n²g‡%\ÕûB\Þdiþ\"˜¬[\÷e!\Ý\n1È¯²\ìþi\Z…®\ÓG‘\ÉW\âÙ¹$f’Ì¸9\ç5¤+Á\îr\Ë8Ÿ6Gm\çÂ®¼““\ÜÖ®Ÿ![6\Î\ÕÀ\ÉL\ò\Þ\õ\Þj¿\nu=\åkrb\ê0¹®6þ\ÊKErc!À\Æq\Ð\÷­\ã$ÙŒ \Ò\Õ¬\ë\æ\Ü£wlæ¤µ‚Tbp€ÀÖœ‘H\×M’}=koD´†mF8d7°û\Ö\í\Ø\æ\å\Ö\ä‘øz{‹Sxl^À\ç\Ô\×7©[¯›û\è|\Ütx\ë\é¯|6“CÑ®/‚µ\ÜR•Ó€GqŽýk\É<[¢FÚ¼\Í~RF\Ð0\0\ô\öª„û™\É\'±\å­d®\Ñ\ç‡%X`\Ò\Â\×P\ÜB¶7‚A\ï]Lž‚\ô0W\òŸ‘ŸÆ¨I¢\Þ\é\÷Q…tž5u\È\ë\ÆkF\Ñ-Xý\æø‡\â¨\'\Ùkk:I½C1Fm\Åy6¥j7\"<)\Å|™o\ñk\á…\Ü\Ò\Ç\'\öý”cp’\ÚL¹^\ÙS\Íu?\nl=\"-^8|Ml\ðÊ²m•\Ê\È{‚+“F¥I]-\r\áQ\ÒV’³>\È\Ñ|8š6šn‚e•…|\×\ñ\Ó\Ã\ö_üL¾\ns¨xuatš\×Z™ü\ëý\õ‘\0\È t9\ë^×©|\ð§‰<4\ã\ÃÚ€ž\áÀ##i\÷ª\ß´KQ]^út›I\ås×¸¯K/Á^«.›µ\ê)µŸ|]ÿ\0‚yx—\á\î‰i«x{_´\ñ]¬²yl°\Â`’6\ÆGA\éœ\×Ëšå¶©\á\íV}\'YÓŒWQ\áLq\ìu\ã\ÓÞ¿]¾$þ\Óÿ\0tÝºF±qug4ŠwDªQXŽ6’\Æ{\ZøK\ö‘°Ð¼k\â]OÀþ\Õ\õ½ \Û7\ò\Ç,\ó\ò³²{\÷\é]´yù­4a$\Ônuß³oƒ‡´ºX\Z”n`[q¯ -5B¸Y3Ÿ~+Á¾Ã¬i\ÞH\á2Á*}\è¤\Ûø\Z\ô}#\Ä\Z\ó0ŽûKK…\é\æE\Æk’µ?i6‘\Ùv\n\Çmª\ß\Ç&›0”\í¯\ÏO\Ú1%“\â\r\Ã,¾XÙžƒ×Šû¯\Åz_‰`Ñšú\ÇF¿µ€.\æy`c=sŠù\ãu£\ë‹¥.£,£¬°\ô\ãÏ O\á\Ôù\íb§\0Ž\Ý+¯ø} ›\Ë\õ‘—\åN‡W+ˆnL$nfq€9¯}ø_\áp!.¨q‘Œ\ô§Ž—±†‡N\n\Ò~†”\ö\ÐX\ÚD%u‰y$\â¹\ÛÏ‰Z\"m¶w0<`\ôú\×}\âo¾£j£v$rAæ¼ƒV\ð	\Ò\',\Ê\ñ‚q†Zù\ê1§=dÏ ¬\êGH\Ç\í)}c:Äºdd\à|bº«/y›ý#l\rÁ–\ÙÃ€=\ë\È/<	\í\äm¤‘9 D[\ñ¯F\ð€\"¶\Ó\â¶\óš\á2r\æ¥³×­w\Êx\ÃM\Ï:/)k±\Ú\Ûi¶¾.‹\ízu\Â\É	ùŠŠ\êü-\áÅ´C\"ds½}\ó\\\çƒ|<-1ki¥\ÚN@“ µzN’TI\Ë\ÞÞµ\àb\'}\"{´cî«ž\áø!Š%\Ä`€?\Zoˆtˆo­ØˆùÁ Žµ‘r&\â9\ëÞ¶gq,{—¿¥sÓ¿R¦­±\ó¿‹l…\Ãy‘„\ðz~µ›£\ÜÄ·\n¢HÐ“Ð‘øV\÷\Ç=7\ÄWv-&™n%U<ˆ\Î\óV·¦]i¶²Msy2Î«¸£19\é^•:\ÓD\ÎIV\ä\ÝjøXF\Ö\Ê]ÃœrzÖ¥\è‰\Ï\È\Ýzúb¾\ðgŠ<Bš²Å¦kWVo‚TyŒ\Ê\Ø\íŠ\÷M\ã­¢ùpø|\õ8s\È\'Þ´ž\Zp\Ð\ÊˆTg»\É\á\ë=Ue#\Ü{2W\Ï\ß>\Z¯‡-¦¿„©O½°-{\ï‚üUg­¤2[H$9\"­üa\ð\Ü\Zÿ\0„nWh$)Á\Ï\n’„W‚œnÎ½>H\î²¨F2:\äV\ÂZ:Ý–QŒ\àdwªK}7\ÄWn£rLT\ã5\èzg†š\ô\ÄR=\Ñ\î\Ã\ñ^¼¥mOhÎŠ\Ï\ã.§\á¿Å§\Ëh\'€‚ª\Ç\Ð\÷¯,\Õ<]&¡{,¯·sœ•\ô¯V\Õþëº•„-¦X\\\Üü»È‰2ù\ô¯,\Öü)}¤\Îa¿²h\\H–#ýk¦•¤Œ*>W j\Þ[)1a@\'v3\Í:\ÞX/.b(I;\Æ\êÈ–\Ð[‚|¹`\ô\ÚN*[6—\íPªI\ÃvF\éZr\Ý\\Ëœý,¹·t\í6\é\ã<\é\\®»\ð\Ó\Ã~!{‰/4M*\æYI\ò‚J\àk\Î}\ë3Iø\á\á}oQm;Qž_j;±%–¤Œ„· lb»\óioym\Öq¤¨~\ì\ñr¸úŠ\Ö\\\×\Ðú\Ês\Ã\âu•™\ó\æ¹\ðF\ïNºK¯\ês\ØH$Á¶¾›\÷yÿ\0e±\ÛÞºk/ŒþÚ¬\Z²Mw¦¨\æxq4d}G\"½.\î;(d16\õH¾\î\Ô,7zšµeh–©*Û°’/,cpÊ±>Æ·§‰œŽ:\ÙU7R:vG\Ì\Þ:\ñü-\ß2v¿µ7¥Ù£[‘åº‚>\æ\î\ã5\Øþ\Ïþ ü$\×Î“©i£\ÄV7¥´Ý¶!¶U\î„J\õ-{áŸ„|XX_iQGt8¿º|\÷û½k‹Ô¾\nj>\"\ã\Ã\Þ$A\Ëi~œ`\Õ\ß	Æ¬\\:³\Ç\Ä\à\ñ	\óTWHú\\»·ñ®¬ºÍ½‚i\íqù°®1¼Hú×¥üø©©ý²\êkK\\6\Ö3\öÖ¾Føk\ñsRK¯°\ßÇ½\âmŒ=\Çú\ð~H®<¦\ÝGD\× \È\á†	9\Ç\å\År\×Áb0TJ«\â\Ð\ç†\"•[B¾kX§\á’5xJ20\àƒ\Æ+\ò\Û\ö•ø{Ž«®\Û[*„ŠiV0F@¥~§\Zü\ßø\÷s§­krŸ›}Ô§¯mÆžSAT”\ßd…^\\©ø_\ÂO«øƒ	Š6 qšú\ÓÀž†\ÃOH\Âa°2\0¯<øi\áx¢»,bƒ9\ïø×¸\é°ªª¦\Õ\Ï_A^Nq&\çÈ¶G\Ð\å”\Ôa\Ì\÷5-´X^-„1ŒSn|1cp\nµ²9>ª\rj\ÛÈ® \àg¥\\\ò\ÎGZùWt{v¹ÇŸi(\ÅÅ¼d\òv\ÆW\Ô4K;ITPlr+³º„\"œ\n\á|M©¤E•\ð¨:“Sy=.\\b™\Æê· \ÈcQ‘œgÒ·|7e5Ã£˜\È\Ç5\Èjž.\Ò\í%Rª\ÏlŒš\Ø\Ð> Û¶\ÐO—ƒ\Æjœ\ZWf±j\'­Yh\Ò%¸*\ÔQ=\Ã\ÚLU”ŒqU<=\ã¸dQsÜ·ZÔ¹¾²\Õ¬n{\ÓV±ƒm½H¤\Ò-u˜Hd0\ë^s\â\ï\öÚ£‰\Æ\ÖêŒÔpk\Ò\ím\å\Ó\Üo!\ã\ìWš\ÔGYW“×¥\nn\Z£9A3\æ!û<\r>é¦‡Iµ‘¹·•¢?•^‹\áš5[\"A·\Ö@\Õ\ô[Û…<\0O½/\ØüÀ>\\‘Z}f}Ì½„z#\È<;\á™|;±aƒË{\ãœWu{t/|=:7]¸\ç­tW\Z:K6\Í`\êZsCgpˆ¹Êž\0\ç5nR»…£d|\ñ\Ïû/\Ç×ªQ°\ì²Z\÷ß†þT\ÑP–þPlm\è{^M\ñ¯NøMˆ¶™)\rÜƒ_B|Ó¼Ce¤\Û\Êú@½€G·\÷L7c\èk×“¼G\ÎIZlúá®„¶VŽ^\ÜÀÁ±®ƒ[\ð6ƒ\â˜Z-B\Æˆ\ÎAI#ù\×=\áß‰\ZNŽ¢\rR½!Êøú„…ÿ\0¾ºWsc©\éº\ÌbK;\È.T\÷‰Á¡\'Ñž\âÿ\0\Ø\ÇÁº\òI%Œ2i³79¶rŸ\÷NEx7Œ?b{BœM¦\\G`vºym\êE}\ôbx¾\éÈ¦8Y\Ç\ã=˜qZ*\óR8¾‡œø‡\Ãþ\nø4–:\æ‰·%vÿ\0¤Á²A‘Ù‡?­y\í\ïì¿©xNw»ø}\â\Ë\Ý+¿\ö}\ó™ onk\Ót=[;¯´\Ýß®«21&DM®}\ì*+ÿ\0^Owº\×Î·\n\ä•Bœc^¢›\è\Î^WM\Ý\îx•\ç~ ø\á\×Æž–{U8:–ˆ›\ÓorVºüI\ðÇ‹“\Ê\Ñ\õh–\áÜ°‚\ðˆd_b¦½SLøˆfYP·Vˆz\à7Ð©¬}\á7\ÃŠù\÷\Z]´Wd‘\ç\Ûþ\æeor;\ÖÑœ’V\ô:©\ãkS\ëR‡\öp³\Ó\Ãn\ó\'\ã\æ‚OSš\óŸ‹^7]&Å­\â\ô‰—\nƒÏ¹®’\ï\ö{\ñ§„!q\à¿\Ë5ž\Ü-†¨7Œv¹¯\ñg€<uo¬­Ï‰4™ARÍ¶\âú‚>µ\í`(ÒTÜ“Ec³i\ÔÃ¸¨Ù³«ø\á¼M\âY\æ™Pds’@\'\ò¯\Ô=M‹G\Óml`P°\ÛÄ±¨€b¾@ýŒü\0\Ój\ÃW•?sd¥²\ß\ß9\n?™¯³_\âUJ°¡¤W\âÿ\0\à]M¥*©G]¾]3G½»v°\Â\ï¸\ô¿>üw\á+½[\Ïx\Z\ÕbN#o›šû3\ãÆ¼4o\0\\\ÆV[\ÇX9\ÆGVý\ë_ø²\ð\é±<\ë!q1¾AÍ–§J„ªwý\Ù\ÅÕ¨ Œ]\ÃGFiP\Æ`p\0¹\æµ\í_Ë\ã‰ªÿ\0o[4‡\"B™9\ñJG\Ð\â¾k\ïÍ³\êpiÓ)\ÕYJF03Ö´¼\ðŠ9\äu\"¹K{\õ\06==M.¬\\=k\ç\ç\ÖN\å\Ý_ZHcn¹·5\â?µ‰u\ñZ’²p\ä\Õ\éw\ò$‹û\É6œW¨Å¤,\äÌ±±\';_®j)¯{b\î—S\åxkÅ¢\ôJ,.\ög\"H\òp\n\Ó\ð×‰¬‘WRIL]™\ÆZ¾’›R²\â(\ÕH=­Ÿ\Û\Ù\\ÿ\0®Ó ˜aŒc8¯RX›Ã’QG\ìRŸ:“<•u½{L\Òe›N·ûdŠ¹p2H?J\ó9><ø\ê\×V\òÊ¼7(\Ñt\õ\Õ\äv¬Z\Ö\Õ``F@\\V–œúu\âl¹³€¸\ã{Æ¦±„\é\Å5(\ÜsŒ\äÕ¥cø=\ñOP\ñ•\n\êc\÷\Ç\ã¯_³¾\éŽ\ÕË§†!ù¶‹ký\Ø\Ð\n\ÛKg‰WÛ¸¯6¢W²;¹‘\Ñ+©\00Õ˜\íÀ;`{VNŸ9s[Qž¿¥(\Ä\ÎO@b>ƒù\ÕI­V\á†·Ö­>\×5wKµ3L˜]\Þø­Z\ìb\ô\Ôù\'\ö‹\ð[¥Ý½\ôQ\âHg\0`\õ½S\á\'Ä‹\íI·‹R\ÒfŠ\Ü dM\ÃÎ»‹~]r#§–2?zW85¡\á\Û\rg\Ã:LH-mµ{eP\n\Å\÷±\ô5\é\ÒM\Å{)]¶‘\âý\Åv b”#úVv£\ð—\ÃÚŒ¿j\Ó\Úm*\ï9\Ø\Èc9ú\rr³\éžÖ®‹Á$¾\ÕI\Ïx\ò~\r^\Z‡‰|*¡®`þÙ±ý¦×—\é[¸´rß¹©‡\ão\rŒX\ê\Ðk/ü²¿]²þ\ð\â¬A\ñ\"}>D_\Ðo4\ö9li\æ\ÅùŽ•>…\ã\ëc	ÈŽ^†¸\"ºhoD›C t#· \Ôz”¼Ž:\çÁ—³´–L!ˆcÉªw¯v±˜µK»›r\ç\×5\'\Â\ïG\ã¯Av¯º]£qZ»R‹\"\á€e=z.“OCEŠ\æV¨®>þÊ°Õ£Å¤\ò\Û(8\òŠ‡F\ô\óX\Ï\à=T][=¬\ï\ä[fi<¦\áÛ°#­zmÆ‘jM¼*Ž\ÇøF3Vt«v°ÁcÒ´§Ì·8ª¸_\÷jÇ‘\Üø\ë]\ð¦¡iiKr6b²Al\ð>•\ì\Z&¡.¥¦y·¢\îQ•\ÆGJ–m6\Öù³5¼r°”dUmf\ò=:\Ç\ÊA°€z4\Úm#–Z#×¾i°\Ùøv\æ\æ(–!qpNc!xÿ\0\Z\ôzW1\ð\Ê\Ô\ÚxIVg‹\Ìo«k \Ôo¢\Óln.§`Â†Gbz\03^uf\çUúT\×,\ó·\í1­K©\ë\Ú]³\r–q–pFW{€\Å|\Å\ã6v³™$“\Æx\'\é^›\ã_\rg[¼\Ô$y\"{‰K\ð\ÜsÓŠåµ¸ŸR\Ó\ä!\Òl¦Wd‘\ë_IY:uMtA—\ÉK¥.§9\à\ÛedŠF^„S\õ	¼ŽX}ºUýÕ­\ÑF2£±ªz\ägs08=\0\í_#[\Þ\ÔúNnZ®\Û7z™\0c#§½fj>,]\ÉÌ²\Ø\Ï^h\Ô7pFq\Åy\×\Ä}\'P“Oi\çN\á}+‚4£)Y5\'(\Æ\èƒTø«-ýû\Çj]”qÞ–\Ö{\íI„\×w	D…ÄŒ×š[\ëÏ¤-4ù7\ô/·?­jX¥\ö²Q¥»0FzdÎº\çF0ZhsS›¨\õ\Ô\õ\Ë7Ð­˜5Æ dté·œs]¦\â}\ÑbŽ\è‚O\è@\ÇÖ¼gJø\\—\ñ‰?\á#¸Œž0\"ŸÆº\Ý3\áu\å«(·\ñHqŸ¹=¾?Pk†T\à\Þ\ç³\n2¶Ç«,zm\î\Ök\Ô`sŽi·^\ß–\Îu™T\ô\×~ø½a\ÇS\Óo#ej¤O\ãH\r\ö”\ñ²Žd³H8ÿ\0g­gì­³2©I·±\×[ø’\ëÃ³l™Y£\Î75vºG‹,u$\Ì]Ç¶zJ\óKOY\êq5¾«\Z\Æ\ä\×c¨5\Âø\Ú\ö\÷Á\÷q\Þ\Ø\Ü,\ò	 \ç›¤\å\êq¹{=O¥\Ù\ãIC€z‘Z¶\×Ch\Ã\ìk\ÉþxÕ¼G¤D\ÌûÜ¯_Z\ílo6±B\ånk•\Þ;›«M\\\ëRO5À<š\ì|+bd•ÑOJ\â\ôU\ó]O5\ê¾¶À\\®03Ò®*\ì\æ¬\ì¬q?\ô¨\è\÷=Y4ùsž?*ùª\ç\â‰<¨Ik%ÙŸ\Êl	\0\ÈaŽ\r}#\ã\ç\Ï(|m\÷¯\ñ\\vw\ZœˆQBÓ½{\Øª¦»62—²Q“\ê:\Ï\ãÅ¾¯·\ñ\ìdcÍ‡³§x»NŒ+xo\Ä\Íd\çŸ\ìýG;°&¼»P\ð½›neVŒ\ö\ÚkŸ¿Ñ¥µ%Uü\ÕC\ã8ú\×s\Ã3\ÌU\ïW^?\Ñn%x¯JþÍ¸?wQ´`\È}\÷/#\ñ®¯B}F\Ô\Çs j\Ñk¶‘Ž<À>½\ë\äi¯Õ¢2²§L3eqš\Þøuu:\ëƒ\ì\Ú\÷\ö)F\\d£Lf¹¥Aš)\Ç\ìk«\ÜIc5³11\Ç;¢{u¯¬\\_6~\É^“J\ÐÌˆQ¥v”}	â¾•\Ý=dÙ„V‚\ãRžO4£ýjZ•\Ü¹*>}+‹\Öuh\îµømŸ,›¹®§R¹–Ž\ä\àc½x\Ä\Þ ´þØ¹¹¹P.y.À®ŠKšVf2v\Øû§\Â3E\'†t\Ób\Ðb¼‹\ö£øŠº†‡‡\ì\Ë\Íy|7\\%»$posú\nù?h‹½+[†\ÏG\ñÅ´Q¾v´˜Œg\Ø\ñ[\Z\öm\ñg\Ôo\å:…\äø-u\ä9üA­\ð\ØgCª\ÕøV«Ô¹\ÍÔ¦\ã\Ï/“\Ä\ÃP\Û¼–ŒOüzkV\åW\èq]f—y«K7¾‚XH\âÿ\0H¼Y#>ûzÖµ·ÁLluë…ˆÿ\0Ë¶§\Ü\Ä¸­m\á§i!K­\'M¶\Ù7ºK2\õ·\ë_C^¤+Aµ©\ÇF2§Q]\Ø\ç\ô(#†r‰]\Ä\0ÃŸÆ£\×\ãœ\ã\0\ô\Ç5\ë)\ð·I\Ò\ì\Þxo·;wÛ•·}\ryŠÀ‰‰lsÁ\í_Z2ƒ³G\ÔÂ¬jJ\éžwz¦9In \öªWVP\ê–\ï@c¬\ê\ò\ì\'c›gx-®F\0\ØO9¯:w½\Ñ\ìG]\Ì\å\ð\Ù\ö«B%A\Æ1œŠ×±\ðšD®°ª>e\"º\'ŽP¬qž¹¨O\ÆJ¼\ô¬e9=Ù²—.\ÆT:t6U~Q\á\èkNµY3±wÁ®rKü¾#;v\ÍOi­\\[ž_\æ\Ïa\Éšº’[Å½Š>	:þu<þ{¸\ñ\rÓ¨þ\ëÂ³´=LÜº\î\0ü>\Õ\Ú\é\ð	€(\ØÇ¾hæ¶†~\ÖG˜ø·\átúÕ‹C&\ÂH\âP>`~µ\á~.øo\â\ï\ÚHª¦¡bNR)×½}­u`\Æ\ß$n\0u¯;\ñ†‰\ö¸J‡úú\ÓUdŽy¥SVyÀ\Í*{]L™ \Z\õ»Xv\Î±¼b¶Q\Â\à\÷ \õ®¶\Þ\ÔwŽ•\É7\Ì\îoEX\ë¼\'\ö\\ŒŠ\õ;X¬Nœb¼\ï\ÂÑ±x\Æ;Š\õk8%KU!€2IÀ­ :¼\Õ\Ï%\ñ\Ä{ã‘p¤\÷¯—uûy³{sep\Ã{ŸÝ¹\Ê\àW\Ò?üYmc\nÁyk3‰P`Á\Âú×ƒ\\\è\Ú&¤O\ö~±\ä\Êÿ\0\ò\Æ\éJ\ó\õ¯¡ÀPtb\äúžN;\õ‡®‡\'‹Z\ÖO+P†H8cr~t\ç\ñ\r¥\ÜÈ™%_\âe \Ö\æ£\àoAjY,\Öþ>\ô,\ô¯<Ô´$²¸w’\Þ]>|Jdúb½d\Ú\Ü\òZDš\ÃG$l6’:ç¡¬<\Íiq\r»\æ©©.ua”(_5q\Í\ÔTW±´Ñ€É»,—…Ist5Š±ú\áoC\á\Ý>8#M˜QÀ\í\Åm\õ>\Ô\×ùŸŽŸýjzŒ\×er\ö\ÐpãŽµ 7­1\çO *–\ÏJéŠ¹Dr>?\Õ~\Ë`È§-Š\ó?øS\Õüq%¥ŠHÎ™Ì¹Á\ï\×5\Ùø€6µ®­º‚\ñƒ–•\Ò\Ú\éb@Ž0@\öX,²\n„+Tv“\×\ä~)\Ä\Üx\ò\\{\ÂÑ¤ªYk­¬þ\æ|>|\ãO\r\ÎK\ÙN<¬±	£n½Ž+éŸ„:-\ØÒ£’\æ%·˜¸D?*\ôY4¨œŒ\Æ2+oEÚ®×´G_\ï(\ÃW^3\ç c“ø‘€\ÄO“	Søü5üú.“r#S±:ú:…t\ö\ÖwH¿=œR{\Æøþb³­5;r¤‰\"\óÚº\r?W³¸ÀK„c\\\Z\ð\Z”\Õ\Ú?Q\Âgyv?J¢\ßk\ë\÷ndj6FKv\Ýiq3ü\Å|\é\ñL1\Ý\ÞD\01#ŒW\×JQ\ã 0#\à_\Z\ô?\'Q[•,ªAÇ¨¯\'8ß±\ô˜y¥-˜uˆ]\\£/~H\êEg[YžIw\ò\Õ\ëv\Í\ÉRp1T!µÜŒ‚+\ç\'¡\ô\ô\æš3~\Øm\Ôc#=\ã¥T¹\Õ<\Çc“¸£š·ydÛ™A\0.{VEÊˆÔ™\n¼Ž1Š\ål\ê‰4L&p\Ä§Òµ\í\ìùW\ÎA<{\×(šªAÁo¡\ìkwL\Öbe\\¹`:“\éP;~–\r¨”\Û»ÿ\0\rÝ©`A\é\Ð\Z\â´K¸.£\0§\é]5¤?g ¡\ãÚ¡«’\Î\òk¥kb¼Œb¹\rX©%J\äšÐµ¸vB	$ƒß¥Z‰N\æ4š\"\'-em\åL[\ä\à\Õ\Ò\Ù|\å@Vk] `c\ÜVÿ\0ƒ\ô\õ\ÍB…†\âGj\ÍCR¥%s\Ðþ\è\rtVgBPt\ã­v~!½“K\Ó\æX\"Wp8\r\Ítš‡‡¥\"ªµkÉ¾\"|l\Ñ<+­\r*\çJ¸¹’`[ÎŽUÀ#¶þµ\êR\Ã\Ê_\n>v¥x¶\Û>eø­\ã\rsL\ñž¡+iN–‘\ìÛ€JœŒ“\í\\_ü&º>£h²\ê:|~c†Œl \ç\ÔW¬ø§\Åž9–ÿ\0u±³\ó†Pn\Î03\\G†>iwV\ò¥Ê¥âŒHÛƒž+\é¡h\ÅEžK»m˜\ö¿c[„:Wˆ\ï4Ë³\Ò”ý;WFš\æ³o\0‹Y\Zoˆ\âÛ»j\Æ…~jÄŸ\n4\áxeµ–H$\î\Ü*¥Ï‚o,¤,Iu“\ïc\éU&­ E>¦¡\ÃýU˜jz-ÿ\0‡n9ý\ä‚ýp{VS|\Ñ5\É\ã}\Å67rd\Ï\î\äúT\Ú\ó\ëz4O\ö]Mf\'þ]µH\ÈCøš\å#×œŸÛžh@notw\Èúü¹¬~\"®}\ö\àÂž)Àƒ\ÏjŽGSü\é\ÊA8¯&›²u\è*¶­t¶¶.\Ù\Çj\ÊW;â›¡4\ÐZÛ½À8\æ½l57Vj©ÅŠ«=Už\É\\³\à¿%\Ë\É\Ñ\÷\ÌKGAÚ½\rˆ(Px\ïY\Z\"\Ê-cU}ÑŽø\Æ+v9Y\08¯®\ÄT•ùS\ÑÌ‹\Øb±515#w6\Ý\Ù\ðÍ¼¯\Ìx\'Ò¬\'†cŒ|µz\Ò}Çš\ÑYW\çN½U¥\Ïw•\àf¹¹\É\ßxw \à~5\Í\ê\Z+!\'vÈ¯L•”ŠÎ¹·IA\È\ÑG8\îy9†CB¦´™\æ\ñ^\êzP&©ÿ\0tœÖ²üU>½¥º\\È¿2¿½z5Î‰\r\Æ~^kX\ðàµˆÉ€\Ñ\ô#\ß)a\ñ1pœU\ÙÃ–\ã3Œ‹\n\Þ\ÖR¥ª»jÞŒù¯^´\Û!\Æ7\ç5\Í3yˆ\Æ3\Ó«¸\ñ¤?e\Ô.b\ôbµÀ\êSœ¹\nTž‡­~oˆ¢\é\ÍÅŸØ˜Jªµ8ÔŽ\Í\'\÷•µK„Š6¼¹\äŸO\\\×	­_G\"n\'i\Îkg[\Ô\ÙX3\Û×Š\ó½F\õ\Ë8*3úW\Z¦®z¾\Ò\Ê\ÄR\ê…e\Ìy9\ÖÆ“\â5\r±FÓž3\Ð\×{$»Ø¡{Œg\õ©-O‘Õ#<t?…n\é\'\Õ|\Ö=\×Ã¾(HW\çû‡¾:W¦èšˆ¾…\\6F:\nù§\Ãú•£‰lsœû×³x?Ux•Wi\àƒ\×Þ¼Ê‘q\Ø\ê„\îzÆž\ßo#½i\ËŽ*—‡\'[¥!O¦9®…t\Ù.\ÜÀ\Åd \Ø\ÝDŒKm2MB\ä\")bN0+\Ý~ø7û*5že]\çµdxÀ¥&•G¨\ë\×tû/³\Â8\à\n\ë§MN&½ý\Ôex¯U‹L\Ò\äiE’\Ù\Æ+\á‰¾ ¶\Ö|n“Ep’\îfk\àŠúk\ö‚º\Õ\ï|?6Ÿ ^\Û\Újsps\÷@\ï_kz\'Œtk“&»\à+]r%\É7ºY\Ãý~_\ð¯¦ÁÁF<\Ì\ðª6Ýd\ÛHqµ¶Œ\íükK\á»Iv·s\ìükÓ®4)\í\äuNAIØ„ù\×C\ð\î\Ö[C)°\ñ”\è\ÌHA‰zg¡®¹%¹)¾§ y²\Ù\Ü\0\ö\ãq\Æ[oQSj¾!U‰ƒZ+xŸ•1.µ¸À\Ø^}\È­bk—·,$¢º\ÊW¬3«þ8\â¹Ò¹«‘•q\â\í*\æv·¸D/’\nI1ø6\rV\Ã^\Ö.Q\á\ò\í\'\'! ‘­\Û?CÁ¯7ñ•†©y¨H\÷ž–\â\Ì\à‰m$\Û*ÿ\0\ß$Ò°t\É-a¸Œ[kº¦wd[j—A\ì[·\åC¹\÷»\0$9\éÿ\0Ö©Œ‘^%Œ¼K¤]\Å\Ã.\Ñ\÷–aÜž9úW¤øW\Ä\Óx‚,´\">pNs\ï^DV§UÎ¥œ$eaš\Î\ðÖšú¦«5\ë\ñ\ZŸ-3\ë\Ü\Ôz\Ì\Â\ÒÉ¾r]¸\õ\î\r¼5m§xf\Ò)­¢yŠn‘™%\'šP\Í\éeØªt¦›”\ïo’Õžng—U\Ìps£N\\·\êr\öpª®\0€«B=@¯@¸\ð¶Ÿp>X¼£\ê‡›q\à¼dÁ0ú8¯¤ŽaJ}l~]>\Æá•¢”’\ìÿ\0Fs0Ã´\ñS6UzÖŒþ½µ\ë\ñ\êœ\Õ\ãxŽ\nŸ\ö†+eR3ø]\Î	\á+acjq\õFuÄ¬;š£-\ã£`\óZ3\0G8ªRÀ\â»!n§\Íb•Kû¬-\ï77#ˆ\çF\Ò\Øw>•4v\'°¬¯#\Åd€u&¶¦£*±±\ç\âªV§‚©Îº6üO½[]Z\ñ\Û\î†\ÅyN£¬A.\åF\Î;\ô\ÅzOÄŒ]\Þ\ÞnË°5\áú\î?˜\Æ#ZùŒ{‹¯/Sú»&¡5€¢»F?’Q¸«r{W¨.wq¼À\ÇA\ô©o\ßS\Ó\Ébžz\Õz\â°$\ñw\ðMnPŒ\ä\÷5\är;\è{OŒeÍ’É€©9=©l\í%\r\÷OA\ÜÐ¾(\Ó\ç#{‘“\È#¿j·g®\é²9\ÌÀtI<KšJ\é¢9`\Ý\îlhV¾L\Ølª“ÁSþy¯_ð§•µI9\ê\Ø\Îk\È,µý,e|\ðPÀ\âº\Í\Çúm›®\Ög+\Ï\nk†¢“wh\Þ4\ÔzŸJøa¼\Ï,\Çl\÷¯d\ðm¥©hÌ¤nÿ\0h\×\ÈZO\ÅÆˆ³\Ú\Ì\ìH\Åv\Ú?\Åmzd«D¹\êO5º\ÜÂ¤\ôGÜš8¶Š5\n\Ê?­jj\Z„v\Ögi\í_)øCâ†®\òÆ³¹lÿ\0x×¬I\âK«\Í!e,F\áÒ½L-b\'\É©\óy…jyu\'_.X®§\â\ë\Ç\Ö5É¦fùT•_LV#$¯\Ê\Ø\ìÕ™®¤\óX\É`N~Zc\ÜÀ\äŽc=\Ã\nú	`±½xx,\÷+Ì—û-x\ËÑ«ýÛ˜š®k¨À\ñ^\ÙÁ{VhÁýkš\Óþ\ZxZ\Æg–\ËMû®r\Æ6Ü¹ú\Z\îeOPJÍ¸µV\')\ö\â¹Ü¤´g·\Ë±\Ìÿ\0\Â#<3‘k|‹eU¿^*Ž§£jþht2\ÆWB¹${]G\ÙæŒ’W\Ð\óQ\É,ŠmÀ²qYº–\Zƒg\Í~(±°‡]\ó!\ñu\æ~F6\Íu\é¹Â»\n\Í5Õ°KË‹}i@ÿ\0Y‰6µ\éWúF«\Æ\Ñ\ê\Zu¥\ðn	ž»Q\\\ç\ìÿ\0\á=Ft¸\Ó\ÍÖ‰>\í\Ê\öses\ô4\ÕHË©>\ÎI\Ü\÷k\Û(/7$\ñ,‹\Ç)ºv•k¥.\ËhV$\0¯\Ö|}\ãÛ‰`Òž\ä=+Š\Ôüs\âÖº0\Ü\ÏyÇ‘©\ÝúW•\í#±\ô\Ôrjµ¾\Ò_3\Þ|a«‹{ûH¸|J¤®}z×¼øS\âUƒ\é\ðù\Ä[¹Q•\Ü¯\Ïû­OZ‘C_ 3\ÖU<š»k¬j¨o2\åc\ç\á\ã0t±•#VWR\ì\Ö\êûž¥,’pEf~‘\Ùø»OºPR\î#\í¸f´\ã\Õm\ä\\‰WŸq_›v\Þ5\Ô#\0-Ü¤…\çi5«c\ñ[·8[û…V\ôŠÁP\ÆS·%T×šýS²E{)¢\Â\ê\"2$Sÿ\0¨\å’\ÖPD†&\í_\Ú|GÕ¶ªF\äqœ	]_\ê(/9ú\Êy¯JŸ\Öû\Ç\ñ9å¶­)~Ú·z>t\ÍG\Õ$²\'\ð®\âm\õÿ\0\Ýv\òx\ÂW#uÌ§>²\Zkø¥Áù®$\Ïa\æZ\õ¨\Õ\Æ\Çy¯\Çü\ÏÁ8V³‚¿t­ùSko†\ãW»¸„#W5\ÃøŸ\Åzv©n±\Ú\\¤„H3^5mþ¹\æ›€\òN>•©\é±h\ÌÁo\Ù\å=K.3_KCNŒUJš\Évÿ\0#\à\ó_\ð\õc*\ñ-\'m,Ÿ\ãsœ\ñ.£\æ\Ï3s–$\ôú\×t\É)l.x\Î1].³*¾w(f\Û\Û5\Í<RIÜ¤ûv¯\Åb=¬Ü\Ö0”}5\ÐÁº\Ó\÷2^™®[Y\ðŠÝ¸ 	8ý+¾’\ÒR\çžI\ÇÖœº<³2“Œž\0®/n\Ñ\Û\ì\Ó\Ý!?xES\÷HÁ5f\Ç\á\ì\ó8,¡•†Nx5\î6\Þ9ù\0\È\ÏN=\ëb\ß\Âc?\ê‡NF3C\Ä\ÊÛ˜ýZ\ržA¥ü1œ‰‚©\Ç\å]\æ‡\ð\ëO·urŠO\î+·‡\Ã\ê¥s*=G¶l´\è\âû±1À+žX‰_F/b–…-\Â6V\è¥\"^ŸÝ®Š\ßK†%]£\éV-¢\Ä*œw¥•\×i¬\ÕVÞ¡È‘±\á¸aK¤\áI\ÏsŠ\ö%G¸\Ð\Ñb\0ü§¯ž…Ø¶pwŽâ¾„ø9\â\ÝT\ðw\Ù\ïŸm\ÊL\È2¹=±_I”Wtª\ÝE¿C\à8\Ë±™eJnj)\õn\È\æe°»; \ÏÒ³\î!l|\ð8\ö\ÅzŽ©VqK<€\Ån™>c\0=k’MgEÕ¤e³\Õ,\î›<¬S«\È\Zý:Ž-\Í^\Ç\ñ†3‡¥…|Ð›\Ð\à/o--Že2CŽ\åª\ñ\êv·-\ò^\Ä\ßR+¼º²L·\éY\ï£A1ù\àC\îV»%-eû\Èƒ\â,\ç,|´k\ÊË£w_s¹\Ì2¶\ÒË‡\ìœ\Õi&€\ãvaŠ\ë\Ã/\"\ØbªO\á˜$r7ý\ôkÎ©•`ª\ë\Ñ\÷X?sL;Q\ÅÑŒ\×\Í?\Õ~+,p\Ê	?\\Šl€Hoœv\Ín\Ë\à»f$‡ž2zm\âŸg\ð\ÇS\ÔI}6K—T?3\Üx•\ò_gyB¢·ž‡\éy_ˆùn>J”¡8\É\ô·7\å¯\à|\îÿ\0dµ·k{k? ·~\òrh\Ò>$\é\×\çj.n?\ç\â9N\á^N\ñ\Ý\Í|m]dgÂ¦\Þ[\ð¯[\ðWÀ}C\\e¾™¬\ãl»w?øW\ç©Zn\Èþ¯©‡À\á\"\åQ\ò§ç©³k\ãMY\Ø\Ý\Ü\Ý^ ;•&\0„ýk¡o\èv¦\ã–\á$D\àU­7\ö~\Ðl™E\ä\×w¾ª\ò\íSø(\êð®¡@‘\Ø\ÛG\nc¢(v\ÓSZN\Ç\Ëb±x&Ó¥\Ì\í\æ‘\ãvºnM¶¨–£¼Û\ê*\ÅýÞ—«DÑf\Î5éŸ³\í\"½\Î\ó\Â\Z^¹lc½²†QŽ¬ƒ?y?Œ>9¥ŸF‰æ¹6\ç\æ?@MiSž*\ñÎ¬#\rˆ¨£R£ƒ\é{5\÷œµ†—¡\Ås\ä\Ü\ê\Æe~Ê {“]\î™\àý\r\íƒDMù#¯›Ö¼ª\ãÁÚ³ŠO\ê\0w8_\ñ¦\Úiúî……m+Th9Û·nW\Û\ït®Hc!\â}N#(\ÄUIÒ¯ý|S—ÀÖ—P\È#­¥\ß\\¥¿„o!\Ö\Ò•”\Ú\î\ÎåŒ}«¶·*,°iú°=IFÿ\0f©l|O\â­Q·š\â-N+ß¼ûB©\ìsœ\Ö\ñ\ÅSª\í©\Ã<;NRsRV\ó¹\ï\Únˆ¢\ÅDC¨\èEy\Æ\Ý\n\çOÓ¤ºˆ:ùl•=³\Í{Oƒ5Èµ].9·±\ö¨¾!h‘k\Z$\ñ¼aƒ!SÇ¨­ev™ù\ï´ý\ç4“m5‹ÀVV\È\Ç\ÊÇ§Ö¶m,RT\äub²4\Èþ\Ãyqc#f[yg=N\rtv\Ãq¼\ã¦;ýkÃ¨\Ü]¥‚M&‡&“68\É\êqžj\ÔZR \0v\íS\Ã¯N3*üQg 8\õ5\È\ä\Í\Ò\îW·³H‡pÞ¸\æµ`¶M¼qI\rºœÁ\Í[\0\àcŠM»	‚Àg€«[£\å\éH‡q©©|\ß-x<\õ\éJ:™\ËDHW\ÊƒY\×\Ò\äcúÕ§‘˜g·¥g]3=½ke¡‡™ys\ó|\Ü.:\Õ\ßøUµž\æ=>\á’\ÙqU«[¼Ú¢\É<§j(\ïš\õ†þ[4\è3 \Ë;×¯„©Rœ¹©»V:•*\Ôù+EIvj\çx›\Æ>4\ñ>¶ú=¶¹s¬\Ê\ós–B¹û¡sŠ\ê|-\ð¾X–\'}n\égQDQ\íü¶\ä~ºkŸ	XX\ê-s;$=O­m\é9qƒ\Â\×\ÐG2®£+°4%W\×n‡£ø\ÂV:žˆ±_K,—h\Û\ZDl\èqSø‡\áû\è\Ènbcql9\'¯Ö°\ô½b\ãDC4*’0\çO\ô¨µ\Ú*\ê\Ñ\Þ\Ú\ëE‚d#klr2?ZT1¸\Ú5œ\çWž£KOG\ÔølÇ‚\òœuB•Nkf»ù¢Ý¶˜\×`ùh_ƒ4³h­\\úŒRø?\ãß† ‹\ì\ó\Ø\Ïg)9,¤8þ†»\ëoøW\Ä1ˆÿ\0´ ù¿‚pPþµ\ßW=\öU9y/s\ä©øp\å‡N¥Dªv¶Ÿy\æ\òhhFpT\×i\ð\Ó7\Ùùd\\ƒ\ï[\ß\ðŒiW±‡·rT\ò05^m\á.¢˜:)\åHÁ¢¦m†\Ì)\Ëjú\Ùw	\ã2}<r‚´^­>›>\Ç\Ì^´\ÖnE\ÍÕŒUû²ycrþ=k­†\Ñ- \nª\04«*\áQ\ßË¶2;û×ˆ£Ê\Ýý¤¦¬Ù“w i°:ûV\ö‘d\0ŒVNk\ö™\Ë\Åu6ÑˆS\Ðb¢1¾¡-e­H•;\ÈÞ¨O©²£ZÄ¿\Õ\ålªŸÆº»\íµ¨ÿ\0Ahbb²ˆ\ßû\Ù\é\\,¶±\È\Ïv\ôZ\å>/üK\Ó<¤\Ïu}tYÀ\áw“\è=\ë\ä~Òº\Å\ÕÔ‡Ožkkf\é?j\ÆYm,C\æz\öˆ\ñ¸:~\Ê\ë\Ï[]ø¿Z\Ñü#gqu¯7\n¥„SP·¾\r|‰\ñ?\ö…\ÕüG{PI\öh\â˜@\ä†Á\ë\ô¯.\×üy¨\ëD›‹™%\Ý\Î³\\\â3\Ë:\0\Ù\Ü@úW]3¼5f8¼û‡³œ¬¼\Ôo\Ù\Ë\\mS\ÂÖŒ\Î\\²+d\õ\é^\ãsl.\ìOBµ\à²\öˆúwƒ¬p\Â%5\ô<C*:WWl\ó\Û\Øø\ó\âÖ|;ãµ¹@Š\íw¼8#ùS\ôIˆXm<w\ã\ð¯Hý£¼0“\éB\ô.\Z\ÕÄ‡¡\à×”xr\ä$j®\Ù\ï\Íxx¸8\Èú<F\é¤u\ö\à(\0cµhE\Z0\È?A\ëY0Ï¹CŒ\éV\è1\0w¯5\êzF¢¨N3–,r(p\Þ\õ˜—€\ç¼Õˆ]_ {š‘\Z	)\ÆzœÔ­\È\Ü1ù\Õd(I\é\ïCN™\É`Ö´Š»1–¤ŒO\\\ñY:•ú[#\÷OzuÞ§JV6\ÞùùTu5«\àÿ\0‡·¾ ½Š\òùY\"S•ˆÿ\03]1ƒ“²F”b®ØŸ|.±©F\î\"9ù‡\Ýµ\írÂº} ‰\0\àcŠŸK\Ò\â\Ò-–4\0b©\ê³\ç<\éŠ\ö)\Ã\Ù\Ä\ð*\Ô\ö²¹\Î^IºN½\ê\ö˜øn\çY³‚d\É9«úv\Ý\ã\'¥8½AèŽŽš<0\Íy?Å¯\\F¦\ö\ÂS\ñü\ÃÐŠ\õ‹R\0k\Å\Ö\òÌ œc‘]Š\ö9³>~\Ò<T—Œ-µ„sƒ…ppO\ã]5¬\÷»dµ¹i\õSÖ¼k¨©WË”\Z?”ƒ\\³>¹\à\æý\â5ý’ž«\÷\ÔV\ÔìŒ“Gºø\'\â­\î‹ Xg(3\Ì2\ò§\ð¯Uµø¬<C\Z\Âûl\ä=\Ôü¦¾_\ðÿ\0Š´ŸGû©TM\Ý[\åq\õ\Ôi\÷3\ØÎ 9–?Q\ÔVt\'\Z½§*¹ÃÁ,}G™\ÆýQè²¶úVU\á7\02j\í\Ôÿ\09\0\ïU2±\ä\ó]R}‡r\í–\ËH·1´³j~hùO«?{HrHÀ\ì*H\Ó\ÍaŽTS\"l”+\\“ž•GYu´³•øùW­h=\ÊÀ§-\è+†ø™¯6‘\áÛ‰¤%P±\ö\Åj\ô\Ø\Çs\à\ß\Ú\Ï\Æ\ò\ê~3\Z\\r3A\0\ÞÀZ\ðV¸2\'V\é\×=+[\Ç:ûø›\Åzû±>|\Ì@\'8^\Ã\ò¬ ß”€£¦}+¡=·d\áûdœ¾µ{Fˆ\Ü\ê¶q§,\ò*\ó\ë‘YŠ\Ø;ù¿*\Ô\ð\åØ³\ñ\r…\Ã\á•&S\È\÷\éN\ãI6~¬ü	³x^\ÕX`ˆ\Ô~•\ë\ö\÷ \ã‘^%\ðsUŽ\ã\Â\ö¬¤1(W\é^›È•p$\ØÃ¡5\æ\'\ïjw\É>&é‰¬\è0•\Ü$—\ô¯•\ô\Û1§\Ü4R9Ê’8\í_Wj\Þy±udiWFM|\á\â\n\î\Ï_¹aip!‘\÷ƒ\åœû\×.<\Ê\è\ôpS\å|¬t\áx\'À&¬‰Hüª\ëY1†·g\nÀ‚jd2\Ø2q\×œ×‰\Ê\î}’±¥×•‚¶E5\õvŒ\íQ¸\ç“Kc¡\êZ‹‚\Öw\Ï}§\×\è	5[\Å\r8X2sÓš¸Òœ¶F3¯NŽ\"]r\ì6\ã\Ë\Ûµ \é:¿ˆ\ß\÷v\ÍGþZ¸=+\×4?„\öv!d•Ž:™9®\Æ\×G²\ÓQB„þ¯F–\r\ï#Ë­ŽKJg\á?†–¶$–14\Ýw°\Ízµœvq@@)\È\'dHzÒ»^y¯Ncy­*VC{6\Ø\óš\æ\ïe.O<V†¡s– 7ZÄº¨›\è]5mJ²}\îA\ëW\ôâ¥¸\ô\ïY\ìwzVŽ˜ \ãI¬\àµ5‘\Ñ@pƒŽEExDŠÀŽ*h\÷~ý\ê)~5\è-Ž	³–¿\Ñbºc±@sÞ¹-gJkvd‘<\Å\õ\Åz\"+’cŠ‹[\Ó\ê\ß;9\ÇQ\\sV6„¬\n\Ôü#es8š\0,\î\å]x9þµg\Ãþ!¿\Ñ\õ\ìµü\è\É\ÂÎ½Öº]_L\Ú\Ì0I«›’È‹ˆUX¦\Ó\È<\æ±r¾Œ\ì[ÿ\Ù','admin',0,0,0,NULL,'2026-10-19 19:15:40','2026-10-19 19:14:53','2026-11-05 00:02:20'),(10,'carl','PBKDF2-SHA256$120000$BcYGqAPX089+fYd5GkmDug==$2YIogAsyLjmNaGIg7bh2J9HvOzcLYTVNcDMsYaxoigo=','carl vincent alicante',NULL,'road 9',NULL,'admin',1,0,0,NULL,NULL,'2026-10-20 00:11:59','2026-10-20 00:12:09'),(11,'red','PBKDF2-SHA256$120000$XAebtAaOGtbXB+VtRbX03w==$Zq9zNbG2gnrxhBnCvtx4tCjBuon21/RcNteFQm0lqPc=','red',NULL,NULL,NULL,'admin',1,0,0,NULL,'2026-10-22 06:21:12','2026-10-20 00:26:25','2026-10-22 06:21:12'),(12,'muksin','PBKDF2-SHA256$120000$LIf0WnsmBhyO5w0H1xyyAQ==$IsQsVHu2fyrwGnP5ys967FSw1e2ekfnOl7qu6WBvivc=','muksin',NULL,NULL,NULL,'admin',1,0,0,NULL,NULL,'2026-10-20 00:29:48','2026-10-20 00:29:57'),(13,'francis','PBKDF2-SHA256$120000$JPLU/DBGqqNjR11jMiJKKg==$eAiNAF5K4tsqdiF2YczF7nZGUsHaeDiCfGrsoPBlknU=','francis',NULL,NULL,NULL,'cashier',1,0,0,NULL,'2026-10-22 12:00:58','2026-10-20 00:30:55','2026-10-22 12:00:58'),(14,'beanch','PBKDF2-SHA256$120000$q6O3G4KokDLODsOqvjLRuQ==$+nUHeUcw0jAy7IyPt6tLWTuvJJDDd4WiHa6iVoJjq0A=','Beanch','09876544567',NULL,NULL,'admin',1,0,0,NULL,NULL,'2026-10-20 07:31:10','2026-10-20 07:31:10'),(15,'jasljsadjbsadsakjdbsajdbsajdbs','PBKDF2-SHA256$120000$JpcDvn0CKBeFj9iXMOHbRw==$HnNdVnqbNe8KVE+B1h/QkaZzC3oFM+y5XqKTnpnmP6U=','Jbasdkjsabdkjsabdkjsabdjkasdbsadbsajkdbsajkdbskadbsjakdbsajkdbsajkdbsajkdbsajkdbsajkdbsajkdbsajkdbas','09423482342','jsandsajdnjkadjkasndjasfbias jkc jkabfuiwbfiabxlajfbqowufbaskjkcbasjcbaoawufbowubaxjckojwbqisjcaxkjcaibcoqwbdjascoajwbcouwbcoauwbcouasbcjasbcojawbouwbbaouwdbwubwuibwaojwbowubuowabuabcksjcbkajwbcuwdubasckjasciqbwubqwubkjxc iwbcubwciuabcuiuwabyuegydtygwqtfy',NULL,'cashier',1,0,0,NULL,NULL,'2026-11-09 02:32:06','2026-11-09 02:32:06');
/*!40000 ALTER TABLE `users` ENABLE KEYS */;
UNLOCK TABLES;
/*!50003 SET @saved_cs_client      = @@character_set_client */ ;
/*!50003 SET @saved_cs_results     = @@character_set_results */ ;
/*!50003 SET @saved_col_connection = @@collation_connection */ ;
/*!50003 SET character_set_client  = utf8mb4 */ ;
/*!50003 SET character_set_results = utf8mb4 */ ;
/*!50003 SET collation_connection  = utf8mb4_0900_ai_ci */ ;
/*!50003 SET @saved_sql_mode       = @@sql_mode */ ;
/*!50003 SET sql_mode              = 'ONLY_FULL_GROUP_BY,STRICT_TRANS_TABLES,NO_ZERO_IN_DATE,NO_ZERO_DATE,ERROR_FOR_DIVISION_BY_ZERO,NO_ENGINE_SUBSTITUTION' */ ;
DELIMITER ;;
/*!50003 CREATE*/ /*!50017 DEFINER=`root`@`localhost`*/ /*!50003 TRIGGER `trg_users_ai` AFTER INSERT ON `users` FOR EACH ROW INSERT INTO change_log (module) VALUES ('users') */;;
DELIMITER ;
/*!50003 SET sql_mode              = @saved_sql_mode */ ;
/*!50003 SET character_set_client  = @saved_cs_client */ ;
/*!50003 SET character_set_results = @saved_cs_results */ ;
/*!50003 SET collation_connection  = @saved_col_connection */ ;
/*!50003 SET @saved_cs_client      = @@character_set_client */ ;
/*!50003 SET @saved_cs_results     = @@character_set_results */ ;
/*!50003 SET @saved_col_connection = @@collation_connection */ ;
/*!50003 SET character_set_client  = utf8mb4 */ ;
/*!50003 SET character_set_results = utf8mb4 */ ;
/*!50003 SET collation_connection  = utf8mb4_0900_ai_ci */ ;
/*!50003 SET @saved_sql_mode       = @@sql_mode */ ;
/*!50003 SET sql_mode              = 'ONLY_FULL_GROUP_BY,STRICT_TRANS_TABLES,NO_ZERO_IN_DATE,NO_ZERO_DATE,ERROR_FOR_DIVISION_BY_ZERO,NO_ENGINE_SUBSTITUTION' */ ;
DELIMITER ;;
/*!50003 CREATE*/ /*!50017 DEFINER=`root`@`localhost`*/ /*!50003 TRIGGER `trg_users_au` AFTER UPDATE ON `users` FOR EACH ROW INSERT INTO change_log (module) VALUES ('users') */;;
DELIMITER ;
/*!50003 SET sql_mode              = @saved_sql_mode */ ;
/*!50003 SET character_set_client  = @saved_cs_client */ ;
/*!50003 SET character_set_results = @saved_cs_results */ ;
/*!50003 SET collation_connection  = @saved_col_connection */ ;
/*!50003 SET @saved_cs_client      = @@character_set_client */ ;
/*!50003 SET @saved_cs_results     = @@character_set_results */ ;
/*!50003 SET @saved_col_connection = @@collation_connection */ ;
/*!50003 SET character_set_client  = utf8mb4 */ ;
/*!50003 SET character_set_results = utf8mb4 */ ;
/*!50003 SET collation_connection  = utf8mb4_0900_ai_ci */ ;
/*!50003 SET @saved_sql_mode       = @@sql_mode */ ;
/*!50003 SET sql_mode              = 'ONLY_FULL_GROUP_BY,STRICT_TRANS_TABLES,NO_ZERO_IN_DATE,NO_ZERO_DATE,ERROR_FOR_DIVISION_BY_ZERO,NO_ENGINE_SUBSTITUTION' */ ;
DELIMITER ;;
/*!50003 CREATE*/ /*!50017 DEFINER=`root`@`localhost`*/ /*!50003 TRIGGER `trg_users_ad` AFTER DELETE ON `users` FOR EACH ROW INSERT INTO change_log (module) VALUES ('users') */;;
DELIMITER ;
/*!50003 SET sql_mode              = @saved_sql_mode */ ;
/*!50003 SET character_set_client  = @saved_cs_client */ ;
/*!50003 SET character_set_results = @saved_cs_results */ ;
/*!50003 SET collation_connection  = @saved_col_connection */ ;

--
-- Table structure for table `vat_rates`
--

DROP TABLE IF EXISTS `vat_rates`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `vat_rates` (
  `id` int NOT NULL AUTO_INCREMENT,
  `name` varchar(50) NOT NULL,
  `percentage` decimal(5,2) NOT NULL,
  `is_active` tinyint(1) NOT NULL DEFAULT '1',
  `created_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `updated_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  UNIQUE KEY `uq_vat_name` (`name`),
  CONSTRAINT `chk_vat_pct` CHECK (((`percentage` >= 0) and (`percentage` <= 100)))
) ENGINE=InnoDB AUTO_INCREMENT=2 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `vat_rates`
--

LOCK TABLES `vat_rates` WRITE;
/*!40000 ALTER TABLE `vat_rates` DISABLE KEYS */;
INSERT INTO `vat_rates` VALUES (1,'VAT',12.00,1,'2026-10-18 12:08:09','2026-11-05 00:02:55');
/*!40000 ALTER TABLE `vat_rates` ENABLE KEYS */;
UNLOCK TABLES;
/*!50003 SET @saved_cs_client      = @@character_set_client */ ;
/*!50003 SET @saved_cs_results     = @@character_set_results */ ;
/*!50003 SET @saved_col_connection = @@collation_connection */ ;
/*!50003 SET character_set_client  = utf8mb4 */ ;
/*!50003 SET character_set_results = utf8mb4 */ ;
/*!50003 SET collation_connection  = utf8mb4_0900_ai_ci */ ;
/*!50003 SET @saved_sql_mode       = @@sql_mode */ ;
/*!50003 SET sql_mode              = 'ONLY_FULL_GROUP_BY,STRICT_TRANS_TABLES,NO_ZERO_IN_DATE,NO_ZERO_DATE,ERROR_FOR_DIVISION_BY_ZERO,NO_ENGINE_SUBSTITUTION' */ ;
DELIMITER ;;
/*!50003 CREATE*/ /*!50017 DEFINER=`root`@`localhost`*/ /*!50003 TRIGGER `trg_vat_rates_ai` AFTER INSERT ON `vat_rates` FOR EACH ROW INSERT INTO change_log (module) VALUES ('vat_rates') */;;
DELIMITER ;
/*!50003 SET sql_mode              = @saved_sql_mode */ ;
/*!50003 SET character_set_client  = @saved_cs_client */ ;
/*!50003 SET character_set_results = @saved_cs_results */ ;
/*!50003 SET collation_connection  = @saved_col_connection */ ;
/*!50003 SET @saved_cs_client      = @@character_set_client */ ;
/*!50003 SET @saved_cs_results     = @@character_set_results */ ;
/*!50003 SET @saved_col_connection = @@collation_connection */ ;
/*!50003 SET character_set_client  = utf8mb4 */ ;
/*!50003 SET character_set_results = utf8mb4 */ ;
/*!50003 SET collation_connection  = utf8mb4_0900_ai_ci */ ;
/*!50003 SET @saved_sql_mode       = @@sql_mode */ ;
/*!50003 SET sql_mode              = 'ONLY_FULL_GROUP_BY,STRICT_TRANS_TABLES,NO_ZERO_IN_DATE,NO_ZERO_DATE,ERROR_FOR_DIVISION_BY_ZERO,NO_ENGINE_SUBSTITUTION' */ ;
DELIMITER ;;
/*!50003 CREATE*/ /*!50017 DEFINER=`root`@`localhost`*/ /*!50003 TRIGGER `trg_vat_rates_au` AFTER UPDATE ON `vat_rates` FOR EACH ROW INSERT INTO change_log (module) VALUES ('vat_rates') */;;
DELIMITER ;
/*!50003 SET sql_mode              = @saved_sql_mode */ ;
/*!50003 SET character_set_client  = @saved_cs_client */ ;
/*!50003 SET character_set_results = @saved_cs_results */ ;
/*!50003 SET collation_connection  = @saved_col_connection */ ;
/*!50003 SET @saved_cs_client      = @@character_set_client */ ;
/*!50003 SET @saved_cs_results     = @@character_set_results */ ;
/*!50003 SET @saved_col_connection = @@collation_connection */ ;
/*!50003 SET character_set_client  = utf8mb4 */ ;
/*!50003 SET character_set_results = utf8mb4 */ ;
/*!50003 SET collation_connection  = utf8mb4_0900_ai_ci */ ;
/*!50003 SET @saved_sql_mode       = @@sql_mode */ ;
/*!50003 SET sql_mode              = 'ONLY_FULL_GROUP_BY,STRICT_TRANS_TABLES,NO_ZERO_IN_DATE,NO_ZERO_DATE,ERROR_FOR_DIVISION_BY_ZERO,NO_ENGINE_SUBSTITUTION' */ ;
DELIMITER ;;
/*!50003 CREATE*/ /*!50017 DEFINER=`root`@`localhost`*/ /*!50003 TRIGGER `trg_vat_rates_ad` AFTER DELETE ON `vat_rates` FOR EACH ROW INSERT INTO change_log (module) VALUES ('vat_rates') */;;
DELIMITER ;
/*!50003 SET sql_mode              = @saved_sql_mode */ ;
/*!50003 SET character_set_client  = @saved_cs_client */ ;
/*!50003 SET character_set_results = @saved_cs_results */ ;
/*!50003 SET collation_connection  = @saved_col_connection */ ;
/*!40103 SET TIME_ZONE=@OLD_TIME_ZONE */;

/*!40101 SET SQL_MODE=@OLD_SQL_MODE */;
/*!40014 SET FOREIGN_KEY_CHECKS=@OLD_FOREIGN_KEY_CHECKS */;
/*!40014 SET UNIQUE_CHECKS=@OLD_UNIQUE_CHECKS */;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
/*!40111 SET SQL_NOTES=@OLD_SQL_NOTES */;

-- Dump completed on 2026-11-11  3:22:22


-- ---------------------- upgrade to schema v6 ---------------------------
-- Schema v6: SMS queue (matches the real dtsims2_db definition)
-- Safe to run more than once. Drops the sandbox's sms_queue first so an earlier
-- guessed version gets replaced (the queue is only temporary messages).
USE `dtsims2_sandbox`;

DROP TABLE IF EXISTS `sms_queue`;
CREATE TABLE `sms_queue` (
  `id` int NOT NULL AUTO_INCREMENT,
  `phone_number` varchar(30) NOT NULL,
  `message` varchar(480) NOT NULL,
  `status` enum('Pending','Sending','Failed') NOT NULL DEFAULT 'Pending',
  `attempts` int NOT NULL DEFAULT '0',
  `created_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  KEY `idx_sms_status` (`status`,`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

INSERT IGNORE INTO `schema_version` (`version`, `description`) VALUES (6, 'SMS queue');

SELECT MAX(version) AS schema_version_now FROM schema_version;   -- should say 6
