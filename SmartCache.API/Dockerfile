# Base image: ASP.NET runtime (production-ready)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

# Build image: SDK with full tools
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj files and restore dependencies
COPY ["SmartCache.API/SmartCache.API.csproj", "SmartCache.API/"]
COPY ["SmartCache.Application/SmartCache.Application.csproj", "SmartCache.Application/"]
COPY ["SmartCache.Domain/SmartCache.Domain.csproj", "SmartCache.Domain/"]
COPY ["SmartCache.Core/SmartCache.Core.csproj", "SmartCache.Core/"]
COPY ["SmartCache.Infrastructure/SmartCache.Infrastructure.csproj", "SmartCache.Infrastructure/"]
COPY ["SmartCache.Persistence/SmartCache.Persistence.csproj", "SmartCache.Persistence/"]

RUN dotnet restore "SmartCache.API/SmartCache.API.csproj"

# Copy all source files and build
COPY . .
WORKDIR "/src/SmartCache.API"
RUN dotnet build "SmartCache.API.csproj" -c Release -o /app/build

# Publish the app
FROM build AS publish
RUN dotnet publish "SmartCache.API.csproj" -c Release -o /app/publish

# Final image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SmartCache.API.dll"]
