using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;
using ExamenP2_PM1_Grupo3.Domain.Entities;

namespace ExamenP2_PM1_Grupo3.Models;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class EditarContactoJson : IValidatableObject
{
    [StringLength(Contacto.MaximoNombre)]
    public string? Nombre { get; init => field = ExigirValor(value); }

    [StringLength(Contacto.MaximoTelefono)]
    public string? Telefono { get; init => field = ExigirValor(value); }

    [Range(typeof(decimal), "-90", "90")]
    public decimal? Latitud { get; init => field = ExigirValor(value); }

    [Range(typeof(decimal), "-180", "180")]
    public decimal? Longitud { get; init => field = ExigirValor(value); }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Nombre is null && Telefono is null && Latitud is null && Longitud is null)
        {
            yield return new ValidationResult("Envía al menos un campo para editar.");
        }

        if (Nombre is not null && string.IsNullOrWhiteSpace(Nombre))
        {
            yield return new ValidationResult("El nombre no puede estar vacío.", [nameof(Nombre)]);
        }

        if (Telefono is not null && string.IsNullOrWhiteSpace(Telefono))
        {
            yield return new ValidationResult("El teléfono no puede estar vacío.", [nameof(Telefono)]);
        }
    }

    // Un campo omitido queda sin valor. Un null enviado explícitamente es inválido.
    private static T ExigirValor<T>(T valor)
    {
        return valor is null ? throw new JsonException("El campo no puede ser null.") : valor;
    }
}
