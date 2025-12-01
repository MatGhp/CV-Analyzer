# Claude Agent Configuration

This directory contains Claude Code agent configuration and custom commands for the CV-Analyzer project.

## Files

### `project_instructions.md`
Comprehensive project documentation for Claude agents, including:
- Project overview and architecture
- Technology stack details
- Development guidelines and coding conventions
- Security best practices
- Testing requirements
- Common patterns and troubleshooting

This file is automatically loaded by Claude Code to provide context about the project.

## Custom Slash Commands

Custom commands are located in the `commands/` directory. Use them by typing `/command-name` in Claude Code.

### Available Commands

#### `/review-code`
Reviews current code changes against CV-Analyzer coding standards and best practices.
- Checks Clean Architecture adherence
- Validates CQRS pattern implementation
- Identifies security vulnerabilities
- Provides actionable feedback

**Usage**:
```
/review-code
```

#### `/api-endpoint`
Scaffolds a new API endpoint following the CQRS + Clean Architecture pattern.
- Creates command/query classes
- Generates handlers and validators
- Adds controller endpoints
- Follows project conventions

**Usage**:
```
/api-endpoint
```
Then follow the prompts to specify feature name and operation type.

#### `/migration`
Helps create and apply Entity Framework Core migrations.
- Reviews entity changes
- Generates migration with proper naming
- Applies migration to database
- Troubleshoots common issues

**Usage**:
```
/migration
```

#### `/component`
Scaffolds a new Angular component with best practices.
- Generates component files (TS, HTML, SCSS, spec)
- Follows standalone component pattern
- Uses signals for state management
- Implements accessibility features

**Usage**:
```
/component
```

#### `/test-plan`
Generates comprehensive test plans for features.
- Creates unit and integration test cases
- Provides test scaffolding code
- Identifies edge cases
- Sets coverage goals

**Usage**:
```
/test-plan
```

#### `/architecture`
Analyzes architecture and provides recommendations.
- Checks Clean Architecture compliance
- Reviews CQRS pattern usage
- Identifies security issues
- Provides prioritized improvements

**Usage**:
```
/architecture
```

## How to Use

1. **Automatic Context Loading**
   - Claude Code automatically loads `project_instructions.md` when working in this repository
   - No action needed - the agent will have full project context

2. **Using Slash Commands**
   - Type `/` in Claude Code to see available commands
   - Select a command or type its name
   - Follow any prompts the command provides

3. **Adding New Commands**
   - Create a new `.md` file in `commands/` directory
   - Add clear instructions for what the command should do
   - The command will be automatically available as `/filename`

## Benefits

- **Consistent Code Quality**: Automated checks against project standards
- **Faster Development**: Scaffolding commands reduce boilerplate
- **Better Onboarding**: New developers get instant project context
- **Reduced Errors**: Guided workflows for complex tasks (migrations, endpoints)
- **Knowledge Preservation**: Project conventions documented and enforced

## Maintenance

- Update `project_instructions.md` when:
  - Adding new dependencies or technologies
  - Changing architectural patterns
  - Updating coding conventions
  - Adding major features

- Update commands when:
  - Project structure changes
  - New patterns are adopted
  - Common tasks need streamlining

## Examples

### Creating a New API Feature

```
User: I need to add a notification system
Claude: I'll help you scaffold the notification API endpoint. Let me use /api-endpoint

[Command runs, asks for details]

User: Feature: Notification, Operation: Create, Type: Command
Claude: [Generates all necessary CQRS files following project conventions]
```

### Code Review Before Committing

```
User: /review-code
Claude: [Reviews all staged changes]
- Checks Clean Architecture compliance
- Validates security best practices
- Suggests improvements
- Provides approval status
```

## Tips

- Use `/review-code` before creating pull requests
- Use `/architecture` periodically to maintain code quality
- Use `/test-plan` when implementing new features
- Use scaffolding commands (`/api-endpoint`, `/component`) to maintain consistency

## Contributing

When adding new commands:
1. Follow the existing command structure
2. Provide clear, actionable instructions
3. Include examples where helpful
4. Document the command in this README
5. Test the command thoroughly before committing
