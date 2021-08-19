FROM mcr.microsoft.com/dotnet/core/aspnet:3.1-buster-slim AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/core/sdk:3.1-buster AS build
WORKDIR /src
COPY ["src/Keda.CosmosDB.Scaler.csproj", "src/"]
RUN dotnet restore "src/Keda.CosmosDB.Scaler.csproj"
COPY . .
WORKDIR "/src/"
RUN dotnet publish "src/Keda.CosmosDB.Scaler.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Keda.CosmosDB.Scaler.dll"]