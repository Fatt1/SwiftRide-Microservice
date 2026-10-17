FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy CPM and build properties
COPY Directory.Build.props Directory.Packages.props ./

# Copy csproj files for Identity Service
COPY src/Services/Identity/Identity.API/Identity.API.csproj src/Services/Identity/Identity.API/

# Copy csproj files for BuildingBlocks
COPY src/BuildingBlocks/Common.Logging/Common.Logging.csproj src/BuildingBlocks/Common.Logging/
COPY src/BuildingBlocks/Contracts/Contracts.csproj src/BuildingBlocks/Contracts/
COPY src/BuildingBlocks/Shared/Shared.csproj src/BuildingBlocks/Shared/
COPY src/BuildingBlocks/EventBus.Messages/EventBus.Messages.csproj src/BuildingBlocks/EventBus.Messages/
COPY src/BuildingBlocks/Infrastructure/Infrastructure.csproj src/BuildingBlocks/Infrastructure/

# Restore dependencies
RUN dotnet restore src/Services/Identity/Identity.API/Identity.API.csproj

# Copy full source code
COPY src/BuildingBlocks/ src/BuildingBlocks/
COPY src/Services/Identity/ src/Services/Identity/

# Build and publish
RUN dotnet publish src/Services/Identity/Identity.API/Identity.API.csproj -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=5000
EXPOSE 5000
USER $APP_UID
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Identity.API.dll"]
