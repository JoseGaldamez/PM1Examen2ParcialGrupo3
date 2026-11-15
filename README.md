# API de contactos · Examen P2 Grupo 3

API REST en ASP.NET Core (.NET 10) para guardar contactos con nombre, teléfono, coordenadas y video. Cloudinary almacena el video y SQL Server guarda los datos del contacto junto con su URL HTTPS.

## URLs

| Entorno | URL base | Swagger UI | OpenAPI JSON |
| --- | --- | --- | --- |
| Producción (Cloud Run) | [Servicio](https://api-pm1-examen2parcial-grupo3-337965348211.europe-west1.run.app/) | [Abrir Swagger](https://api-pm1-examen2parcial-grupo3-337965348211.europe-west1.run.app/swagger) | [Ver documento](https://api-pm1-examen2parcial-grupo3-337965348211.europe-west1.run.app/openapi/v1.json) |
| Local, perfil `http` | [http://localhost:5164](http://localhost:5164) | [Abrir Swagger local](http://localhost:5164/swagger) | [Ver documento local](http://localhost:5164/openapi/v1.json) |

En Swagger, abre una operación y selecciona **Try it out** para enviar una petición. En el `PUT`, elige `application/json` para cambiar los datos o `multipart/form-data` para adjuntar un video.

## Ejecutar en local

1. Instala el SDK de .NET 10 y ten a mano una cuenta de Cloudinary y una base de datos SQL Server accesible desde tu equipo.
2. Ejecuta [`sql/contactos.sql`](sql/contactos.sql) en la base de datos que usará la API. El script crea `dbo.contactos`. La tabla se administra con ese script.
3. Si acabas de clonar el repositorio, crea `ExamenP2_PM1_Grupo3/Properties/launchSettings.json` con el perfil de abajo. Si ya tienes el archivo, completa las variables del perfil `http` que vas a usar. Git y Docker excluyen este archivo local.

   ```json
   {
     "profiles": {
       "http": {
         "commandName": "Project",
         "applicationUrl": "http://localhost:5164",
         "environmentVariables": {
           "ASPNETCORE_ENVIRONMENT": "Development",
           "CLOUDINARY_URL": "cloudinary://API_KEY:API_SECRET@CLOUD_NAME",
           "ConnectionStrings__Contactos": "Server=tcp:SERVIDOR,1433;Database=BASE;User Id=USUARIO;Password=CONTRASEÑA;Encrypt=True;"
         }
       }
     }
   }
   ```

4. Desde la raíz del repositorio, inicia la API.

   ```powershell
   dotnet run --project ExamenP2_PM1_Grupo3 --launch-profile http
   ```

## Endpoints

| Método y ruta | Entrada | Respuesta exitosa |
| --- | --- | --- |
| `GET /api/contactos` | `buscar` es opcional. Busca coincidencias parciales en el nombre o teléfono. Si se omite o está vacío, lista todos. | `200 OK` con un arreglo ordenado por ID descendente. Devuelve `[]` si no hay coincidencias. |
| `POST /api/contactos` | `multipart/form-data` con `nombre`, `telefono`, `latitud`, `longitud` y `archivo`. Todos son obligatorios. | `201 Created` con el contacto guardado. |
| `GET /api/contactos/{id}/archivo` | ID entero del contacto. | `302 Found` con la URL de Cloudinary en el encabezado `Location`. |
| `PUT /api/contactos/{id}` | `application/json` o `multipart/form-data`. Los cuatro campos del contacto son obligatorios. En multipart, `archivo` es opcional. Si se incluye, reemplaza el video. | `200 OK` con el contacto actualizado. |
| `PATCH /api/contactos/{id}` | `application/json` con al menos uno de estos campos: `nombre`, `telefono`, `latitud` o `longitud`. Los campos omitidos y el video se conservan. | `200 OK` con el contacto actualizado. |
| `DELETE /api/contactos/{id}` | ID entero del contacto. | `204 No Content`. Elimina el registro de SQL Server. |

Las rutas por ID devuelven `404 Not Found` si el contacto no existe. Al reemplazar el video o eliminar un contacto, el archivo anterior permanece en Cloudinary.

En una petición multipart, `archivo` contiene el binario del video. En el JSON de respuesta, `archivo` contiene la URL guardada en la columna `VideoUrl` de SQL Server.

```json
{
  "id": 1,
  "nombre": "Ana López",
  "telefono": "012345678",
  "latitud": 14.48,
  "longitud": -88.78,
  "archivo": "https://res.cloudinary.com/mi-cuenta/video/upload/contactos/ejemplo.mp4"
}
```

## Ejemplos en PowerShell

Usa la URL local para probar con tu propia base de datos. Sustituye `1` por el ID del contacto que quieras editar.

```powershell
$api = 'http://localhost:5164'

# Buscar contactos
Invoke-RestMethod "$api/api/contactos?buscar=Ana"

# Crear un contacto con un video que esté en el directorio actual
curl.exe -X POST "$api/api/contactos" -F "nombre=Ana López" -F "telefono=012345678" -F "latitud=14.48" -F "longitud=-88.78" -F "archivo=@video.mp4;type=video/mp4"

# Actualizar todos los datos y conservar el video
$datos = @{ nombre = 'Ana López'; telefono = '012345678'; latitud = 14.48; longitud = -88.78 }
Invoke-RestMethod -Method Put -Uri "$api/api/contactos/1" -ContentType 'application/json' -Body ($datos | ConvertTo-Json)

# Cambiar solo el teléfono
$cambio = @{ telefono = '012345679' }
Invoke-RestMethod -Method Patch -Uri "$api/api/contactos/1" -ContentType 'application/json' -Body ($cambio | ConvertTo-Json)
```

## Restricciones y errores

- `nombre` admite hasta **150 caracteres** y `telefono` hasta **30**. Envía el teléfono como texto para conservar ceros iniciales.
- `latitud` admite valores de **−90 a 90** y `longitud` de **−180 a 180**, incluidos los extremos. Usa punto decimal en multipart. SQL Server guarda hasta siete decimales.
- `PATCH` devuelve `400` si el objeto está vacío, contiene un campo desconocido, incluye `null` o intenta editar `archivo`.
- El video debe tener contenido y pesar como máximo **20 MB (20 000 000 bytes)**. Si está vacío o supera el límite, la API devuelve `400 Bad Request`.
- La API valida el MIME y la firma del archivo. Admite MP4 (`video/mp4`), 3GP (`video/3gpp`), WebM (`video/webm`), MOV (`video/quicktime`), MKV (`video/matroska` o `video/x-matroska`) y MPEG-TS (`video/mp2t`). También admite `application/octet-stream` si reconoce la firma de un formato admitido. Un MIME o una firma incompatibles devuelven `415 Unsupported Media Type`.
- La app debe mostrar `mensaje` cuando aparezca en el JSON de error de carga. Para otros errores de validación, consulta `errors` en la respuesta de ASP.NET Core.

```json
{
  "mensaje": "El archivo debe pesar como máximo 20 MB."
}
```

## Desplegar en Cloud Run

1. Configura el despliegue desde la rama `main` con el [`Dockerfile`](Dockerfile) de la raíz.
2. En **Contenedores → Variables y secretos**, configura `CLOUDINARY_URL` y `ConnectionStrings__Contactos` para el servicio. Guarda los valores reales en la configuración del despliegue y en tu perfil local cuando lo necesites.
3. Comprueba que Cloud Run puede conectarse a SQL Server y que `dbo.contactos` ya existe en la base indicada. El contenedor escucha en el puerto que Cloud Run inyecta en `PORT`. Tras desplegar, abre el Swagger de producción.

## Estructura del código

| Proyecto | Responsabilidad |
| --- | --- |
| `ExamenP2_PM1_Grupo3` | Controladores, validación HTTP, configuración y Swagger. |
| `ExamenP2_PM1_Grupo3.Application` | Casos de uso, interfaces de persistencia y almacenamiento, y DTO de respuesta. |
| `ExamenP2_PM1_Grupo3.Domain` | Modelo `Contacto` y límites de sus datos. |
| `ExamenP2_PM1_Grupo3.Infrastructure` | Repositorio SQL Server y almacenamiento en Cloudinary. |
