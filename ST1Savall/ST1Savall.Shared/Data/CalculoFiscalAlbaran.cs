namespace ST1Savall.Shared.Data;

/// <summary>Totales fiscales de un albarán de venta en moneda de empresa.</summary>
public sealed record CalculoFiscalAlbaran(
    decimal BaseImponible,
    decimal CuotaIva,
    decimal CuotaRecargo,
    decimal ImporteRetencion,
    decimal TotalDocumento,
    decimal PorcentajeRetencion,
    int ModoRetencion)
{
    public decimal TotalAntesRetencion => BaseImponible + CuotaIva + CuotaRecargo;

    public static CalculoFiscalAlbaran Crear(
        decimal unidades,
        decimal precio,
        decimal descuento1,
        decimal descuento2,
        TipoIvaSage50? tipoIva,
        ClienteSage50 cliente)
    {
        Func<decimal, decimal> redondear = n => Math.Round(n, 6, MidpointRounding.AwayFromZero);
        var baseImponible = redondear(unidades * precio * (1m - descuento1 / 100m) * (1m - descuento2 / 100m));
        var cuotaIva = redondear(baseImponible * (tipoIva?.Iva ?? 0m) / 100m);
        var cuotaRecargo = cliente.Recargo
            ? redondear(baseImponible * (tipoIva?.Recarg ?? 0m) / 100m)
            : 0m;
        var totalAntesRetencion = baseImponible + cuotaIva + cuotaRecargo;

        // Sage almacena el porcentaje y modo de retención no fiscal en el cliente.
        var porcentajeRetencion = cliente.Retnofisc ? cliente.Tpcretnofi : 0m;
        var modoRetencion = cliente.Retnofisc ? cliente.Modretnofi : 0;
        var baseRetencion = modoRetencion == 1 ? baseImponible : totalAntesRetencion;
        var importeRetencion = porcentajeRetencion > 0m
            ? redondear(baseRetencion * porcentajeRetencion / 100m)
            : 0m;

        return new CalculoFiscalAlbaran(
            baseImponible,
            cuotaIva,
            cuotaRecargo,
            importeRetencion,
            redondear(totalAntesRetencion - importeRetencion),
            porcentajeRetencion,
            modoRetencion);
    }
}
