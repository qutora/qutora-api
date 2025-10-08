# Qutora Document Management System

![Docker Pulls](https://img.shields.io/docker/pulls/qutora/qutora-api)
![Docker Image Size](https://img.shields.io/docker/image-size/qutora/qutora-api)
![License](https://img.shields.io/github/license/qutora/qutora-api)
![Build Status](https://img.shields.io/github/actions/workflow/status/qutora/qutora-api/docker-publish.yml)

**Qutora** is a comprehensive, enterprise-grade document management system built with .NET 9 and Clean Architecture principles. It provides a robust REST API that can be integrated into any application requiring document management capabilities.

## What is Qutora?

Qutora is designed for organizations that need secure, scalable document management:

- **Enterprise Document Management**: Secure storage and management of business documents
- **Integration Projects**: Adding document capabilities to existing applications  
- **Compliance Requirements**: Systems needing audit trails and approval workflows
- **API-First Approach**: Teams building custom frontends or mobile applications

## Key Features

- **Multi-Database Support**: SQL Server, PostgreSQL, MySQL
- **Multiple Storage Providers**: File System, MinIO, FTP, SFTP
- **Advanced Authentication**: JWT tokens and API key authentication
- **Document Management**: Upload, versioning, metadata, and categorization
- **Secure Document Sharing**: Time-limited, password-protected sharing
- **Approval Workflows**: Configurable approval processes
- **Audit Logging**: Comprehensive activity tracking
- **Role-Based Access Control**: Fine-grained permissions system
- **RESTful API**: Clean, well-documented endpoints

## Architecture

Built with Clean Architecture principles:

```
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│ Presentation    │────│ Application     │────│ Infrastructure  │
│ Layer           │    │ Layer           │    │ Layer           │
│ (API/UI)        │    │ (Services)      │    │ (Data/Storage)  │
└─────────────────┘    └─────────────────┘    └─────────────────┘
                                │
                                │
                       ┌─────────────────┐
                       │ Domain Layer    │
                       │ (Entities/Rules)│
                       └─────────────────┘
```

### Project Structure

This repository contains the **API backend** and **shared components** of the Qutora ecosystem. The complete Qutora platform consists of multiple applications:

```
Qutora Ecosystem
├── This Repository
│   ├── Qutora.API                          # REST API backend
│   ├── Qutora.Application                  # Business services & interfaces
│   ├── Qutora.Domain                       # Entities & business logic
│   ├── Qutora.Infrastructure               # Data access & technical services
│   ├── Qutora.Shared                       # DTOs, models & common utilities
│   └── Qutora.Database.*                   # Multi-database providers
├── Qutora.UI (Private)                     # Admin web interface  
└── Qutora.PublicViewer (Private)           # Document viewer app
```

**Why this structure?**
- **Qutora.Shared**: Essential shared components (DTOs, enums, models) that all applications depend on
- **Clean Architecture**: Domain-driven design with clear separation of concerns
- **Service Distribution**: Business services in Application layer, technical services in Infrastructure layer
- **Scalability**: Core API can be deployed independently while sharing contracts via Qutora.Shared

**Building Your Own Frontend:**
Since `Qutora.UI` and `Qutora.PublicViewer` are not publicly available, you can:
- Build your own admin interface using the REST API
- Create custom document viewers for your specific needs  
- Reference `Qutora.Shared` DTOs and models for API contracts
- Use Swagger UI for API documentation (available at `/swagger`)
- Leverage existing authentication and authorization patterns

**PublicViewer Development Guide:**
If you're building a document viewer application, consider implementing these proven features:
- **Multi-format Support**: PDF (PDF.js), Images, Text files, Office documents
- **Security Features**: Right-click protection, text selection blocking, watermarks
- **Share Controls**: Password protection, view limits, expiration dates, download permissions
- **User Experience**: Loading states, error handling, mobile responsiveness
- **Advanced Features**: Print control, developer tools blocking, usage analytics

The API provides all necessary endpoints for secure document sharing and access control.

## Tech Stack

- **.NET 9**: Latest framework features
- **Entity Framework Core**: Multi-database ORM
- **ASP.NET Core**: Web API framework
- **JWT Authentication**: Secure token-based auth
- **Serilog**: Structured logging
- **Docker**: Containerization support

## Quick Start with Docker

### Using Docker Compose (Recommended)

```bash
# Clone repository
git clone https://github.com/qutora/qutora-api.git
cd qutora-api/samples

# Copy and configure environment file
cp env.sqlserver.example .env
nano .env  # Edit with your settings

# Start services
docker-compose -f docker-compose.sqlserver.yml --env-file .env up -d
```

**Available Examples:**
- **SQL Server Express** - Ready for testing and development
- **PostgreSQL** - Coming soon (testing in progress)
- **MySQL** - Coming soon (testing in progress)

For detailed setup instructions, see [samples/README.md](samples/README.md).

## Documentation

- **[Docker Compose Examples](samples/README.md)** - Quick start with Docker
- **[Contributing Guide](CONTRIBUTING.md)** - How to contribute

## Database Support

Switch between database providers by setting `Database__Provider`:

| Database   | Provider Value | Connection String Example |
|------------|----------------|---------------------------|
| SQL Server | `SqlServer`    | `Server=localhost;Database=QutoraDB;Integrated Security=true;` |
| PostgreSQL | `PostgreSQL`   | `Host=localhost;Database=qutora_db;Username=qutora_user;Password=pass;` |
| MySQL      | `MySQL`        | `Server=localhost;Database=qutora_db;Uid=qutora_user;Pwd=pass;` |

## Storage Providers

Built-in storage providers with extensible architecture:

- **File System**: Local file storage
- **MinIO**: S3-compatible object storage  
- **FTP/SFTP**: Remote file servers
- **Extensible**: Easy to add custom providers

## Security Features

- **JWT Token Authentication**: Secure user sessions
- **API Key Authentication**: For external integrations
- **Role-Based Authorization**: Granular permission control
- **Data Encryption**: Sensitive data protection
- **Audit Trail**: Complete activity logging
- **CORS Protection**: Cross-origin request security

## Development

### Prerequisites
- .NET 9 SDK
- Docker (optional)
- SQL Server/PostgreSQL/MySQL

### Build & Run
```bash
# Clone repository
git clone https://github.com/qutora/qutora-api.git
cd qutora-api

# Restore packages
dotnet restore

# Set connection string
export ConnectionStrings__DefaultConnection="your-connection-string"
export Database__Provider="SqlServer"
export Jwt__Key="your-jwt-secret-key-32-chars-minimum"

# Run API
dotnet run --project Qutora.API
```

### Docker Build
```bash
# Build image
./docker-build.sh v1.0.0

# Build and push to Docker Hub
./docker-build.sh v1.0.0 true
```

## Contributing

We welcome contributions! Please see our [Contributing Guide](CONTRIBUTING.md) for details.

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests if applicable  
5. Submit a pull request

## License

This project is licensed under the [MIT License](LICENSE) - see the LICENSE file for details.

This is an open-source project. You are free to use, modify, and distribute this software under the terms of the MIT License.

## Support

- **Issues**: [Report bugs or request features](https://github.com/qutora/qutora-api/issues)
- **Discussions**: [Community support](https://github.com/qutora/qutora-api/discussions)
- **API Documentation**: Swagger UI available at `/swagger` endpoint

## Project Status

- **Docker Hub**: [qutora/qutora-api](https://hub.docker.com/r/qutora/qutora-api)
- **License**: MIT
- **Maintenance**: Actively maintained

---

**Built with .NET 9, Entity Framework Core, and Docker** 
