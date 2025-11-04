# CV Analyzer - Angular Frontend

Modern Angular 19 application for CV/Resume analysis using zoneless architecture, standalone components, and signals.

## 🏗️ Architecture

### Folder Structure (Best Practices)

```
src/app/
├── core/                    # Singleton services, guards, interceptors
│   ├── guards/             # Route guards
│   ├── interceptors/       # HTTP interceptors (API, auth, error handling)
│   ├── models/             # Core domain models and interfaces
│   └── services/           # Singleton services (API, auth, state)
├── features/               # Feature modules (lazy-loaded)
│   ├── resume-upload/      # Resume upload feature
│   └── resume-analysis/    # Resume analysis and results feature
├── shared/                 # Shared components, directives, pipes
│   ├── components/         # Reusable UI components
│   ├── directives/         # Shared directives
│   └── pipes/              # Shared pipes
└── environments/           # Environment configurations
```

### Key Technologies

- **Angular 19** - Latest version with zoneless change detection
- **Standalone Components** - Modern Angular architecture (no NgModules)
- **Signals** - Reactive state management
- **SCSS** - Styling with variables and mixins
- **TypeScript Strict Mode** - Enhanced type safety
- **Routing** - Client-side routing with lazy loading

## 🚀 Development

### Prerequisites

- Node.js 18+ and npm
- Angular CLI (`npm install -g @angular/cli`)

### Install Dependencies

```bash
npm install
```

### Development Server

```bash
npm start
# or
ng serve
```

Navigate to `http://localhost:4200/`. The application automatically reloads when you change source files.

### API Proxy

The dev server is configured to proxy API requests:
- `/api/*` → `http://localhost:5000` (.NET Backend)
- `/ai/*` → `http://localhost:8000` (Python AI Service)

Configuration in [`proxy.conf.json`](proxy.conf.json).

### Build

```bash
# Development build
npm run build

# Production build
npm run build -- --configuration=production
```

Build artifacts are stored in `dist/`.

### Running Tests

```bash
# Unit tests
npm test

# End-to-end tests
npm run e2e
```

## 📁 Key Files

| File | Purpose |
|------|---------|
| `src/app/core/models/resume.model.ts` | Domain models (Resume, Suggestion, etc.) |
| `src/app/core/services/resume.service.ts` | HTTP service for resume operations |
| `src/app/core/interceptors/api.interceptor.ts` | Global HTTP interceptor |
| `src/environments/environment.ts` | Development environment config |
| `src/environments/environment.prod.ts` | Production environment config |
| `proxy.conf.json` | API proxy configuration |

## 🔧 Configuration

### Environment Variables

**Development** (`src/environments/environment.ts`):
```typescript
export const environment = {
  production: false,
  apiUrl: 'http://localhost:5000/api',
  aiServiceUrl: 'http://localhost:8000'
};
```

**Production** (`src/environments/environment.prod.ts`):
```typescript
export const environment = {
  production: true,
  apiUrl: '/api',
  aiServiceUrl: '/ai'
};
```

## 🎨 Code Style

- **Strict TypeScript** - All type checks enabled
- **Standalone Components** - No NgModules
- **Functional Guards/Interceptors** - Modern Angular patterns
- **Inject Function** - Dependency injection with `inject()`
- **Signals** - Reactive state with `signal()`, `computed()`, `effect()`

## 📦 Integration with Backend

### .NET Backend API (`http://localhost:5000/api`)

Endpoints:
- `POST /api/resumes/upload` - Upload resume file
- `GET /api/resumes/{id}` - Get resume details
- `GET /api/resumes/user/{userId}` - Get user resumes
- `POST /api/resumes/{id}/analyze` - Trigger AI analysis

### Python AI Service (`http://localhost:8000`)

Endpoints:
- `POST /analyze` - Analyze resume content
- `GET /health` - Health check

## 🐳 Docker Deployment

The frontend will be containerized and served via nginx. Build configuration TBD.

## 📚 Resources

- [Angular Documentation](https://angular.dev)
- [Angular Signals](https://angular.dev/guide/signals)
- [Standalone Components](https://angular.dev/guide/components/importing)
