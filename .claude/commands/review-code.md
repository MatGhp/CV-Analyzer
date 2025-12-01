# Code Review Command

Review the current code changes against CV-Analyzer coding standards and best practices.

## What to Check

### Backend (C#)
- Clean Architecture adherence (proper layer separation)
- CQRS pattern implementation (commands vs queries)
- FluentValidation validators for all commands
- Async/await usage for I/O operations
- Proper error handling and logging
- Security considerations (secrets, SQL injection, XSS)
- Naming conventions (PascalCase for public, camelCase with underscore for private)
- Null safety (nullable reference types)
- XML documentation for public APIs
- Unit test coverage

### Frontend (TypeScript/Angular)
- Standalone component usage
- Signal-based state management where appropriate
- Proper use of RxJS operators
- Type safety (no 'any' types)
- Accessibility (ARIA labels, semantic HTML)
- Responsive design implementation
- Error handling in HTTP calls
- Proper unsubscription (or async pipe usage)
- Component unit tests

### General
- No hardcoded secrets or API keys
- Proper git commit message format (conventional commits)
- Code documentation and comments where needed
- Performance considerations
- Security vulnerabilities (OWASP Top 10)

## Review Steps

1. Run `git status` and `git diff` to see changes
2. Identify files changed and their layer (Domain/Application/Infrastructure/API/Frontend)
3. Review each file against the relevant checklist above
4. Provide actionable feedback with code examples
5. Highlight security concerns if any
6. Suggest improvements for code quality

## Output Format

Provide a structured review with:
- **Summary**: Overall assessment
- **Issues Found**: Critical/High/Medium/Low priority issues
- **Recommendations**: Specific improvements with code examples
- **Approval Status**: Approved / Needs Changes / Blocked
