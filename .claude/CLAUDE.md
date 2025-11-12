# InventoryHub - Multi-Tenant SaaS Platform

## Project Overview
InventoryHub is a production-grade, multi-tenant SaaS inventory management platform built with .NET 10, Blazor WebAssembly, and ASP.NET Core. This project demonstrates enterprise-level architecture patterns including tenant isolation, subscription management, and comprehensive security.

## Architecture Principles

### Multi-Tenancy Strategy
- **Tenant Isolation**: Database-per-tenant approach for maximum data isolation and scalability
- **Tenant Context**: Middleware-based tenant resolution from JWT claims
- **Connection String Management**: Dynamic connection string resolution per tenant
- **Shared Infrastructure**: Authentication, subscription management, and admin services

### Technology Stack
- **Backend**: ASP.NET Core Minimal API (.NET 10)
- **Frontend**: Blazor WebAssembly (.NET 10)
- **Database**: PostgreSQL/SQL Server with Entity Framework Core
- **Authentication**: JWT tokens with ASP.NET Identity
- **Caching**: Redis for distributed caching
- **Monitoring**: Serilog + Application Insights

### Security Requirements
- All endpoints must validate JWT tokens
- Tenant ID must be verified from authenticated claims
- Row-level security enforcement in database queries
- API rate limiting per tenant tier
- Input validation on all endpoints
- SQL injection prevention via parameterized queries

### Code Quality Standards
- SOLID principles enforcement
- Dependency injection for all services
- Async/await for all I/O operations
- Comprehensive error handling with specific exception types
- Unit test coverage > 80%
- Integration tests for critical flows

### Performance Requirements
- API response time < 200ms (95th percentile)
- Database queries optimized with proper indexing
- Caching strategy: Redis (L1) + In-memory (L2)
- Lazy loading for navigation properties
- Pagination for all list endpoints (max 100 items)

## Project Structure

```
InventoryHub/
├── .claude/                    # Claude Code autonomous agent configuration
│   ├── agents/                 # Specialized subagents
│   ├── skills/                 # Custom automation skills
│   ├── commands/               # Slash commands
│   ├── settings.json           # Hooks and automation
│   └── CLAUDE.md              # This file
├── FullStackApp/
│   ├── ServerApp/             # ASP.NET Core API
│   │   ├── Domain/            # Domain entities and value objects
│   │   ├── Data/              # EF Core contexts and repositories
│   │   ├── Services/          # Business logic layer
│   │   ├── Middleware/        # Tenant context, auth, rate limiting
│   │   ├── Endpoints/         # Minimal API endpoints
│   │   └── Program.cs
│   ├── ClientApp/             # Blazor WebAssembly
│   │   ├── Services/          # API clients and state management
│   │   ├── Pages/             # Routable components
│   │   ├── Components/        # Reusable UI components
│   │   └── Program.cs
│   ├── Shared/                # Shared DTOs and contracts
│   └── Tests/                 # Unit and integration tests
└── README.md
```

## Development Workflow

### Before Coding
1. Review relevant domain entities and repositories
2. Check existing patterns in similar features
3. Verify tenant isolation is enforced
4. Ensure proper error handling strategy

### During Development
1. Write unit tests first (TDD approach)
2. Implement business logic in service layer
3. Create minimal API endpoints
4. Add integration tests for critical paths
5. Update API documentation

### After Coding
1. Run automated tests (unit + integration)
2. Perform security audit (SQL injection, XSS, auth bypass)
3. Check performance benchmarks
4. Update documentation
5. Commit with descriptive message

## Key Patterns

### Repository Pattern
```csharp
public interface ITenantRepository<T> where T : class
{
    Task<T?> GetByIdAsync(Guid id);
    Task<IEnumerable<T>> GetAllAsync(int skip, int take);
    Task<T> AddAsync(T entity);
    Task UpdateAsync(T entity);
    Task DeleteAsync(Guid id);
}
```

### Tenant Context Resolution
```csharp
// Middleware extracts tenant ID from JWT claims
// All database queries automatically filtered by tenant
```

### Error Handling
```csharp
// Use custom exceptions: TenantNotFoundException, UnauthorizedAccessException
// Return proper HTTP status codes: 401, 403, 404, 422, 429, 500
// Log all exceptions with tenant context
```

## API Design Standards

### Endpoint Naming
- Use RESTful conventions
- Version APIs: `/api/v1/`
- Resource-based: `/api/v1/products`, `/api/v1/orders`

### Response Format
```json
{
  "success": true,
  "data": {},
  "error": null,
  "metadata": {
    "page": 1,
    "pageSize": 20,
    "totalCount": 100
  }
}
```

### Authentication Flow
1. User logs in → receives JWT with tenant claim
2. Client includes JWT in Authorization header
3. Middleware validates JWT and sets tenant context
4. All queries automatically scoped to tenant

## Testing Strategy

### Unit Tests
- Test business logic in isolation
- Mock repositories and external services
- Verify edge cases and error handling

### Integration Tests
- Test API endpoints end-to-end
- Use in-memory database or test container
- Verify tenant isolation

### Performance Tests
- Load testing with k6 or Artillery
- Target: 1000 req/s per tenant tier

## Deployment

### Environment Configuration
- Development: SQLite for rapid prototyping
- Staging: PostgreSQL with test data
- Production: PostgreSQL with backups, Redis cluster

### CI/CD Pipeline
1. Run automated tests
2. Security scanning (OWASP dependency check)
3. Build Docker images
4. Deploy to Kubernetes cluster
5. Run smoke tests

## Monitoring & Observability

### Metrics to Track
- API response times per endpoint
- Error rates per tenant
- Database query performance
- Cache hit rates
- Active user sessions

### Logging Standards
- Include tenant ID in all log entries
- Use structured logging (Serilog)
- Log levels: Debug, Info, Warning, Error, Critical

## Known Issues & Debt
- [ ] Port numbers are hardcoded - move to configuration
- [ ] In-memory product data - migrate to database
- [ ] Missing comprehensive error pages
- [ ] No tenant onboarding flow yet

## Future Enhancements
- [ ] GraphQL API support
- [ ] Real-time updates via SignalR
- [ ] Advanced analytics dashboard
- [ ] Mobile app with MAUI
- [ ] Webhook support for integrations
- [ ] Multi-region deployment

## Resources
- [ASP.NET Core Best Practices](https://learn.microsoft.com/en-us/aspnet/core/)
- [Multi-Tenancy Patterns](https://docs.microsoft.com/en-us/azure/architecture/guide/multitenant/overview)
- [Blazor WebAssembly Guide](https://learn.microsoft.com/en-us/aspnet/core/blazor/)

---

**Last Updated**: 2025-11-12
**Maintainer**: Autonomous Claude Code Agent
**Status**: Active Development - Multi-Tenant SaaS Transformation
