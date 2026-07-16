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
            END
        ");

        // Ensure service planning columns exist in databases created before planning was introduced.
        await context.Database.ExecuteSqlRawAsync(@"
            IF OBJECT_ID(N'Operarios', N'U') IS NOT NULL
            BEGIN
                IF COL_LENGTH('Operarios', 'EstadoLaboral') IS NULL ALTER TABLE Operarios ADD EstadoLaboral NVARCHAR(20) NOT NULL CONSTRAINT DF_Operarios_EstadoLaboral DEFAULT ('Activo');
                IF COL_LENGTH('Operarios', 'MotivoInactividad') IS NULL ALTER TABLE Operarios ADD MotivoInactividad NVARCHAR(30) NULL;
                IF COL_LENGTH('Operarios', 'InactivoDesde') IS NULL ALTER TABLE Operarios ADD InactivoDesde DATETIME2 NULL;
                IF COL_LENGTH('Operarios', 'InactivoHasta') IS NULL ALTER TABLE Operarios ADD InactivoHasta DATETIME2 NULL;
                IF COL_LENGTH('Operarios', 'HoraInicioJornada') IS NULL ALTER TABLE Operarios ADD HoraInicioJornada TIME NULL CONSTRAINT DF_Operarios_HoraInicioJornada DEFAULT ('08:00');
                IF COL_LENGTH('Operarios', 'HoraFinJornada') IS NULL ALTER TABLE Operarios ADD HoraFinJornada TIME NULL CONSTRAINT DF_Operarios_HoraFinJornada DEFAULT ('17:00');
                IF COL_LENGTH('Operarios', 'MinutosMaximosDiarios') IS NULL ALTER TABLE Operarios ADD MinutosMaximosDiarios INT NOT NULL CONSTRAINT DF_Operarios_MinutosMaximosDiarios DEFAULT (480);
                IF COL_LENGTH('Operarios', 'MinutosMaximosSemanales') IS NULL ALTER TABLE Operarios ADD MinutosMaximosSemanales INT NOT NULL CONSTRAINT DF_Operarios_MinutosMaximosSemanales DEFAULT (2400);
                IF COL_LENGTH('Operarios', 'TrabajaSabados') IS NULL ALTER TABLE Operarios ADD TrabajaSabados BIT NOT NULL CONSTRAINT DF_Operarios_TrabajaSabados DEFAULT (0);
                IF COL_LENGTH('Operarios', 'TrabajaDomingos') IS NULL ALTER TABLE Operarios ADD TrabajaDomingos BIT NOT NULL CONSTRAINT DF_Operarios_TrabajaDomingos DEFAULT (0);
            END

            IF OBJECT_ID(N'Solicitudes', N'U') IS NOT NULL
            BEGIN
                IF COL_LENGTH('Solicitudes', 'FechaHoraInicioPlanificada') IS NULL ALTER TABLE Solicitudes ADD FechaHoraInicioPlanificada DATETIME2 NULL;
                IF COL_LENGTH('Solicitudes', 'FechaHoraFinPlanificada') IS NULL ALTER TABLE Solicitudes ADD FechaHoraFinPlanificada DATETIME2 NULL;
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

        // Renumber ContenedoresTipos starting from 1
        await context.Database.ExecuteSqlRawAsync(@"
            IF EXISTS (SELECT * FROM sysobjects WHERE name='ContenedoresTipos' and xtype='U')
            BEGIN
                IF OBJECT_ID('tempdb..#IdMap') IS NOT NULL DROP TABLE #IdMap;
                
                SELECT 
                    IdTipo AS OldId, 
                    ROW_NUMBER() OVER (ORDER BY IdTipo) AS NewId
                INTO #IdMap
                FROM ContenedoresTipos;

                IF EXISTS (SELECT 1 FROM #IdMap WHERE OldId <> NewId)
                BEGIN
                    -- Temporarily disable foreign keys referencing ContenedoresTipos
                    ALTER TABLE Contenedores NOCHECK CONSTRAINT ALL;

                    -- Update Contenedores references
                    UPDATE c
                    SET c.IdTipo = m.NewId
                    FROM Contenedores c
                    INNER JOIN #IdMap m ON c.IdTipo = m.OldId;

                    -- Save data to temporary table
                    IF OBJECT_ID('tempdb..#TempTipos') IS NOT NULL DROP TABLE #TempTipos;
                    
                    SELECT Descripcion, CapacidadMetrosCubicos, LargoCm, AnchoCm, AltoCm, NewId
                    INTO #TempTipos
                    FROM ContenedoresTipos t
                    INNER JOIN #IdMap m ON t.IdTipo = m.OldId;

                    DELETE FROM ContenedoresTipos;

                    SET IDENTITY_INSERT ContenedoresTipos ON;

                    INSERT INTO ContenedoresTipos (IdTipo, Descripcion, CapacidadMetrosCubicos, LargoCm, AnchoCm, AltoCm)
                    SELECT NewId, Descripcion, CapacidadMetrosCubicos, LargoCm, AnchoCm, AltoCm
                    FROM #TempTipos;

                    SET IDENTITY_INSERT ContenedoresTipos OFF;

                    -- Drop temporary tables
                    DROP TABLE #TempTipos;
                    
                    -- Enable foreign keys
                    ALTER TABLE Contenedores CHECK CONSTRAINT ALL;
                END

                -- Reseed identity
                DECLARE @MaxId INT = ISNULL((SELECT MAX(IdTipo) FROM ContenedoresTipos), 0);
                DBCC CHECKIDENT ('ContenedoresTipos', RESEED, @MaxId);
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

        // Seed some sample container types if empty
        if (!await context.ContenedoresTipos.AnyAsync())
        {
            context.ContenedoresTipos.AddRange(
                new ContenedorTipo { Descripcion = "Contenedor 5m³ con puerta", CapacidadMetrosCubicos = 5.00m, LargoCm = 300, AnchoCm = 180, AltoCm = 100 },
                new ContenedorTipo { Descripcion = "Patera 3m³", CapacidadMetrosCubicos = 3.00m, LargoCm = 250, AnchoCm = 150, AltoCm = 80 }
            );
            await context.SaveChangesAsync();
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
            new EstadoSolicitud { IdEstado = 1, Descripcion = "Introducido no enviado", BgColor = "#ffffff", TextColor = "#002060" },
            new EstadoSolicitud { IdEstado = 2, Descripcion = "Whatsapp enviado", BgColor = "#38b449", TextColor = "#ffffff" },
            new EstadoSolicitud { IdEstado = 3, Descripcion = "Leer observaciones", BgColor = "#dbe5f1", TextColor = "#002060" },
            new EstadoSolicitud { IdEstado = 4, Descripcion = "No seguir contenedor", BgColor = "#ffc000", TextColor = "#000000" },
            new EstadoSolicitud { IdEstado = 5, Descripcion = "Finalizado servicio", BgColor = "#8db4e2", TextColor = "#002060" },
            new EstadoSolicitud { IdEstado = 6, Descripcion = "Anulado / reprogramado", BgColor = "#ff0000", TextColor = "#ffffff" },
            new EstadoSolicitud { IdEstado = 7, Descripcion = "Falta disponibilidad contenedor", BgColor = "#ffffff", TextColor = "#ff0000" }
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
