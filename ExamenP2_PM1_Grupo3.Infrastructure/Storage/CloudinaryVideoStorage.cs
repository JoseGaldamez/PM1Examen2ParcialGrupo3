using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using ExamenP2_PM1_Grupo3.Application.Exceptions;
using ExamenP2_PM1_Grupo3.Application.Interfaces;
using ExamenP2_PM1_Grupo3.Domain.Entities;

namespace ExamenP2_PM1_Grupo3.Infrastructure.Storage;

public sealed class CloudinaryVideoStorage(string cloudinaryUrl) : IAlmacenamientoVideo
{
    private readonly Cloudinary cloudinary = new(cloudinaryUrl);

    public async Task<string> SubirAsync(string publicId, Stream contenido, string nombreArchivo,
        CancellationToken cancellationToken)
    {
        var resultado = await cloudinary.UploadAsync(new VideoUploadParams
        {
            File = new FileDescription(nombreArchivo, contenido),
            PublicId = publicId,
            Overwrite = false
        }, cancellationToken);

        var url = resultado.SecureUrl;
        if (resultado.Error is not null || url is null || url.Scheme != Uri.UriSchemeHttps ||
            url.AbsoluteUri.Length > Contacto.MaximoVideoUrl)
        {
            throw new InvalidOperationException("Cloudinary no devolvió una URL válida para el video.");
        }

        if (resultado.Width <= 0 || resultado.Height <= 0)
        {
            throw new VideoInvalidoException();
        }

        return url.AbsoluteUri;
    }

    public async Task EliminarAsync(string publicId)
    {
        var resultado = await cloudinary.DestroyAsync(new DeletionParams(publicId)
        {
            ResourceType = ResourceType.Video
        });
        if (resultado.Error is not null || resultado.Result is not ("ok" or "not found"))
        {
            throw new InvalidOperationException("Cloudinary no confirmó la eliminación del video.");
        }
    }
}
