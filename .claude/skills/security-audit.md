# Security Audit Skill

This skill performs comprehensive security audits of the codebase, identifying vulnerabilities and security weaknesses.

## What This Skill Does

1. **OWASP Top 10 Scan**
   - SQL Injection detection
   - XSS vulnerabilities
   - Broken authentication
   - Security misconfigurations
   - Sensitive data exposure

2. **Multi-Tenant Security**
   - Tenant isolation verification
   - Cross-tenant access attempts
   - Tenant context enforcement
   - Connection string security

3. **Authentication & Authorization**
   - JWT validation checks
   - Token expiration enforcement
   - Role-based access control
   - Missing authorization checks

4. **Data Protection**
   - Encryption at rest
   - HTTPS enforcement
   - Secrets management
   - Logging sensitive data

5. **Input Validation**
   - SQL injection via EF Core
   - Command injection
   - File upload validation
   - Request size limits

## Scan Areas

### Code Analysis
- C# source files
- Razor components
- API endpoints
- Database queries
- Configuration files

### Configuration Review
- appsettings.json security
- CORS policies
- Authentication setup
- Authorization policies
- Rate limiting

### Dependencies
- NuGet package vulnerabilities
- Outdated packages
- Known CVEs

## Usage

- "Run security audit"
- "Check for vulnerabilities"
- "Audit tenant isolation"
- "Scan for SQL injection"

## Output Format

### 🔴 Critical (Fix Immediately)
- Security vulnerabilities with high risk
- Potential data breach scenarios

### 🟠 High Priority
- Security weaknesses
- Missing security controls

### 🟡 Medium Priority
- Security improvements
- Hardening recommendations

### 🟢 Best Practices
- Additional security measures
- Defense in depth

### 📋 Detailed Report
- Vulnerability descriptions
- Affected code locations
- Remediation steps
- Code examples for fixes
- Security resources

## Automated Fixes

Where possible, the skill will offer to automatically fix:
- Add missing authorization attributes
- Update vulnerable packages
- Add input validation
- Implement rate limiting
- Add security headers
