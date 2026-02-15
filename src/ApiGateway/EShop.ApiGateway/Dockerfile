FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY src/BuildingBlocks/EShop.Shared/EShop.Shared.csproj src/BuildingBlocks/EShop.Shared/
COPY src/ApiGateway/EShop.ApiGateway/EShop.ApiGateway.csproj src/ApiGateway/EShop.ApiGateway/
RUN dotnet restore src/ApiGateway/EShop.ApiGateway/EShop.ApiGateway.csproj

COPY src/BuildingBlocks/EShop.Shared/ src/BuildingBlocks/EShop.Shared/
COPY src/ApiGateway/ src/ApiGateway/
RUN dotnet publish src/ApiGateway/EShop.ApiGateway/EShop.ApiGateway.csproj -c Release -o /app/publish --no-restore

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "EShop.ApiGateway.dll"]
