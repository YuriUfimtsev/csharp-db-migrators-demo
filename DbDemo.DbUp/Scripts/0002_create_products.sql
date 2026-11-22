CREATE TABLE products (
    id          serial        PRIMARY KEY,
    name        varchar(200)  NOT NULL,
    price       numeric(10,2) NOT NULL,
    category_id int           REFERENCES categories (id),
    created_at  timestamptz   NOT NULL DEFAULT now()
);
