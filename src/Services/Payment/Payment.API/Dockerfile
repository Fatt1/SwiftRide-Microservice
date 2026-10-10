FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY Directory.Build.props Directory.Packages.props ./
COPY src/Services/Payment/Payment.API/Payment.API.csproj src/Services/Payment/Payment.API/
COPY src/Services/Payment/Payment.Application/Payment.Application.csproj src/Services/Payment/Payment.Application/
COPY src/Services/Payment/Payment.Domain/Payment.Domain.csproj src/Services/Payment/Payment.Domain/
COPY src/Services/Payment/Payment.Infrastructure/Payment.Infrastructure.csproj src/Services/Payment/Payment.Infrastructure/
COPY src/BuildingBlocks/Common.Logging/Common.Logging.csproj src/BuildingBlocks/Common.Logging/
COPY src/BuildingBlocks/Contracts/Contracts.csproj src/BuildingBlocks/Contracts/
COPY src/BuildingBlocks/Shared/Shared.csproj src/BuildingBlocks/Shared/
COPY src/BuildingBlocks/EventBus.Messages/EventBus.Messages.csproj src/BuildingBlocks/EventBus.Messages/
COPY src/BuildingBlocks/Infrastructure/Infrastructure.csproj src/BuildingBlocks/Infrastructure/
RUN dotnet restore src/Services/Payment/Payment.API/Payment.API.csproj
COPY src/BuildingBlocks/ src/BuildingBlocks/
COPY src/Services/Payment/ src/Services/Payment/
RUN dotnet publish src/Services/Payment/Payment.API/Payment.API.csproj -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=5000
EXPOSE 5000
USER $APP_UID
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Payment.API.dll"]
