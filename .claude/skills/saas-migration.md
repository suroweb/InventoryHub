# SaaS Migration Skill

This skill automates the process of converting a single-tenant application into a multi-tenant SaaS application.

## What This Skill Does

When invoked, this skill will:

1. **Analyze Current Architecture**
   - Review existing database models
   - Identify tenant-specific data
   - Map current endpoints

2. **Design Multi-Tenant Schema**
   - Add tenant entity
   - Add tenant foreign keys to relevant tables
   - Design connection string management

3. **Implement Tenant Context**
   - Create tenant context middleware
   - Add tenant resolution logic
   - Implement tenant-aware DbContext

4. **Update Data Access Layer**
   - Add tenant filters to queries
   - Create base repository with tenant isolation
   - Update existing repositories

5. **Secure Endpoints**
   - Add JWT authentication
   - Implement tenant validation
   - Add authorization policies

6. **Create Subscription Management**
   - Design subscription tiers
   - Implement billing integration hooks
   - Add usage tracking

## Usage

Simply mention "migrate to SaaS" or "convert to multi-tenant" and this skill will guide you through the complete transformation.

## Output

- Complete database migration scripts
- Updated entity models
- Middleware components
- Repository implementations
- API endpoint modifications
- Authentication setup
- Admin interfaces
