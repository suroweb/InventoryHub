# InventoryHub - Multi-Tenant SaaS Architecture

## 🎯 Overview

InventoryHub is a production-grade, multi-tenant SaaS inventory management platform built with .NET 10, showcasing enterprise-level architecture patterns and autonomous AI agent development capabilities.

## 🤖 Autonomous Agent Development

This project was built using **Claude Code's autonomous agent system** with:

### Agent Infrastructure
Located in `.claude/` directory:

- **5 Specialized Agents**
  - `code-reviewer.md` - Code quality and security reviews
  - `architect.md` - System architecture and design decisions
  - `security-auditor.md` - OWASP Top 10 vulnerability scanning
  - `database-designer.md` - Schema design and optimization
  - `testing-specialist.md` - Comprehensive test coverage

- **4 Automation Skills**
  - `saas-migration.md` - Single to multi-tenant conversion
  - `api-generator.md` - CRUD endpoint generation
  - `security-audit.md` - Automated security scanning
  - `performance-optimization.md` - Query and cache optimization

- **Slash Commands**
  - `/review` - Trigger code review
  - `/secure` - Run security audit
  - `/optimize` - Analyze performance
  - `/test` - Generate test suites

- **Automated Hooks** (`.claude/settings.json`)
  - Pre-commit: Run tests
  - Post-edit: Code formatting
  - Pre-write: Secret scanning

## 🏗️ Architecture Principles

### Multi-Tenancy Strategy: Database-per-Tenant

**Why Database-per-Tenant?**
- ✅ **Strongest Data Isolation**: Complete physical separation
- ✅ **Independent Scaling**: Scale tenants individually
- ✅ **Compliance-Ready**: Easier GDPR, HIPAA, SOC2 compliance
- ✅ **Backup/Restore**: Per-tenant data management
- ✅ **Custom Extensions**: Tenant-specific schema modifications

**Trade-offs:**
- ⚠️ Higher operational overhead
- ⚠️ Connection pool management complexity
- ⚠️ Database provisioning automation required

**Alternative Patterns Considered:**
1. **Shared Database, Shared Schema** - Simpler but higher data leakage risk
2. **Shared Database, Separate Schema** - Middle ground, complex migrations

### Technology Stack

**Backend**
- ASP.NET Core 10 (Minimal APIs)
- Entity Framework Core 10 (PostgreSQL)
- ASP.NET Identity (Authentication)
- JWT Bearer Tokens
- Serilog (Structured Logging)

**Frontend** (Blazor WebAssembly)
- .NET 10 Blazor WASM
- Component-based architecture
- JWT authentication integration

**Infrastructure**
- PostgreSQL (Primary database)
- Redis (Planned: distributed caching)
- Kubernetes (Planned: orchestration)

## 📦 Project Structure

```
InventoryHub/
├── .claude/                          # 🤖 Autonomous Agent Configuration
│   ├── agents/                       # Specialized AI agents
│   │   ├── code-reviewer.md
│   │   ├── architect.md
│   │   ├── security-auditor.md
│   │   ├── database-designer.md
│   │   └── testing-specialist.md
│   ├── skills/                       # Automation skills
│   │   ├── saas-migration.md
│   │   ├── api-generator.md
│   │   ├── security-audit.md
│   │   └── performance-optimization.md
│   ├── commands/                     # Slash commands
│   │   ├── review.md
│   │   ├── secure.md
│   │   ├── optimize.md
│   │   └── test.md
│   ├── settings.json                 # Hooks and automation
│   └── CLAUDE.md                     # Project context
│
├── FullStackApp/
│   ├── ServerApp/                    # ASP.NET Core API
│   │   ├── Domain/
│   │   │   └── Entities/             # Domain models
│   │   │       ├── Tenant.cs         # Tenant entity
│   │   │       ├── ApplicationUser.cs
│   │   │       ├── Product.cs
│   │   │       ├── Category.cs
│   │   │       └── Supplier.cs
│   │   │
│   │   ├── Data/
│   │   │   ├── Contexts/             # EF Core contexts
│   │   │   │   ├── MasterDbContext.cs    # Tenant metadata
│   │   │   │   └── TenantDbContext.cs    # Tenant data
│   │   │   └── Repositories/         # Data access layer
│   │   │       ├── IRepository.cs
│   │   │       ├── Repository.cs
│   │   │       └── ProductRepository.cs
│   │   │
│   │   ├── Services/                 # Business logic
│   │   │   ├── AuthService.cs        # JWT authentication
│   │   │   ├── TenantService.cs      # Tenant context
│   │   │   └── SubscriptionService.cs # Billing/limits
│   │   │
│   │   ├── Middleware/               # Custom middleware
│   │   │   ├── TenantResolutionMiddleware.cs
│   │   │   └── RateLimitingMiddleware.cs
│   │   │
│   │   ├── Endpoints/                # Minimal API endpoints
│   │   │   ├── AuthEndpoints.cs
│   │   │   ├── TenantEndpoints.cs
│   │   │   └── ProductEndpoints.cs
│   │   │
│   │   └── Program.cs                # Application entry point
│   │
│   ├── ClientApp/                    # Blazor WebAssembly
│   │   ├── Pages/
│   │   ├── Components/
│   │   └── Services/
│   │
│   ├── Shared/                       # Shared contracts
│   │   ├── Models/
│   │   │   ├── BaseEntity.cs
│   │   │   └── SubscriptionTier.cs
│   │   └── DTOs/
│   │       ├── AuthDTOs.cs
│   │       ├── ProductDTOs.cs
│   │       └── TenantDTOs.cs
│   │
│   └── Tests/                        # Test projects
│       ├── Unit/
│       └── Integration/
│
├── README.md                         # User documentation
└── ARCHITECTURE.md                   # This file
```

## 🔐 Security Architecture

### Authentication Flow
1. **User Registration**
   - Validate tenant exists and subscription is active
   - Check user limit for tenant tier
   - Create user in MasterDbContext
   - Generate JWT with tenant claim

2. **User Login**
   - Verify credentials via ASP.NET Identity
   - Check tenant subscription status
   - Update last login timestamp
   - Issue JWT token with claims:
     - `sub`: User ID
     - `email`: User email
     - `TenantId`: Tenant identifier

3. **Request Authorization**
   - JWT middleware validates token
   - Tenant resolution middleware extracts tenant
   - Repository queries automatically filtered by tenant

### Tenant Isolation Strategy

**Three-Layer Defense:**

1. **Middleware Layer** (`TenantResolutionMiddleware.cs:17`)
   - Resolves tenant from subdomain, JWT claim, or header
   - Sets tenant context for request
   - Rejects requests without valid tenant

2. **Database Layer** (`TenantDbContext.cs:49-75`)
   - Global query filters on all entities
   - Automatic TenantId injection on insert
   - SaveChanges validation

3. **Repository Layer** (`Repository.cs:13-51`)
   - All queries inherit tenant filter
   - No cross-tenant data access possible

### Security Controls

- ✅ JWT with 256-bit HMAC-SHA256
- ✅ Password requirements (8+ chars, complexity)
- ✅ Account lockout (5 attempts, 15 min)
- ✅ HTTPS enforcement
- ✅ CORS with origin validation
- ✅ Rate limiting per tenant tier
- ✅ SQL injection prevention (EF Core parameterization)
- ✅ XSS protection (Blazor auto-escaping)
- ✅ Secrets in environment variables

## 📊 Database Schema

### Master Database
Stores tenant metadata and user authentication:

```sql
Tenants
├── Id (PK, UUID)
├── Name
├── Subdomain (Unique)
├── ConnectionString (Encrypted)
├── SubscriptionTier (Enum: Free/Starter/Pro/Enterprise)
├── SubscriptionExpiresAt
├── MaxUsers, MaxProducts, ApiRateLimit
└── IsActive

ApplicationUsers (ASP.NET Identity)
├── Id (PK)
├── TenantId (FK → Tenants)
├── Email, PasswordHash
├── FirstName, LastName
└── LastLoginAt
```

### Tenant Databases
Each tenant has their own database with this schema:

```sql
Products
├── Id (PK, UUID)
├── TenantId (Indexed)
├── Name, Description, SKU
├── Price, CostPrice, Stock
├── CategoryId (FK → Categories)
├── SupplierId (FK → Suppliers)
└── CreatedAt, UpdatedAt, IsDeleted

Categories
├── Id (PK, UUID)
├── TenantId (Indexed)
├── Name, Description
└── Audit fields

Suppliers
├── Id (PK, UUID)
├── TenantId (Indexed)
├── Name, ContactName, Email, Phone
├── Address, City, Country
└── Audit fields
```

## 🔄 Request Flow

### Authenticated API Request

```
1. Client → HTTPS Request + JWT Token
         ↓
2. CORS Middleware → Validate origin
         ↓
3. JWT Middleware → Validate token, extract claims
         ↓
4. Tenant Resolution Middleware → Resolve tenant context
         ↓
5. Rate Limiting Middleware → Check tenant quota
         ↓
6. Authorization → Verify user permissions
         ↓
7. Repository → Query with tenant filter
         ↓
8. Response Cache → Check cache (if GET)
         ↓
9. Client ← JSON Response + Rate limit headers
```

## 💰 Subscription Tiers

| Feature | Free | Starter | Professional | Enterprise |
|---------|------|---------|--------------|------------|
| **Users** | 1 | 5 | 25 | Unlimited |
| **Products** | 10 | 100 | 1,000 | Unlimited |
| **API Rate Limit** | 60/min | 300/min | 1,000/min | 5,000/min |
| **Storage** | 100 MB | 1 GB | 10 GB | Custom |
| **Support** | Community | Email | Priority | Dedicated |
| **Custom Domain** | ❌ | ❌ | ✅ | ✅ |
| **API Access** | ✅ | ✅ | ✅ | ✅ |
| **Webhooks** | ❌ | ❌ | ✅ | ✅ |

Implementation: `Shared/Models/SubscriptionTier.cs:6`

## 🚀 API Endpoints

### Authentication
```
POST /api/auth/register    - Register new user
POST /api/auth/login       - Login and get JWT token
```

### Tenant Management
```
POST /api/tenants                    - Create new tenant
POST /api/tenants/{id}/upgrade       - Upgrade subscription
GET  /api/tenants/{id}/usage         - Get usage statistics
```

### Products (Tenant-specific)
```
GET    /api/v1/products              - List products (paginated)
GET    /api/v1/products/{id}         - Get product details
POST   /api/v1/products              - Create product
PUT    /api/v1/products/{id}         - Update product
DELETE /api/v1/products/{id}         - Delete product (soft)
GET    /api/v1/products/search?q=    - Search products
```

### System
```
GET /health    - Health check
GET /api       - API information
GET /swagger   - OpenAPI documentation
```

## 🧪 Testing Strategy

### Unit Tests
- Service layer business logic
- Repository operations (in-memory DB)
- Middleware tenant resolution
- Subscription limit enforcement

### Integration Tests
- End-to-end API flows
- Tenant isolation verification
- Authentication/authorization
- Rate limiting

### Performance Tests
- Load testing with k6
- Database query optimization
- Cache hit rate analysis

## 📈 Performance Optimizations

1. **Response Caching** - 5-minute cache with origin variance
2. **Database Indexes** - TenantId, SKU, foreign keys
3. **Query Filters** - Global filters compiled once
4. **Connection Pooling** - Shared pool per tenant database
5. **Lazy Loading Disabled** - Explicit Include() statements

**Targets:**
- API response time: <200ms (p95)
- Database queries: <50ms
- Page load: <2s

## 🛠️ Development Setup

### Prerequisites
```bash
- .NET 10 SDK
- PostgreSQL 16+
- Node.js (for client tooling)
```

### Quick Start
```bash
# 1. Clone and navigate
git clone <repo>
cd InventoryHub

# 2. Update connection strings in appsettings.json
# 3. Create master database
createdb InventoryHub_Master

# 4. Run migrations (when created)
cd FullStackApp/ServerApp
dotnet ef database update --context MasterDbContext

# 5. Run the application
dotnet run

# 6. Access Swagger UI
open http://localhost:5000/swagger
```

## 🔮 Roadmap

### Phase 1: MVP (Complete ✅)
- ✅ Multi-tenant architecture
- ✅ JWT authentication
- ✅ Product CRUD APIs
- ✅ Subscription tiers
- ✅ Rate limiting

### Phase 2: Enhanced Features (Planned)
- [ ] EF Core migrations
- [ ] Database seeding
- [ ] Category & Supplier CRUD
- [ ] Blazor WebAssembly UI
- [ ] Real-time updates (SignalR)

### Phase 3: Advanced Features (Planned)
- [ ] Webhook system
- [ ] API versioning
- [ ] GraphQL endpoint
- [ ] Analytics dashboard
- [ ] Audit logging
- [ ] Data export (CSV, Excel)

### Phase 4: Production Ready (Planned)
- [ ] Docker containerization
- [ ] Kubernetes deployment
- [ ] CI/CD pipeline (GitHub Actions)
- [ ] Automated testing
- [ ] Performance monitoring
- [ ] Backup/restore procedures

## 🤝 Contributing

This project demonstrates autonomous AI agent development. The `.claude/` directory contains the agent configuration that built this system.

## 📄 License

See LICENSE file.

---

**Built with Claude Code Autonomous Agents** 🤖
*Demonstrating production-grade multi-tenant SaaS architecture*
