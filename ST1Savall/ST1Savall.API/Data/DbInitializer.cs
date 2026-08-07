using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ST1Savall.Shared.Data;
using System.Threading.Tasks;

namespace ST1Savall.API.Data;

public static class DbInitializer
{
    public static async Task InitializeAsync(
        ApplicationDbContext context,
        SageGestionDbContext sageGestionContext,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager)
    {
        // Ensure database is created
        await context.Database.EnsureCreatedAsync();

        // Mantener los artículos de Sage y restaurar la tabla local del CRUD de tipos.
        await context.Database.ExecuteSqlRawAsync(@"
            IF COL_LENGTH('Contenedores', 'CodigoArticulo') IS NULL
            BEGIN
                ALTER TABLE Contenedores
                ADD CodigoArticulo NVARCHAR(20) NOT NULL CONSTRAINT DF_Contenedores_CodigoArticulo DEFAULT ('');
            END;

            IF OBJECT_ID('ContenedoresTipos', 'U') IS NULL
            BEGIN
                CREATE TABLE ContenedoresTipos (
                    IdTipo INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                    Descripcion NVARCHAR(50) NOT NULL,
                    CapacidadMetrosCubicos DECIMAL(5,2) NULL,
                    LargoCm INT NULL,
                    AnchoCm INT NULL,
                    AltoCm INT NULL
                );
            END;

            IF NOT EXISTS (SELECT 1 FROM ContenedoresTipos)
            BEGIN
                INSERT INTO ContenedoresTipos (Descripcion, CapacidadMetrosCubicos, LargoCm, AnchoCm, AltoCm)
                VALUES (N'Contenedor 5m³ con puerta', 5.00, 300, 180, 100),
                       (N'Patera 3m³', 3.00, 250, 150, 80);
            END;

            -- SQL Server compila el lote antes de ejecutar ALTER TABLE. Usar SQL
            -- dinámico evita que una base anterior falle al no tener aún IdTipo.
            IF COL_LENGTH('Contenedores', 'IdTipo') IS NULL
                EXEC(N'ALTER TABLE Contenedores ADD IdTipo INT NULL;');

            DECLARE @TipoPredeterminado INT = (SELECT MIN(IdTipo) FROM ContenedoresTipos);
            IF @TipoPredeterminado IS NOT NULL
                EXEC sp_executesql N'UPDATE Contenedores SET IdTipo = @Tipo WHERE IdTipo IS NULL;',
                    N'@Tipo INT', @Tipo = @TipoPredeterminado;

            IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Contenedores_ContenedoresTipos_IdTipo')
                EXEC(N'ALTER TABLE Contenedores ADD CONSTRAINT FK_Contenedores_ContenedoresTipos_IdTipo
                    FOREIGN KEY (IdTipo) REFERENCES ContenedoresTipos(IdTipo);');
        ");

        // Remove the retired shifts and employee-schedule schema from existing databases.
        await context.Database.ExecuteSqlRawAsync(@"
            IF OBJECT_ID(N'HorariosOperarios', N'U') IS NOT NULL DROP TABLE HorariosOperarios;
            IF OBJECT_ID(N'Turnos', N'U') IS NOT NULL DROP TABLE Turnos;

            DECLARE @ColumnName sysname, @ConstraintName sysname, @Sql nvarchar(max);
            DECLARE RemovedScheduleColumns CURSOR LOCAL FAST_FORWARD FOR
                SELECT name FROM (VALUES
                    (N'MinutosMaximosDiarios'), (N'MinutosMaximosSemanales'),
                    (N'TrabajaSabados'), (N'TrabajaDomingos')) AS ColumnsToRemove(name)
                WHERE COL_LENGTH(N'Operarios', name) IS NOT NULL;
            OPEN RemovedScheduleColumns;
            FETCH NEXT FROM RemovedScheduleColumns INTO @ColumnName;
            WHILE @@FETCH_STATUS = 0
            BEGIN
                SET @ConstraintName = NULL;
                SELECT @ConstraintName = dc.name
                FROM sys.default_constraints dc
                INNER JOIN sys.columns c ON c.default_object_id = dc.object_id
                WHERE dc.parent_object_id = OBJECT_ID(N'Operarios') AND c.name = @ColumnName;
                IF @ConstraintName IS NOT NULL
                BEGIN
                    SET @Sql = N'ALTER TABLE Operarios DROP CONSTRAINT ' + QUOTENAME(@ConstraintName);
                    EXEC sp_executesql @Sql;
                END
                SET @Sql = N'ALTER TABLE Operarios DROP COLUMN ' + QUOTENAME(@ColumnName);
                EXEC sp_executesql @Sql;
                FETCH NEXT FROM RemovedScheduleColumns INTO @ColumnName;
            END
            CLOSE RemovedScheduleColumns;
            DEALLOCATE RemovedScheduleColumns;
        ");

        // Ensure new Parametros columns exist in databases created before these fields were added.
        await context.Database.ExecuteSqlRawAsync(@"
            IF OBJECT_ID(N'Parametros', N'U') IS NOT NULL
            BEGIN
                IF COL_LENGTH('Parametros', 'AvisoTiempoServicio') IS NULL
                    ALTER TABLE Parametros ADD AvisoTiempoServicio INT NOT NULL
                        CONSTRAINT DF_Parametros_AvisoTiempoServicio DEFAULT (0);

                IF COL_LENGTH('Parametros', 'AvisoTiempoContenedor') IS NULL
                    ALTER TABLE Parametros ADD AvisoTiempoContenedor INT NOT NULL
                        CONSTRAINT DF_Parametros_AvisoTiempoContenedor DEFAULT (0);

                IF COL_LENGTH('Parametros', 'PathImagenes') IS NULL
                    ALTER TABLE Parametros ADD PathImagenes VARCHAR(255) NULL;

                IF COL_LENGTH('Parametros', 'PathFirmas') IS NULL
                    ALTER TABLE Parametros ADD PathFirmas VARCHAR(255) NULL;

                IF COL_LENGTH('Parametros', 'EstadoReprogramacion') IS NULL
                    ALTER TABLE Parametros ADD EstadoReprogramacion INT NULL;

                IF COL_LENGTH('Parametros', 'EstadoIniciado') IS NULL
                    ALTER TABLE Parametros ADD EstadoIniciado INT NULL;

                IF COL_LENGTH('Parametros', 'EstadoFinalizado') IS NULL
                    ALTER TABLE Parametros ADD EstadoFinalizado INT NULL;

                IF COL_LENGTH('Parametros', 'EstadoAdjudicado') IS NULL
                    ALTER TABLE Parametros ADD EstadoAdjudicado INT NULL;

                IF COL_LENGTH('Parametros', 'EstadoPendiente') IS NULL
                    ALTER TABLE Parametros ADD EstadoPendiente INT NULL;

                IF COL_LENGTH('Parametros', 'AdminPassword') IS NULL
                    ALTER TABLE Parametros ADD AdminPassword NVARCHAR(100) NULL;
            END

            IF OBJECT_ID(N'Solicitudes', N'U') IS NOT NULL
            BEGIN
                IF EXISTS (
                    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
                    WHERE TABLE_NAME = 'Solicitudes' AND COLUMN_NAME = 'IdOperario' AND (CHARACTER_MAXIMUM_LENGTH < 100 OR CHARACTER_MAXIMUM_LENGTH IS NULL)
                )
                BEGIN
                    ALTER TABLE Solicitudes ALTER COLUMN IdOperario NVARCHAR(100) NULL;
                END

                IF COL_LENGTH('Solicitudes', 'ComentariosOficina') IS NULL
                    ALTER TABLE Solicitudes ADD ComentariosOficina NVARCHAR(MAX) NULL;

                IF COL_LENGTH('Solicitudes', 'ObservacionesConductor') IS NULL
                    ALTER TABLE Solicitudes ADD ObservacionesConductor VARCHAR(MAX) NULL;

                IF COL_LENGTH('Solicitudes', 'FirmaNombre') IS NULL
                    ALTER TABLE Solicitudes ADD FirmaNombre VARCHAR(50) NULL;

                IF COL_LENGTH('Solicitudes', 'FirmaDni') IS NULL
                    ALTER TABLE Solicitudes ADD FirmaDni VARCHAR(25) NULL;

                IF COL_LENGTH('Solicitudes', 'FirmaPath') IS NULL
                    ALTER TABLE Solicitudes ADD FirmaPath VARCHAR(255) NULL;
            END

            IF OBJECT_ID(N'Motivos', N'U') IS NULL
            BEGIN
                CREATE TABLE Motivos (
                    IdMotivo INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Motivos PRIMARY KEY,
                    Motivo VARCHAR(255) NOT NULL
                );
            END

            SET IDENTITY_INSERT Motivos ON;
            IF NOT EXISTS (SELECT 1 FROM Motivos WHERE IdMotivo = 23) INSERT INTO Motivos (IdMotivo, Motivo) VALUES (23, N'Agenda llena');
            IF NOT EXISTS (SELECT 1 FROM Motivos WHERE IdMotivo = 25) INSERT INTO Motivos (IdMotivo, Motivo) VALUES (25, N'Camión roto');
            IF NOT EXISTS (SELECT 1 FROM Motivos WHERE IdMotivo = 16) INSERT INTO Motivos (IdMotivo, Motivo) VALUES (16, N'Cliente anula servicio');
            IF NOT EXISTS (SELECT 1 FROM Motivos WHERE IdMotivo = 13) INSERT INTO Motivos (IdMotivo, Motivo) VALUES (13, N'Cliente no contesta al teléfono');
            IF NOT EXISTS (SELECT 1 FROM Motivos WHERE IdMotivo = 11) INSERT INTO Motivos (IdMotivo, Motivo) VALUES (11, N'Cliente nos pide otro día');
            IF NOT EXISTS (SELECT 1 FROM Motivos WHERE IdMotivo = 24) INSERT INTO Motivos (IdMotivo, Motivo) VALUES (24, N'Conductor indica para otro día');
            IF NOT EXISTS (SELECT 1 FROM Motivos WHERE IdMotivo = 19) INSERT INTO Motivos (IdMotivo, Motivo) VALUES (19, N'Conductor indica que ya se hizo');
            IF NOT EXISTS (SELECT 1 FROM Motivos WHERE IdMotivo = 22) INSERT INTO Motivos (IdMotivo, Motivo) VALUES (22, N'Conductor no disponible');
            IF NOT EXISTS (SELECT 1 FROM Motivos WHERE IdMotivo = 4)  INSERT INTO Motivos (IdMotivo, Motivo) VALUES (4, N'Conductor no le da tiempo');
            IF NOT EXISTS (SELECT 1 FROM Motivos WHERE IdMotivo = 14) INSERT INTO Motivos (IdMotivo, Motivo) VALUES (14, N'Conductor se le pasa');
            IF NOT EXISTS (SELECT 1 FROM Motivos WHERE IdMotivo = 18) INSERT INTO Motivos (IdMotivo, Motivo) VALUES (18, N'Exceso de vertido');
            IF NOT EXISTS (SELECT 1 FROM Motivos WHERE IdMotivo = 26) INSERT INTO Motivos (IdMotivo, Motivo) VALUES (26, N'Nadie en obra');
            IF NOT EXISTS (SELECT 1 FROM Motivos WHERE IdMotivo = 20) INSERT INTO Motivos (IdMotivo, Motivo) VALUES (20, N'No estan llenos');
            IF NOT EXISTS (SELECT 1 FROM Motivos WHERE IdMotivo = 17) INSERT INTO Motivos (IdMotivo, Motivo) VALUES (17, N'No hay contendor disponible');
            IF NOT EXISTS (SELECT 1 FROM Motivos WHERE IdMotivo = 21) INSERT INTO Motivos (IdMotivo, Motivo) VALUES (21, N'No se puede acceder a él');
            IF NOT EXISTS (SELECT 1 FROM Motivos WHERE IdMotivo = 15) INSERT INTO Motivos (IdMotivo, Motivo) VALUES (15, N'Oficina no pasamos bien orden');
            IF NOT EXISTS (SELECT 1 FROM Motivos WHERE IdMotivo = 27) INSERT INTO Motivos (IdMotivo, Motivo) VALUES (27, N'OVP No vigente');
            SET IDENTITY_INSERT Motivos OFF;
        ");

        // Ensure service planning columns exist in databases created before planning was introduced.
        await context.Database.ExecuteSqlRawAsync(@"
            IF OBJECT_ID(N'Operarios', N'U') IS NOT NULL
            BEGIN
                IF COL_LENGTH('Operarios', 'EstadoLaboral') IS NULL ALTER TABLE Operarios ADD EstadoLaboral NVARCHAR(20) NOT NULL CONSTRAINT DF_Operarios_EstadoLaboral DEFAULT ('Activo');
                IF COL_LENGTH('Operarios', 'MotivoInactividad') IS NULL ALTER TABLE Operarios ADD MotivoInactividad NVARCHAR(30) NULL;
                IF COL_LENGTH('Operarios', 'InactivoDesde') IS NULL ALTER TABLE Operarios ADD InactivoDesde DATETIME2 NULL;
                IF COL_LENGTH('Operarios', 'InactivoHasta') IS NULL ALTER TABLE Operarios ADD InactivoHasta DATETIME2 NULL;
                IF COL_LENGTH('Operarios', 'inicio_jornada') IS NULL ALTER TABLE Operarios ADD inicio_jornada TIME NOT NULL CONSTRAINT DF_Operarios_InicioJornada DEFAULT ('08:00');
                IF COL_LENGTH('Operarios', 'fin_jornada') IS NULL ALTER TABLE Operarios ADD fin_jornada TIME NOT NULL CONSTRAINT DF_Operarios_FinJornada DEFAULT ('17:00');
                IF COL_LENGTH('Operarios', 'inicio_descanso') IS NULL ALTER TABLE Operarios ADD inicio_descanso TIME NULL;
                IF COL_LENGTH('Operarios', 'fin_descanso') IS NULL ALTER TABLE Operarios ADD fin_descanso TIME NULL;
            END

            IF OBJECT_ID(N'Ausencias', N'U') IS NULL
            BEGIN
                CREATE TABLE Ausencias (
                    IdAusencia INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Ausencias PRIMARY KEY,
                    IdConductor INT NOT NULL,
                    FechaInicio DATE NOT NULL,
                    FechaFin DATE NOT NULL,
                    Tipo VARCHAR(150) NOT NULL,
                    CONSTRAINT FK_Ausencias_Operarios_IdConductor FOREIGN KEY (IdConductor) REFERENCES Operarios(IdOperario) ON DELETE CASCADE
                );
                CREATE INDEX IX_Ausencias_IdConductor_FechaInicio ON Ausencias(IdConductor, FechaInicio);
            END

            IF OBJECT_ID(N'Solicitudes', N'U') IS NOT NULL
            BEGIN
                IF COL_LENGTH('Solicitudes', 'FechaHoraInicioPlanificada') IS NULL ALTER TABLE Solicitudes ADD FechaHoraInicioPlanificada DATETIME2 NULL;
                IF COL_LENGTH('Solicitudes', 'FechaHoraFinPlanificada') IS NULL ALTER TABLE Solicitudes ADD FechaHoraFinPlanificada DATETIME2 NULL;
                IF COL_LENGTH('Solicitudes', 'NotificacionInicioVisualizada') IS NULL ALTER TABLE Solicitudes ADD NotificacionInicioVisualizada BIT NOT NULL CONSTRAINT DF_Solicitudes_NotificacionInicioVisualizada DEFAULT (0);
                IF COL_LENGTH('Solicitudes', 'Bloqueado') IS NULL ALTER TABLE Solicitudes ADD Bloqueado BIT NOT NULL CONSTRAINT DF_Solicitudes_Bloqueado DEFAULT (0);
                IF COL_LENGTH('Solicitudes', 'DuracionPlanificadaMinutos') IS NULL ALTER TABLE Solicitudes ADD DuracionPlanificadaMinutos INT NULL;
                IF COL_LENGTH('Solicitudes', 'DuracionViajeMinutos') IS NULL ALTER TABLE Solicitudes ADD DuracionViajeMinutos INT NULL;
                IF COL_LENGTH('Solicitudes', 'DuracionOperacionMinutos') IS NULL ALTER TABLE Solicitudes ADD DuracionOperacionMinutos INT NULL;
                IF COL_LENGTH('Solicitudes', 'IdPlantaOrigen') IS NULL ALTER TABLE Solicitudes ADD IdPlantaOrigen INT NULL;
                IF COL_LENGTH('Solicitudes', 'IdPlantaDescarga') IS NULL ALTER TABLE Solicitudes ADD IdPlantaDescarga INT NULL;
                IF COL_LENGTH('Solicitudes', 'IdPlantaRegreso') IS NULL ALTER TABLE Solicitudes ADD IdPlantaRegreso INT NULL;
                IF COL_LENGTH('Solicitudes', 'DistanciaOrigenObraMetros') IS NULL ALTER TABLE Solicitudes ADD DistanciaOrigenObraMetros INT NULL;
                IF COL_LENGTH('Solicitudes', 'DistanciaObraDescargaMetros') IS NULL ALTER TABLE Solicitudes ADD DistanciaObraDescargaMetros INT NULL;
                IF COL_LENGTH('Solicitudes', 'DistanciaDescargaRegresoMetros') IS NULL ALTER TABLE Solicitudes ADD DistanciaDescargaRegresoMetros INT NULL;
                IF COL_LENGTH('Solicitudes', 'MinutosOrigenObra') IS NULL ALTER TABLE Solicitudes ADD MinutosOrigenObra INT NULL;
                IF COL_LENGTH('Solicitudes', 'MinutosObraDescarga') IS NULL ALTER TABLE Solicitudes ADD MinutosObraDescarga INT NULL;
                IF COL_LENGTH('Solicitudes', 'MinutosDescargaRegreso') IS NULL ALTER TABLE Solicitudes ADD MinutosDescargaRegreso INT NULL;
                IF COL_LENGTH('Solicitudes', 'DistanciaTotalMetros') IS NULL ALTER TABLE Solicitudes ADD DistanciaTotalMetros INT NULL;
                IF COL_LENGTH('Solicitudes', 'LatitudOrigen') IS NULL ALTER TABLE Solicitudes ADD LatitudOrigen DECIMAL(9,6) NULL;
                IF COL_LENGTH('Solicitudes', 'LongitudOrigen') IS NULL ALTER TABLE Solicitudes ADD LongitudOrigen DECIMAL(9,6) NULL;
                IF COL_LENGTH('Solicitudes', 'LatitudObra') IS NULL ALTER TABLE Solicitudes ADD LatitudObra DECIMAL(9,6) NULL;
                IF COL_LENGTH('Solicitudes', 'LongitudObra') IS NULL ALTER TABLE Solicitudes ADD LongitudObra DECIMAL(9,6) NULL;
                IF COL_LENGTH('Solicitudes', 'LatitudDescarga') IS NULL ALTER TABLE Solicitudes ADD LatitudDescarga DECIMAL(9,6) NULL;
                IF COL_LENGTH('Solicitudes', 'LongitudDescarga') IS NULL ALTER TABLE Solicitudes ADD LongitudDescarga DECIMAL(9,6) NULL;
                IF COL_LENGTH('Solicitudes', 'LatitudRegreso') IS NULL ALTER TABLE Solicitudes ADD LatitudRegreso DECIMAL(9,6) NULL;
                IF COL_LENGTH('Solicitudes', 'LongitudRegreso') IS NULL ALTER TABLE Solicitudes ADD LongitudRegreso DECIMAL(9,6) NULL;
                IF COL_LENGTH('Solicitudes', 'DuracionModificadaManualmente') IS NULL ALTER TABLE Solicitudes ADD DuracionModificadaManualmente BIT NOT NULL CONSTRAINT DF_Solicitudes_DuracionManual DEFAULT (0);
                IF COL_LENGTH('Solicitudes', 'FechaCalculoRuta') IS NULL ALTER TABLE Solicitudes ADD FechaCalculoRuta DATETIME2 NULL;
                IF COL_LENGTH('Solicitudes', 'ProveedorCalculoRuta') IS NULL ALTER TABLE Solicitudes ADD ProveedorCalculoRuta NVARCHAR(30) NULL;
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('Solicitudes') AND name = 'IX_Solicitudes_Conductor_Inicio_Fin')
                    CREATE INDEX IX_Solicitudes_Conductor_Inicio_Fin ON Solicitudes(IdConductor, FechaHoraInicioPlanificada, FechaHoraFinPlanificada);
            END
        ");

        // General, temporary cache for route-provider segments. Expired results are removed at startup.
        await context.Database.ExecuteSqlRawAsync(@"
            IF OBJECT_ID(N'RutasCache', N'U') IS NULL
            BEGIN
                CREATE TABLE RutasCache (
                    IdRutaCache BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_RutasCache PRIMARY KEY,
                    ClaveRuta NVARCHAR(64) NOT NULL,
                    LatitudOrigen DECIMAL(9,6) NOT NULL,
                    LongitudOrigen DECIMAL(9,6) NOT NULL,
                    LatitudDestino DECIMAL(9,6) NOT NULL,
                    LongitudDestino DECIMAL(9,6) NOT NULL,
                    ModoViaje NVARCHAR(20) NOT NULL,
                    PreferenciaRuta NVARCHAR(30) NOT NULL,
                    DistanciaMetros INT NOT NULL,
                    DuracionSegundos INT NOT NULL,
                    FechaCalculoUtc DATETIME2 NOT NULL,
                    FechaExpiracionUtc DATETIME2 NOT NULL,
                    UltimoUsoUtc DATETIME2 NOT NULL,
                    NumeroUsos INT NOT NULL
                );
                CREATE UNIQUE INDEX IX_RutasCache_ClaveRuta ON RutasCache(ClaveRuta);
                CREATE INDEX IX_RutasCache_FechaExpiracionUtc ON RutasCache(FechaExpiracionUtc);
            END
            ELSE
            BEGIN
                IF EXISTS (SELECT 1 FROM sys.key_constraints WHERE parent_object_id = OBJECT_ID('RutasCache') AND name = 'UQ_RutasCache_ClaveRuta')
                    ALTER TABLE RutasCache DROP CONSTRAINT UQ_RutasCache_ClaveRuta;
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('RutasCache') AND name = 'IX_RutasCache_ClaveRuta')
                    CREATE UNIQUE INDEX IX_RutasCache_ClaveRuta ON RutasCache(ClaveRuta);
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('RutasCache') AND name = 'IX_RutasCache_FechaExpiracionUtc')
                    CREATE INDEX IX_RutasCache_FechaExpiracionUtc ON RutasCache(FechaExpiracionUtc);
                DELETE FROM RutasCache WHERE FechaExpiracionUtc <= SYSUTCDATETIME();
            END
        ");

        // Ensure additional container fields exist in databases created before they were introduced.
        await context.Database.ExecuteSqlRawAsync(@"
            IF OBJECT_ID(N'Solicitudes', N'U') IS NOT NULL
            BEGIN
                IF COL_LENGTH('Solicitudes', 'AlbaranPlanta') IS NULL
                    ALTER TABLE Solicitudes ADD AlbaranPlanta VARCHAR(20) NULL;
                IF COL_LENGTH('Solicitudes', 'AlbaranSerieSage') IS NULL
                    ALTER TABLE Solicitudes ADD AlbaranSerieSage VARCHAR(2) NULL;
                IF COL_LENGTH('Solicitudes', 'AlbaranNumeroSage') IS NULL
                    ALTER TABLE Solicitudes ADD AlbaranNumeroSage VARCHAR(10) NULL;
                IF COL_LENGTH('Solicitudes', 'TipoResiduo') IS NULL
                    ALTER TABLE Solicitudes ADD TipoResiduo VARCHAR(150) NULL;
                IF OBJECT_ID('SolicitudFotos', 'U') IS NULL
                BEGIN
                    CREATE TABLE SolicitudFotos (
                        Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                        IdSolicitud INT NOT NULL,
                        RutaArchivo VARCHAR(255) NOT NULL,
                        NombreArchivo VARCHAR(150) NULL,
                        FechaCreacion DATETIME2 NOT NULL
                    );
                    CREATE INDEX IX_SolicitudFotos_IdSolicitud_FechaCreacion ON SolicitudFotos (IdSolicitud, FechaCreacion);
                END;
                IF COL_LENGTH('Solicitudes', 'CodigoAmbosEntrega') IS NULL
                    ALTER TABLE Solicitudes ADD CodigoAmbosEntrega NVARCHAR(20) NULL;
                IF COL_LENGTH('Solicitudes', 'CodigoAmbosRecogida') IS NULL
                    ALTER TABLE Solicitudes ADD CodigoAmbosRecogida NVARCHAR(20) NULL;
            END

            IF OBJECT_ID(N'Tareas', N'U') IS NOT NULL
            BEGIN
                IF COL_LENGTH('Tareas', 'Recoger1') IS NULL ALTER TABLE Tareas ADD Recoger1 BIT NOT NULL CONSTRAINT DF_Tareas_Recoger1 DEFAULT (0);
                IF COL_LENGTH('Tareas', 'Recoger2') IS NULL ALTER TABLE Tareas ADD Recoger2 BIT NOT NULL CONSTRAINT DF_Tareas_Recoger2 DEFAULT (0);
                IF COL_LENGTH('Tareas', 'Entrega1') IS NULL ALTER TABLE Tareas ADD Entrega1 BIT NOT NULL CONSTRAINT DF_Tareas_Entrega1 DEFAULT (0);
                IF COL_LENGTH('Tareas', 'Entrega2') IS NULL ALTER TABLE Tareas ADD Entrega2 BIT NOT NULL CONSTRAINT DF_Tareas_Entrega2 DEFAULT (0);

                UPDATE Tareas SET Recoger1 = 0 WHERE Recoger1 IS NULL;
                UPDATE Tareas SET Recoger2 = 0 WHERE Recoger2 IS NULL;
                UPDATE Tareas SET Entrega1 = 0 WHERE Entrega1 IS NULL;
                UPDATE Tareas SET Entrega2 = 0 WHERE Entrega2 IS NULL;

                IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Tareas') AND name = 'Recoger1' AND is_nullable = 1) ALTER TABLE Tareas ALTER COLUMN Recoger1 BIT NOT NULL;
                IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Tareas') AND name = 'Recoger2' AND is_nullable = 1) ALTER TABLE Tareas ALTER COLUMN Recoger2 BIT NOT NULL;
                IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Tareas') AND name = 'Entrega1' AND is_nullable = 1) ALTER TABLE Tareas ALTER COLUMN Entrega1 BIT NOT NULL;
                IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Tareas') AND name = 'Entrega2' AND is_nullable = 1) ALTER TABLE Tareas ALTER COLUMN Entrega2 BIT NOT NULL;
            END
        ");

        // Create TareasRelaciones table if it does not exist (in case DB already existed)
        await context.Database.ExecuteSqlRawAsync(@"
            IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='TareasRelaciones' and xtype='U')
            BEGIN
                CREATE TABLE TareasRelaciones (
                    IdTareaOrigen INT NOT NULL,
                    IdTareaDestino INT NOT NULL,
                    CONSTRAINT PK_TareasRelaciones PRIMARY KEY (IdTareaOrigen, IdTareaDestino),
                    CONSTRAINT FK_TareasRelaciones_Tareas_Origen FOREIGN KEY (IdTareaOrigen) REFERENCES Tareas (IdTarea) ON DELETE NO ACTION,
                    CONSTRAINT FK_TareasRelaciones_Tareas_Destino FOREIGN KEY (IdTareaDestino) REFERENCES Tareas (IdTarea) ON DELETE NO ACTION
                );
            END
        ");

        try
        {
            // Create the EstadosSolicitud table if it doesn't exist
            await context.Database.ExecuteSqlRawAsync(@"
                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='EstadosSolicitud' and xtype='U')
                BEGIN
                    CREATE TABLE EstadosSolicitud (
                        IdEstado INT PRIMARY KEY,
                        Descripcion NVARCHAR(100) NULL
                    );
                END
            ");

            // Ensure Plantas has at least one record to act as default
            await context.Database.ExecuteSqlRawAsync(@"
                IF NOT EXISTS (SELECT 1 FROM Plantas)
                BEGIN
                    INSERT INTO Plantas (Nombre, Direccion, Poblacion, CodigoPostal) 
                    VALUES ('Planta Principal', 'Dirección Principal', 'Población Principal', '00000');
                END
            ");

            // Ensure Contenedores has IdPlanta column (step 1: Add column as NULL first to avoid batch compilation error)
            await context.Database.ExecuteSqlRawAsync(@"
                IF EXISTS (SELECT * FROM sysobjects WHERE name='Contenedores' and xtype='U')
                BEGIN
                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Contenedores') AND name = 'IdPlanta')
                    BEGIN
                        ALTER TABLE Contenedores ADD IdPlanta INT NULL;
                    END
                END
            ");

            // Ensure Contenedores has IdPlanta column values set (step 2: Set default values dynamically)
            await context.Database.ExecuteSqlRawAsync(@"
                IF EXISTS (SELECT * FROM sysobjects WHERE name='Contenedores' and xtype='U')
                BEGIN
                    IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Contenedores') AND name = 'IdPlanta')
                    BEGIN
                        DECLARE @DefaultPlantaId INT;
                        SELECT TOP 1 @DefaultPlantaId = IdPlanta FROM Plantas;
                        
                        IF @DefaultPlantaId IS NOT NULL
                        BEGIN
                            -- Run via sp_executesql to defer compilation of update statement until the column exists
                            EXEC sp_executesql N'UPDATE Contenedores SET IdPlanta = @plantaId WHERE IdPlanta IS NULL', N'@plantaId INT', @DefaultPlantaId;
                        END
                    END
                END
            ");

            // Ensure Contenedores has IdPlanta column NOT NULL and Foreign Key set (step 3)
            await context.Database.ExecuteSqlRawAsync(@"
                IF EXISTS (SELECT * FROM sysobjects WHERE name='Contenedores' and xtype='U')
                BEGIN
                    IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Contenedores') AND name = 'IdPlanta')
                    BEGIN
                        -- Set column to NOT NULL
                        ALTER TABLE Contenedores ALTER COLUMN IdPlanta INT NOT NULL;
                        
                        -- Add foreign key constraint
                        IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_Contenedores_Plantas_IdPlanta')
                        BEGIN
                            ALTER TABLE Contenedores
                            ADD CONSTRAINT FK_Contenedores_Plantas_IdPlanta
                            FOREIGN KEY (IdPlanta) REFERENCES Plantas(IdPlanta);
                        END
                    END
                END
            ");

            // Create the Prioridades table if it doesn't exist
            await context.Database.ExecuteSqlRawAsync(@"
                IF EXISTS (SELECT * FROM sysobjects WHERE name='Prioridades' and xtype='U')
                BEGIN
                    IF COLUMNPROPERTY(OBJECT_ID('Prioridades'), 'IdPrioridad', 'IsIdentity') = 0
                    BEGIN
                        DROP TABLE Prioridades;
                    END
                END

                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Prioridades' and xtype='U')
                BEGIN
                    CREATE TABLE Prioridades (
                        IdPrioridad INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                        Codigo INT NULL,
                        Descripcion NVARCHAR(100) COLLATE Modern_Spanish_CI_AS NULL,
                        BgColor NVARCHAR(20) COLLATE Modern_Spanish_CI_AS NULL,
                        TextColor NVARCHAR(20) COLLATE Modern_Spanish_CI_AS NULL
                    );
                END
            ");

            // Create the Tareas table if it doesn't exist
            await context.Database.ExecuteSqlRawAsync(@"
                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Tareas' and xtype='U')
                BEGIN
                    CREATE TABLE Tareas (
                        IdTarea INT PRIMARY KEY,
                        Tarea NVARCHAR(150) NOT NULL
                    );
                END
            ");
            
            // Add presentation and filtering columns to EstadosSolicitud if they don't exist
            await context.Database.ExecuteSqlRawAsync(@"
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('EstadosSolicitud') AND name = 'BgColor')
                BEGIN
                    ALTER TABLE EstadosSolicitud ADD BgColor NVARCHAR(20) NULL;
                END
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('EstadosSolicitud') AND name = 'TextColor')
                BEGIN
                    ALTER TABLE EstadosSolicitud ADD TextColor NVARCHAR(20) NULL;
                END
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('EstadosSolicitud') AND name = 'Filtrar')
                BEGIN
                    ALTER TABLE EstadosSolicitud ADD Filtrar BIT NOT NULL CONSTRAINT DF_EstadosSolicitud_Filtrar DEFAULT 0;
                END
            ");
            
            // Add the foreign key constraint on Solicitudes if it doesn't exist
            await context.Database.ExecuteSqlRawAsync(@"
                IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_Solicitudes_EstadosSolicitud_Estado')
                BEGIN
                    ALTER TABLE Solicitudes
                    ADD CONSTRAINT FK_Solicitudes_EstadosSolicitud_Estado
                    FOREIGN KEY (Estado) REFERENCES EstadosSolicitud(IdEstado);
                END
            ");

            // Add client and FechaInicial columns if they don't exist
            await context.Database.ExecuteSqlRawAsync(@"
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Solicitudes') AND name = 'NombreCliente')
                BEGIN
                    ALTER TABLE Solicitudes ADD NombreCliente NVARCHAR(200) NULL;
                END
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Solicitudes') AND name = 'DireccionCliente')
                BEGIN
                    ALTER TABLE Solicitudes ADD DireccionCliente NVARCHAR(200) NULL;
                END
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Solicitudes') AND name = 'PoblacionCliente')
                BEGIN
                    ALTER TABLE Solicitudes ADD PoblacionCliente NVARCHAR(100) NULL;
                END
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Solicitudes') AND name = 'TelefonoCliente')
                BEGIN
                    ALTER TABLE Solicitudes ADD TelefonoCliente NVARCHAR(20) NULL;
                END
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Solicitudes') AND name = 'FechaInicial')
                BEGIN
                    ALTER TABLE Solicitudes ADD FechaInicial DATETIME2 NULL;
                END
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Solicitudes') AND name = 'Encargado')
                BEGIN
                    ALTER TABLE Solicitudes ADD Encargado NVARCHAR(100) NULL;
                END
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Solicitudes') AND name = 'Movil')
                BEGIN
                    ALTER TABLE Solicitudes ADD Movil NVARCHAR(20) NULL;
                END
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Solicitudes') AND name = 'NombreObra')
                BEGIN
                    ALTER TABLE Solicitudes ADD NombreObra NVARCHAR(200) NULL;
                END
            ");

            // Seed sample data for these columns where null
            await context.Database.ExecuteSqlRawAsync(@"
                UPDATE Solicitudes SET 
                    NombreCliente = COALESCE(NombreCliente, 'CONSTRUCCIONES SAVALL S.L.'),
                    DireccionCliente = COALESCE(DireccionCliente, 'Calle Mayor 12, Pta 4'),
                    PoblacionCliente = COALESCE(PoblacionCliente, 'Gandia'),
                    TelefonoCliente = COALESCE(TelefonoCliente, '600123456'),
                    FechaInicial = COALESCE(FechaInicial, DATEADD(day, -5, GETDATE()))
                WHERE NombreCliente IS NULL OR FechaInicial IS NULL;
            ");

            // Add decimal coordinate and update tracking columns to Solicitudes if they don't exist
            await context.Database.ExecuteSqlRawAsync(@"
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Solicitudes') AND name = 'Latitud')
                BEGIN
                    ALTER TABLE Solicitudes ADD Latitud DECIMAL(9,6) NULL;
                END
            ");

            await context.Database.ExecuteSqlRawAsync(@"
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Solicitudes') AND name = 'Longitud')
                BEGIN
                    ALTER TABLE Solicitudes ADD Longitud DECIMAL(9,6) NULL;
                END
            ");

            await context.Database.ExecuteSqlRawAsync(@"
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Solicitudes') AND name = 'FechaActualizacion')
                BEGIN
                    ALTER TABLE Solicitudes ADD FechaActualizacion DATETIME NULL;
                END
            ");

            await context.Database.ExecuteSqlRawAsync(@"
                SET QUOTED_IDENTIFIER ON;
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Solicitudes') AND name = 'Ubicacion')
                BEGIN
                    ALTER TABLE Solicitudes ADD Ubicacion AS CASE WHEN Latitud IS NOT NULL AND Longitud IS NOT NULL THEN geography::Point(CAST(Latitud AS float), CAST(Longitud AS float), 4326) ELSE NULL END PERSISTED;
                END
            ");

            await context.Database.ExecuteSqlRawAsync(@"
                IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Solicitudes') AND name = 'IdConductor' AND is_nullable = 0)
                BEGIN
                    ALTER TABLE Solicitudes ALTER COLUMN IdConductor INT NULL;
                END
            ");
        }
        catch { }

        // Seed Roles
        string[] roleNames = { "Administrador", "Conductor", "Operario" };
        foreach (var roleName in roleNames)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new IdentityRole(roleName));
            }
        }

        // Seed Admin User
        var adminEmail = "admin@savall.com";
        var adminUser = await userManager.FindByEmailAsync(adminEmail);
        if (adminUser == null)
        {
            adminUser = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true,
                Tecnico = "Admin"
            };
            var result = await userManager.CreateAsync(adminUser, "Admin123!");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, "Administrador");
            }
        }

        // Seed some sample cargos if empty
        if (!await context.Cargos.AnyAsync())
        {
            context.Cargos.AddRange(
                new Cargo { Descripcion = "Conductor" },
                new Cargo { Descripcion = "Operario de Planta" },
                new Cargo { Descripcion = "Administrativo" }
            );
            await context.SaveChangesAsync();
        }

       // Seed or update Tareas
await context.Database.ExecuteSqlRawAsync(@"SET IDENTITY_INSERT Tareas ON;");
        var tareas = new List<Tarea>
        {
            new Tarea { IdTarea = 9, NombreTarea = "1 CAMBIO 5m³" },
            new Tarea { IdTarea = 8, NombreTarea = "1 CAMBIO 7m³ BALDAS" },
            new Tarea { IdTarea = 21, NombreTarea = "1 CAMBIO ARENA" },
            new Tarea { IdTarea = 24, NombreTarea = "1 CAMBIO CAJÓN  30m³" },
            new Tarea { IdTarea = 25, NombreTarea = "1 CAMBIO CAJÓN 40m³" },
            new Tarea { IdTarea = 29, NombreTarea = "1 CAMBIO medio ZA RECICLA-ARENA" },
            new Tarea { IdTarea = 19, NombreTarea = "1 CAMBIO ZAH ALGUEÑA" },
            new Tarea { IdTarea = 18, NombreTarea = "1 CAMBIO ZAH RECICLADA" },
            new Tarea { IdTarea = 14, NombreTarea = "1 ENTREGA 5m³" },
            new Tarea { IdTarea = 1, NombreTarea = "1 ENTREGA 7m³BALDAS" },
            new Tarea { IdTarea = 20, NombreTarea = "1 ENTREGA ARENA" },
            new Tarea { IdTarea = 22, NombreTarea = "1 ENTREGA CAJÓN  30m³" },
            new Tarea { IdTarea = 23, NombreTarea = "1 ENTREGA CAJÓN  40m³" },
            new Tarea { IdTarea = 30, NombreTarea = "1 ENTREGA medio ZA RECICLA-ARENA" },
            new Tarea { IdTarea = 17, NombreTarea = "1 ENTREGA ZAH ALGUEÑA" },
            new Tarea { IdTarea = 16, NombreTarea = "1 ENTREGA ZAH RECICLADA" },
            new Tarea { IdTarea = 11, NombreTarea = "1 MOVIMIENTO" },
            new Tarea { IdTarea = 15, NombreTarea = "1 PORTE" },
            new Tarea { IdTarea = 10, NombreTarea = "1 RETIRADA 5m³" },
            new Tarea { IdTarea = 7, NombreTarea = "1 RETIRADA 7m³BALDAS" },
            new Tarea { IdTarea = 26, NombreTarea = "1 RETIRADA CAJÓN  30m³" },
            new Tarea { IdTarea = 27, NombreTarea = "1 RETIRADA CAJÓN  40m³" },
            new Tarea { IdTarea = 32, NombreTarea = "CONTENEDOR AGUA" },
            new Tarea { IdTarea = 33, NombreTarea = "RECORDATORIO" },
            new Tarea { IdTarea = 31, NombreTarea = "TASA" },
            new Tarea { IdTarea = 28, NombreTarea = "VACACIONES" }
        };

        foreach (var tarea in tareas)
        {
            var dbTarea = await context.Tareas.FindAsync(tarea.IdTarea);
            if (dbTarea == null)
            {
                context.Tareas.Add(tarea);
            }
        }
        await context.SaveChangesAsync();
        await context.Database.ExecuteSqlRawAsync(@"SET IDENTITY_INSERT Tareas OFF;");

        // Seed TareasRelaciones
        if (!context.TareasRelaciones.Any())
        {
            var dbTareas = await context.Tareas.ToListAsync();
            var relaciones = new List<TareaRelacion>();

            foreach (var origen in dbTareas)
            {
                var nombreOrigen = origen.NombreTarea.ToUpper();
                
                foreach (var destino in dbTareas)
                {
                    var nombreDestino = destino.NombreTarea.ToUpper();

                    // Regla 1: Si la última fue ENTREGA o CAMBIO, se permite RETIRADA, CAMBIO, MOVIMIENTO, PORTE
                    if (nombreOrigen.Contains("ENTREGA") || nombreOrigen.Contains("CAMBIO"))
                    {
                        if (nombreDestino.Contains("RETIRADA") || 
                            nombreDestino.Contains("CAMBIO") || 
                            nombreDestino.Contains("MOVIMIENTO") || 
                            nombreDestino.Contains("PORTE"))
                        {
                            relaciones.Add(new TareaRelacion { IdTareaOrigen = origen.IdTarea, IdTareaDestino = destino.IdTarea });
                        }
                    }
                    // Regla 2: Si la última fue RETIRADA, se permite ENTREGA, PORTE
                    else if (nombreOrigen.Contains("RETIRADA"))
                    {
                        if (nombreDestino.Contains("ENTREGA") || nombreDestino.Contains("PORTE"))
                        {
                            relaciones.Add(new TareaRelacion { IdTareaOrigen = origen.IdTarea, IdTareaDestino = destino.IdTarea });
                        }
                    }
                    // Regla 3: Si la última fue MOVIMIENTO o PORTE, se permite cualquiera de los flujos operativos estándar
                    else if (nombreOrigen.Contains("MOVIMIENTO") || nombreOrigen.Contains("PORTE"))
                    {
                        if (nombreDestino.Contains("ENTREGA") || 
                            nombreDestino.Contains("RETIRADA") || 
                            nombreDestino.Contains("CAMBIO") || 
                            nombreDestino.Contains("MOVIMIENTO") || 
                            nombreDestino.Contains("PORTE"))
                        {
                            relaciones.Add(new TareaRelacion { IdTareaOrigen = origen.IdTarea, IdTareaDestino = destino.IdTarea });
                        }
                    }
                }
            }

            if (relaciones.Any())
            {
                context.TareasRelaciones.AddRange(relaciones);
                await context.SaveChangesAsync();
            }
        }

        // Seed or update EstadosSolicitud
        var estados = new List<EstadoSolicitud>
        {
            new EstadoSolicitud { IdEstado = 1, Descripcion = "Introducido no enviado", BgColor = "#ffffff", TextColor = "#002060", Filtrar = true },
            new EstadoSolicitud { IdEstado = 2, Descripcion = "Whatsapp enviado", BgColor = "#38b449", TextColor = "#ffffff" },
            new EstadoSolicitud { IdEstado = 3, Descripcion = "Leer observaciones", BgColor = "#dbe5f1", TextColor = "#002060", Filtrar = true },
            new EstadoSolicitud { IdEstado = 4, Descripcion = "No seguir contenedor", BgColor = "#ffc000", TextColor = "#000000" },
            new EstadoSolicitud { IdEstado = 5, Descripcion = "Finalizado servicio", BgColor = "#8db4e2", TextColor = "#002060" },
            new EstadoSolicitud { IdEstado = 6, Descripcion = "Anulado / reprogramado", BgColor = "#ff0000", TextColor = "#ffffff", Filtrar = true },
            new EstadoSolicitud { IdEstado = 7, Descripcion = "Falta disponibilidad contenedor", BgColor = "#ffffff", TextColor = "#ff0000" },
            new EstadoSolicitud { IdEstado = 8, Descripcion = "Servicio iniciado", BgColor = "#198754", TextColor = "#ffffff", Filtrar = true },
            new EstadoSolicitud { IdEstado = 9, Descripcion = "Adjudicado", BgColor = "#0d6efd", TextColor = "#ffffff", Filtrar = true }
        };

        foreach (var estado in estados)
        {
            var dbEstado = await context.EstadosSolicitud.FindAsync(estado.IdEstado);
            if (dbEstado == null)
            {
                context.EstadosSolicitud.Add(estado);
            }
            else
            {
                if (dbEstado.Descripcion != estado.Descripcion)
                {
                    dbEstado.Descripcion = estado.Descripcion;
                }
                if (dbEstado.BgColor != estado.BgColor)
                {
                    dbEstado.BgColor = estado.BgColor;
                }
                if (dbEstado.TextColor != estado.TextColor)
                {
                    dbEstado.TextColor = estado.TextColor;
                }
            }
        }
        await context.SaveChangesAsync();

        var parametroDb = await context.Parametros.FirstOrDefaultAsync();
        if (parametroDb != null)
        {
            var validEstados = await context.EstadosSolicitud.Select(e => e.IdEstado).ToListAsync();
            bool modified = false;

            if (validEstados.Count > 0)
            {
                int defaultId = validEstados.First();

                if (!parametroDb.EstadoPendiente.HasValue || !validEstados.Contains(parametroDb.EstadoPendiente.Value))
                {
                    parametroDb.EstadoPendiente = validEstados.Contains(1) ? 1 : defaultId;
                    modified = true;
                }
                if (!parametroDb.EstadoAdjudicado.HasValue || !validEstados.Contains(parametroDb.EstadoAdjudicado.Value))
                {
                    parametroDb.EstadoAdjudicado = validEstados.Contains(2) ? 2 : (validEstados.Contains(9) ? 9 : defaultId);
                    modified = true;
                }
                if (!parametroDb.EstadoIniciado.HasValue || !validEstados.Contains(parametroDb.EstadoIniciado.Value))
                {
                    parametroDb.EstadoIniciado = validEstados.Contains(3) ? 3 : (validEstados.Contains(8) ? 8 : defaultId);
                    modified = true;
                }
                if (!parametroDb.EstadoFinalizado.HasValue || !validEstados.Contains(parametroDb.EstadoFinalizado.Value))
                {
                    parametroDb.EstadoFinalizado = validEstados.Contains(4) ? 4 : (validEstados.Contains(5) ? 5 : defaultId);
                    modified = true;
                }
                if (!parametroDb.EstadoReprogramacion.HasValue || !validEstados.Contains(parametroDb.EstadoReprogramacion.Value))
                {
                    parametroDb.EstadoReprogramacion = validEstados.Contains(5) ? 5 : (validEstados.Contains(6) ? 6 : defaultId);
                    modified = true;
                }
            }

            if (modified) await context.SaveChangesAsync();
        }

        // Seed some sample operarios if empty
        if (!await context.Operarios.AnyAsync())
        {
            var conductorCargo = await context.Cargos.FirstOrDefaultAsync(c => c.Descripcion == "Conductor");
            int? cargoId = conductorCargo?.IdCargo;

            context.Operarios.AddRange(
                new Operario { Nombre = "ALEXIS", Telefono = "600111222", IdCargo = cargoId, Activo = true, Obras = true, Mensajes = true },
                new Operario { Nombre = "DAVID", Telefono = "600333444", IdCargo = cargoId, Activo = true, Obras = true, Mensajes = true },
                new Operario { Nombre = "VICTOR", Telefono = "600555666", IdCargo = cargoId, Activo = true, Obras = true, Mensajes = true }
            );
            await context.SaveChangesAsync();
        }

        // Ensure we have at least 100 sample solicitudes
        var existingCount = await context.Solicitudes.CountAsync();
        if (existingCount < 100)
        {
            var operariosList = await context.Operarios.ToListAsync();
            var alexisId = operariosList.FirstOrDefault(o => o.Nombre == "ALEXIS")?.IdOperario ?? 1;
            var davidId = operariosList.FirstOrDefault(o => o.Nombre == "DAVID")?.IdOperario ?? 2;
            var victorId = operariosList.FirstOrDefault(o => o.Nombre == "VICTOR")?.IdOperario ?? 3;

            var random = new Random();
            var listToAdd = new List<Solicitud>();

            if (existingCount == 0)
            {
                listToAdd.AddRange(new[]
                {
                    new Solicitud { IdConductor = alexisId, IdTipoTarea = 1, FechaSolicitud = DateTime.Today.AddDays(-1), FechaTarea = DateTime.Today, Prioridad = 1, CodigoEntrega = "ENT-101", Estado = 1, Observaciones = "Primer servicio introducido", NombreCliente = "CONSTRUCCIONES SAVALL S.L.", DireccionCliente = "Calle Mayor 12, Pta 4", PoblacionCliente = "Gandia", TelefonoCliente = "600123456", FechaInicial = DateTime.Today.AddDays(-5) },
                    new Solicitud { IdConductor = davidId, IdTipoTarea = 2, FechaSolicitud = DateTime.Today.AddDays(-1), FechaTarea = DateTime.Today, Prioridad = 2, CodigoRecogida = "REC-202", Estado = 2, Observaciones = "Enviado por WhatsApp", NombreCliente = "CONSTRUCCIONES SAVALL S.L.", DireccionCliente = "Calle Mayor 12, Pta 4", PoblacionCliente = "Gandia", TelefonoCliente = "600123456", FechaInicial = DateTime.Today.AddDays(-5) },
                    new Solicitud { IdConductor = alexisId, IdTipoTarea = 3, FechaSolicitud = DateTime.Today.AddDays(-2), FechaTarea = DateTime.Today, Prioridad = 3, CodigoEntrega = "SUS-303", Estado = 3, Observaciones = "LEER OBSERVACIONES URGENTES", NombreCliente = "CONSTRUCCIONES SAVALL S.L.", DireccionCliente = "Calle Mayor 12, Pta 4", PoblacionCliente = "Gandia", TelefonoCliente = "600123456", FechaInicial = DateTime.Today.AddDays(-5) },
                    new Solicitud { IdConductor = davidId, IdTipoTarea = 1, FechaSolicitud = DateTime.Today.AddDays(-1), FechaTarea = DateTime.Today, Prioridad = 1, CodigoEntrega = "ENT-104", Estado = 4, Observaciones = "No seguimiento contenedor", NombreCliente = "CONSTRUCCIONES SAVALL S.L.", DireccionCliente = "Calle Mayor 12, Pta 4", PoblacionCliente = "Gandia", TelefonoCliente = "600123456", FechaInicial = DateTime.Today.AddDays(-5) },
                    new Solicitud { IdConductor = victorId, IdTipoTarea = 2, FechaSolicitud = DateTime.Today.AddDays(-1), FechaTarea = DateTime.Today, Prioridad = 2, CodigoRecogida = "REC-205", Estado = 5, Observaciones = "Finalizado correctamente", NombreCliente = "CONSTRUCCIONES SAVALL S.L.", DireccionCliente = "Calle Mayor 12, Pta 4", PoblacionCliente = "Gandia", TelefonoCliente = "600123456", FechaInicial = DateTime.Today.AddDays(-5) },
                    new Solicitud { IdConductor = alexisId, IdTipoTarea = 1, FechaSolicitud = DateTime.Today.AddDays(-3), FechaTarea = DateTime.Today, Prioridad = 3, CodigoEntrega = "ENT-106", Estado = 6, Observaciones = "Anulado por el cliente", NombreCliente = "CONSTRUCCIONES SAVALL S.L.", DireccionCliente = "Calle Mayor 12, Pta 4", PoblacionCliente = "Gandia", TelefonoCliente = "600123456", FechaInicial = DateTime.Today.AddDays(-5) },
                    new Solicitud { IdConductor = victorId, IdTipoTarea = 2, FechaSolicitud = DateTime.Today.AddDays(-1), FechaTarea = DateTime.Today, Prioridad = 1, CodigoRecogida = "REC-207", Estado = 7, Observaciones = "Falta disponibilidad del contenedor de 5m³", NombreCliente = "CONSTRUCCIONES SAVALL S.L.", DireccionCliente = "Calle Mayor 12, Pta 4", PoblacionCliente = "Gandia", TelefonoCliente = "600123456", FechaInicial = DateTime.Today.AddDays(-5) }
                });
            }

            int needed = 100 - (existingCount + listToAdd.Count);
            string[] clientes = { "PRODUCCIONES HNOS SAVALL S.A.", "EDIFICACIONES GANDIA SL", "REFORMAS LEVANTE", "OBRAS Y VÍAS S.L.", "CONSTRUCTORA PLAYA DE GANDIA" };
            string[] poblaciones = { "Gandia", "Oliva", "Daimuz", "Xeraco", "Bellreguard" };
            string[] direcciones = { "Av. de la Mar 45", "Calle San Vicente 12", "C/ Pintor Sorolla 8", "Plaza España 3", "Carrer Major 99" };
            string[] observaciones = { "Llamar antes de entregar", "Cuidado con cables eléctricos", "Dejar en la acera", "Requiere camión pequeño", "Urgente por la mañana", "Ninguna", "Confirmar con encargado" };

            for (int i = 0; i < needed; i++)
            {
                int conductorId = random.Next(1, 4) switch { 1 => alexisId, 2 => davidId, _ => victorId };
                int tipoTarea = random.Next(1, 4); // 1 = Entrega, 2 = Recogida, 3 = Sustitucion
                int estado = random.Next(1, 8); // Estado 1 al 7
                int clienteIdx = random.Next(clientes.Length);

                listToAdd.Add(new Solicitud
                {
                    IdConductor = conductorId,
                    IdTipoTarea = tipoTarea,
                    FechaSolicitud = DateTime.Today.AddDays(-random.Next(1, 5)),
                    FechaTarea = DateTime.Today,
                    FechaPrevista = DateTime.Today,
                    Prioridad = random.Next(1, 4),
                    CodigoEntrega = tipoTarea != 2 ? $"ENT-{200 + i}" : null,
                    CodigoRecogida = tipoTarea != 1 ? $"REC-{200 + i}" : null,
                    Estado = estado,
                    Observaciones = observaciones[random.Next(observaciones.Length)],
                    NombreCliente = clientes[clienteIdx],
                    DireccionCliente = direcciones[clienteIdx],
                    PoblacionCliente = poblaciones[clienteIdx],
                    TelefonoCliente = $"600{random.Next(100000, 999999)}",
                    FechaInicial = DateTime.Today.AddDays(-random.Next(5, 10))
                });
            }

            context.Solicitudes.AddRange(listToAdd);
        }

        // Shift dates dynamically so the dashboard always has today's data in development/demo
        var todayCount = await context.Solicitudes.CountAsync(s => s.FechaTarea == DateTime.Today);
        if (todayCount == 0 && await context.Solicitudes.AnyAsync())
        {
            var allSols = await context.Solicitudes.ToListAsync();
            foreach (var sol in allSols)
            {
                sol.FechaTarea = DateTime.Today;
                sol.FechaPrevista = DateTime.Today;
                sol.FechaSolicitud = DateTime.Today.AddDays(-1);
            }
            await context.SaveChangesAsync();
        }

        // Initialize SageGestion DB with tipo_iva table if it does not exist
        try
        {
            await sageGestionContext.Database.ExecuteSqlRawAsync(@"
                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='tipo_iva' and xtype='U')
                BEGIN
                    CREATE TABLE tipo_iva (
                        CODIGO char(2) DEFAULT '' NOT NULL CONSTRAINT PK_tipo_iva PRIMARY KEY,
                        NOMBRE char(50) DEFAULT '' NOT NULL,
                        IVA numeric(20, 2) NOT NULL,
                        RECARG numeric(20, 2) DEFAULT 0.00 NOT NULL,
                        CTA_IV_SOP char(8) DEFAULT '' NOT NULL,
                        CTA_IV_REP char(8) DEFAULT '' NOT NULL,
                        CTA_RE_SOP char(8) DEFAULT '' NOT NULL,
                        CTA_RE_REP char(8) DEFAULT '' NOT NULL,
                        VISTA bit DEFAULT 1 NULL,
                        COMUNITARI int DEFAULT 1 NOT NULL,
                        INMOVIL bit DEFAULT 0 NOT NULL,
                        IVA_CEE char(2) DEFAULT '' NOT NULL,
                        DEDUCE bit DEFAULT 1 NOT NULL,
                        EXENTO bit DEFAULT 0 NOT NULL,
                        AG_VIAJE bit DEFAULT 0 NOT NULL,
                        PENDEVREP char(8) DEFAULT '' NOT NULL,
                        PENDEDSOP char(8) DEFAULT '' NOT NULL,
                        GUID char(50) DEFAULT '' NOT NULL,
                        IMPORTAR datetime DEFAULT NULL NULL,
                        RECSOPCDEV char(8) DEFAULT '' NOT NULL,
                        RECREPCDEV char(8) DEFAULT '' NOT NULL,
                        GRUPOIVA int DEFAULT 0 NOT NULL,
                        IVAEQUIERP char(2) DEFAULT '' NOT NULL,
                        TERRITERP int DEFAULT 0 NOT NULL,
                        GUID_ID char(50) DEFAULT newid() NOT NULL,
                        CREATED datetime DEFAULT getdate() NOT NULL,
                        MODIFIED datetime DEFAULT getdate() NOT NULL,
                        TIPO int DEFAULT 0 NOT NULL,
                        IGIC_IMPLI bit DEFAULT 0 NOT NULL,
                        PRTIVSOPND char(8) DEFAULT '' NOT NULL,
                        PRTIVSNDPD char(8) DEFAULT '' NOT NULL,
                        TIPO_IMP int DEFAULT 0 NOT NULL,
                        CERO bit DEFAULT 0 NOT NULL,
                        B_INV bit DEFAULT 0 NOT NULL
                    );
                END
            ");

            if (!await sageGestionContext.TipoIva.AnyAsync())
            {
                sageGestionContext.TipoIva.AddRange(
                    new TipoIvaSage50 { Codigo = "01", Nombre = "IVA GENERAL", Iva = 21.00m, Recarg = 5.20m, Vista = true },
                    new TipoIvaSage50 { Codigo = "02", Nombre = "IVA REDUCIDO", Iva = 10.00m, Recarg = 1.40m, Vista = true },
                    new TipoIvaSage50 { Codigo = "03", Nombre = "IVA SUPERREDUCIDO", Iva = 4.00m, Recarg = 0.50m, Vista = true },
                    new TipoIvaSage50 { Codigo = "04", Nombre = "EXENTO DE IVA", Iva = 0.00m, Recarg = 0.00m, Vista = true }
                );
                await sageGestionContext.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            // Suppress error or log in dev
            Console.WriteLine($"Error initializing SageGestion DB (tipo_iva): {ex.Message}");
        }
    }
}
