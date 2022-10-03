# https://hub.docker.com/_/microsoft-dotnet

# Restore, build and publish project.
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /
COPY src/Scaler.Demo/OrderProcessor/ src/Scaler.Demo/OrderProcessor/
COPY src/Scaler.Demo/Shared/ src/Scaler.Demo/Shared/

WORKDIR /src/Scaler.Demo/OrderProcessor
RUN dotnet publish --configuration Release --output /app

# Stage application.
FROM mcr.microsoft.com/dotnet/runtime:6.0
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "Keda.CosmosDb.Scaler.Demo.OrderProcessor.dll"]
