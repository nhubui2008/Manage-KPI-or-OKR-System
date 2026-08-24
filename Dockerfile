# syntax=docker/dockerfile:1

# Stage 1: Build & Publish
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project definition and restore dependencies to optimize layer caching
COPY ["Manage-KPI-or-OKR-System.csproj", "./"]
RUN dotnet restore "Manage-KPI-or-OKR-System.csproj"

# Copy full source and publish
COPY . .
RUN dotnet publish "Manage-KPI-or-OKR-System.csproj" \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Environment defaults
ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_HTTP_PORTS=8080 \
    DOTNET_RUNNING_IN_CONTAINER=true

# Create directory for Data Protection keys
RUN mkdir -p /app/App_Data/DataProtection-Keys

# Copy published files from build stage
COPY --from=build /app/publish .

# Expose web port
EXPOSE 8080

ENTRYPOINT ["dotnet", "Manage-KPI-or-OKR-System.dll"]
