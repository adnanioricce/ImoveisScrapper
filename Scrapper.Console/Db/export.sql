PRAGMA foreign_keys=OFF;
BEGIN TRANSACTION;
CREATE TABLE Imoveis(

	Id INT PRIMARY KEY

	, QuantidadeBanheiros INT

	, QuantidadeQuartos INT

	, QuantidadeVagas INT

	, Preco DECIMAL(10,2)

	, Titulo NVARCHAR(512)

	, Endereco NVARCHAR(512)

	, Adicionais NVARCHAR(512)	

	, Status NVARCHAR(64)

);
COMMIT;
