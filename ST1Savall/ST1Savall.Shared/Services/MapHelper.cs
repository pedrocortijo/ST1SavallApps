using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace ST1Savall
{
    public static class MapHelper
    {
        /// <summary>
        /// Normaliza y limpia los componentes de una dirección para construir una consulta de geolocalización.
        /// Remueve prefijos conflictivos como "C/", simplifica provincias complejas con barras inclinadas,
        /// y une los componentes con comas.
        /// </summary>
        public static string BuildNormalizedQuery(string? direccion, string? poblacion, string? provincia, string? codigoPostal)
        {
            var parts = new List<string>();

            if (!string.IsNullOrWhiteSpace(direccion))
            {
                // Limpia abreviaturas conflictivas como "C/" o "C/ " al inicio de la dirección
                var cleanedDir = Regex.Replace(direccion.Trim(), @"^(?i)C/+\s*", "");
                // Elimina textos entre paréntesis tipo "(VER CALLEJERO)", "(VER MAPA)", etc.
                cleanedDir = Regex.Replace(cleanedDir, @"\s*\([^)]*\)", "").Trim();
                if (!string.IsNullOrWhiteSpace(cleanedDir))
                {
                    parts.Add(cleanedDir);
                }
            }

            if (!string.IsNullOrWhiteSpace(poblacion))
            {
                var cleanedPob = Regex.Replace(poblacion.Trim(), @"\s*\([^)]*\)", "").Trim();
                if (!string.IsNullOrWhiteSpace(cleanedPob))
                {
                    parts.Add(cleanedPob);
                }
            }

            if (!string.IsNullOrWhiteSpace(provincia))
            {
                // Si la provincia contiene una barra (ej. ALICANTE/ALACANT), tomamos solo la primera parte
                var cleanProv = provincia.Split('/')[0].Trim();
                cleanProv = Regex.Replace(cleanProv, @"\s*\([^)]*\)", "").Trim();
                if (!string.IsNullOrWhiteSpace(cleanProv))
                {
                    parts.Add(cleanProv);
                }
            }

            if (!string.IsNullOrWhiteSpace(codigoPostal))
            {
                parts.Add(codigoPostal.Trim());
            }

            return parts.Count > 0 ? string.Join(", ", parts) : string.Empty;
        }
    }
}
