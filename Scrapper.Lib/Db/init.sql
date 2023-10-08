CREATE TABLE Imoveis(
	Id SERIAL PRIMARY KEY
	, QuantidadeBanheiros INT
	, QuantidadeQuartos INT
	, QuantidadeVagas INT
	, Preco DECIMAL(10,2)
	, Titulo VARCHAR(512)
	, Endereco VARCHAR(512)
	, Adicionais VARCHAR(512)	
	, Status VARCHAR(64)
);
CREATE TABLE Imagens(
	Id SERIAL PRIMARY KEY
	,IdImovel INT	
	,Url VARCHAR(512)
);