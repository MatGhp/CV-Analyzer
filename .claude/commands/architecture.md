# Architecture Analysis Command

Analyze the current architecture and provide recommendations for CV-Analyzer.

## Analysis Areas

### 1. Clean Architecture Compliance
- **Domain Layer**: Check for zero external dependencies, only domain logic
- **Application Layer**: Verify business logic separation, CQRS pattern usage
- **Infrastructure Layer**: Review external service integrations
- **API Layer**: Check for thin controllers, proper middleware

### 2. CQRS Pattern
- **Commands**: Verify state-changing operations are commands
- **Queries**: Verify read operations are queries
- **Handlers**: Check separation of concerns
- **Validation**: Ensure FluentValidation usage

### 3. Dependency Flow
- Check dependency direction (Domain ← Application ← Infrastructure ← API)
- Identify any circular dependencies
- Review dependency injection registrations

### 4. Code Quality
- **Naming Conventions**: PascalCase, camelCase adherence
- **Async/Await**: Proper usage in I/O operations
- **Error Handling**: Global exception middleware, domain exceptions
- **Logging**: Structured logging with Serilog

### 5. Security Architecture
- **Authentication**: JWT implementation review
- **Authorization**: Attribute-based access control
- **Secret Management**: Azure Key Vault usage
- **Data Protection**: Encryption at rest/in transit
- **Input Validation**: SQL injection, XSS prevention

### 6. Performance
- **Database Queries**: N+1 query detection, AsNoTracking usage
- **Caching**: Identify caching opportunities
- **Async Processing**: Queue-based background jobs
- **API Response Times**: Check for bottlenecks

### 7. Scalability
- **Stateless Design**: Verify API is stateless
- **Horizontal Scaling**: Identify scaling bottlenecks
- **Background Workers**: Queue processing scalability
- **Database**: Connection pooling, index optimization

### 8. Frontend Architecture
- **Component Structure**: Standalone components, proper organization
- **State Management**: Signal usage, reactive patterns
- **Code Splitting**: Lazy loading implementation
- **Performance**: Change detection strategy

## Tasks to Perform

1. **Read Key Files**
   - Domain entities
   - Application handlers
   - Infrastructure services
   - API controllers
   - Frontend components and services

2. **Identify Issues**
   - Architecture violations
   - Anti-patterns
   - Performance bottlenecks
   - Security vulnerabilities

3. **Provide Recommendations**
   - Prioritized improvement list
   - Code refactoring suggestions
   - Architecture enhancements
   - Best practice implementations

## Output Format

### Summary
- Overall architecture health score (1-10)
- Major strengths
- Critical issues

### Detailed Findings

#### Layer Analysis
For each layer (Domain, Application, Infrastructure, API):
- Compliance with Clean Architecture
- Issues found
- Recommendations

#### Pattern Analysis
- CQRS adherence
- DDD implementation
- Service patterns

#### Code Quality Metrics
- Estimated code coverage
- Cyclomatic complexity concerns
- Code duplication

#### Security Analysis
- Vulnerabilities found
- Best practices followed
- Recommendations

### Recommended Actions

Priority-ordered list of improvements:
1. **Critical** (fix immediately)
2. **High** (fix soon)
3. **Medium** (plan for next sprint)
4. **Low** (consider for future)

Each action should include:
- Description
- Rationale
- Implementation guidance
- Estimated effort
