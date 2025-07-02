# Qutora Document Management System

![Docker Pulls](https://img.shields.io/docker/pulls/qutora/qutora-api)
![Docker Image Size](https://img.shields.io/docker/image-size/qutora/qutora-api)
![Docker Image Version](https://img.shields.io/docker/v/qutora/qutora-api)

**Qutora** is a comprehensive, enterprise-grade document management system built with .NET 9 and designed for scalability, security, and multi-database support.

## 🚀 Quick Start

> **🚨 CRITICAL WARNING**: Always use persistent volumes for `/app/keys` in production! Without persistent volumes, **ALL ENCRYPTED DATA WILL BE LOST** when the container is restarted. This includes user passwords, sensitive documents, and other encrypted information.

```bash
# 1. Download the environment template
curl -o .env https://raw.githubusercontent.com/qutora/qutora-api/main/env.example

# 2. Edit .env file with your configuration
nano .env

# 3. Start with SQL Server
docker run -d \
  --name qutora-api \
  --env-file .env \
  -p 8080:8080 \
  qutora/qutora-api:latest
```

## 📋 Features

- **Multi-Database Support**: SQL Server, PostgreSQL, MySQL
- **Modular Storage**: FileSystem, MinIO, FTP, SFTP
- **Enterprise Security**: JWT authentication, role-based access
- **Document Approval Workflow**: Built-in approval system
- **API-First Design**: RESTful API with comprehensive documentation
- **Health Monitoring**: Built-in health checks and monitoring
- **Docker Ready**: Production-ready containerization

## 🛠 Supported Databases

| Database | Version | Status |
|----------|---------|--------|
| SQL Server | 2019+ | ✅ Fully Supported |
| PostgreSQL | 12+ | ✅ Fully Supported |
| MySQL | 8.0+ | ✅ Fully Supported |

## 🐳 Docker Compose Examples

### SQL Server Setup
```yaml
version: '3.8'
services:
  qutora-api:
    image: qutora/qutora-api:latest
    environment:
      - Database__Provider=SqlServer
      - ConnectionStrings__DefaultConnection=Server=sqlserver;Database=QutoraDB;User Id=sa;Password=YourPassword123!;TrustServerCertificate=true;
    ports:
      - "8080:8080"
    depends_on:
      - sqlserver
      
  sqlserver:
    image: mcr.microsoft.com/mssql/server:2022-latest
    environment:
      - ACCEPT_EULA=Y
      - SA_PASSWORD=YourPassword123!
    ports:
      - "1433:1433"
```

### PostgreSQL Setup
```yaml
version: '3.8'
services:
  qutora-api:
    image: qutora/qutora-api:latest
    environment:
      - Database__Provider=PostgreSQL
      - ConnectionStrings__DefaultConnection=Host=postgresql;Database=qutora_db;Username=qutora_user;Password=YourPassword123;
    ports:
      - "8080:8080"
    depends_on:
      - postgresql
      
  postgresql:
    image: postgres:16-alpine
    environment:
      - POSTGRES_DB=qutora_db
      - POSTGRES_USER=qutora_user
      - POSTGRES_PASSWORD=YourPassword123
    ports:
      - "5432:5432"
```

### MySQL Setup
```yaml
version: '3.8'
services:
  qutora-api:
    image: qutora/qutora-api:latest
    environment:
      - Database__Provider=MySQL
      - ConnectionStrings__DefaultConnection=Server=mysql;Database=qutora_db;Uid=qutora_user;Pwd=YourPassword123;
    ports:
      - "8080:8080"
    depends_on:
      - mysql
      
  mysql:
    image: mysql:8.0
    environment:
      - MYSQL_DATABASE=qutora_db
      - MYSQL_USER=qutora_user
      - MYSQL_PASSWORD=YourPassword123
      - MYSQL_ROOT_PASSWORD=RootPassword123
    ports:
      - "3306:3306"
```

## ⚙️ Environment Variables

### Required Configuration
| Variable | Description | Example |
|----------|-------------|---------|
| `DATABASE_PROVIDER` | Database type | `SqlServer`, `PostgreSQL`, `MySQL` |
| `ConnectionStrings__DefaultConnection` | Database connection string | See examples above |
| `Jwt__Key` | JWT secret key (32+ chars) | `your-secure-jwt-key-here` |

### Optional Configuration
| Variable | Description | Default |
|----------|-------------|---------|
| `ASPNETCORE_ENVIRONMENT` | Environment | `Production` |
| `Storage__DefaultProvider` | Storage provider | `filesystem` |
| `Logging__LogLevel__Default` | Log level | `Information` |
| `AllowedOrigins__0` | CORS origin | `https://localhost:7001` |

## 🔒 Security Configuration

### JWT Configuration (Required)
```bash
# Generate a secure JWT key
JWT_SECRET_KEY=$(openssl rand -base64 32)

# Set in environment
export Jwt__Key="$JWT_SECRET_KEY"
export Jwt__Issuer="YourCompanyAPI"
export Jwt__Audience="YourCompanyAPI"
```

### CORS Configuration
```bash
# Allow your frontend domains
export AllowedOrigins__0="https://your-app.com"
export AllowedOrigins__1="https://admin.your-app.com"
```

## 📁 Storage Providers

### FileSystem (Default)
```bash
export Storage__DefaultProvider="filesystem"
export Storage__FileSystem__BasePath="/app/data"
```

### MinIO S3-Compatible
```bash
export Storage__DefaultProvider="minio"
export Storage__MinIO__Endpoint="your-minio-server:9000"
export Storage__MinIO__AccessKey="your-access-key"
export Storage__MinIO__SecretKey="your-secret-key"
```

### FTP/SFTP
```bash
export Storage__DefaultProvider="ftp"  # or "sftp"
export Storage__FTP__Host="your-ftp-server"
export Storage__FTP__Username="username"
export Storage__FTP__Password="password"
```

## 🏥 Health Checks

The application includes comprehensive health checks:

```bash
# Check application health
curl http://localhost:8080/health

# Check database connectivity
curl http://localhost:8080/health/database

# Check storage provider
curl http://localhost:8080/health/storage
```

## 📊 Monitoring

### Built-in Endpoints
- `/health` - Application health status
- `/health/ready` - Readiness probe
- `/health/live` - Liveness probe
- `/metrics` - Prometheus metrics (if enabled)

### Docker Health Check
```dockerfile
HEALTHCHECK --interval=30s --timeout=3s --start-period=10s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1
```

## 🔧 Advanced Configuration

### Data Protection Keys Management

Qutora automatically detects and adapts to different key management scenarios:

#### 1. Docker Volume (Recommended for Production)
```bash
# Keys persist across container restarts
docker run -d --name qutora-api \
  -v qutora_keys:/app/keys \
  qutora/qutora-api:latest
```

#### 2. Kubernetes Secrets
```yaml
apiVersion: v1
kind: Secret
metadata:
  name: qutora-dataprotection-keys
data:
  key-xxx.xml: <base64-encoded-key>
---
# Mount as read-only volume
volumeMounts:
- name: dataprotection-keys
  mountPath: /app/keys
  readOnly: true
```

#### 3. Internal Generation (Development)
```bash
# No volume mount - keys generated internally
docker run -d --name qutora-api qutora/qutora-api:latest
```

### Custom Storage Provider
Create your own storage provider by implementing `IStorageProviderAdapter`:

```csharp
[ProviderType("custom")]
public class CustomStorageProvider : BaseStorageProvider
{
    // Implementation
}
```

### Database Migrations
The application automatically handles database migrations on startup:

1. **Migration-First**: Attempts EF Core migrations
2. **Fallback**: Uses `EnsureCreated()` if migrations fail
3. **Logging**: Detailed migration logs for troubleshooting

## 🐛 Troubleshooting

### Common Issues

1. **Database Connection Failed**
   ```bash
   # Check connection string format
   # SQL Server: Server=host;Database=db;User Id=user;Password=pass;TrustServerCertificate=true;
   # PostgreSQL: Host=host;Database=db;Username=user;Password=pass;
   # MySQL: Server=host;Database=db;Uid=user;Pwd=pass;
   ```

2. **JWT Authentication Failed**
   ```bash
   # Ensure JWT key is at least 32 characters
   export Jwt__Key="your-very-long-and-secure-jwt-secret-key-here"
   ```

3. **CORS Issues**
   ```bash
   # Add your frontend URL to allowed origins
   export AllowedOrigins__0="https://your-frontend.com"
   ```

### Debug Mode
```bash
# Enable debug logging
docker run -e Logging__LogLevel__Default=Debug qutora/qutora-api:latest
```

## 📖 Documentation

- [API Documentation](https://qutora.io/docs/api)
- [Configuration Guide](https://qutora.io/docs/configuration)
- [Deployment Guide](https://qutora.io/docs/deployment)
- [Custom Providers](https://qutora.io/docs/custom-providers)

## 🤝 Support

- **GitHub Issues**: [Report bugs and request features](https://github.com/qutora/qutora-api/issues)
- **Discussions**: [Community support](https://github.com/qutora/qutora-api/discussions)
- **Email Support**: [info@qutora.io](mailto:info@qutora.io)
- **Documentation**: [qutora.io](https://qutora.io)

## 📄 License

This project is licensed under the **MIT License** - see the [LICENSE](https://github.com/qutora/qutora-api/blob/main/LICENSE) file for details.

This is an open-source project. You are free to use, modify, and distribute this software under the terms of the MIT License.

## 🏷️ Tags

`document-management` `enterprise` `dotnet` `docker` `multi-database` `api` `workflow` `storage` `security` `commercial-license`

---

**Built with ❤️ using .NET 9, Entity Framework Core, and Docker** 