# Etapa base de runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8090
EXPOSE 8091

# Etapa de build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copia apenas o arquivo de projeto primeiro (para cache eficiente)
COPY ["/GestaoCondominio.ControlePortaria.Api/GestaoCondominio.ControlePortaria.Api.csproj", "/GestaoCondominio.ControlePortaria.Api/appsettings.json", "/GestaoCondominio.ControlePortaria.Api/Data/", "./"]
RUN dotnet restore "GestaoCondominio.ControlePortaria.Api.csproj"

# Copia todo o restante do código-fonte
COPY . .

# Compila a aplicação (sem apphost)
RUN dotnet build "GestaoCondominio.ControlePortaria.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build /p:UseAppHost=false

# Publica a aplicação (sem apphost)
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "GestaoCondominio.ControlePortaria.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Imagem final
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENV ASPNETCORE_URLS=http://+:8090
ENTRYPOINT ["dotnet", "GestaoCondominio.ControlePortaria.Api.dll"]