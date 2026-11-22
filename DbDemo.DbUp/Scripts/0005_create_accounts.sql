CREATE TABLE accounts (
    id      serial        PRIMARY KEY,
    owner   varchar(100)  NOT NULL,
    balance numeric(12,2) NOT NULL
);

INSERT INTO accounts (owner, balance) VALUES ('Анна', 100), ('Борис', 50), ('Вера', 200);
