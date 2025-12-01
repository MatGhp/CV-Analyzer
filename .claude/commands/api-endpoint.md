# Create API Endpoint Command

Scaffold a new API endpoint following the CQRS + Clean Architecture pattern used in CV-Analyzer.

## What to Create

Ask the user for:
1. **Feature name** (e.g., "Resume", "User", "Notification")
2. **Operation** (e.g., "Create", "Update", "Delete", "GetById", "GetList")
3. **Is it a command or query?** (Commands modify state, Queries read state)

## Files to Generate

### For Commands (Create/Update/Delete)

1. **Command Class**: `backend/src/CVAnalyzer.Application/Features/{Feature}/Commands/{Action}Command.cs`
   - Implement `IRequest<TResponse>`
   - Include required properties

2. **Command Handler**: `backend/src/CVAnalyzer.Application/Features/{Feature}/Commands/{Action}CommandHandler.cs`
   - Implement `IRequestHandler<TCommand, TResponse>`
   - Include constructor with dependencies (IApplicationDbContext, ILogger)
   - Implement Handle method with async/await

3. **Command Validator**: `backend/src/CVAnalyzer.Application/Features/{Feature}/Commands/{Action}CommandValidator.cs`
   - Extend `AbstractValidator<TCommand>`
   - Define validation rules

4. **Controller Endpoint**: `backend/src/CVAnalyzer.API/Controllers/{Feature}Controller.cs`
   - Add POST/PUT/DELETE endpoint
   - Include `[HttpPost]`, `[Authorize]`, `[ProducesResponseType]` attributes
   - Return appropriate status codes

### For Queries (Get operations)

1. **Query Class**: `backend/src/CVAnalyzer.Application/Features/{Feature}/Queries/{Action}Query.cs`
   - Implement `IRequest<TResponse>`

2. **Query Handler**: `backend/src/CVAnalyzer.Application/Features/{Feature}/Queries/{Action}QueryHandler.cs`
   - Implement `IRequestHandler<TQuery, TResponse>`
   - Use `.AsNoTracking()` for read-only queries

3. **Query Validator** (if needed): Similar to command validator

4. **Controller Endpoint**: Add GET endpoint to controller

### DTOs (if needed)

Create response DTOs in `backend/src/CVAnalyzer.API/Models/{Feature}/`

## Example Structure

For a "CreateNotification" command:

```
backend/src/CVAnalyzer.Application/Features/Notifications/
  Commands/
    CreateNotificationCommand.cs
    CreateNotificationCommandHandler.cs
    CreateNotificationCommandValidator.cs

backend/src/CVAnalyzer.API/Controllers/
  NotificationsController.cs
```

## Implementation Guidelines

- Use dependency injection for services
- Add proper logging with structured logging
- Include XML documentation comments
- Handle errors appropriately
- Follow async/await best practices
- Add unit tests in `backend/tests/CVAnalyzer.UnitTests/`

Generate the scaffolding code following CV-Analyzer conventions.
