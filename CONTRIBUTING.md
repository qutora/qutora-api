# Contributing to Qutora

Thank you for your interest in contributing to Qutora! We welcome contributions from the community.

## 🚀 Quick Start

1. **Fork the repository**
2. **Create a feature branch**: `git checkout -b feature/amazing-feature`
3. **Make your changes**
4. **Test your changes**
5. **Submit a pull request**

## 📋 Development Setup

### Prerequisites
- .NET 9 SDK
- Docker (optional but recommended)
- SQL Server, PostgreSQL, or MySQL

### Local Development
```bash
# Clone your fork
git clone https://github.com/YOUR-USERNAME/qutora-api.git
cd qutora-api

# Restore dependencies
dotnet restore

# Set environment variables
export ConnectionStrings__DefaultConnection="your-connection-string"
export Database__Provider="SqlServer"
export Jwt__Key="your-jwt-secret-key-32-chars-minimum"

# Run the API
dotnet run --project Qutora.API
```

### Docker Development
```bash
# Start development environment
docker-compose -f docker-compose.yml -f docker-compose.dev.yml up -d

# View logs
docker-compose -f docker-compose.yml -f docker-compose.dev.yml logs -f
```

##  Release Process

### Triggering Releases

We use commit message tags to control releases:

#### Stable Release
```bash
git commit -m "feat: Add new storage provider [release]"
git push origin main
# → Triggers: v1.0.1, v1.0, v1, latest tags
```

#### Preview Release
```bash
git commit -m "feat: Experimental feature [release] [preview]"
git push origin main
# → Triggers: v1.0.1-preview, preview tags
```

#### No Release
```bash
git commit -m "docs: Update documentation"
git push origin main
# → No Docker release, only CI tests
```

### Automatic Versioning

- **Patch increment**: Automatic (v1.0.0 → v1.0.1)
- **Minor/Major**: Manual tag creation required
- **Preview**: Add `[preview]` tag to commit message

## 🧪 Testing

### Running Tests
```bash
# Unit tests
dotnet test

# Integration tests with Docker
docker-compose -f docker-compose.test.yml up --abort-on-container-exit
```

### Testing Docker Build
```bash
# Test local build
./docker-build.sh dev-test false

# Test full pipeline
./docker-build.sh v1.0.0-dev true
```

##  Code Style

### .NET Conventions
- Follow [Microsoft C# Coding Conventions](https://docs.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions)
- Use `PascalCase` for public members
- Use `camelCase` for private fields
- Add XML documentation for public APIs

### File Organization
```
Qutora.API/          # REST API controllers
Qutora.Application/  # Service interfaces
Qutora.Domain/       # Entities and business logic
Qutora.Infrastructure/ # Implementations
Qutora.Shared/       # DTOs and common models
```

##  Docker Guidelines

### Dockerfile Best Practices
- Use multi-stage builds
- Minimize layer count
- Use specific base image tags
- Set health checks
- Run as non-root user

### Docker Compose
- Environment-specific overrides
- Named volumes for persistence
- Health checks for services
- Resource limits

##  Documentation

### Code Documentation
- XML documentation for public APIs
- Inline comments for complex logic
- README updates for new features

### API Documentation
- Update API documentation
- Include example requests/responses
- Document error codes

##  Security

### Reporting Security Issues
- **DO NOT** open public issues for security vulnerabilities
- Email security issues to: [SECURITY_EMAIL]
- Include detailed reproduction steps

### Security Guidelines
- Never commit secrets or credentials
- Use environment variables for configuration
- Validate all user inputs
- Follow OWASP security practices

##  Pull Request Process

### Before Submitting
- [ ] Code builds without errors
- [ ] Tests pass locally
- [ ] Documentation updated
- [ ] No breaking changes (or clearly documented)
- [ ] Security implications considered

### PR Description Template
```markdown
## Description
Brief description of changes

## Type of Change
- [ ] Bug fix
- [ ] New feature
- [ ] Breaking change
- [ ] Documentation update

## Testing
- [ ] Unit tests added/updated
- [ ] Integration tests pass
- [ ] Manual testing completed

## Checklist
- [ ] Code follows style guidelines
- [ ] Self-review completed
- [ ] Documentation updated
- [ ] No merge conflicts
```

### Review Process
1. **Automated checks**: CI/CD pipeline must pass
2. **Code review**: At least one maintainer approval
3. **Testing**: Manual testing if needed
4. **Merge**: Squash and merge preferred

##  Commit Message Format

### Standard Format
```
type(scope): description [tags]

Examples:
feat(api): Add user authentication endpoint [release]
fix(storage): Resolve file upload bug
docs(readme): Update installation instructions
test(unit): Add category service tests
```

### Types
- `feat`: New feature
- `fix`: Bug fix
- `docs`: Documentation
- `style`: Formatting changes
- `refactor`: Code refactoring
- `test`: Adding tests
- `chore`: Maintenance tasks

### Special Tags
- `[release]`: Trigger Docker release
- `[preview]`: Create preview release
- `[breaking]`: Breaking change

##  Community

### Code of Conduct
- Be respectful and inclusive
- Focus on constructive feedback
- Help newcomers learn
- Collaborate openly

### Getting Help
- **Issues**: For bugs and feature requests
- **Discussions**: For questions and ideas
- **Discord**: [Community Discord link]
- **Email**: [SUPPORT_EMAIL]

##  License

By contributing to Qutora, you agree that your contributions will be licensed under the [MIT License](LICENSE).
