# https://hub.docker.com/_/microsoft-dotnet

# Restore, build and publish project.
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /
COPY src/Scaler/ src/Scaler/

WORKDIR /src/Scaler
RUN dotnet publish --configuration Release --output /app

# Stage application.
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "Keda.CosmosDb.Scaler.dll"]
