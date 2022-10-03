# https://hub.docker.com/_/microsoft-dotnet

# Restore, build and publish project.
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /
COPY src/Scaler.Demo/OrderGenerator/ src/Scaler.Demo/OrderGenerator/
COPY src/Scaler.Demo/Shared/ src/Scaler.Demo/Shared/

WORKDIR /src/Scaler.Demo/OrderGenerator
RUN dotnet publish --configuration Release --output /app

# Stage application.
FROM mcr.microsoft.com/dotnet/runtime:6.0
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "Keda.CosmosDb.Scaler.Demo.OrderGenerator.dll"]
