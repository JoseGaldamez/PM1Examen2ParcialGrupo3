using ExamenP2_PM1_Grupo3.Application.DTOs;
using ExamenP2_PM1_Grupo3.Application.Interfaces;

namespace ExamenP2_PM1_Grupo3.Application.Services;

public sealed class ConsultarContactos(IContactoRepository contactos)
{
    public Task<IReadOnlyList<ContactoResponse>> ListarAsync(string? buscar, CancellationToken cancellationToken)
    {
        return contactos.ListarAsync(buscar?.Trim(), cancellationToken);
    }

    public Task<string?> ObtenerArchivoAsync(int id, CancellationToken cancellationToken)
    {
        return contactos.ObtenerArchivoAsync(id, cancellationToken);
    }
}
