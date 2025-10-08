# Etapa de build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copiar csproj y restaurar dependencias
COPY AS.NotificationService.csproj ./
RUN dotnet restore AS.NotificationService.csproj

# Copiar todo el código y compilar
COPY . .
RUN dotnet publish AS.NotificationService.csproj -c Release -o /app

# Etapa de runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "AS.NotificationService.dll"]
