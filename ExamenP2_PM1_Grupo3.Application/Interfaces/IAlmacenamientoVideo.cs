namespace ExamenP2_PM1_Grupo3.Application.Interfaces;

public interface IAlmacenamientoVideo
{
    Task<string> SubirAsync(string publicId, Stream contenido, string nombreArchivo,
        CancellationToken cancellationToken);

    Task EliminarAsync(string publicId);
}
