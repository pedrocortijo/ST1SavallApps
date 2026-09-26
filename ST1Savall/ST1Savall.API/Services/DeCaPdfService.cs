using System;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using DevExpress.Drawing;
using DevExpress.XtraPrinting;
using DevExpress.XtraPrinting.BarCode;
using DevExpress.XtraPrinting.Drawing;
using DevExpress.XtraReports.UI;
using ST1Savall.Shared.Data;

namespace ST1Savall.API.Services;

/// <summary>
/// Generador del Documento Electrónico de Control Administrativo (DeCA) conforme a la
/// Orden FOM/2861/2012 y normativa de transporte de residuos en contenedores de obra.
/// </summary>
public sealed class DeCaPdfService
{
    private static readonly CultureInfo Es = CultureInfo.GetCultureInfo("es-ES");

    public byte[] Crear(
        Solicitud solicitud,
        Parametro parametros,
        string tipoOperacion, // "ENTREGA" | "RETIRADA"
        string? clienteNombre,
        string? clienteCif,
        string? camionMatricula,
        string? conductorNombre,
        Planta? plantaDestino,
        decimal? cubicajeM3,
        string? rutaFirma,
        string urlDescargaQr)
    {
        using var report = CrearReport(
            solicitud,
            parametros,
            tipoOperacion,
            clienteNombre,
            clienteCif,
            camionMatricula,
            conductorNombre,
            plantaDestino,
            cubicajeM3,
            rutaFirma,
            urlDescargaQr);

        using var stream = new MemoryStream();
        report.ExportToPdf(stream, CrearOpcionesPdfSoloLectura());
        return stream.ToArray();
    }

    public byte[] GenerarQrPng(string texto)
    {
        var r = new XtraReport
        {
            PageWidth = 260,
            PageHeight = 260,
            Margins = new DXMargins(10, 10, 10, 10)
        };
        var b = new DetailBand { HeightF = 240 };
        r.Bands.Add(b);

        var qrCode = new XRBarCode
        {
            LocationFloat = new DevExpress.Utils.PointFloat(0, 0),
            SizeF = new SizeF(240, 240),
            AutoModule = true,
            ShowText = false,
            Symbology = new QRCodeGenerator
            {
                CompactionMode = QRCodeCompactionMode.Byte,
                ErrorCorrectionLevel = QRCodeErrorCorrectionLevel.M,
                Version = QRCodeVersion.AutoVersion
            },
            Text = texto
        };
        b.Controls.Add(qrCode);

        using var ms = new MemoryStream();
        r.ExportToImage(ms, new ImageExportOptions { Format = DevExpress.Drawing.DXImageFormat.Png });
        return ms.ToArray();
    }

    private static PdfExportOptions CrearOpcionesPdfSoloLectura()
    {
        var opciones = new PdfExportOptions();
        opciones.PasswordSecurityOptions.EncryptionLevel = PdfEncryptionLevel.AES256;
        opciones.PasswordSecurityOptions.PermissionsPassword = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        opciones.PasswordSecurityOptions.PermissionsOptions.ChangingPermissions = ChangingPermissions.None;
        opciones.PasswordSecurityOptions.PermissionsOptions.EnableCopying = false;
        opciones.PasswordSecurityOptions.PermissionsOptions.EnableScreenReaders = true;
        opciones.PasswordSecurityOptions.PermissionsOptions.PrintingPermissions = PrintingPermissions.HighResolution;
        return opciones;
    }

    public static XtraReport CrearReport(
        Solicitud s,
        Parametro p,
        string tipoOperacion,
        string? clienteNombre,
        string? clienteCif,
        string? camionMatricula,
        string? conductorNombre,
        Planta? plantaDestino,
        decimal? cubicajeM3,
        string? rutaFirma,
        string urlDescargaQr)
    {
        var r = new XtraReport
        {
            DisplayName = $"DeCA_{s.IdSolicitud}_{tipoOperacion}",
            PageWidth = 827,
            PageHeight = 1169,
            Margins = new DXMargins(35, 35, 30, 30)
        };

        var b = new DetailBand { HeightF = 1100 };
        r.Bands.Add(b);

        float y = 10;

        // Encabezado Principal Oficial
        Box(b, 0, y, 757, 65, Color.FromArgb(240, 244, 248), Color.FromArgb(13, 110, 253), 2);
        L(b, "DOCUMENTO ELECTRÓNICO DE CONTROL ADMINISTRATIVO (DeCA)", 10, y + 6, 737, 22, 13, true, TextAlignment.MiddleCenter, Color.FromArgb(13, 110, 253));
        L(b, "Transporte Público de Mercancías por Carretera · Residuos de Construcción (Contenedores de Obra)", 10, y + 28, 737, 16, 9.5f, false, TextAlignment.MiddleCenter);
        L(b, "Conforme a Orden FOM/2861/2012 y ROTT · Documento Válido para Inspección de Transporte", 10, y + 44, 737, 14, 8f, false, TextAlignment.MiddleCenter, Color.FromArgb(108, 117, 125));
        y += 75;

        // Bloque de Trazabilidad e Identificación
        var esEntrega = string.Equals(tipoOperacion, "ENTREGA", StringComparison.OrdinalIgnoreCase);
        var tituloTipo = esEntrega ? "TIPO DE SERVICIO: COLOCACIÓN / ENTREGA DE CONTENEDOR VACÍO" : "TIPO DE SERVICIO: RETIRADA DE RESIDUOS DE CONSTRUCCIÓN (RCDs)";
        var colorTipo = esEntrega ? Color.FromArgb(25, 135, 84) : Color.FromArgb(220, 53, 69);

        Box(b, 0, y, 757, 28, Color.FromArgb(248, 249, 250), colorTipo, 1.5f);
        L(b, tituloTipo, 10, y + 4, 450, 20, 10, true, TextAlignment.MiddleLeft, colorTipo);
        var fechaEmision = s.FechaHoraEmisionDeCa ?? DateTime.Now;
        L(b, $"EMISIÓN PREVIA: {fechaEmision:dd/MM/yyyy HH:mm:ss}", 460, y + 4, 287, 20, 9.5f, true, TextAlignment.MiddleRight, Color.FromArgb(33, 37, 41));
        y += 34;

        // Subtítulo con Identificador y Solicitud
        L(b, $"Nº Solicitud: #{s.IdSolicitud}  |  Identificador DeCA: {s.GuidDeCa ?? "N/A"}", 0, y, 757, 18, 9, true, TextAlignment.MiddleLeft);
        y += 22;

        // SECCIÓN 1: CARGADOR CONTRACTUAL & SECCIÓN 2: TRANSPORTISTA EFECTIVO
        float colW = 373;
        float hBloque1 = 120;

        // Cargador Contractual (Cliente / Obra)
        Box(b, 0, y, colW, hBloque1, Color.White, Color.FromArgb(206, 212, 218), 1);
        Box(b, 0, y, colW, 22, Color.FromArgb(233, 236, 239), Color.FromArgb(206, 212, 218), 1);
        L(b, "1. CARGADOR CONTRACTUAL (CONTRATANTE)", 8, y + 2, colW - 16, 18, 8.5f, true, TextAlignment.MiddleLeft);

        var nomCli = !string.IsNullOrWhiteSpace(clienteNombre) ? clienteNombre.Trim() : (s.NombreCliente?.Trim() ?? "Cliente");
        var cifCli = !string.IsNullOrWhiteSpace(clienteCif) ? clienteCif.Trim() : "No especificado";
        L(b, $"Nombre / Razón Social: {nomCli}", 8, y + 26, colW - 16, 18, 8.5f, true);
        L(b, $"N.I.F. / C.I.F.: {cifCli}", 8, y + 44, colW - 16, 16, 8.5f);
        L(b, $"Obra: {s.NombreObra?.Trim() ?? "N/A"}", 8, y + 62, colW - 16, 16, 8f);
        L(b, $"Dirección: {s.DireccionCliente?.Trim() ?? ""}, {s.PoblacionCliente?.Trim() ?? ""}", 8, y + 80, colW - 16, 32, 8f, false, TextAlignment.TopLeft, Color.Black, true);

        // Transportista Efectivo (Savall Contenedores)
        float xTransp = colW + 11;
        Box(b, xTransp, y, colW, hBloque1, Color.White, Color.FromArgb(206, 212, 218), 1);
        Box(b, xTransp, y, colW, 22, Color.FromArgb(233, 236, 239), Color.FromArgb(206, 212, 218), 1);
        L(b, "2. TRANSPORTISTA EFECTIVO", xTransp + 8, y + 2, colW - 16, 18, 8.5f, true, TextAlignment.MiddleLeft);

        var empNombre = !string.IsNullOrWhiteSpace(p.Empresa) ? p.Empresa.Trim() : "SAVALL CONTENEDORES S.L.";
        var autorizTransp = !string.IsNullOrWhiteSpace(p.AutorizacionTransporte) ? p.AutorizacionTransporte.Trim() : "AUTORIZACIÓN PÚBLICA DE TRANSPORTE";
        L(b, $"Empresa: {empNombre}", xTransp + 8, y + 26, colW - 16, 18, 8.5f, true);
        L(b, "C.I.F.: B54099023", xTransp + 8, y + 44, colW - 16, 16, 8.5f);
        L(b, $"Autorización Transporte: {autorizTransp}", xTransp + 8, y + 62, colW - 16, 16, 8.5f, true, TextAlignment.MiddleLeft, Color.FromArgb(13, 110, 253));
        L(b, "Domicilio: Avda. Doctor Rico, 18, Alicante", xTransp + 8, y + 80, colW - 16, 16, 8f);
        L(b, "Inscrita en el Registro Mercantil de Alicante", xTransp + 8, y + 96, colW - 16, 16, 7.5f, false, TextAlignment.MiddleLeft, Color.FromArgb(108, 117, 125));
        y += hBloque1 + 10;

        // SECCIÓN 3: VEHÍCULO Y CONDUCTOR
        Box(b, 0, y, 757, 44, Color.White, Color.FromArgb(206, 212, 218), 1);
        Box(b, 0, y, 757, 20, Color.FromArgb(233, 236, 239), Color.FromArgb(206, 212, 218), 1);
        L(b, "3. VEHÍCULO PORTA-CONTENEDORES Y CONDUCTOR", 8, y + 1, 741, 18, 8.5f, true);

        var matriculaTxt = !string.IsNullOrWhiteSpace(camionMatricula) ? camionMatricula.Trim() : "Sin asignar";
        var conductorTxt = !string.IsNullOrWhiteSpace(conductorNombre) ? conductorNombre.Trim() : (s.ConductorNombre?.Trim() ?? "Conductor de flota");
        L(b, $"Matrícula del Camión: {matriculaTxt}", 8, y + 22, 280, 18, 9f, true);
        L(b, $"Conductor: {conductorTxt}", 300, y + 22, 440, 18, 9f);
        y += 54;

        // SECCIÓN 4: ORIGEN Y DESTINO DEL TRANSPORTE
        float hBloqueTrayecto = 75;
        Box(b, 0, y, colW, hBloqueTrayecto, Color.White, Color.FromArgb(206, 212, 218), 1);
        Box(b, 0, y, colW, 20, Color.FromArgb(233, 236, 239), Color.FromArgb(206, 212, 218), 1);
        L(b, "4. ORIGEN DEL TRAYECTO (LUGAR DE SALIDA)", 8, y + 1, colW - 16, 18, 8.5f, true);

        Box(b, xTransp, y, colW, hBloqueTrayecto, Color.White, Color.FromArgb(206, 212, 218), 1);
        Box(b, xTransp, y, colW, 20, Color.FromArgb(233, 236, 239), Color.FromArgb(206, 212, 218), 1);
        L(b, "5. DESTINO DEL TRAYECTO (LUGAR DE LLEGADA)", xTransp + 8, y + 1, colW - 16, 18, 8.5f, true);

        string origenTexto, destinoTexto;
        if (esEntrega)
        {
            origenTexto = "Instalaciones / Parque de Contenedores Savall\nAlicante / Base Logística de Transporte";
            destinoTexto = $"Obra: {s.NombreObra?.Trim() ?? ""}\n{s.DireccionCliente?.Trim() ?? ""}, {s.PoblacionCliente?.Trim() ?? ""}";
        }
        else
        {
            origenTexto = $"Obra de Salida: {s.NombreObra?.Trim() ?? ""}\n{s.DireccionCliente?.Trim() ?? ""}, {s.PoblacionCliente?.Trim() ?? ""}";
            var nomPlanta = plantaDestino?.Nombre?.Trim() ?? "Planta de Valorización / Reciclaje";
            var dirPlanta = $"{plantaDestino?.Direccion?.Trim() ?? ""}, {plantaDestino?.Poblacion?.Trim() ?? ""}".Trim(',', ' ');
            destinoTexto = $"Planta Autorizada: {nomPlanta}\n{dirPlanta}";
        }

        L(b, origenTexto, 8, y + 23, colW - 16, 48, 8.5f, false, TextAlignment.TopLeft, Color.Black, true);
        L(b, destinoTexto, xTransp + 8, y + 23, colW - 16, 48, 8.5f, false, TextAlignment.TopLeft, Color.Black, true);
        y += hBloqueTrayecto + 10;

        // SECCIÓN 6: NATURALEZA Y VOLUMEN DE LA CARGA
        float hBloqueCarga = 110;
        Box(b, 0, y, 757, hBloqueCarga, Color.White, Color.FromArgb(206, 212, 218), 1);
        Box(b, 0, y, 757, 20, Color.FromArgb(233, 236, 239), Color.FromArgb(206, 212, 218), 1);
        L(b, "6. NATURALEZA, VOLUMEN Y DESCRIPCIÓN DE LA CARGA TRANSPORTADA", 8, y + 1, 741, 18, 8.5f, true);

        if (esEntrega)
        {
            var entregados = string.Join(", ", new[] { s.CodigoEntrega, s.CodigoAmbosEntrega }.Where(c => !string.IsNullOrWhiteSpace(c)));
            L(b, "Naturaleza de la Carga: EQUIPOS / CONTENEDORES DE OBRA VACÍOS", 8, y + 24, 450, 18, 9f, true);
            L(b, $"Nº Serie de Contenedor(es) entregado(s): {(string.IsNullOrWhiteSpace(entregados) ? "En transporte" : entregados)}", 8, y + 42, 450, 18, 8.5f);
            var m3 = cubicajeM3 ?? s.CubicajeM3DeCa ?? 6.0m;
            L(b, $"Capacidad / Volumen unitario estimado: {m3:0.##} m³", 8, y + 60, 450, 18, 8.5f);
            L(b, "Estado físico de carga: Vacío, limpio y apto para colocación en obra de construcción.", 8, y + 78, 450, 18, 8f, false, TextAlignment.MiddleLeft, Color.FromArgb(108, 117, 125));
        }
        else
        {
            var retirados = string.Join(", ", new[] { s.CodigoRecogida, s.CodigoAmbosRecogida }.Where(c => !string.IsNullOrWhiteSpace(c)));
            var residuo = !string.IsNullOrWhiteSpace(s.TipoResiduo) ? s.TipoResiduo.Trim() : "RCDs / Escombros de Construcción";
            L(b, $"Naturaleza de la Carga: RESIDUOS DE CONSTRUCCIÓN Y DEMOLICIÓN (RCDs) - {residuo}", 8, y + 24, 450, 18, 9f, true);
            L(b, $"Nº Serie de Contenedor(es) retirado(s): {(string.IsNullOrWhiteSpace(retirados) ? "Registrado en obra" : retirados)}", 8, y + 42, 450, 18, 8.5f);
            var m3 = cubicajeM3 ?? s.CubicajeM3DeCa ?? 6.0m;
            var pesoEst = s.KgAlbaran.HasValue ? $"{s.KgAlbaran.Value:N0} Kg (báscula)" : $"{Math.Round(m3 * 1200m, 0):N0} Kg (estimado según densidad de RCDs)";
            L(b, $"Volumen contenedor: {m3:0.##} m³  |  Peso de referencia: {pesoEst}", 8, y + 60, 450, 18, 8.5f, true);
            L(b, "Destino de valorización: Planta gestora autorizada de residuos no peligrosos.", 8, y + 78, 450, 18, 8f, false, TextAlignment.MiddleLeft, Color.FromArgb(108, 117, 125));
        }

        // Subcuadro de firma de recepción si existe
        Box(b, 470, y + 23, 277, hBloqueCarga - 28, Color.FromArgb(248, 249, 250), Color.FromArgb(222, 226, 230), 1);
        L(b, "CONFORMIDAD EN OBRA:", 476, y + 25, 265, 14, 7.5f, true);
        L(b, $"Firmante: {s.FirmaNombre?.Trim() ?? "Verificado en dispositivo"}", 476, y + 40, 265, 14, 7.5f);
        L(b, $"DNI: {s.FirmaDni?.Trim() ?? "Registrado electrónicamente"}", 476, y + 54, 265, 14, 7.5f);

        var imgFirma = CargarImagen(rutaFirma ?? s.FirmaPath);
        if (imgFirma != null)
        {
            b.Controls.Add(new XRPictureBox
            {
                ImageSource = imgFirma,
                Sizing = ImageSizeMode.ZoomImage,
                LocationFloat = new DevExpress.Utils.PointFloat(560, y + 68),
                SizeF = new SizeF(110, 34)
            });
        }
        else
        {
            L(b, "[REGISTRO DIGITAL SELLADO]", 476, y + 74, 265, 20, 8f, true, TextAlignment.MiddleCenter, Color.FromArgb(108, 117, 125));
        }
        y += hBloqueCarga + 12;

        // SECCIÓN 7: CÓDIGO QR ENLAZADO Y CONTROL DE INSPECCIÓN
        float hBloqueQr = 145;
        Box(b, 0, y, 757, hBloqueQr, Color.FromArgb(254, 254, 255), Color.FromArgb(13, 110, 253), 1.5f);

        // Control XRBarCode con QRCodeGenerator nativo de DevExpress
        var qrCode = new XRBarCode
        {
            LocationFloat = new DevExpress.Utils.PointFloat(15, y + 12),
            SizeF = new SizeF(120, 120),
            AutoModule = true,
            ShowText = false,
            Symbology = new QRCodeGenerator
            {
                CompactionMode = QRCodeCompactionMode.Byte,
                ErrorCorrectionLevel = QRCodeErrorCorrectionLevel.M,
                Version = QRCodeVersion.AutoVersion
            },
            Text = !string.IsNullOrWhiteSpace(urlDescargaQr) ? urlDescargaQr : "https://savall.app/api/deca/sin-url"
        };
        b.Controls.Add(qrCode);

        float xTextoQr = 148;
        L(b, "CÓDIGO QR DE CONTROL ADMINISTRATIVO EN CARRETERA (DeCA)", xTextoQr, y + 12, 595, 20, 10.5f, true, TextAlignment.MiddleLeft, Color.FromArgb(13, 110, 253));
        L(b, "INSTRUCCIÓN PARA EL AGENTE DE TRANSPORTE / GUARDIA CIVIL:", xTextoQr, y + 34, 595, 16, 8.5f, true, TextAlignment.MiddleLeft, Color.FromArgb(220, 53, 69));
        L(b, "El escaneo de este código QR redirige a la descarga directa del documento PDF nativo oficial (máx. 5 MB) sin requerir autenticación ni contraseñas, acreditando la emisión previa al transporte y la inalterabilidad de los datos según la normativa vigente.", xTextoQr, y + 50, 595, 36, 8f, false, TextAlignment.TopLeft, Color.FromArgb(33, 37, 41), true);

        Box(b, xTextoQr, y + 90, 595, 28, Color.FromArgb(240, 244, 248), Color.FromArgb(206, 212, 218), 1);
        L(b, $"URL de Verificación Oficial: {urlDescargaQr}", xTextoQr + 8, y + 94, 579, 20, 8f, true, TextAlignment.MiddleLeft, Color.FromArgb(13, 110, 253));
        y += hBloqueQr + 10;

        // SECCIÓN 8: PIE LEGAL Y CONSERVACIÓN OBLIGATORIA
        Box(b, 0, y, 757, 50, Color.FromArgb(248, 249, 250), Color.FromArgb(222, 226, 230), 1);
        L(b, "CONSERVACIÓN LEGAL OBLIGATORIA: Conforme a la legislación de transporte terrestre en España, tanto el cargador contractual como la empresa transportista deben conservar los archivos digitales de control durante un período mínimo de un (1) año.", 8, y + 4, 741, 24, 7.5f, true, TextAlignment.MiddleCenter, Color.FromArgb(73, 80, 87), true);
        L(b, $"SAVALL CONTENEDORES S.L. · C.I.F. B54099023 · Registro Mercantil de Alicante · Documento generado electrónicamente el {DateTime.Now:dd/MM/yyyy HH:mm:ss}", 8, y + 30, 741, 16, 7f, false, TextAlignment.MiddleCenter, Color.FromArgb(108, 117, 125));

        return r;
    }

    private static ImageSource? CargarImagen(string? ruta)
    {
        if (string.IsNullOrWhiteSpace(ruta) || !File.Exists(ruta)) return null;
        try
        {
            return new ImageSource(false, File.ReadAllBytes(ruta));
        }
        catch
        {
            return null;
        }
    }

    private static void Box(Band b, float x, float y, float w, float h, Color fondo, Color borde, float bordeAncho = 1)
    {
        b.Controls.Add(new XRPanel
        {
            LocationFloat = new DevExpress.Utils.PointFloat(x, y),
            SizeF = new SizeF(w, h),
            BackColor = fondo,
            BorderColor = borde,
            BorderWidth = (int)Math.Max(1, bordeAncho),
            Borders = BorderSide.All
        });
    }

    private static void L(
        Band b,
        string texto,
        float x,
        float y,
        float w,
        float h,
        float fontSize,
        bool bold = false,
        TextAlignment align = TextAlignment.MiddleLeft,
        Color? color = null,
        bool wrap = false)
    {
        b.Controls.Add(new XRLabel
        {
            Text = texto ?? string.Empty,
            LocationFloat = new DevExpress.Utils.PointFloat(x, y),
            SizeF = new SizeF(w, h),
            Font = new DXFont("Arial", fontSize, bold ? DXFontStyle.Bold : DXFontStyle.Regular),
            TextAlignment = align,
            ForeColor = color ?? Color.Black,
            Borders = BorderSide.None,
            Padding = new PaddingInfo(2, 2, 0, 0),
            WordWrap = wrap,
            Multiline = wrap,
            CanGrow = false
        });
    }
}
