FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copia el archivo de solución
COPY ["AS.NotificationService.sln", "./"]

# Copia todos los archivos .csproj
COPY ["Api/*.csproj", "Api/"]
COPY ["Application/*.csproj", "Application/"]
COPY ["Domain/*.csproj", "Domain/"]
COPY ["Infrastructure/*.csproj", "Infrastructure/"]

# Restore de dependencias
RUN dotnet restore "Api/Api.csproj" --disable-parallel --no-cache

# Copia todo el código fuente
COPY . .

# Build del proyecto
WORKDIR "/src/Api"
RUN dotnet build "Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Ajusta el nombre del DLL según tu proyecto Api
ENTRYPOINT ["dotnet", "Api.dll"]