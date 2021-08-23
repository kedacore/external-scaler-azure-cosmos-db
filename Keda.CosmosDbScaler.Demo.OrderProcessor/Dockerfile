# https://hub.docker.com/_/microsoft-dotnet

# Restore, build and publish project.
FROM mcr.microsoft.com/dotnet/sdk:3.1 AS build
WORKDIR /source
COPY Keda.CosmosDbScaler.Demo.OrderProcessor/ Keda.CosmosDbScaler.Demo.OrderProcessor/
COPY Keda.CosmosDbScaler.Demo.Shared/ Keda.CosmosDbScaler.Demo.Shared/

WORKDIR /source/Keda.CosmosDbScaler.Demo.OrderProcessor
RUN dotnet publish --configuration Release --output /app

# Stage application.
FROM mcr.microsoft.com/dotnet/aspnet:3.1
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "Keda.CosmosDbScaler.Demo.OrderProcessor.dll"]
