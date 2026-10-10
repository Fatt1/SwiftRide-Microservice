# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 5001
EXPOSE 5002

# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy Directory props for Central Package Management (CPM) and build properties
COPY ["Directory.Build.props", "./"]
COPY ["Directory.Packages.props", "./"]

# Copy Clean Architecture layers of Matching Service
COPY ["src/Services/Matching/Matching.API/Matching.API.csproj", "src/Services/Matching/Matching.API/"]
COPY ["src/Services/Matching/Matching.Application/Matching.Application.csproj", "src/Services/Matching/Matching.Application/"]
COPY ["src/Services/Matching/Matching.Domain/Matching.Domain.csproj", "src/Services/Matching/Matching.Domain/"]
COPY ["src/Services/Matching/Matching.Infrastructure/Matching.Infrastructure.csproj", "src/Services/Matching/Matching.Infrastructure/"]

# Copy BuildingBlocks projects
COPY ["src/BuildingBlocks/Common.Logging/Common.Logging.csproj", "src/BuildingBlocks/Common.Logging/"]
COPY ["src/BuildingBlocks/Contracts/Contracts.csproj", "src/BuildingBlocks/Contracts/"]
COPY ["src/BuildingBlocks/EventBus.Messages/EventBus.Messages.csproj", "src/BuildingBlocks/EventBus.Messages/"]
COPY ["src/BuildingBlocks/Infrastructure/Infrastructure.csproj", "src/BuildingBlocks/Infrastructure/"]
COPY ["src/BuildingBlocks/Shared/Shared.csproj", "src/BuildingBlocks/Shared/"]

RUN dotnet restore "./src/Services/Matching/Matching.API/Matching.API.csproj"
COPY . .
WORKDIR "/src/src/Services/Matching/Matching.API"
RUN dotnet build "./Matching.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Matching.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Matching.API.dll"]