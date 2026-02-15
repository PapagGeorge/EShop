FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY src/BuildingBlocks/EShop.Shared/EShop.Shared.csproj src/BuildingBlocks/EShop.Shared/
COPY src/Services/Ordering/EShop.Ordering.Domain/EShop.Ordering.Domain.csproj src/Services/Ordering/EShop.Ordering.Domain/
COPY src/Services/Ordering/EShop.Ordering.Application/EShop.Ordering.Application.csproj src/Services/Ordering/EShop.Ordering.Application/
COPY src/Services/Ordering/EShop.Ordering.Infrastructure/EShop.Ordering.Infrastructure.csproj src/Services/Ordering/EShop.Ordering.Infrastructure/
COPY src/Services/Ordering/EShop.Ordering.API/EShop.Ordering.API.csproj src/Services/Ordering/EShop.Ordering.API/
RUN dotnet restore src/Services/Ordering/EShop.Ordering.API/EShop.Ordering.API.csproj

COPY src/BuildingBlocks/EShop.Shared/ src/BuildingBlocks/EShop.Shared/
COPY src/Services/Ordering/ src/Services/Ordering/
RUN dotnet publish src/Services/Ordering/EShop.Ordering.API/EShop.Ordering.API.csproj -c Release -o /app/publish --no-restore

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "EShop.Ordering.API.dll"]
