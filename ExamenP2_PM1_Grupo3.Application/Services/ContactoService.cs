using ExamenP2_PM1_Grupo3.Application.DTOs;
using ExamenP2_PM1_Grupo3.Application.Exceptions;
using ExamenP2_PM1_Grupo3.Application.Interfaces;
using ExamenP2_PM1_Grupo3.Domain.Entities;

namespace ExamenP2_PM1_Grupo3.Application.Services;

public sealed class ContactoService(IContactoRepository contactos, IAlmacenamientoVideo videos)
{
    public Task<ContactoResponse> CrearAsync(
        string nombre, string telefono, decimal latitud, decimal longitud,
        Stream video, string nombreArchivo, CancellationToken cancellationToken)
    {
        return GuardarConVideoAsync(video, nombreArchivo, async url =>
        {
            var contacto = new Contacto(
                nombre.Trim(),
                telefono.Trim(),
                decimal.Round(latitud, Contacto.DecimalesCoordenadas),
                decimal.Round(longitud, Contacto.DecimalesCoordenadas),
                url);
            var id = await contactos.CrearAsync(contacto, cancellationToken);
            return new ContactoResponse(id, contacto.Nombre, contacto.Telefono,
                contacto.Latitud, contacto.Longitud, contacto.VideoUrl);
        }, cancellationToken);
    }

    public async Task<ContactoResponse?> ActualizarAsync(int id, string? nombre, string? telefono,
        decimal? latitud, decimal? longitud, Stream? video, string? nombreArchivo,
        CancellationToken cancellationToken)
    {
        Task<ContactoResponse?> GuardarAsync(string? url) => contactos.ActualizarAsync(
            id, nombre?.Trim(), telefono?.Trim(),
            RedondearCoordenada(latitud), RedondearCoordenada(longitud), url, cancellationToken);

        if (video is null)
        {
            return await GuardarAsync(null);
        }

        if (await contactos.ObtenerArchivoAsync(id, cancellationToken) is null)
        {
            return null;
        }

        return await GuardarConVideoAsync(video, nombreArchivo!, GuardarAsync, cancellationToken);
    }

    public Task<bool> EliminarAsync(int id, CancellationToken cancellationToken)
    {
        return contactos.EliminarAsync(id, cancellationToken);
    }

    private static decimal? RedondearCoordenada(decimal? valor)
    {
        return valor.HasValue ? decimal.Round(valor.Value, Contacto.DecimalesCoordenadas) : null;
    }

    private async Task<T> GuardarConVideoAsync<T>(Stream video, string nombreArchivo,
        Func<string, Task<T>> guardar, CancellationToken cancellationToken)
    {
        var publicId = $"contactos/{Guid.NewGuid():N}";
        T resultado;
        try
        {
            var url = await videos.SubirAsync(publicId, video, nombreArchivo, cancellationToken);
            resultado = await guardar(url);
        }
        catch (Exception error) when (error is not PersistenciaInciertaException)
        {
            try
            {
                await videos.EliminarAsync(publicId);
            }
            catch (Exception errorAlEliminar)
            {
                throw new AggregateException(
                    $"No se pudo revertir la carga de Cloudinary ({publicId}).", error, errorAlEliminar);
            }

            throw;
        }

        if (resultado is null)
        {
            await videos.EliminarAsync(publicId);
        }

        return resultado;
    }
}
