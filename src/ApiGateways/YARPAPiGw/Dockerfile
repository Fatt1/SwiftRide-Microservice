FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy CPM and build properties
COPY Directory.Build.props Directory.Packages.props ./

# Copy csproj for YARP API Gateway
COPY src/ApiGateways/YARPAPiGw/YARPAPiGw.csproj src/ApiGateways/YARPAPiGw/

# Restore dependencies
RUN dotnet restore src/ApiGateways/YARPAPiGw/YARPAPiGw.csproj

# Copy source code
COPY src/ApiGateways/YARPAPiGw/ src/ApiGateways/YARPAPiGw/

# Build and publish
RUN dotnet publish src/ApiGateways/YARPAPiGw/YARPAPiGw.csproj -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=5000
EXPOSE 5000
USER $APP_UID
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "YARPAPiGw.dll"]
