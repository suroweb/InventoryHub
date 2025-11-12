# API Generator Skill

This skill automatically generates complete CRUD API endpoints following best practices for multi-tenant SaaS applications.

## What This Skill Does

Given an entity name, this skill generates:

1. **Domain Entity**
   - Entity class with proper properties
   - Inherits from BaseEntity
   - Includes tenant foreign key
   - Navigation properties

2. **Repository Interface & Implementation**
   - ITenantRepository<T> implementation
   - Tenant-filtered queries
   - Async operations
   - Pagination support

3. **Service Layer**
   - Business logic validation
   - Error handling
   - DTOs for request/response
   - AutoMapper configuration

4. **API Endpoints**
   - GET /api/v1/{entity} - List with pagination
   - GET /api/v1/{entity}/{id} - Get by ID
   - POST /api/v1/{entity} - Create
   - PUT /api/v1/{entity}/{id} - Update
   - DELETE /api/v1/{entity}/{id} - Soft delete

5. **Unit Tests**
   - Service layer tests
   - Repository tests
   - Mock setup

6. **Integration Tests**
   - Endpoint tests
   - Tenant isolation verification

## Usage Examples

- "Generate CRUD API for Order entity"
- "Create API endpoints for Customer"
- "Generate repository and service for Invoice"

## Configuration

Specify entity properties:
```
Entity: Order
Properties:
- OrderNumber (string, required, unique)
- OrderDate (DateTime)
- TotalAmount (decimal)
- Status (enum: Pending, Completed, Cancelled)
- CustomerId (Guid, foreign key)
```

## Output

Complete, production-ready code including:
- Entity definition
- EF Core configuration
- Repository pattern
- Service layer
- API endpoints
- Comprehensive tests
- API documentation
