using Microsoft.AspNetCore.Mvc;

namespace ExamenP2_PM1_Grupo3.Models;

public sealed class ActualizarContactoForm : ContactoForm
{
    [FromForm(Name = CampoArchivo)]
    public IFormFile? Archivo { get; init; }
}
