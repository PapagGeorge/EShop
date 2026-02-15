FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY src/BuildingBlocks/EShop.Shared/EShop.Shared.csproj src/BuildingBlocks/EShop.Shared/
COPY src/Services/Catalog/EShop.Catalog.API/EShop.Catalog.API.csproj src/Services/Catalog/EShop.Catalog.API/
RUN dotnet restore src/Services/Catalog/EShop.Catalog.API/EShop.Catalog.API.csproj

COPY src/BuildingBlocks/EShop.Shared/ src/BuildingBlocks/EShop.Shared/
COPY src/Services/Catalog/ src/Services/Catalog/
RUN dotnet publish src/Services/Catalog/EShop.Catalog.API/EShop.Catalog.API.csproj -c Release -o /app/publish --no-restore

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "EShop.Catalog.API.dll"]
