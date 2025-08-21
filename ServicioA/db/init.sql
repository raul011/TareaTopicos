-- Crear base de datos
--CREATE DATABASE universidad;
--\c universidad;

-- Tabla Plan de Estudios
CREATE TABLE plan_estudios (
    id INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    nombre VARCHAR(120) NOT NULL,
    anio INT NOT NULL
);

-- Tabla Materias
CREATE TABLE materias (
    id INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    nombre VARCHAR(120) NOT NULL,
    creditos INT NOT NULL CHECK (creditos > 0)
);

-- Tabla intermedia (pertenencia Plan-Materia)
CREATE TABLE plan_materias (
    id INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    plan_id INT NOT NULL,
    materia_id INT NOT NULL,
    semestre INT NOT NULL CHECK (semestre BETWEEN 1 AND 10),
    CONSTRAINT fk_plan FOREIGN KEY (plan_id) REFERENCES plan_estudios(id) ON DELETE CASCADE,
    CONSTRAINT fk_materia FOREIGN KEY (materia_id) REFERENCES materias(id) ON DELETE CASCADE,
    CONSTRAINT uq_plan_materia UNIQUE (plan_id, materia_id)
);

-- Datos iniciales (planes)
INSERT INTO plan_estudios (nombre, anio) VALUES
('Ingeniería de Sistemas', 2025),
('Ingeniería Informática', 2025);

-- Datos iniciales (materias)
INSERT INTO materias (nombre, creditos) VALUES
('Calculo I', 5),
('Programación I', 4),
('Base de Datos I', 4),
('Física I', 5),
('Contabilidad I', 3),
('Estructuras de Datos', 4),
('Programación Orientada a Objetos', 4),
('Sistemas Operativos', 5),
('Redes de Computadoras', 5),
('Ingeniería de Software I', 4),
('Desarrollo Web', 4),
('Inteligencia Artificial', 5),
('Ciberseguridad', 5),
('Arquitectura de Computadoras', 4),
('Compiladores', 4);

-- 
INSERT INTO plan_materias (plan_id, materia_id, semestre) VALUES
-- Ingeniería de Sistemas
(1, 1, 1), -- Calculo I
(1, 2, 1), -- Programación I
(1, 3, 2), -- Base de Datos I
(1, 6, 2), -- Estructuras de Datos
(1, 7, 3), -- Programación Orientada a Objetos
(1, 8, 4), -- Sistemas Operativos
(1, 9, 5), -- Redes de Computadoras
(1, 10, 5), -- Ingeniería de Software I
(1, 11, 6), -- Desarrollo Web
(1, 12, 7), -- Inteligencia Artificial
(1, 13, 8), -- Ciberseguridad
(1, 14, 3), -- Arquitectura de Computadoras
(1, 15, 8), -- Compiladores

-- Ingeniería Informática
(2, 1, 1), -- Calculo I
(2, 4, 1), -- Física I
(2, 5, 2), -- Contabilidad I
(2, 2, 1), -- Programación I
(2, 3, 2), -- Base de Datos I
(2, 6, 2), -- Estructuras de Datos
(2, 7, 3), -- Programación Orientada a Objetos
(2, 8, 4), -- Sistemas Operativos
(2, 9, 5), -- Redes de Computadoras
(2, 10, 5), -- Ingeniería de Software I
(2, 11, 6), -- Desarrollo Web
(2, 12, 7), -- Inteligencia Artificial
(2, 13, 8), -- Ciberseguridad
(2, 14, 3), -- Arquitectura de Computadoras
(2, 15, 8); -- Compiladores
