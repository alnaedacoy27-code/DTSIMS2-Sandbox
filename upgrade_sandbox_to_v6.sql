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
