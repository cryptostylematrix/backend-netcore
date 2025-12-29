# syntax=docker/dockerfile:1

# ===============================
# Build stage (.NET 10 SDK)
# ===============================
FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
WORKDIR /src

COPY . .

RUN dotnet restore src/API/CryptoStyle.Api/CryptoStyle.Api.csproj
RUN dotnet publish src/API/CryptoStyle.Api/CryptoStyle.Api.csproj \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false

# ===============================
# Runtime stage (.NET 10 ASP.NET)
# ===============================
FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview AS runtime
WORKDIR /app

# App listens on 8080 INSIDE container
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "CryptoStyle.Api.dll"]