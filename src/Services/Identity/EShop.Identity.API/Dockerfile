FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY src/BuildingBlocks/EShop.Shared/EShop.Shared.csproj src/BuildingBlocks/EShop.Shared/
COPY src/Services/Identity/EShop.Identity.Domain/EShop.Identity.Domain.csproj src/Services/Identity/EShop.Identity.Domain/
COPY src/Services/Identity/EShop.Identity.Application/EShop.Identity.Application.csproj src/Services/Identity/EShop.Identity.Application/
COPY src/Services/Identity/EShop.Identity.Infrastructure/EShop.Identity.Infrastructure.csproj src/Services/Identity/EShop.Identity.Infrastructure/
COPY src/Services/Identity/EShop.Identity.API/EShop.Identity.API.csproj src/Services/Identity/EShop.Identity.API/
RUN dotnet restore src/Services/Identity/EShop.Identity.API/EShop.Identity.API.csproj

COPY src/BuildingBlocks/EShop.Shared/ src/BuildingBlocks/EShop.Shared/
COPY src/Services/Identity/ src/Services/Identity/
RUN dotnet publish src/Services/Identity/EShop.Identity.API/EShop.Identity.API.csproj -c Release -o /app/publish --no-restore

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "EShop.Identity.API.dll"]
