# Test Plan Generation Command

Generate a comprehensive test plan for a feature in CV-Analyzer.

## What to Analyze

Ask the user:
1. **Feature name** or **Files to test**
2. **Test type**: Unit, Integration, or Both

## Test Categories

### Backend Tests

#### Unit Tests
Test individual components in isolation:
- **Command Handlers**: Verify business logic, validation, error handling
- **Query Handlers**: Verify data retrieval logic
- **Validators**: Test all validation rules and edge cases
- **Services**: Mock dependencies, test service methods
- **Mappers**: Test entity to DTO conversions

**Tools**: xUnit, NSubstitute (mocking), FluentAssertions

#### Integration Tests
Test full request pipeline:
- **API Endpoints**: Test HTTP requests/responses
- **Database Operations**: Use in-memory database
- **Authentication**: Test JWT token validation
- **Background Workers**: Test queue processing

**Tools**: xUnit, EF Core InMemory, WebApplicationFactory

### Frontend Tests

#### Component Tests
- **Rendering**: Verify template renders correctly
- **User Interactions**: Test button clicks, form submissions
- **Input/Output**: Test @Input properties and @Output events
- **State Management**: Test signal updates
- **Conditional Rendering**: Test @if/@for logic

**Tools**: Jasmine, Karma, TestBed

#### Service Tests
- **HTTP Calls**: Mock HttpClient, test API interactions
- **State Updates**: Test signal-based state changes
- **Error Handling**: Test error scenarios
- **Authentication**: Test token management

**Tools**: Jasmine, HttpClientTestingModule

## Test Plan Structure

Generate a test plan with:

### 1. Test Scope
- Features covered
- Files to test
- Dependencies to mock

### 2. Test Cases

For each test case, include:
- **Test Name**: Descriptive name following convention
- **Given**: Setup/preconditions
- **When**: Action being tested
- **Then**: Expected outcome
- **Priority**: Critical/High/Medium/Low

### 3. Test Implementation Outline

Provide code scaffolding for key tests:

**Backend Example**:
```csharp
public class CreateResumeCommandHandlerTests
{
    private readonly IApplicationDbContext _context;
    private readonly CreateResumeCommandHandler _handler;

    public CreateResumeCommandHandlerTests()
    {
        _context = Substitute.For<IApplicationDbContext>();
        _handler = new CreateResumeCommandHandler(_context, /*...*/);
    }

    [Fact]
    public async Task Handle_ValidCommand_CreatesResume()
    {
        // Arrange
        var command = new CreateResumeCommand { /* ... */ };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeEmpty();
    }
}
```

**Frontend Example**:
```typescript
describe('ComponentName', () => {
  let component: ComponentNameComponent;
  let fixture: ComponentFixture<ComponentNameComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ComponentNameComponent]
    }).compileComponents();

    fixture = TestBed.createComponent(ComponentNameComponent);
    component = fixture.componentInstance;
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
```

### 4. Edge Cases and Scenarios

Identify:
- Null/undefined inputs
- Invalid data formats
- Authorization failures
- Network errors
- Boundary conditions

### 5. Coverage Goals

- Target: 80%+ code coverage for business logic
- Critical paths: 100% coverage
- UI components: Focus on user interactions

### 6. Manual Testing Checklist

For features requiring manual testing:
- User flow testing
- Cross-browser testing
- Mobile responsiveness
- Accessibility testing (screen readers, keyboard navigation)

## Output

Provide:
1. Detailed test plan document
2. Code scaffolding for priority tests
3. List of mocks/test data needed
4. Recommended test execution order
5. Success criteria
