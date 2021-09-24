# https://hub.docker.com/_/microsoft-dotnet

# Restore, build and publish project.
FROM mcr.microsoft.com/dotnet/sdk:3.1 AS build
WORKDIR /source
COPY Scaler.Demo/OrderProcessor/ Scaler.Demo/OrderProcessor/
COPY Scaler.Demo/Shared/ Scaler.Demo/Shared/

WORKDIR /source/Scaler.Demo/OrderProcessor
RUN dotnet publish --configuration Release --output /app

# Stage application.
FROM mcr.microsoft.com/dotnet/aspnet:3.1
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "Keda.CosmosDb.Scaler.Demo.OrderProcessor.dll"]
