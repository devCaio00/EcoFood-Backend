-- Executar uma vez apenas em bancos criados pela versao antiga de Database/init.sql.
USE ecofood;

ALTER TABLE Usuarios ADD COLUMN SenhaHash VARCHAR(255) NULL;

ALTER TABLE Doacoes
    DROP FOREIGN KEY fk_doacoes_doador,
    ADD CONSTRAINT fk_doacoes_doador_restrict FOREIGN KEY (DoadorId)
        REFERENCES Usuarios(Id) ON DELETE RESTRICT;

ALTER TABLE Reservas
    DROP FOREIGN KEY fk_reservas_receptor,
    ADD CONSTRAINT fk_reservas_receptor_restrict FOREIGN KEY (ReceptorId)
        REFERENCES Usuarios(Id) ON DELETE RESTRICT;
