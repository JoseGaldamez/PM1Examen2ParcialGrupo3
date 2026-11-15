using ExamenP2_PM1_Grupo3.Application.DTOs;
using ExamenP2_PM1_Grupo3.Domain.Entities;

namespace ExamenP2_PM1_Grupo3.Application.Interfaces;

public interface IContactoRepository
{
    Task<int> CrearAsync(Contacto contacto, CancellationToken cancellationToken);

    Task<IReadOnlyList<ContactoResponse>> ListarAsync(string? buscar, CancellationToken cancellationToken);

    Task<string?> ObtenerArchivoAsync(int id, CancellationToken cancellationToken);

    Task<ContactoResponse?> ActualizarAsync(int id, string? nombre, string? telefono,
        decimal? latitud, decimal? longitud, string? videoUrl, CancellationToken cancellationToken);

    Task<bool> EliminarAsync(int id, CancellationToken cancellationToken);
}
