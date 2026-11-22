INSERT INTO categories (name) VALUES ('Периферия'), ('Мониторы'), ('Хранение данных');

INSERT INTO products (name, price, category_id) VALUES
  ('Мышь',         990,   (SELECT id FROM categories WHERE name = 'Периферия')),
  ('Клавиатура',   2490,  (SELECT id FROM categories WHERE name = 'Периферия')),
  ('Монитор 27"',  21990, (SELECT id FROM categories WHERE name = 'Мониторы')),
  ('SSD 1 ТБ',     6990,  (SELECT id FROM categories WHERE name = 'Хранение данных')),
  ('Флешка 64 ГБ', 590,   (SELECT id FROM categories WHERE name = 'Хранение данных'));
