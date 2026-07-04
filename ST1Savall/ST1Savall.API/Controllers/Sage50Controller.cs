using Microsoft.AspNetCore.Mvc;
using ST1Savall.Shared.Data;
using System.Collections.Generic;

namespace ST1Savall.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class Sage50Controller : ControllerBase
{
    [HttpGet("empresas")]
    public ActionResult<IEnumerable<EmpresaSage50>> GetEmpresas()
    {
        var empresas = new List<EmpresaSage50>
        {
            new() { 
                IdEmpresa = 1, 
                Nombre = "CONSTRUCCIONES SAVALL S.L. (SAGE50)",
                NombreCliente = "Construcciones Savall S.L.",
                DireccionCliente = "Av. de la Constitución 123",
                PoblacionCliente = "Valencia",
                TelefonoCliente = "963456789",
                CodigoPostalCliente = "46001"
            },
            new() { 
                IdEmpresa = 2, 
                Nombre = "PRODUCCIONES HNOS SAVALL S.A. (SAGE50)",
                NombreCliente = "Producciones Hnos Savall S.A.",
                DireccionCliente = "Calle Mayor 45",
                PoblacionCliente = "Alicante",
                TelefonoCliente = "965123456",
                CodigoPostalCliente = "03001"
            },
            new() { 
                IdEmpresa = 3, 
                Nombre = "SAVALL LOGÍSTICA S.L. (SAGE50)",
                NombreCliente = "Savall Logística S.L.",
                DireccionCliente = "Polígono Industrial La Luz, Nave 8",
                PoblacionCliente = "Castellón",
                TelefonoCliente = "964789012",
                CodigoPostalCliente = "12001"
            }
        };
        return Ok(empresas);
    }
}
