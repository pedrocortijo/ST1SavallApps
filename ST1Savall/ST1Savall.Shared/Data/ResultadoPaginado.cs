namespace ST1Savall.Shared.Data;

public sealed class ResultadoPaginado<T>
{
    public IReadOnlyList<T> Datos { get; set; } = [];
    public int TotalRegistros { get; set; }
    public int Pagina { get; set; }
    public int TamanoPagina { get; set; }

    public int TotalPaginas => TamanoPagina <= 0
        ? 0
        : (int)Math.Ceiling(TotalRegistros / (double)TamanoPagina);
}
