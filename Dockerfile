FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/API/CryptoStyle.Api/CryptoStyle.Api.csproj", "src/API/CryptoStyle.Api/"]
COPY ["src/Modules/Contracts/Contracts.Infrastructure/Contracts.Infrastructure.csproj", "src/Modules/Contracts/Contracts.Infrastructure/"]
COPY ["src/Modules/Contracts/Contracts.Application/Contracts.Application.csproj", "src/Modules/Contracts/Contracts.Application/"]
COPY ["src/BuildingBlocks/Common/Common.csproj", "src/BuildingBlocks/Common/"]
COPY ["src/Modules/Contracts/Contracts.Dto/Contracts.Dto.csproj", "src/Modules/Contracts/Contracts.Dto/"]
COPY ["src/Modules/Contracts/Contracts.Presentation/Contracts.Presentation.csproj", "src/Modules/Contracts/Contracts.Presentation/"]
COPY ["src/Libs/TonSdk.Core/TonSdk.Core.csproj", "src/Libs/TonSdk.Core/"]
COPY ["src/Libs/TonSdk.Client/TonSdk.Client.csproj", "src/Libs/TonSdk.Client/"]
COPY ["src/Modules/Matrix/Matrix.Infrastructure/Matrix.Infrastructure.csproj", "src/Modules/Matrix/Matrix.Infrastructure/"]
COPY ["src/Modules/Matrix/Matrix.Application/Matrix.Application.csproj", "src/Modules/Matrix/Matrix.Application/"]
COPY ["src/Modules/Matrix/Matrix.Dto/Matrix.Dto.csproj", "src/Modules/Matrix/Matrix.Dto/"]
COPY ["src/Modules/Matrix/Matrix.Presentation/Matrix.Presentation.csproj", "src/Modules/Matrix/Matrix.Presentation/"]
RUN dotnet restore "src/API/CryptoStyle.Api/CryptoStyle.Api.csproj"
COPY . .
WORKDIR "/src/src/API/CryptoStyle.Api"
RUN dotnet build "./CryptoStyle.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./CryptoStyle.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CryptoStyle.Api.dll"]
