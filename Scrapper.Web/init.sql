CREATE TABLE imoveis (
    id SERIAL PRIMARY KEY,
    codigo VARCHAR(50),
    url VARCHAR(255),
    imagem_url VARCHAR(255),
    descricao TEXT,
    endereco VARCHAR(255),
    preco VARCHAR(50)
);