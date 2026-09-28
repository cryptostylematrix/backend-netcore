-- Query Tool connected to postgres, as administrator.
-- Set the password afterward in Login/Group Roles > cs_ui_app > Properties > Definition.
-- Deliberately fails if the role already exists.
CREATE ROLE cs_ui_app LOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION;
