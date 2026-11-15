FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ExamenP2_PM1_Grupo3/*.csproj ExamenP2_PM1_Grupo3/
COPY ExamenP2_PM1_Grupo3.Domain/*.csproj ExamenP2_PM1_Grupo3.Domain/
COPY ExamenP2_PM1_Grupo3.Application/*.csproj ExamenP2_PM1_Grupo3.Application/
COPY ExamenP2_PM1_Grupo3.Infrastructure/*.csproj ExamenP2_PM1_Grupo3.Infrastructure/
RUN dotnet restore ExamenP2_PM1_Grupo3/ExamenP2_PM1_Grupo3.csproj
COPY . .
RUN dotnet publish ExamenP2_PM1_Grupo3/ExamenP2_PM1_Grupo3.csproj -c Release --no-restore -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "ExamenP2_PM1_Grupo3.dll"]
