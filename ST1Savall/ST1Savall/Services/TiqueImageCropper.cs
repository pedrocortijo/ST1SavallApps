#if WINDOWS
using OpenCvSharp;

namespace ST1Savall.Services;

internal static class TiqueImageCropper
{
    public static byte[] RecortarSiSeDetecta(byte[] imageData)
    {
        using var original = Cv2.ImDecode(imageData, ImreadModes.Color);
        if (original.Empty()) return imageData;

        using var gris = new Mat();
        using var bordes = new Mat();
        Cv2.CvtColor(original, gris, ColorConversionCodes.BGR2GRAY);
        Cv2.GaussianBlur(gris, gris, new OpenCvSharp.Size(5, 5), 0);
        Cv2.Canny(gris, bordes, 60, 180);
        Cv2.FindContours(bordes, out OpenCvSharp.Point[][] contornos, out _, RetrievalModes.List, ContourApproximationModes.ApproxSimple);

        var areaMinima = original.Width * original.Height * 0.18;
        var esquinas = contornos
            .Select(contorno => Cv2.ApproxPolyDP(contorno, Cv2.ArcLength(contorno, true) * 0.02, true))
            .Where(poligono => poligono.Length == 4 && Cv2.IsContourConvex(poligono) && Math.Abs(Cv2.ContourArea(poligono)) >= areaMinima)
            .OrderByDescending(poligono => Math.Abs(Cv2.ContourArea(poligono)))
            .FirstOrDefault();
        if (esquinas is null) return imageData;

        var origen = OrdenarEsquinas(esquinas);
        var ancho = (int)Math.Max(Distancia(origen[0], origen[1]), Distancia(origen[2], origen[3]));
        var alto = (int)Math.Max(Distancia(origen[0], origen[3]), Distancia(origen[1], origen[2]));
        if (ancho < 300 || alto < 300) return imageData;

        using var transformacion = Cv2.GetPerspectiveTransform(origen, new[]
        {
            new Point2f(0, 0), new Point2f(ancho - 1, 0), new Point2f(ancho - 1, alto - 1), new Point2f(0, alto - 1)
        });
        using var recortado = new Mat();
        Cv2.WarpPerspective(original, recortado, transformacion, new OpenCvSharp.Size(ancho, alto), InterpolationFlags.Cubic, BorderTypes.Replicate);
        Cv2.ImEncode(".jpg", recortado, out var codificado, new[] { (int)ImwriteFlags.JpegQuality, 95 });
        return codificado;
    }

    private static Point2f[] OrdenarEsquinas(OpenCvSharp.Point[] puntos)
    {
        var ordenados = puntos.Select(p => new Point2f(p.X, p.Y)).OrderBy(p => p.Y).ToArray();
        var arriba = ordenados[..2].OrderBy(p => p.X).ToArray();
        var abajo = ordenados[2..].OrderBy(p => p.X).ToArray();
        return new[] { arriba[0], arriba[1], abajo[1], abajo[0] };
    }

    private static double Distancia(Point2f primero, Point2f segundo) => Math.Sqrt(Math.Pow(primero.X - segundo.X, 2) + Math.Pow(primero.Y - segundo.Y, 2));
}
#endif