# Qutora Docker Security Guide

## 🔐 Data Protection Keys Security

### 🚨 CRITICAL DATA LOSS WARNING
**WITHOUT PERSISTENT KEY STORAGE, ALL ENCRYPTED DATA WILL BE PERMANENTLY LOST!**

When you restart a Docker container without persistent key storage:
- ✅ Database data survives (stored in database volume)
- ❌ **Encryption keys are LOST** (generated new keys)
- ❌ **ALL encrypted data becomes UNRECOVERABLE**
- ❌ User passwords, documents, API keys become inaccessible

### ⚠️ Critical Security Issue
**NEVER commit Data Protection keys to version control!** These keys are used to encrypt sensitive data and must be kept secure.

### 🛡️ Secure Key Management

#### 1. Production Environment
```bash
# Use Docker volumes for persistent keys
docker run -d \
  --name qutora-api \
  -v qutora_keys:/app/keys \
  -e ASPNETCORE_ENVIRONMENT=Production \
  qutora/qutora-api:latest
```

#### 2. Docker Compose with Named Volumes
```yaml
services:
  qutora-api:
    image: qutora/qutora-api:latest
    volumes:
      - qutora_keys:/app/keys
    environment:
      - ASPNETCORE_ENVIRONMENT=Production

volumes:
  qutora_keys:
    driver: local
```

#### 3. Azure Key Vault Integration (Recommended for Production)
```csharp
// In Program.cs - Add this configuration
if (app.Environment.IsProduction())
{
    builder.Services.AddDataProtection()
        .SetApplicationName("Qutora.DocumentSharing")
        .PersistKeysToAzureKeyVault(new Uri("https://your-keyvault.vault.azure.net/"), 
            new DefaultAzureCredential());
}
```

#### 4. AWS Systems Manager Parameter Store
```csharp
builder.Services.AddDataProtection()
    .SetApplicationName("Qutora.DocumentSharing")
    .PersistKeysToAwsSystemsManager("/qutora/dataprotection");
```

### 🚨 Security Checklist

- [ ] **Keys Directory Permissions**: Set to 700 (owner read/write/execute only)
- [ ] **Volume Encryption**: Use encrypted Docker volumes in production
- [ ] **Backup Strategy**: Securely backup keys (encrypted)
- [ ] **Key Rotation**: Implement automatic key rotation
- [ ] **Access Control**: Limit access to keys directory
- [ ] **Monitoring**: Monitor key access and usage

### 🔧 Environment-Specific Configuration

#### Development
```bash
# Development - keys are auto-generated and stored locally
export ASPNETCORE_ENVIRONMENT=Development
# Keys will be created in ./keys directory (ignored by Git)
```

#### Staging
```bash
# Staging - use dedicated key storage
export ASPNETCORE_ENVIRONMENT=Staging
export DataProtection__KeyVaultUri=https://qutora-staging-kv.vault.azure.net/
```

#### Production
```bash
# Production - use secure key management service
export ASPNETCORE_ENVIRONMENT=Production
export DataProtection__KeyVaultUri=https://qutora-prod-kv.vault.azure.net/
```

### 🛠️ Key Management Commands

#### Backup Keys (Development Only)
```bash
# Create encrypted backup
tar -czf keys-backup-$(date +%Y%m%d).tar.gz keys/
gpg --symmetric --cipher-algo AES256 keys-backup-*.tar.gz
rm keys-backup-*.tar.gz
```

#### Restore Keys
```bash
# Decrypt and restore
gpg --decrypt keys-backup-*.tar.gz.gpg | tar -xzf -
```

#### Key Rotation
```bash
# Force new key generation (development)
rm -rf keys/*
# Application will generate new keys on startup
```

### 📋 Best Practices

1. **Never Version Control Keys**: Always add `keys/` to `.gitignore`
2. **Use Managed Services**: Azure Key Vault, AWS KMS, HashiCorp Vault
3. **Encrypt at Rest**: Use encrypted storage for key files
4. **Regular Rotation**: Implement automatic key rotation policy
5. **Access Logging**: Log all key access attempts
6. **Backup Strategy**: Secure, encrypted backups with tested restore procedures

### 🚀 Docker Production Setup

```yaml
# docker-compose.prod.yml
version: '3.8'
services:
  qutora-api:
    image: qutora/qutora-api:latest
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - DataProtection__KeyVaultUri=${KEY_VAULT_URI}
      - AZURE_CLIENT_ID=${AZURE_CLIENT_ID}
      - AZURE_CLIENT_SECRET=${AZURE_CLIENT_SECRET}
      - AZURE_TENANT_ID=${AZURE_TENANT_ID}
    # No local keys volume needed when using Key Vault
```

### ⚡ Quick Fix for Existing Installations

If you have existing keys in production:

1. **Backup Current Keys**:
   ```bash
   docker cp qutora-api:/app/keys ./keys-backup
   ```

2. **Migrate to Secure Storage**:
   ```bash
   # Upload to Key Vault or secure storage
   az keyvault secret set --vault-name "qutora-keyvault" --name "dataprotection-keys" --file ./keys-backup
   ```

3. **Update Configuration**:
   ```bash
   # Update environment variables to use Key Vault
   docker run -d --name qutora-api-new \
     -e DataProtection__KeyVaultUri=https://qutora-keyvault.vault.azure.net/ \
     qutora/qutora-api:latest
   ```

### 📞 Support

For security-related questions, contact [info@qutora.io](mailto:info@qutora.io) 