# Qutora Docker Compose Samples

This directory contains ready-to-use Docker Compose examples for running Qutora Document Management System with different databases.

## Available Examples

### SQL Server Express Setup
- **File**: `docker-compose.sqlserver.yml`
- **Database**: SQL Server 2022 Express
- **Status**: Ready for testing and development

> **Note**: PostgreSQL and MySQL examples are not included yet as I'm still completing the testing phase for these databases. They will be added in a future update.

## Quick Start - SQL Server Express

### 1. Prepare Environment File
```bash
cd samples/
cp env.sqlserver.example .env
nano .env  # Edit configuration
```

### 2. Start with Docker Compose
```bash
# API and SQL Server Express
docker-compose -f docker-compose.sqlserver.yml --env-file .env up -d
```

### 3. Access Information
- **API**: http://localhost:8080
- **Swagger UI**: http://localhost:8080/swagger
- **SQL Server**: localhost:1433
- **SQL Server Credentials**: sa / (see MSSQL_SA_PASSWORD in .env)

### 4. Initial Setup
After starting the API, perform one-time system initialization:

**Step 1: Check system status**
```bash
curl http://localhost:8080/api/auth/system-status
```

Response if not initialized:
```json
{
  "isInitialized": false,
  "version": "1.0.0",
  "timestamp": "2024-01-01T00:00:00Z"
}
```

**Step 2: Create first admin user**
```bash
curl -X POST http://localhost:8080/api/auth/initial-setup \
  -H "Content-Type: application/json" \
  -d '{
    "email": "admin@qutora.local",
    "password": "AdminPassword123!",
    "firstName": "Admin",
    "lastName": "User",
    "organizationName": "My Organization"
  }'
```

**What happens during initial setup:**
1. Creates Admin role with all permissions
2. Creates first admin user
3. Sets `SystemSettings.IsInitialized = true`
4. Creates default storage provider (Local Storage)
5. Creates default storage bucket
6. Creates physical directories on filesystem

**Important**: Initial setup can only be run **once**. After successful setup, this endpoint will return an error.

## Security Warnings

### CRITICAL: Data Protection Keys

**Read this before deleting Docker volumes!**

```bash
# These volumes are CRITICAL:
qutora_keys             # Encryption keys - DO NOT DELETE!
qutora_data             # Document files
qutora_sqlserver_data   # Database
```

**What is stored in `qutora_keys`?**

The `qutora_keys` volume contains the master encryption key used to encrypt sensitive data across the entire system:
- Storage provider credentials and secrets (MinIO, FTP, SFTP access keys)
- SMTP server credentials and passwords
- Other sensitive configuration data stored in the database

**Important**: 
- If `qutora_keys` volume is deleted, all encrypted data will be **permanently lost**!
- Keep this volume secure and backed up
- Never share or expose this volume in production environments

### Configuration Checklist

Make sure to change in your `.env` file:

- `MSSQL_SA_PASSWORD` - Change default password (min 8 chars, uppercase, lowercase, numbers, symbols)
- `JWT_SECRET_KEY` - At least 32 characters (recommended: `openssl rand -base64 64`)
- `ALLOWED_ORIGIN_*` - Your frontend URLs

## Service Management

### Starting Containers
```bash
# Start
docker-compose -f docker-compose.sqlserver.yml --env-file .env up -d

# Follow logs
docker-compose -f docker-compose.sqlserver.yml --env-file .env logs -f

# API logs only
docker-compose -f docker-compose.sqlserver.yml --env-file .env logs -f qutora-api
```

### Stopping Containers
```bash
# Stop (data is preserved)
docker-compose -f docker-compose.sqlserver.yml --env-file .env down

# Stop and DELETE volumes (DANGEROUS!)
docker-compose -f docker-compose.sqlserver.yml --env-file .env down -v
```

### Service Status
```bash
# Check all services status
docker-compose -f docker-compose.sqlserver.yml --env-file .env ps

# Check API is running
curl http://localhost:8080/api/auth/system-status
```
 