-- Query Tool connected to postgres, as administrator, with Auto-commit ON.
-- Run this file by itself: CREATE DATABASE cannot run inside a transaction.
-- Administrator remains the owner. Deliberately fails if the database exists.
CREATE DATABASE cs_ui TEMPLATE template0;
