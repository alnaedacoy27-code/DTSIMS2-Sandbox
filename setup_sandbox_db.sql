-- Run this ONCE in MySQL Workbench (as root) before starting the sandbox app.
-- It creates a separate schema so the original dtsims2_db is never touched.

CREATE DATABASE IF NOT EXISTS dtsims2_sandbox
  CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci;

-- Same login as the original app (change if yours differs)
GRANT ALL PRIVILEGES ON dtsims2_sandbox.* TO 'darwin_server'@'%';
GRANT ALL PRIVILEGES ON dtsims2_sandbox.* TO 'darwin_server'@'localhost';
FLUSH PRIVILEGES;

-- Next step: Server > Data Import > "Import from Self-Contained File"
-- pick dtsims2_db.sql, set "Default Target Schema" = dtsims2_sandbox, Start Import.
-- (The dump has no CREATE DATABASE / USE lines, so it lands in whatever target you pick.)
