using System.ComponentModel.DataAnnotations;
using ExamenP2_PM1_Grupo3.Domain.Entities;

namespace ExamenP2_PM1_Grupo3.Models;

public sealed class ActualizarContactoJson
{
    [Required, StringLength(Contacto.MaximoNombre)]
    public required string Nombre { get; init; }

    [Required, StringLength(Contacto.MaximoTelefono)]
    public required string Telefono { get; init; }

    [Range(typeof(decimal), "-90", "90")]
    public required decimal Latitud { get; init; }

    [Range(typeof(decimal), "-180", "180")]
    public required decimal Longitud { get; init; }
}
