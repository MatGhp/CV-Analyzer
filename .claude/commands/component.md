# Create Angular Component Command

Scaffold a new Angular component following CV-Analyzer frontend conventions.

## Information Needed

Ask the user:
1. **Component name** (e.g., "resume-list", "notification-badge")
2. **Feature area** (e.g., "dashboard", "auth", "shared")
3. **Component type**:
   - Feature component (in features/)
   - Shared component (in shared/components/)
   - Core component (rare, usually services go in core/)

## What to Create

### 1. Component Files

Generate with Angular CLI:
```bash
cd frontend
ng generate component {feature-area}/{component-name} --standalone
```

This creates:
- `{component-name}.component.ts` - Component logic
- `{component-name}.component.html` - Template
- `{component-name}.component.scss` - Styles
- `{component-name}.component.spec.ts` - Tests

### 2. Component Structure

**TypeScript Component** should include:
- Standalone component decorator
- Signal-based state management (prefer signals over observables)
- Input/Output properties with proper typing
- Lifecycle hooks if needed (OnInit, OnDestroy)
- Dependency injection in constructor
- Public methods for template usage, private for internal logic

**Template** should include:
- Semantic HTML5 elements
- TailwindCSS + DaisyUI classes for styling
- Lucide icons for iconography
- ARIA labels for accessibility
- @if/@for control flow (not *ngIf/*ngFor)
- Proper event binding

**Styles** should:
- Use @apply for common Tailwind patterns
- Keep component-specific overrides minimal
- Follow mobile-first responsive design

### 3. Integration

- Add route in `app.routes.ts` if it's a routable component
- Import in parent component if it's a child component
- Add to exports if it's a shared component used across features

## Example Component Template

```typescript
import { Component, signal, input, output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { LucideAngularModule, Home } from 'lucide-angular';

@Component({
  selector: 'app-{component-name}',
  standalone: true,
  imports: [CommonModule, LucideAngularModule],
  templateUrl: './{component-name}.component.html',
  styleUrl: './{component-name}.component.scss'
})
export class {ComponentName}Component {
  // Signals for reactive state
  readonly dataSignal = signal<DataType | null>(null);

  // Inputs (with transform if needed)
  readonly inputData = input.required<string>();

  // Outputs
  readonly itemClicked = output<string>();

  // Icons
  readonly HomeIcon = Home;

  constructor(
    private readonly service: SomeService
  ) {}

  handleClick(item: string): void {
    this.itemClicked.emit(item);
  }
}
```

## Best Practices

- Use standalone components (no NgModules)
- Prefer signals for local state
- Use computed() for derived state
- Use effect() sparingly (only for side effects)
- Implement OnPush change detection where possible
- Use async pipe for observables in templates
- Keep components focused (Single Responsibility)
- Extract reusable logic to services
- Write unit tests for component behavior

## Accessibility Checklist

- [ ] Semantic HTML elements (<button>, <nav>, <article>)
- [ ] ARIA labels for icon-only buttons
- [ ] Keyboard navigation support
- [ ] Color contrast meets WCAG AA (4.5:1)
- [ ] Form inputs have associated labels
- [ ] Focus indicators visible

Generate the component following these conventions.
