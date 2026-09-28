FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/API/CryptoStyle.Api/CryptoStyle.Api.csproj", "src/API/CryptoStyle.Api/"]
COPY ["src/BuildingBlocks/Common/Common.csproj", "src/BuildingBlocks/Common/"]
COPY ["src/BuildingBlocks/IntegrationEvents/IntegrationEvents.csproj", "src/BuildingBlocks/IntegrationEvents/"]
COPY ["src/BuildingBlocks/IntegrationRequests/IntegrationRequests.csproj", "src/BuildingBlocks/IntegrationRequests/"]
COPY ["src/BuildingBlocks/MessageBroker/MessageBroker.csproj", "src/BuildingBlocks/MessageBroker/"]
COPY ["src/Libs/TonSdk.Client/TonSdk.Client.csproj", "src/Libs/TonSdk.Client/"]
COPY ["src/Libs/TonSdk.Core/TonSdk.Core.csproj", "src/Libs/TonSdk.Core/"]
COPY ["src/Modules/Contracts/Contracts.Application/Contracts.Application.csproj", "src/Modules/Contracts/Contracts.Application/"]
COPY ["src/Modules/Contracts/Contracts.Dto/Contracts.Dto.csproj", "src/Modules/Contracts/Contracts.Dto/"]
COPY ["src/Modules/Contracts/Contracts.Infrastructure/Contracts.Infrastructure.csproj", "src/Modules/Contracts/Contracts.Infrastructure/"]
COPY ["src/Modules/Contracts/Contracts.Presentation/Contracts.Presentation.csproj", "src/Modules/Contracts/Contracts.Presentation/"]
COPY ["src/Modules/ReferalProgram/ReferalProgram.Application/ReferalProgram.Application.csproj", "src/Modules/ReferalProgram/ReferalProgram.Application/"]
COPY ["src/Modules/ReferalProgram/ReferalProgram.Core/ReferalProgram.Core.csproj", "src/Modules/ReferalProgram/ReferalProgram.Core/"]
COPY ["src/Modules/ReferalProgram/ReferalProgram.Dto/ReferalProgram.Dto.csproj", "src/Modules/ReferalProgram/ReferalProgram.Dto/"]
COPY ["src/Modules/ReferalProgram/ReferalProgram.Infrastructure/ReferalProgram.Infrastructure.csproj", "src/Modules/ReferalProgram/ReferalProgram.Infrastructure/"]
COPY ["src/Modules/ReferalProgram/ReferalProgram.Presentation/ReferalProgram.Presentation.csproj", "src/Modules/ReferalProgram/ReferalProgram.Presentation/"]
COPY ["src/Modules/ScheduledTasks/ScheduledTasks.Application/ScheduledTasks.Application.csproj", "src/Modules/ScheduledTasks/ScheduledTasks.Application/"]
COPY ["src/Modules/ScheduledTasks/ScheduledTasks.Core/ScheduledTasks.Core.csproj", "src/Modules/ScheduledTasks/ScheduledTasks.Core/"]
COPY ["src/Modules/ScheduledTasks/ScheduledTasks.Infrastructure/ScheduledTasks.Infrastructure.csproj", "src/Modules/ScheduledTasks/ScheduledTasks.Infrastructure/"]
COPY ["src/Modules/UI/UI.Application/UI.Application.csproj", "src/Modules/UI/UI.Application/"]
COPY ["src/Modules/UI/UI.Core/UI.Core.csproj", "src/Modules/UI/UI.Core/"]
COPY ["src/Modules/UI/UI.Dto/UI.Dto.csproj", "src/Modules/UI/UI.Dto/"]
COPY ["src/Modules/UI/UI.Infrastructure/UI.Infrastructure.csproj", "src/Modules/UI/UI.Infrastructure/"]
COPY ["src/Modules/UI/UI.Presentation/UI.Presentation.csproj", "src/Modules/UI/UI.Presentation/"]
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
