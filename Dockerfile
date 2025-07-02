# =============================================================================
# Qutora Document Management System - Multi-Database Docker Image
# Supports: SQL Server, PostgreSQL, MySQL
# =============================================================================

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

# Set build arguments
ARG BUILDPLATFORM
ARG TARGETPLATFORM
ARG VERSION=latest
ARG BUILD_DATE
ARG VCS_REF

# Add metadata labels
LABEL org.opencontainers.image.title="Qutora Document Management System"
LABEL org.opencontainers.image.description="Enterprise document management system with multi-database support"
LABEL org.opencontainers.image.version="${VERSION}"
LABEL org.opencontainers.image.created="${BUILD_DATE}"
LABEL org.opencontainers.image.revision="${VCS_REF}"
LABEL org.opencontainers.image.vendor="Qutora"
LABEL org.opencontainers.image.licenses="MIT"
LABEL org.opencontainers.image.source="https://github.com/qutora/qutora-api"
LABEL org.opencontainers.image.documentation="https://github.com/qutora/qutora-api/blob/main/README.md"

WORKDIR /src

# Copy project files for dependency restoration (layer caching optimization)
COPY ["Qutora.API/Qutora.API.csproj", "Qutora.API/"]
COPY ["Qutora.Application/Qutora.Application.csproj", "Qutora.Application/"]
COPY ["Qutora.Domain/Qutora.Domain.csproj", "Qutora.Domain/"]
COPY ["Qutora.Infrastructure/Qutora.Infrastructure.csproj", "Qutora.Infrastructure/"]
COPY ["Qutora.Shared/Qutora.Shared.csproj", "Qutora.Shared/"]
COPY ["Qutora.Database.Abstractions/Qutora.Database.Abstractions.csproj", "Qutora.Database.Abstractions/"]
COPY ["Qutora.Database.SqlServer/Qutora.Database.SqlServer.csproj", "Qutora.Database.SqlServer/"]
COPY ["Qutora.Database.PostgreSQL/Qutora.Database.PostgreSQL.csproj", "Qutora.Database.PostgreSQL/"]
COPY ["Qutora.Database.MySQL/Qutora.Database.MySQL.csproj", "Qutora.Database.MySQL/"]

# Restore dependencies (cached layer if project files haven't changed)
RUN dotnet restore "Qutora.API/Qutora.API.csproj" --runtime linux-x64

# Copy source code
COPY . .

# Build application
WORKDIR "/src/Qutora.API"
RUN dotnet build "Qutora.API.csproj" -c Release -o /app/build --no-restore --runtime linux-x64

# Publish stage
FROM build AS publish
RUN dotnet publish "Qutora.API.csproj" \
    -c Release \
    -o /app/publish \
    --no-restore \
    --runtime linux-x64 \
    --self-contained false \
    /p:UseAppHost=false \
    /p:PublishTrimmed=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final

# Add metadata to final image
LABEL org.opencontainers.image.title="Qutora Document Management System"
LABEL org.opencontainers.image.description="Enterprise document management system with multi-database support"
LABEL org.opencontainers.image.version="${VERSION}"
LABEL org.opencontainers.image.licenses="MIT"

# Install system dependencies
RUN apt-get update && \
    apt-get install -y --no-install-recommends \
        curl \
        ca-certificates \
        tzdata && \
    rm -rf /var/lib/apt/lists/* && \
    apt-get clean

# Set timezone (can be overridden with environment variable)
ENV TZ=UTC

# Create application directory
WORKDIR /app

# Create non-root user for security
RUN groupadd --gid 1001 appuser && \
    useradd --uid 1001 --gid appuser --shell /bin/bash --create-home appuser

# Copy published application
COPY --from=publish /app/publish .

# Create directories and set permissions
RUN mkdir -p /app/data /app/keys /app/logs && \
    chown -R appuser:appuser /app && \
    chmod 700 /app/keys

# Switch to non-root user
USER appuser

# Expose ports
EXPOSE 8080
EXPOSE 8443

# Environment variables
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_HTTP_PORTS=8080
ENV ASPNETCORE_HTTPS_PORTS=8443

# Set entrypoint
ENTRYPOINT ["dotnet", "Qutora.API.dll"] 