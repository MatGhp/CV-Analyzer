# CV Analyzer - Angular Frontend

Modern Angular 20 application for CV/Resume analysis using zoneless architecture, standalone components, and signals.

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

- Angular 20 (zoneless, standalone components, signals)
- TypeScript strict mode, client-side routing with lazy loading
- Styling stack:
  - Tailwind CSS + PostCSS + Autoprefixer
  - DaisyUI (themeable components/utilities)
  - Open Props (design tokens) via CDN
  - SCSS for small utilities
- Added libraries (for speed and polish):
  - @angular/cdk — a11y, overlays, drag-drop, etc.
  - lucide-angular — icon components (not yet used in template)
  - @tanstack/angular-query-experimental — async caching (scaffold only)
  - @ngx-translate/core + @ngx-translate/http-loader — i18n (scaffold only)
  - ngx-dropzone — file drop area (optional; package is deprecated upstream)

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

Configuration in [`proxy.conf.json`](proxy.conf.json). The legacy `/ai/*` proxy is no longer used (Agent Service is integrated into the backend).

### Build

```bash
# Development build
npm run build

# Production build
npm run build -- --configuration=production
```

Build artifacts are stored in `dist/`.

Tailwind should be auto-detected by Angular when `tailwind.config.js` and `postcss.config.cjs` are present and `tailwindcss` is installed (handled by `npm install`).

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
| `tailwind.config.js` | Tailwind configuration (content globs, DaisyUI) |
| `postcss.config.cjs` | PostCSS config (tailwindcss, autoprefixer) |
| `src/styles.scss` | Global styles; includes Tailwind layers and small utilities |
| `src/index.html` | Adds Open Props via CDN for design tokens |

## 🔧 Configuration

### Environment Variables

**Development** (`src/environments/environment.ts`):
```typescript
export const environment = {
  production: false,
  apiUrl: 'http://localhost:5000/api'
};
```

**Production** (`src/environments/environment.prod.ts`):
```typescript
export const environment = {
  production: true,
  apiUrl: '/api'
};
```

## 🎨 Code Style

- Strict TypeScript — All type checks enabled
- Standalone Components — No NgModules
- Functional Guards/Interceptors — Modern Angular patterns
- `inject()` function — Dependency injection
- Signals — `signal()`, `computed()`, `effect()`
- Tailwind/DaisyUI — Utility-first styling with themes

## 📦 Integration with Backend

### .NET Backend API (`http://localhost:5000/api`)

Endpoints:
- `POST /api/resumes/upload` - Upload resume file
- `GET /api/resumes/{id}` - Get resume details
- `GET /api/resumes/user/{userId}` - Get user resumes
- `POST /api/resumes/{id}/analyze` - Trigger AI analysis

### Agent Service (Integrated)

The Agent Framework runs inside the backend process. See `.github/agent-framework.md` and `docs/AGENT_FRAMEWORK.md`.

## 🐳 Docker Deployment

The frontend is containerized and served via nginx; the nginx config uses internal DNS to reach the API in Azure Container Apps.

## 🧩 Notes on 3rd‑party libraries

- Tailwind/DaisyUI are integrated; you can use utilities and classes in templates. Example:
  - Buttons: `btn btn-primary`, `btn btn-accent`
  - Cards: `bg-base-100 rounded-xl shadow p-6`
- Open Props is loaded via CDN in `src/index.html`. If you prefer local import, uncomment the import in `src/styles.scss` and ensure the path `open-props/open-props.min.css` resolves in your environment.
- lucide-angular, ngx-translate, TanStack Angular Query, and ngx-dropzone are installed but not yet wired into components. They’re available for upcoming features:
  - lucide-angular: icon components
  - ngx-translate: i18n service + HTTP loader
  - Angular Query: data fetching/cache
  - ngx-dropzone: drag-and-drop file upload (note: upstream package is deprecated)

For an overview of the MVP UI and next steps, see `docs/FRONTEND_MVP.md`.

## 📚 Resources

- [Angular Documentation](https://angular.dev)
- [Angular Signals](https://angular.dev/guide/signals)
- [Standalone Components](https://angular.dev/guide/components/importing)
