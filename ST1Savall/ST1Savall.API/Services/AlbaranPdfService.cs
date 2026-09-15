using System.Collections.Concurrent;
using System.Globalization;
using DevExpress.Drawing;
using DevExpress.XtraPrinting;
using DevExpress.XtraPrinting.Drawing;
using DevExpress.XtraReports.UI;
using ST1Savall.Shared.Data;

namespace ST1Savall.API.Services;

public sealed class AlbaranPdfService
{
    static readonly CultureInfo Es = CultureInfo.GetCultureInfo("es-ES");
    static readonly ConcurrentDictionary<string, byte[]> CacheAssets = new();

    private static ImageSource? ObtenerImagenAsset(string nombre)
    {
        var bytes = CacheAssets.GetOrAdd(nombre, n =>
        {
            var resName = "ST1Savall.API.Assets." + n;
            using var s = typeof(AlbaranPdfService).Assembly.GetManifestResourceStream(resName);
            if (s != null)
            {
                using var ms = new MemoryStream();
                s.CopyTo(ms);
                return ms.ToArray();
            }

            var ruta = Path.Combine(AppContext.BaseDirectory, "Assets", n);
            return File.Exists(ruta) ? File.ReadAllBytes(ruta) : Array.Empty<byte>();
        });

        return bytes.Length > 0 ? new ImageSource(false, bytes) : null;
    }

    private static ImageSource? CargarImagenArchivo(string? ruta)
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

    public byte[] Crear(string empresa, AlbaranVentaSage50 a, ClienteSage50 c, ObraComunSage50? obra,
        IReadOnlyList<LineaAlbaranVentaSage50> lineas, TipoIvaSage50? iva, string? firmante, string? dniFirmante, string? rutaFirma, IReadOnlyList<string> fotos, string? contenedorEntregado, string? contenedorRetirado, Solicitud? solicitudPlanta, Camion? camion)
    {
        using var report = CrearReport(empresa, a, c, obra, lineas, iva, firmante, dniFirmante, rutaFirma, fotos, contenedorEntregado, contenedorRetirado, solicitudPlanta, camion);
        using var stream = new MemoryStream();
        report.ExportToPdf(stream);
        return stream.ToArray();
    }

    public static XtraReport CrearReport(string empresa, AlbaranVentaSage50 a, ClienteSage50 c, ObraComunSage50? obra,
        IReadOnlyList<LineaAlbaranVentaSage50> ls, TipoIvaSage50? iva, string? firmante, string? dniFirmante, string? firma, IReadOnlyList<string> fotos, string? contenedorEntregado, string? contenedorRetirado, Solicitud? solicitudPlanta, Camion? camion)
    {
        var r = new XtraReport { DisplayName = "ALBARAN_" + a.LETRA.Trim() + "_" + a.NUMERO.Trim(), PageWidth = 827, PageHeight = 1169, Margins = new DXMargins(40,40,35,35) };
        var b = new ReportHeaderBand { HeightF = 1030, PageBreak = PageBreak.AfterBand }; r.Bands.Add(b);
        var imgCabeceraAlbaran = ObtenerImagenAsset("CabeceraAlbaran.png");
        if (imgCabeceraAlbaran != null)
            b.Controls.Add(new XRPictureBox { ImageSource = imgCabeceraAlbaran, Sizing = ImageSizeMode.ZoomImage, LocationFloat = new DevExpress.Utils.PointFloat(0,0), SizeF = new System.Drawing.SizeF(747,170) });
        // Cabecera según el modelo de albarán: ficha a la izquierda y cliente a la derecha.
        C(b,"ALBARÁN",0,180,132,28,12,true,TextAlignment.MiddleCenter); C(b,"FECHA",132,180,129,28,12,true,TextAlignment.MiddleCenter); C(b,"CLIENTE",261,180,129,28,12,true,TextAlignment.MiddleCenter); C(b,"PÁG.",390,180,85,28,12,true,TextAlignment.MiddleCenter);
        C(b,a.LETRA.Trim()+" "+a.NUMERO.Trim(),0,208,132,25,10,false,TextAlignment.MiddleCenter); C(b,a.FECHA.ToString("dd/MM/yy"),132,208,129,25,10,false,TextAlignment.MiddleCenter); C(b,a.CLIENTE.Trim(),261,208,129,25,10,false,TextAlignment.MiddleCenter); C(b,"1",390,208,85,25,10,false,TextAlignment.MiddleCenter);
        L(b,c.Nombre.Trim(),500,181,247,25,12,true); L(b,c.Direccion.Trim(),500,206,247,20,9); L(b,c.Codpost.Trim()+"    "+c.Poblacion.Trim(),500,226,247,20,9); L(b,c.Provincia.Trim(),500,246,247,20,9); L(b,c.Cif.Trim(),500,266,247,20,9);
        C(b,"OBRA",0,242,475,25,10,true,TextAlignment.MiddleCenter); C(b,string.IsNullOrWhiteSpace(obra?.Nombre)?a.OBRA.Trim():obra.Nombre.Trim(),0,267,475,62,10);
        Head(b,340); if(ls.Count>0) Row(b,368,ls[0]);
        // El modelo no incluye un bloque de bases, IVA ni total en esta sección.
        L(b,"CONTENEDOR ENTREGADO: "+(contenedorEntregado?.Trim()??""),0,400,220,18,8,true);
        L(b,"CONTENEDOR RETIRADO: "+(contenedorRetirado?.Trim()??""),0,418,220,18,8,true);
        L(b,"RECEPCIONADO EN OBRA POR:",0,444,220,18,8,true); L(b,firmante?.Trim()??"",0,462,220,18,8); L(b,"DNI: "+(dniFirmante?.Trim()??""),0,480,220,18,8); L(b,"FIRMA:",0,498,100,18,8,true);
        var imgFirma = CargarImagenArchivo(firma);
        if (imgFirma != null)
            b.Controls.Add(new XRPictureBox { ImageSource = imgFirma, Sizing = ImageSizeMode.ZoomImage, LocationFloat = new DevExpress.Utils.PointFloat(0,516), SizeF = new System.Drawing.SizeF(165,82) });
        else
            L(b,"",0,516,165,82,8);
        L(b,"IMAGEN CONTENEDOR EN OBRA",230,400,240,18,7.5f,false,TextAlignment.MiddleCenter);
        L(b,"IMAGEN CONTENEDOR VACIADO EN PLANTA",490,400,240,18,7.5f,false,TextAlignment.MiddleCenter);
        var fotosValidas = fotos.Select(CargarImagenArchivo).Where(img => img != null).Take(2).ToList();
        for (int i = 0; i < fotosValidas.Count; i++)
        {
            b.Controls.Add(new XRPictureBox { ImageSource = fotosValidas[i]!, Sizing = ImageSizeMode.ZoomImage, LocationFloat = new DevExpress.Utils.PointFloat(230 + i * 260, 420), SizeF = new System.Drawing.SizeF(240, 165), Borders = BorderSide.All });
        }
        L(b,"PROTECCIÓN DE DATOS: Responsable del Tratamiento: SAVALL CONTENEDORES S.L. Finalidad: dar correcto cumplimiento al contrato suscrito.",0,620,747,18,6.5f,false,TextAlignment.MiddleCenter);
        L(b,"Puede ejercer sus derechos de acceso, rectificación y supresión en Avda. Doctor Rico, 18, Alicante.",0,640,747,18,6.5f,false,TextAlignment.MiddleCenter);
        L(b,"Inscrita en el Registro Mercantil de Alicante.",0,662,747,18,7.5f,false,TextAlignment.MiddleCenter);
        var paginaPesaje = new DetailBand { HeightF = 760 };
        r.Bands.Add(paginaPesaje);
        CrearPaginaPesaje(paginaPesaje, a, c, obra, ls, solicitudPlanta, camion);
        return r;
    }
    static void CrearPaginaPesaje(Band b, AlbaranVentaSage50 a, ClienteSage50 c, ObraComunSage50? obra,
        IReadOnlyList<LineaAlbaranVentaSage50> lineas, Solicitud? solicitud, Camion? camion)
    {
        var neto = solicitud?.KgAlbaran ?? 0;
        var tara = camion?.TaraKg ?? 0;
        var bruto = tara + neto;
        var lineaArticulo = lineas.FirstOrDefault(l => !string.IsNullOrWhiteSpace(solicitud?.TipoResiduo)
            && l.ARTICULO.Trim().Equals(solicitud.TipoResiduo.Trim(), StringComparison.OrdinalIgnoreCase))
            ?? lineas.FirstOrDefault();
        var codigoArticulo = !string.IsNullOrWhiteSpace(solicitud?.TipoResiduo)
            ? solicitud!.TipoResiduo!.Trim()
            : (lineaArticulo?.ARTICULO?.Trim() ?? string.Empty);
        var nombreArticulo = lineaArticulo?.DEFINICION?.Trim() ?? string.Empty;
        var fecha = solicitud?.FechaTarea ?? a.FECHA;
        var hora = solicitud?.HoraPesaje?.ToString(@"hh\:mm") ?? string.Empty;
        var nombreObra = !string.IsNullOrWhiteSpace(obra?.Nombre) ? obra!.Nombre.Trim() : a.OBRA.Trim();

        var imgCabeceraPesaje = ObtenerImagenAsset("CabeceraPesaje.png");
        if (imgCabeceraPesaje != null)
            b.Controls.Add(new XRPictureBox { ImageSource = imgCabeceraPesaje, Sizing = ImageSizeMode.ZoomImage, LocationFloat = new DevExpress.Utils.PointFloat(0, 0), SizeF = new System.Drawing.SizeF(747, 150) });
        L(b,"Albarán número:",0,158,125,20,10,true); L(b,solicitud?.AlbaranPlanta?.Trim() ?? string.Empty,125,158,110,20,10);
        L(b,"Obra:",240,158,55,20,10,true); L(b,nombreObra,295,158,450,20,10);
        L(b,"Fecha:",0,188,60,20,10,true); L(b,fecha.ToString("dd/MM/yyyy"),65,188,115,20,10);
        L(b,"Hora:",190,188,50,20,10,true); L(b,hora,240,188,70,20,10);
        L(b,"Vehículo:",320,188,75,20,10,true); L(b,camion?.Matricula?.Trim() ?? string.Empty,395,188,145,20,10);
        L(b,"Cliente:",0,224,70,20,10,true); L(b,c.Nombre?.Trim() ?? string.Empty,75,224,385,20,10);
        L(b,"C.I.F.:",470,224,60,20,10,true); L(b,c.Cif?.Trim() ?? string.Empty,530,224,210,20,10);
        L(b,"Obra:",0,247,55,20,10,true); L(b,nombreObra,75,247,385,20,10);
        L(b,"Población:",470,247,85,20,10,true); L(b,c.Poblacion?.Trim() ?? string.Empty,555,247,185,20,10);
        L(b,"Provincia:",0,270,80,20,10,true); L(b,c.Provincia?.Trim() ?? string.Empty,80,270,380,20,10);
        L(b,"Teléfono:",470,270,75,20,10,true); L(b,c.Telefono?.Trim() ?? string.Empty,545,270,195,20,10);
        C(b,"Concepto",0,310,320,26,9,true,TextAlignment.MiddleLeft); C(b,"Kg. Bruto",320,310,80,26,9,true,TextAlignment.MiddleCenter);
        C(b,"Kg. Tara",400,310,80,26,9,true,TextAlignment.MiddleCenter); C(b,"Kg. Neto",480,310,80,26,9,true,TextAlignment.MiddleCenter);
        C(b,"Precio",560,310,90,26,9,true,TextAlignment.MiddleCenter); C(b,"Importe",650,310,97,26,9,true,TextAlignment.MiddleCenter);
        C(b,"",0,336,320,58,9);
        if (!string.IsNullOrWhiteSpace(codigoArticulo) && !string.IsNullOrWhiteSpace(nombreArticulo) && !codigoArticulo.Equals(nombreArticulo, StringComparison.OrdinalIgnoreCase))
        {
            L(b,codigoArticulo,4,340,312,18,9,true);
            L(b,nombreArticulo,4,360,312,30,8.5f,false,TextAlignment.MiddleLeft,BorderSide.None,true);
        }
        else
        {
            var textoUnico = !string.IsNullOrWhiteSpace(codigoArticulo) ? codigoArticulo : nombreArticulo;
            L(b,textoUnico,4,350,312,30,9,false,TextAlignment.MiddleLeft,BorderSide.None,true);
        }
        C(b,K(bruto),320,336,80,58,10,false,TextAlignment.MiddleCenter);
        C(b,K(tara),400,336,80,58,10,false,TextAlignment.MiddleCenter); C(b,K(neto),480,336,80,58,10,false,TextAlignment.MiddleCenter);
        C(b,"",560,336,90,58,10); C(b,"",650,336,97,58,10);
        L(b,"Conforme cliente:",100,405,160,20,10,true);
        var imgFirmaPesaje = ObtenerImagenAsset("FirmaPesaje.png");
        if (imgFirmaPesaje != null)
            b.Controls.Add(new XRPictureBox { ImageSource = imgFirmaPesaje, Sizing = ImageSizeMode.ZoomImage, LocationFloat = new DevExpress.Utils.PointFloat(110, 430), SizeF = new System.Drawing.SizeF(100, 80) });
        L(b,"I.V.A.",560,405,90,20,10,true,TextAlignment.MiddleCenter); C(b,"",650,405,97,20,10);
        L(b,"TOTAL",560,430,90,20,10,true,TextAlignment.MiddleCenter); C(b,"",650,430,97,20,10);
        L(b,"SABOSPA S.L. CIF B-03989920 Inscrita en el Registro Mercantil de Alicante, Tomo 1736, Folio 197, Hoja A-26776, Inscripción 1ª",0,700,747,18,7.5f,false,TextAlignment.MiddleCenter);
    }
    static string K(int kg) => kg.ToString("0", Es);
    static void Head(Band b,float y){ L(b,"CÓDIGO",0,y,90,20,8,true,TextAlignment.MiddleCenter); L(b,"DESCRIPCIÓN",90,y,370,20,8,true); L(b,"UNID.",460,y,75,20,8,true,TextAlignment.MiddleRight); L(b,"PRECIO",535,y,100,20,8,true,TextAlignment.MiddleRight); L(b,"IMPORTE",635,y,112,20,8,true,TextAlignment.MiddleRight); }
    static void Row(Band b,float y,LineaAlbaranVentaSage50 l){ L(b,l.ARTICULO.Trim(),0,y,90,20,7.5f); L(b,l.DEFINICION.Trim(),90,y,370,20,7.5f); L(b,N(l.UNIDADES),460,y,75,20,7.5f,false,TextAlignment.MiddleRight); }
    static void Empty(Band b,float y){ C(b,"",0,y,90,23,7.5f); C(b,"",90,y,370,23,7.5f); C(b,"",460,y,75,23,7.5f); C(b,"",535,y,100,23,7.5f); C(b,"",635,y,112,23,7.5f); }
    static void C(Band b,string t,float x,float y,float w,float h,float f,bool bold=false,TextAlignment a=TextAlignment.MiddleLeft)=>L(b,t,x,y,w,h,f,bold,a,BorderSide.All);
    static void L(Band b,string t,float x,float y,float w,float h,float f,bool bold=false,TextAlignment a=TextAlignment.MiddleLeft,BorderSide sides=BorderSide.None,bool wrap=false)=>b.Controls.Add(new XRLabel { Text=t, LocationFloat=new DevExpress.Utils.PointFloat(x,y), SizeF=new System.Drawing.SizeF(w,h), Font=new DXFont("Arial",f,bold?DXFontStyle.Bold:DXFontStyle.Regular), TextAlignment=a, Borders=sides, Padding=new PaddingInfo(4,4,0,0), CanGrow=false, WordWrap=wrap, Multiline=wrap });
    static string N(decimal n)=>n.ToString("N2",Es);
}