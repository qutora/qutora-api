# Qutora API

A modern, secure, and scalable document management API built with .NET 9 and Clean Architecture principles.

## 🚀 What is Qutora API?

Qutora API is a comprehensive document management system designed for enterprise-level applications. It provides a robust REST API that can be integrated into any application or system requiring document management capabilities.

**What Qutora API is:**
- A complete backend API for document management
- A secure, enterprise-ready solution
- A highly extensible and modular system
- A multi-tenant capable platform

**What Qutora API is not:**
- A ready-to-use web application (UI not included)
- A simple file storage solution
- A basic CRUD API without business logic

## ✨ Key Features

- **Multi-Database Support**: SQL Server, PostgreSQL, MySQL
- **Multiple Storage Providers**: File System, MinIO, FTP, SFTP
- **Advanced Authentication**: JWT tokens and API key authentication
- **Document Management**: Upload, versioning, metadata, and categorization
- **Secure Document Sharing**: Time-limited, password-protected sharing
- **Approval Workflows**: Configurable approval processes
- **Audit Logging**: Comprehensive activity tracking
- **Role-Based Access Control**: Fine-grained permissions system
- **RESTful API**: Clean, well-documented endpoints

## 🏗️ Architecture

Built with Clean Architecture principles:

- **Domain Layer**: Core business entities and rules
- **Application Layer**: Use cases and interfaces
- **Infrastructure Layer**: Data access, external services
- **API Layer**: REST endpoints and controllers

## 🛠️ Tech Stack

- **.NET 9**: Latest framework features
- **Entity Framework Core**: Multi-database ORM
- **ASP.NET Core**: Web API framework
- **JWT Authentication**: Secure token-based auth
- **Serilog**: Structured logging
- **Mapster**: High-performance object mapping
- **System.Text.Json**: Built-in JSON serialization


## 🎯 Target Use Cases

Qutora API is designed for organizations that need:

- **Enterprise Document Management**: Secure storage and management of business documents
- **Integration Projects**: Adding document capabilities to existing applications
- **Multi-Tenant Solutions**: SaaS applications requiring document management per tenant
- **Compliance Requirements**: Systems needing audit trails and approval workflows
- **Hybrid Storage Needs**: Organizations using multiple storage backends
- **API-First Approach**: Teams building custom frontends or mobile applications

## 🏢 Enterprise Ready

- **Scalable Architecture**: Handles high-volume document operations
- **Security First**: Built-in authentication, authorization, and audit logging
- **Multi-Database Support**: Choose the database that fits your infrastructure
- **Extensible Design**: Add custom features without modifying core system
- **Production Tested**: Battle-tested in enterprise environments

## 🛡️ Security Features

- **JWT Token Authentication**: Secure user sessions
- **API Key Authentication**: For external integrations
- **Role-Based Authorization**: Granular permission control
- **Rate Limiting**: Prevent API abuse
- **CORS Protection**: Cross-origin request security
- **Data Encryption**: Sensitive data protection
- **Audit Trail**: Complete activity logging

## 🗄️ Database Support

Qutora API supports multiple database providers through Entity Framework Core:

- **SQL Server** (Recommended for production)
- **PostgreSQL** (Open-source alternative)
- **MySQL** (Lightweight option)

Switch between providers by changing the `Database.Provider` configuration.

## 📦 Extensible Storage Architecture

One of Qutora API's key strengths is its **highly extensible storage provider system**:

### Built-in Providers
- **File System**: Local file storage for development and small deployments
- **MinIO**: S3-compatible object storage for scalable cloud deployments
- **FTP/SFTP**: Remote file servers for legacy system integration

### Extensibility at Core
- **Plugin Architecture**: Easy to add new storage providers without modifying core code
- **Provider Interface**: Well-defined contracts for implementing custom storage solutions
- **Dynamic Configuration**: Storage providers can be configured and switched at runtime
- **Multi-Provider Support**: Use different storage providers for different document types or tenants

### Potential Extensions
The architecture supports integration with:
- Cloud storage services (Azure Blob, AWS S3, Google Cloud Storage)
- Enterprise storage systems (SharePoint, OneDrive, Box)
- Custom storage solutions specific to your organization
- Hybrid storage configurations

## 📊 Key Technical Features

- **Clean Architecture**: Separation of concerns with clear layer boundaries
- **UnitOfWork Pattern**: Consistent transaction management
- **Value Comparers**: Proper EF Core collection handling
- **Current User Service**: Seamless user context tracking
- **Reference Handling**: Advanced JSON serialization with circular reference support
- **Optimistic Concurrency**: Built-in conflict resolution

## 🚀 Getting Started

**Important Note**: This repository contains only the API layer. 

### For Developers
- Comprehensive API documentation will be available
- Integration examples and SDKs coming soon
- Developer community and support channels

### For Organizations
- Professional support and consulting available
- Custom development and integration services
- Enterprise licensing and deployment assistance

## 🤝 Community & Support

- **Issues**: Report bugs and request features via GitHub Issues
- **Discussions**: Join community discussions for best practices and use cases
- **Documentation**: Comprehensive guides and API references (coming soon)

## 📄 License

This project is licensed under the Qutora Software License v1.0 - see the [LICENSE](LICENSE) file for details.

**Note**: This license permits internal, non-commercial use only. For commercial licensing, please contact us at info@qutora.io

## 📞 Support

For questions, issues, or commercial licensing:
- Email: info@qutora.io
- Issues: GitHub Issues tab

---

**Qutora API** - Secure, scalable document management for modern applications. 
