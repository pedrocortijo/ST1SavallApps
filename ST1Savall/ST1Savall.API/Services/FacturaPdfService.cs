using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;

namespace ST1Savall.API.Services;

public sealed class FacturaPdfService
{
    public byte[] Unir(byte[] factura, IEnumerable<byte[]> albaranes)
    {
        using var salida = new PdfDocument();
        AgregarPaginas(salida, factura);
        foreach (var albaran in albaranes) AgregarPaginas(salida, albaran);
        using var stream = new MemoryStream();
        salida.Save(stream, false);
        return stream.ToArray();
    }

    private static void AgregarPaginas(PdfDocument salida, byte[] contenido)
    {
        using var entrada = PdfReader.Open(new MemoryStream(contenido), PdfDocumentOpenMode.Import);
        for (var indice = 0; indice < entrada.PageCount; indice++) salida.AddPage(entrada.Pages[indice]);
    }
}
