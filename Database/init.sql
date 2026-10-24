-- ============================================================
-- EcoFood - criacao do banco de dados MySQL
-- ODS 12 (Consumo e Producao Responsaveis) / ODS 2 (Fome Zero)
-- ============================================================
CREATE DATABASE IF NOT EXISTS ecofood
CHARACTER SET utf8mb4
COLLATE utf8mb4_unicode_ci;
USE ecofood;
-- ------------------------------------------------------------
-- Usuarios: doadores, receptores e admin na mesma tabela
-- ------------------------------------------------------------
CREATE TABLE IF NOT EXISTS Usuarios (
Id INT AUTO_INCREMENT PRIMARY KEY,
Nome VARCHAR(150) NOT NULL,
Email VARCHAR(150) NOT NULL UNIQUE,
Telefone VARCHAR(20),
TipoUsuario ENUM('Doador','Receptor','Admin') NOT NULL,
Endereco VARCHAR(255) NOT NULL,
DataCadastro DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
) ENGINE=InnoDB;
-- ------------------------------------------------------------
-- Doacoes: excedentes publicados por um doador
-- ------------------------------------------------------------
CREATE TABLE IF NOT EXISTS Doacoes (
Id INT AUTO_INCREMENT PRIMARY KEY,
DoadorId INT NOT NULL,
Descricao VARCHAR(255) NOT NULL,
Categoria VARCHAR(80),
Quantidade INT NOT NULL,
UnidadeMedida VARCHAR(20) NOT NULL DEFAULT 'kg',
ValidadeAlimento DATE NULL,
Status ENUM('Disponivel','Reservado','Retirado','Expirado','Cancelado')
NOT NULL DEFAULT 'Disponivel',
Observacoes VARCHAR(500),
DataCriacao DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
CONSTRAINT fk_doacoes_doador FOREIGN KEY (DoadorId)
REFERENCES Usuarios(Id) ON DELETE CASCADE
) ENGINE=InnoDB;
-- ------------------------------------------------------------
-- Reservas: vinculo entre doacao e receptor
-- EnderecoConfirmado e EnderecoRetiradaSnapshot sao a regra
-- de confirmacao manual de endereco do EcoFood
-- ------------------------------------------------------------
CREATE TABLE IF NOT EXISTS Reservas (
Id INT AUTO_INCREMENT PRIMARY KEY,
DoacaoId INT NOT NULL,
ReceptorId INT NOT NULL,
EnderecoConfirmado BOOLEAN NOT NULL DEFAULT FALSE,
EnderecoRetiradaSnapshot VARCHAR(255) NOT NULL,
DataReserva DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
DataRetirada DATETIME NULL,
Status ENUM('Pendente','Concluida','Cancelada') NOT NULL DEFAULT
'Pendente',
CONSTRAINT fk_reservas_doacao FOREIGN KEY (DoacaoId)
REFERENCES Doacoes(Id) ON DELETE CASCADE,
CONSTRAINT fk_reservas_receptor FOREIGN KEY (ReceptorId)
REFERENCES Usuarios(Id) ON DELETE CASCADE,
CONSTRAINT uq_reserva_doacao UNIQUE (DoacaoId)
) ENGINE=InnoDB;
-- ------------------------------------------------------------
-- Indices de apoio para as consultas mais frequentes
-- ------------------------------------------------------------
CREATE INDEX idx_doacoes_status ON Doacoes(Status);
CREATE INDEX idx_doacoes_doador ON Doacoes(DoadorId);
CREATE INDEX idx_usuarios_tipo ON Usuarios(TipoUsuario);
-- ------------------------------------------------------------
-- Dados de exemplo para desenvolvimento e apresentacao
-- ------------------------------------------------------------
INSERT INTO Usuarios (Nome, Email, Telefone, TipoUsuario, Endereco) VALUES
('Padaria Pao Nosso',
 'padaria@exemplo.com',
 '31999990001', 'Doador',
'R. das Flores, 100 - Contagem/MG'),
('Mercado Bom Preco',
 'mercado@exemplo.com',
 '31999990002', 'Doador',
'Av. Central, 500 - Contagem/MG'),
('Restaurante Sabor',
 'restaurante@exemplo.com','31999990003', 'Doador',
'R. do Comercio, 80 - Contagem/MG'),
('ONG Mesa Solidaria',
 'ong@exemplo.com',
 '31999990004', 'Receptor',
'R. da Esperanca, 50 - Contagem/MG'),
('Abrigo Novo Lar',
 'abrigo@exemplo.com',
 '31999990005', 'Receptor',
'Tv. da Uniao, 20 - Contagem/MG');
INSERT INTO Doacoes (DoadorId, Descricao, Categoria, Quantidade,
UnidadeMedida, ValidadeAlimento, Observacoes) VALUES
(1, 'Paes e bisnagas do dia',
 'Panificados',
 15, 'kg', CURDATE() +
INTERVAL 1 DAY, 'Retirar ate as 19h.'),
(2, 'Frutas e verduras variadas', 'Hortifruti',
 30, 'kg', CURDATE() +
INTERVAL 2 DAY, NULL),
(3, 'Marmitas prontas do almoco', 'Pratos prontos',22, 'un', CURDATE(),
'Consumir no mesmo dia.');