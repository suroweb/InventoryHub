# InventoryHub - Complete API Specification

**API Version:** 1.0
**Base URL:** `https://api.inventoryhub.com` or `https://{tenant}.inventoryhub.com`
**Protocol:** HTTPS only
**Authentication:** JWT Bearer Token
**Response Format:** JSON
**Date Format:** ISO 8601 (UTC)

---

## Table of Contents

1. [Authentication](#authentication)
2. [Error Handling](#error-handling)
3. [Rate Limiting](#rate-limiting)
4. [Pagination](#pagination)
5. [Endpoints](#endpoints)
   - [Authentication Endpoints](#authentication-endpoints)
   - [Tenant Management](#tenant-management)
   - [Product Endpoints](#product-endpoints)
   - [Analytics Endpoints](#analytics-endpoints)
   - [Alert Endpoints](#alert-endpoints)
   - [Webhook Endpoints](#webhook-endpoints)
   - [AI & Forecasting Endpoints](#ai--forecasting-endpoints)

---

## Authentication

All API requests (except `/api/auth/*`) require authentication via JWT Bearer token.

### Obtaining a Token

**Endpoint:** `POST /api/auth/login`

**Request:**
```json
{
  "email": "user@example.com",
  "password": "SecurePassword123!"
}
```

**Response (200 OK):**
```json
{
  "success": true,
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expiresAt": "2025-11-20T10:30:00Z",
  "user": {
    "id": "550e8400-e29b-41d4-a716-446655440000",
    "email": "user@example.com",
    "firstName": "John",
    "lastName": "Doe",
    "tenantId": "123e4567-e89b-12d3-a456-426614174000"
  }
}
```

### Using the Token

Include the token in the `Authorization` header:

```
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

**Token Claims:**
- `nameid`: User ID (Guid)
- `email`: User email
- `TenantId`: Tenant ID (Guid)
- `exp`: Expiration timestamp

**Token Lifetime:** 7 days

---

## Error Handling

### Standard Error Response

```json
{
  "success": false,
  "error": {
    "code": "ERROR_CODE",
    "message": "Human-readable error message",
    "details": {
      "field": "Additional context"
    }
  },
  "timestamp": "2025-11-13T10:30:00Z",
  "path": "/api/v1/products"
}
```

### HTTP Status Codes

| Code | Meaning | Description |
|------|---------|-------------|
| 200 | OK | Request succeeded |
| 201 | Created | Resource created successfully |
| 204 | No Content | Request succeeded, no body returned |
| 400 | Bad Request | Invalid request data |
| 401 | Unauthorized | Missing or invalid authentication |
| 403 | Forbidden | Insufficient permissions |
| 404 | Not Found | Resource not found |
| 422 | Unprocessable Entity | Validation failed |
| 429 | Too Many Requests | Rate limit exceeded |
| 500 | Internal Server Error | Server error |

### Common Error Codes

| Error Code | Description |
|------------|-------------|
| `TENANT_NOT_FOUND` | Tenant not found or subscription expired |
| `UNAUTHORIZED` | Authentication required |
| `FORBIDDEN` | Insufficient permissions |
| `VALIDATION_ERROR` | Input validation failed |
| `RATE_LIMIT_EXCEEDED` | API rate limit exceeded |
| `RESOURCE_NOT_FOUND` | Requested resource not found |
| `DUPLICATE_RESOURCE` | Resource already exists |

---

## Rate Limiting

API requests are rate-limited per tenant based on subscription tier.

### Limits by Tier

| Tier | Requests per Minute |
|------|---------------------|
| Free | 60 |
| Starter | 300 |
| Professional | 1,000 |
| Enterprise | 5,000 |

### Rate Limit Headers

Every response includes rate limit headers:

```
X-RateLimit-Limit: 1000
X-RateLimit-Remaining: 847
X-RateLimit-Reset: 2025-11-13T10:31:00Z
```

### Rate Limit Exceeded Response

**Status:** 429 Too Many Requests

```json
{
  "error": "Rate limit exceeded",
  "message": "API rate limit of 1000 requests per minute exceeded",
  "retryAfter": 45
}
```

---

## Pagination

List endpoints support pagination using `skip` and `take` query parameters.

### Request

```
GET /api/v1/products?skip=20&take=20
```

### Response

```json
{
  "data": [...],
  "metadata": {
    "skip": 20,
    "take": 20,
    "total": 485
  }
}
```

**Default:** `take=20`
**Maximum:** `take=100`

---

## Endpoints

### Authentication Endpoints

#### POST /api/auth/register

Register a new user account.

**Public:** Yes
**Request Body:**
```json
{
  "email": "newuser@example.com",
  "password": "SecurePassword123!",
  "firstName": "Jane",
  "lastName": "Smith",
  "tenantId": "123e4567-e89b-12d3-a456-426614174000"
}
```

**Response (201 Created):**
```json
{
  "success": true,
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "user": {
    "id": "550e8400-e29b-41d4-a716-446655440001",
    "email": "newuser@example.com",
    "firstName": "Jane",
    "lastName": "Smith"
  }
}
```

**Validation:**
- Email: Required, valid email format
- Password: Min 8 chars, requires digit, uppercase, lowercase, symbol
- First/Last Name: Required

#### POST /api/auth/login

Authenticate user and obtain JWT token.

**Public:** Yes
**Request/Response:** See [Authentication](#authentication) section

---

### Tenant Management

#### POST /api/tenants

Create a new tenant (admin only).

**Auth:** Required (ManageSettings permission)
**Request Body:**
```json
{
  "name": "Acme Corporation",
  "subdomain": "acme",
  "adminEmail": "admin@acme.com",
  "tier": "Professional"
}
```

**Response (201 Created):**
```json
{
  "id": "123e4567-e89b-12d3-a456-426614174000",
  "name": "Acme Corporation",
  "subdomain": "acme",
  "connectionString": "Host=localhost;Database=InventoryHub_123e4567...",
  "subscriptionTier": "Professional",
  "subscriptionExpiresAt": "2026-11-13T10:30:00Z",
  "isActive": true,
  "maxUsers": 50,
  "maxProducts": 10000,
  "apiRateLimit": 1000
}
```

#### POST /api/tenants/{tenantId}/upgrade

Upgrade tenant subscription tier.

**Auth:** Required (ManageSettings permission)
**Request Body:**
```json
{
  "newTier": "Enterprise"
}
```

**Response (200 OK):**
```json
{
  "success": true,
  "tenant": {
    "id": "123e4567-e89b-12d3-a456-426614174000",
    "subscriptionTier": "Enterprise",
    "maxUsers": -1,
    "maxProducts": -1,
    "apiRateLimit": 5000
  }
}
```

#### GET /api/tenants/{tenantId}/usage

Get tenant usage statistics.

**Auth:** Required (ViewAnalytics permission)

**Response (200 OK):**
```json
{
  "tenantId": "123e4567-e89b-12d3-a456-426614174000",
  "currentUsers": 23,
  "maxUsers": 50,
  "currentProducts": 1247,
  "maxProducts": 10000,
  "apiRateLimit": 1000,
  "storageUsedMB": 245.67,
  "subscriptionTier": "Professional",
  "subscriptionExpiresAt": "2026-11-13T10:30:00Z"
}
```

---

### Product Endpoints

#### GET /api/v1/products

List all products (paginated).

**Auth:** Required (ViewProducts permission)
**Cache:** 5 minutes
**Query Parameters:**
- `skip` (int, default: 0) - Number of records to skip
- `take` (int, default: 20, max: 100) - Number of records to return

**Response (200 OK):**
```json
{
  "data": [
    {
      "id": "550e8400-e29b-41d4-a716-446655440000",
      "tenantId": "123e4567-e89b-12d3-a456-426614174000",
      "sku": "WIDGET-001",
      "name": "Premium Widget",
      "description": "High-quality widget for enterprise use",
      "price": 29.99,
      "costPrice": 15.50,
      "categoryId": "660e8400-e29b-41d4-a716-446655440000",
      "category": {
        "id": "660e8400-e29b-41d4-a716-446655440000",
        "name": "Widgets"
      },
      "supplierId": "770e8400-e29b-41d4-a716-446655440000",
      "supplier": {
        "id": "770e8400-e29b-41d4-a716-446655440000",
        "name": "Widget Supplier Inc"
      },
      "isActive": true,
      "createdAt": "2025-01-15T10:30:00Z",
      "updatedAt": "2025-11-10T14:20:00Z"
    }
  ],
  "metadata": {
    "skip": 0,
    "take": 20,
    "total": 1247
  }
}
```

#### GET /api/v1/products/{id}

Get a single product by ID.

**Auth:** Required (ViewProducts permission)
**Cache:** 5 minutes

**Response (200 OK):**
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "tenantId": "123e4567-e89b-12d3-a456-426614174000",
  "sku": "WIDGET-001",
  "name": "Premium Widget",
  "description": "High-quality widget for enterprise use",
  "price": 29.99,
  "costPrice": 15.50,
  "categoryId": "660e8400-e29b-41d4-a716-446655440000",
  "category": {
    "id": "660e8400-e29b-41d4-a716-446655440000",
    "name": "Widgets",
    "description": "Widget products"
  },
  "supplierId": "770e8400-e29b-41d4-a716-446655440000",
  "supplier": {
    "id": "770e8400-e29b-41d4-a716-446655440000",
    "name": "Widget Supplier Inc",
    "contactPerson": "John Supplier",
    "email": "john@supplier.com"
  },
  "stockLevels": [
    {
      "locationId": "880e8400-e29b-41d4-a716-446655440000",
      "quantity": 150,
      "reorderPoint": 50,
      "maxQuantity": 500
    }
  ],
  "isActive": true,
  "createdAt": "2025-01-15T10:30:00Z",
  "updatedAt": "2025-11-10T14:20:00Z",
  "createdBy": "admin@acme.com"
}
```

**Response (404 Not Found):**
```json
{
  "error": "Product not found"
}
```

#### POST /api/v1/products

Create a new product.

**Auth:** Required (CreateProducts permission)
**Request Body:**
```json
{
  "sku": "WIDGET-002",
  "name": "Standard Widget",
  "description": "Affordable widget for small businesses",
  "price": 19.99,
  "costPrice": 10.00,
  "categoryId": "660e8400-e29b-41d4-a716-446655440000",
  "supplierId": "770e8400-e29b-41d4-a716-446655440000",
  "isActive": true
}
```

**Validation:**
- SKU: Required, max 50 chars, unique per tenant
- Name: Required, max 200 chars
- Price: Required, must be > 0
- CostPrice: Optional, must be >= 0

**Response (201 Created):**
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440001",
  "sku": "WIDGET-002",
  "name": "Standard Widget",
  "price": 19.99,
  "createdAt": "2025-11-13T10:30:00Z"
}
```

**Location Header:** `/api/v1/products/550e8400-e29b-41d4-a716-446655440001`

#### PUT /api/v1/products/{id}

Update an existing product.

**Auth:** Required (EditProducts permission)
**Request Body:** Same as POST (all fields)

**Response (204 No Content):** Success, no body

**Response (404 Not Found):**
```json
{
  "error": "Product not found"
}
```

#### DELETE /api/v1/products/{id}

Soft delete a product.

**Auth:** Required (DeleteProducts permission)

**Response (204 No Content):** Success, no body

**Note:** This is a soft delete - sets `isDeleted = true`

#### GET /api/v1/products/search

Search products by name, SKU, or description.

**Auth:** Required (ViewProducts permission)
**Query Parameters:**
- `query` (string, required) - Search term

**Response (200 OK):**
```json
{
  "data": [
    {
      "id": "550e8400-e29b-41d4-a716-446655440000",
      "sku": "WIDGET-001",
      "name": "Premium Widget",
      "price": 29.99
    }
  ]
}
```

---

### Analytics Endpoints

#### GET /api/v1/analytics/dashboard

Get dashboard KPIs and metrics.

**Auth:** Required (ViewAnalytics permission)
**Cache:** 1 minute

**Response (200 OK):**
```json
{
  "totalProducts": 1247,
  "totalActiveProducts": 1189,
  "lowStockProducts": 23,
  "outOfStockProducts": 5,
  "totalRevenue": 245673.89,
  "totalOrders": 3421,
  "averageOrderValue": 71.82,
  "inventoryValue": 187953.45,
  "topSellingProduct": {
    "id": "550e8400-e29b-41d4-a716-446655440000",
    "name": "Premium Widget",
    "revenue": 15234.67
  },
  "recentOrders": 127,
  "pendingOrders": 34,
  "period": {
    "from": "2025-11-01T00:00:00Z",
    "to": "2025-11-13T10:30:00Z"
  }
}
```

#### GET /api/v1/analytics/revenue

Get revenue analytics with breakdown.

**Auth:** Required (ViewAnalytics permission)
**Cache:** 5 minutes
**Query Parameters:**
- `from` (datetime, required) - Start date (ISO 8601)
- `to` (datetime, required) - End date (ISO 8601)

**Response (200 OK):**
```json
{
  "totalRevenue": 245673.89,
  "totalOrders": 3421,
  "averageOrderValue": 71.82,
  "period": {
    "from": "2025-11-01T00:00:00Z",
    "to": "2025-11-13T10:30:00Z"
  },
  "byCategory": [
    {
      "categoryId": "660e8400-e29b-41d4-a716-446655440000",
      "categoryName": "Widgets",
      "revenue": 123456.78,
      "orders": 1876
    }
  ],
  "byMonth": [
    {
      "month": "2025-11",
      "revenue": 45673.89,
      "orders": 621
    }
  ]
}
```

#### GET /api/v1/analytics/top-products

Get top-selling products by revenue.

**Auth:** Required (ViewAnalytics permission)
**Cache:** 5 minutes
**Query Parameters:**
- `count` (int, default: 10, max: 100) - Number of products

**Response (200 OK):**
```json
{
  "data": [
    {
      "productId": "550e8400-e29b-41d4-a716-446655440000",
      "productName": "Premium Widget",
      "sku": "WIDGET-001",
      "revenue": 15234.67,
      "unitsSold": 508,
      "rank": 1
    }
  ]
}
```

#### GET /api/v1/analytics/stock

Get stock analytics including ABC analysis.

**Auth:** Required (ViewAnalytics permission)
**Cache:** 5 minutes

**Response (200 OK):**
```json
{
  "totalStockValue": 187953.45,
  "totalUnits": 45672,
  "lowStockCount": 23,
  "outOfStockCount": 5,
  "overstockCount": 12,
  "abcAnalysis": {
    "aProducts": 125,
    "bProducts": 374,
    "cProducts": 748,
    "aRevenue": 196538.70,
    "bRevenue": 39307.74,
    "cRevenue": 9827.45
  },
  "stockTurnoverRate": 4.2
}
```

**ABC Analysis:**
- **A Products:** Top 20% by revenue (contribute 80% of total revenue)
- **B Products:** Next 30% by revenue (contribute 15% of total revenue)
- **C Products:** Bottom 50% by revenue (contribute 5% of total revenue)

#### GET /api/v1/analytics/sales-trend

Get sales trend data.

**Auth:** Required (ViewAnalytics permission)
**Cache:** 5 minutes
**Query Parameters:**
- `days` (int, default: 30, max: 365) - Number of days

**Response (200 OK):**
```json
{
  "data": [
    {
      "date": "2025-11-01",
      "revenue": 8234.56,
      "orders": 114,
      "averageOrderValue": 72.23
    },
    {
      "date": "2025-11-02",
      "revenue": 9127.89,
      "orders": 128,
      "averageOrderValue": 71.31
    }
  ],
  "summary": {
    "totalRevenue": 245673.89,
    "totalOrders": 3421,
    "averageOrderValue": 71.82,
    "trend": "increasing",
    "growthRate": 12.5
  }
}
```

---

### Alert Endpoints

#### GET /api/v1/alerts/unread

Get unread alerts for current user.

**Auth:** Required (ManageAlerts permission)

**Response (200 OK):**
```json
{
  "data": [
    {
      "id": "990e8400-e29b-41d4-a716-446655440000",
      "type": "LowStock",
      "priority": "High",
      "title": "Low Stock Alert",
      "message": "Product 'Premium Widget' is running low on stock at Main Warehouse",
      "relatedEntityId": "550e8400-e29b-41d4-a716-446655440000",
      "relatedEntityType": "Product",
      "isRead": false,
      "createdAt": "2025-11-13T09:15:00Z"
    }
  ],
  "count": 5
}
```

**Alert Types:**
- `LowStock` - Stock below reorder point
- `OutOfStock` - Stock at zero
- `HighValue` - High-value transaction
- `SystemWarning` - System warning
- `SecurityAlert` - Security event

**Alert Priorities:**
- `Low` - Informational
- `Medium` - Needs attention
- `High` - Urgent action required
- `Critical` - Immediate action required

#### GET /api/v1/alerts

Get all alerts (paginated).

**Auth:** Required (ManageAlerts permission)
**Query Parameters:**
- `skip` (int, default: 0)
- `take` (int, default: 20, max: 100)

**Response:** Same structure as unread alerts with pagination

#### POST /api/v1/alerts/{alertId}/read

Mark alert as read.

**Auth:** Required (ManageAlerts permission)

**Response (204 No Content):** Success

#### POST /api/v1/alerts/{alertId}/dismiss

Dismiss (soft delete) an alert.

**Auth:** Required (ManageAlerts permission)

**Response (204 No Content):** Success

#### POST /api/v1/alerts/check-stock

Manually trigger stock level check (creates low stock alerts).

**Auth:** Required (ManageAlerts permission)

**Response (200 OK):**
```json
{
  "alertsCreated": 5,
  "lowStockProducts": 23,
  "outOfStockProducts": 5
}
```

---

### Webhook Endpoints

#### GET /api/v1/webhooks

List all webhooks for current tenant.

**Auth:** Required (ManageIntegrations permission)

**Response (200 OK):**
```json
{
  "data": [
    {
      "id": "aa0e8400-e29b-41d4-a716-446655440000",
      "tenantId": "123e4567-e89b-12d3-a456-426614174000",
      "name": "Slack Notifications",
      "url": "https://hooks.slack.com/services/...",
      "events": ["product.created", "order.created", "stock.low"],
      "secret": "whsec_...",
      "isActive": true,
      "createdAt": "2025-10-01T10:00:00Z"
    }
  ]
}
```

#### POST /api/v1/webhooks

Create a new webhook.

**Auth:** Required (ManageIntegrations permission)
**Request Body:**
```json
{
  "name": "Order Notifications",
  "url": "https://api.example.com/webhooks/orders",
  "events": ["order.created", "order.completed"],
  "secret": "my_secret_key"
}
```

**Supported Events:**
- `product.created`, `product.updated`, `product.deleted`
- `order.created`, `order.updated`, `order.completed`, `order.cancelled`
- `stock.low`, `stock.zero`
- `alert.critical`

**Response (201 Created):**
```json
{
  "id": "aa0e8400-e29b-41d4-a716-446655440000",
  "name": "Order Notifications",
  "url": "https://api.example.com/webhooks/orders",
  "events": ["order.created", "order.completed"],
  "secret": "whsec_...",
  "isActive": true
}
```

**Webhook Payload:**
```json
{
  "event": "order.created",
  "timestamp": "2025-11-13T10:30:00Z",
  "data": {
    "orderId": "bb0e8400-e29b-41d4-a716-446655440000",
    "orderNumber": "ORD-2025-001234",
    "totalAmount": 149.99
  }
}
```

**Webhook Signature:**
- Header: `X-Webhook-Signature`
- Algorithm: HMAC-SHA256
- Payload: Raw request body JSON
- Validation: `HMAC-SHA256(secret, request_body) == X-Webhook-Signature`

#### PUT /api/v1/webhooks/{webhookId}/toggle

Enable or disable a webhook.

**Auth:** Required (ManageIntegrations permission)
**Request Body:**
```json
{
  "isActive": false
}
```

**Response (204 No Content):** Success

#### DELETE /api/v1/webhooks/{webhookId}

Delete a webhook.

**Auth:** Required (ManageIntegrations permission)

**Response (204 No Content):** Success

#### POST /api/v1/webhooks/test

Test webhook delivery.

**Auth:** Required (ManageIntegrations permission)
**Request Body:**
```json
{
  "webhookId": "aa0e8400-e29b-41d4-a716-446655440000",
  "testData": {
    "message": "Test webhook delivery"
  }
}
```

**Response (200 OK):**
```json
{
  "success": true,
  "responseCode": 200,
  "responseTime": 145,
  "message": "Webhook delivered successfully"
}
```

---

### AI & Forecasting Endpoints

#### GET /api/v1/ai/forecast/{productId}

Get AI-powered demand forecast for a product.

**Auth:** Required (ViewAnalytics permission)
**Query Parameters:**
- `days` (int, default: 30, max: 365) - Days to forecast

**Response (200 OK):**
```json
{
  "productId": "550e8400-e29b-41d4-a716-446655440000",
  "productName": "Premium Widget",
  "daysAhead": 30,
  "forecast": [
    {
      "date": "2025-11-14",
      "predictedDemand": 17,
      "confidence": 0.85,
      "lowerBound": 12,
      "upperBound": 22
    },
    {
      "date": "2025-11-15",
      "predictedDemand": 18,
      "confidence": 0.83,
      "lowerBound": 13,
      "upperBound": 23
    }
  ],
  "trend": "increasing",
  "insights": "Demand is trending upward based on recent sales patterns. Recommend increasing stock levels."
}
```

**Forecast Algorithm:**
1. Retrieve 6 months of historical sales data
2. Use AI (DeepSeek/Ollama/OpenAI) to identify patterns
3. Generate daily predictions with confidence scores
4. Calculate upper/lower bounds (confidence intervals)
5. Identify overall trend direction

#### GET /api/v1/ai/reorder-recommendation/{productId}

Get smart reorder recommendation.

**Auth:** Required (ViewAnalytics permission)

**Response (200 OK):**
```json
{
  "productId": "550e8400-e29b-41d4-a716-446655440000",
  "productName": "Premium Widget",
  "currentStock": 45,
  "reorderPoint": 50,
  "recommendedQuantity": 150,
  "reasoning": "Based on forecasted demand of 18 units/day and 7-day lead time, you will run out of stock in 2.5 days. Recommended order quantity covers 30 days of demand plus safety stock.",
  "urgency": "High",
  "estimatedStockoutDate": "2025-11-15",
  "estimatedCost": 2325.00,
  "supplier": {
    "id": "770e8400-e29b-41d4-a716-446655440000",
    "name": "Widget Supplier Inc",
    "leadTimeDays": 7
  }
}
```

**Reorder Calculation:**
```
Recommended Quantity = (Average Daily Demand × Forecast Period) + Safety Stock
Safety Stock = Z-Score × Std Deviation × sqrt(Lead Time)
Stockout Date = Current Date + (Current Stock / Average Daily Demand)
```

#### GET /api/v1/ai/recommendations/{customerId}

Get smart product recommendations for a customer.

**Auth:** Required (ViewAnalytics permission)

**Response (200 OK):**
```json
{
  "customerId": "cc0e8400-e29b-41d4-a716-446655440000",
  "customerName": "Acme Corp",
  "recommendations": [
    {
      "productId": "550e8400-e29b-41d4-a716-446655440001",
      "productName": "Standard Widget",
      "sku": "WIDGET-002",
      "price": 19.99,
      "relevanceScore": 0.92,
      "reasoning": "Frequently purchased together with Premium Widget"
    },
    {
      "productId": "550e8400-e29b-41d4-a716-446655440002",
      "productName": "Widget Accessory",
      "sku": "ACC-001",
      "price": 9.99,
      "relevanceScore": 0.87,
      "reasoning": "Complementary product based on purchase history"
    }
  ],
  "based_on": {
    "purchase_history": 15,
    "similar_customers": 234
  }
}
```

#### GET /api/v1/ai/seasonality/{productId}

Analyze seasonal patterns for a product.

**Auth:** Required (ViewAnalytics permission)

**Response (200 OK):**
```json
{
  "productId": "550e8400-e29b-41d4-a716-446655440000",
  "productName": "Premium Widget",
  "hasSeasonality": true,
  "monthlyPatterns": [
    {
      "month": 1,
      "monthName": "January",
      "averageDemand": 350,
      "seasonalMultiplier": 0.85
    },
    {
      "month": 12,
      "monthName": "December",
      "averageDemand": 620,
      "seasonalMultiplier": 1.45
    }
  ],
  "peakPeriods": [
    {
      "period": "November-December",
      "reason": "Holiday season",
      "increase": 45
    }
  ],
  "insights": "Strong holiday seasonality detected. Recommend increasing inventory 45% in Q4."
}
```

#### GET /api/v1/ai/optimize-stock/{locationId}

Get AI-powered stock optimization recommendations for a location.

**Auth:** Required (ViewAnalytics permission)

**Response (200 OK):**
```json
{
  "locationId": "880e8400-e29b-41d4-a716-446655440000",
  "locationName": "Main Warehouse",
  "totalProducts": 1247,
  "optimizations": [
    {
      "productId": "550e8400-e29b-41d4-a716-446655440000",
      "productName": "Premium Widget",
      "currentStock": 450,
      "recommendedStock": 200,
      "action": "Reduce",
      "reasoning": "Overstock detected. Current stock covers 90 days of demand. Recommend reduction to 30-day supply.",
      "potentialSavings": 3875.00
    },
    {
      "productId": "550e8400-e29b-41d4-a716-446655440001",
      "productName": "Standard Widget",
      "currentStock": 25,
      "recommendedStock": 150,
      "action": "Increase",
      "reasoning": "Understock detected. Current stock covers 2 days of demand. Urgent reorder needed.",
      "urgency": "Critical"
    }
  ],
  "totalPotentialSavings": 15420.00,
  "implementationPriority": "High"
}
```

#### POST /api/v1/ai/query

Process natural language query about inventory.

**Auth:** Required (ViewAnalytics permission)
**Request Body:**
```json
{
  "query": "What products need reordering this week?"
}
```

**Response (200 OK):**
```json
{
  "query": "What products need reordering this week?",
  "answer": "Based on current stock levels and demand forecasts, 5 products need reordering this week:\n\n1. Standard Widget (SKU: WIDGET-002) - 3 days until stockout\n2. Widget Accessory (SKU: ACC-001) - 5 days until stockout\n3. Premium Gadget (SKU: GADGET-001) - 6 days until stockout\n4. Basic Tool (SKU: TOOL-001) - 7 days until stockout\n5. Advanced Kit (SKU: KIT-001) - 7 days until stockout\n\nTotal estimated reorder cost: $8,450.00",
  "context": {
    "productsAnalyzed": 1247,
    "lowStockProducts": 23,
    "urgentReorders": 5
  }
}
```

**Example Queries:**
- "What products need reordering this week?"
- "Show me top selling products this month"
- "Which products are overstocked?"
- "What's my total inventory value?"
- "Which suppliers have the most pending orders?"

#### POST /api/v1/ai/analyze

Analyze custom data with AI.

**Auth:** Required (ViewAnalytics permission)
**Request Body:**
```json
{
  "data": "2025-11-01: 120 units\n2025-11-02: 135 units\n2025-11-03: 118 units",
  "analysisType": "trend"
}
```

**Analysis Types:**
- `trend` - Identify trends in data
- `insights` - Generate business insights
- `anomalies` - Detect anomalies
- `summary` - Summarize data

**Response (200 OK):**
```json
{
  "analysisType": "trend",
  "insights": [
    "Slight upward trend detected over the 3-day period",
    "Average daily volume: 124.3 units",
    "Variability: Low (7.5% coefficient of variation)",
    "No significant anomalies detected"
  ],
  "confidence": 0.78,
  "model": "deepseek-chat"
}
```

---

## Appendix

### Subscription Tiers Comparison

| Feature | Free | Starter | Professional | Enterprise |
|---------|------|---------|--------------|------------|
| Max Users | 3 | 10 | 50 | Unlimited |
| Max Products | 100 | 1,000 | 10,000 | Unlimited |
| API Rate Limit/min | 60 | 300 | 1,000 | 5,000 |
| Data Export | CSV only | CSV, Excel | CSV, Excel, PDF | All formats |
| AI Forecasting | ❌ | ✅ (100 requests/mo) | ✅ (1,000/mo) | ✅ Unlimited |
| Webhooks | ❌ | 3 | 10 | Unlimited |
| Priority Support | ❌ | ❌ | ✅ | ✅ 24/7 |
| Price/month | $0 | $29 | $79 | $199 |

### Permissions Reference

**Product Permissions:** ViewProducts, CreateProducts, EditProducts, DeleteProducts
**Inventory Permissions:** ViewInventory, AdjustInventory, TransferStock
**Order Permissions:** ViewOrders, CreateOrders, EditOrders, CancelOrders
**Category Permissions:** ManageCategories
**Supplier Permissions:** ViewSuppliers, ManageSuppliers
**Analytics Permissions:** ViewAnalytics, ExportData
**User Management:** ViewUsers, ManageUsers, AssignRoles
**System Permissions:** ManageSettings, ViewAuditLogs, ManageIntegrations, ManageLocations
**Reporting:** ViewReports, CreateReports, ScheduleReports
**Advanced Features:** ManageAutomation, ManageAlerts, AccessAPI

### Webhook Event Reference

**Product Events:**
- `product.created` - New product created
- `product.updated` - Product updated
- `product.deleted` - Product deleted (soft delete)

**Order Events:**
- `order.created` - New order created
- `order.updated` - Order status changed
- `order.completed` - Order marked as delivered
- `order.cancelled` - Order cancelled

**Stock Events:**
- `stock.low` - Stock below reorder point
- `stock.zero` - Stock depleted
- `stock.adjusted` - Manual stock adjustment
- `stock.transferred` - Stock transferred between locations

**Alert Events:**
- `alert.created` - New alert created
- `alert.critical` - Critical alert raised

---

**Document Version:** 1.0
**Last Updated:** 2025-11-13
**Contact:** api-support@inventoryhub.com
**Status:** Production

---

**Rate Limits:** All endpoints subject to per-tenant rate limiting
**SLA:** 99.9% uptime guarantee for Professional and Enterprise tiers
**Support:** support@inventoryhub.com or in-app chat for paid tiers
