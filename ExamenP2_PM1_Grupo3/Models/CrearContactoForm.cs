using System.ComponentModel.DataAnnotations;
using ExamenP2_PM1_Grupo3.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace ExamenP2_PM1_Grupo3.Models;

public abstract class ContactoForm
{
    internal const string CampoArchivo = "archivo";

    [Required, StringLength(Contacto.MaximoNombre)]
    public string Nombre { get; init; } = null!;

    [Required, StringLength(Contacto.MaximoTelefono)]
    public string Telefono { get; init; } = null!;

    [Required]
    public string Latitud { get; init; } = null!;

    [Required]
    public string Longitud { get; init; } = null!;
}

public sealed class CrearContactoForm : ContactoForm
{
    [Required, FromForm(Name = CampoArchivo)]
    public IFormFile Archivo { get; init; } = null!;
}
