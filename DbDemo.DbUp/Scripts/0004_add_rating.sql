-- DbUp не умеет откатывать. Отмена изменения делается новым скриптом.
ALTER TABLE products ADD COLUMN rating int NOT NULL DEFAULT 0;
