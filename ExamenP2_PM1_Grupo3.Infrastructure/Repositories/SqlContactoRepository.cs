using System.Data;
using ExamenP2_PM1_Grupo3.Application.DTOs;
using ExamenP2_PM1_Grupo3.Application.Exceptions;
using ExamenP2_PM1_Grupo3.Application.Interfaces;
using ExamenP2_PM1_Grupo3.Domain.Entities;
using Microsoft.Data.SqlClient;

namespace ExamenP2_PM1_Grupo3.Infrastructure.Repositories;

public sealed class SqlContactoRepository(string connectionString) : IContactoRepository
{
    public async Task<int> CrearAsync(Contacto contacto, CancellationToken cancellationToken)
    {
        await using var conexion = new SqlConnection(connectionString);
        await conexion.OpenAsync(cancellationToken);
        using var transaccion = conexion.BeginTransaction();
        using var comando = conexion.CreateCommand();
        comando.Transaction = transaccion;
        comando.CommandText = """
            INSERT INTO dbo.contactos (Nombre, Telefono, Latitud, Longitud, VideoUrl)
            OUTPUT INSERTED.Id
            VALUES (@Nombre, @Telefono, @Latitud, @Longitud, @VideoUrl);
            """;
        AgregarDatos(comando, contacto.Nombre, contacto.Telefono, contacto.Latitud,
            contacto.Longitud, contacto.VideoUrl);

        var id = (int)(await comando.ExecuteScalarAsync(cancellationToken)
            ?? throw new InvalidOperationException("SQL Server no devolvió el ID del contacto."));
        await ConfirmarAsync(transaccion);
        return id;
    }

    public async Task<IReadOnlyList<ContactoResponse>> ListarAsync(string? buscar,
        CancellationToken cancellationToken)
    {
        await using var conexion = new SqlConnection(connectionString);
        await conexion.OpenAsync(cancellationToken);
        using var comando = conexion.CreateCommand();
        comando.CommandText = """
            SELECT Id, Nombre, Telefono, Latitud, Longitud, VideoUrl
            FROM dbo.contactos
            WHERE @Buscar = N'' OR CHARINDEX(@Buscar, Nombre) > 0 OR CHARINDEX(@Buscar, Telefono) > 0
            ORDER BY Id DESC;
            """;
        comando.Parameters.Add("@Buscar", SqlDbType.NVarChar, -1).Value = buscar ?? string.Empty;

        var contactos = new List<ContactoResponse>();
        await using var lector = await comando.ExecuteReaderAsync(cancellationToken);
        while (await lector.ReadAsync(cancellationToken))
        {
            contactos.Add(LeerContacto(lector));
        }

        return contactos;
    }

    public async Task<string?> ObtenerArchivoAsync(int id, CancellationToken cancellationToken)
    {
        await using var conexion = new SqlConnection(connectionString);
        await conexion.OpenAsync(cancellationToken);
        using var comando = conexion.CreateCommand();
        comando.CommandText = "SELECT VideoUrl FROM dbo.contactos WHERE Id = @Id;";
        comando.Parameters.Add("@Id", SqlDbType.Int).Value = id;
        return (string?)await comando.ExecuteScalarAsync(cancellationToken);
    }

    public async Task<ContactoResponse?> ActualizarAsync(int id, string? nombre, string? telefono,
        decimal? latitud, decimal? longitud, string? videoUrl, CancellationToken cancellationToken)
    {
        await using var conexion = new SqlConnection(connectionString);
        await conexion.OpenAsync(cancellationToken);
        using var transaccion = conexion.BeginTransaction();
        using var comando = conexion.CreateCommand();
        comando.Transaction = transaccion;
        comando.CommandText = """
            UPDATE dbo.contactos
            SET Nombre = COALESCE(@Nombre, Nombre),
                Telefono = COALESCE(@Telefono, Telefono),
                Latitud = COALESCE(@Latitud, Latitud),
                Longitud = COALESCE(@Longitud, Longitud),
                VideoUrl = COALESCE(@VideoUrl, VideoUrl)
            OUTPUT INSERTED.Id, INSERTED.Nombre, INSERTED.Telefono,
                INSERTED.Latitud, INSERTED.Longitud, INSERTED.VideoUrl
            WHERE Id = @Id;
            """;
        comando.Parameters.Add("@Id", SqlDbType.Int).Value = id;
        AgregarDatos(comando, nombre, telefono, latitud, longitud, videoUrl);

        ContactoResponse? contacto;
        await using (var lector = await comando.ExecuteReaderAsync(cancellationToken))
        {
            contacto = await lector.ReadAsync(cancellationToken) ? LeerContacto(lector) : null;
        }

        await ConfirmarAsync(transaccion);
        return contacto;
    }

    public async Task<bool> EliminarAsync(int id, CancellationToken cancellationToken)
    {
        await using var conexion = new SqlConnection(connectionString);
        await conexion.OpenAsync(cancellationToken);
        using var comando = conexion.CreateCommand();
        comando.CommandText = "DELETE FROM dbo.contactos WHERE Id = @Id;";
        comando.Parameters.Add("@Id", SqlDbType.Int).Value = id;
        return await comando.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    private static ContactoResponse LeerContacto(SqlDataReader lector)
    {
        return new ContactoResponse(lector.GetInt32(0), lector.GetString(1), lector.GetString(2),
            lector.GetDecimal(3), lector.GetDecimal(4), lector.GetString(5));
    }

    private static async Task ConfirmarAsync(SqlTransaction transaccion)
    {
        try
        {
            await transaccion.CommitAsync(CancellationToken.None);
        }
        catch (Exception error)
        {
            // Si se perdió la confirmación, la fila podría haberse guardado con esta URL.
            throw new PersistenciaInciertaException(error);
        }
    }

    private static void AgregarDatos(SqlCommand comando, string? nombre, string? telefono,
        decimal? latitud, decimal? longitud, string? videoUrl)
    {
        comando.Parameters.Add("@Nombre", SqlDbType.NVarChar, Contacto.MaximoNombre).Value =
            (object?)nombre ?? DBNull.Value;
        comando.Parameters.Add("@Telefono", SqlDbType.NVarChar, Contacto.MaximoTelefono).Value =
            (object?)telefono ?? DBNull.Value;
        AgregarCoordenada(comando, "@Latitud", 9, latitud);
        AgregarCoordenada(comando, "@Longitud", 10, longitud);
        comando.Parameters.Add("@VideoUrl", SqlDbType.NVarChar, Contacto.MaximoVideoUrl).Value =
            (object?)videoUrl ?? DBNull.Value;
    }

    private static void AgregarCoordenada(SqlCommand comando, string nombre, byte precision, decimal? valor)
    {
        var parametro = comando.Parameters.Add(nombre, SqlDbType.Decimal);
        parametro.Precision = precision;
        parametro.Scale = Contacto.DecimalesCoordenadas;
        parametro.Value = (object?)valor ?? DBNull.Value;
    }
}
