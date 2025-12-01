# Database Migration Command

Help create and apply Entity Framework Core migrations for schema changes in CV-Analyzer.

## Steps to Perform

1. **Understand the Change**
   - Ask what entities are being modified/added
   - Review the entity changes in `backend/src/CVAnalyzer.Domain/Entities/`
   - Check if entity configurations need updates in `backend/src/CVAnalyzer.Infrastructure/Persistence/Configurations/`

2. **Check DbContext**
   - Verify the entity is added to `ApplicationDbContext.cs`
   - Check if DbSet<TEntity> property exists
   - Confirm entity configuration is applied in OnModelCreating

3. **Create Migration**
   - Navigate to Infrastructure project
   - Run migration command with descriptive name
   ```bash
   cd backend/src/CVAnalyzer.Infrastructure
   dotnet ef migrations add {MigrationName} --startup-project ../CVAnalyzer.API
   ```

4. **Review Migration Files**
   - Check generated migration in `Migrations/` folder
   - Review Up() method for correct schema changes
   - Review Down() method for rollback logic
   - Verify ModelSnapshot is updated

5. **Apply Migration**
   ```bash
   dotnet ef database update --startup-project ../CVAnalyzer.API
   ```

6. **Verify**
   - Confirm migration was applied successfully
   - Check database schema matches expectations
   - Test the feature that required the migration

## Migration Naming Conventions

Use descriptive names that explain the change:
- `AddUserAuthenticationSupport` (adding new feature)
- `AddEmailVerificationToUser` (adding column)
- `RemoveDeprecatedResumeFields` (removing columns)
- `CreateNotificationTable` (new table)
- `UpdateResumeIndexes` (index changes)

## Common Issues

- **Connection string errors**: Verify appsettings.Development.json exists with valid connection string
- **Migration conflicts**: If multiple migrations exist, ensure they're in correct order
- **Pending migrations**: Check with `dotnet ef migrations list`
- **Rollback**: Use `dotnet ef database update {PreviousMigrationName}` to rollback

## Best Practices

- Always review generated migration code before applying
- Test migrations on local database first
- Include migration in the same commit as entity changes
- Add data seeding if needed (in ApplicationDbContextSeed)
- Document breaking changes in migration comments
