# Task 5: Frontend Updates for Async Processing

**Estimated Time**: 1 day  
**Priority**: P0 (Required for user experience)  
**Dependencies**: Task 4 (API Updates) - Status endpoint must be deployed

---

## Overview

Update Angular frontend to support async resume processing with real-time status polling, candidate info display, and improved UX with loading states.

---

## Prerequisites

✅ Task 4 completed:
- API returns 202 Accepted on upload
- Status endpoint available (`GET /api/resumes/{id}/status`)
- Analysis endpoint includes CandidateInfo

---

## Deliverables

### 1. TypeScript Models

**File**: `frontend/src/app/core/models/resume.model.ts`

**Add new types**:
```typescript
export interface UploadResponse {
  resumeId: string;
  status: string;
  message: string;
}

export interface ResumeStatus {
  resumeId: string;
  status: 'pending' | 'processing' | 'complete' | 'failed';
  progress: number;
  errorMessage?: string;
}

export interface CandidateInfo {
  fullName: string;
  email: string;
  phone?: string;
  location?: string;
  skills: string[];
  yearsOfExperience?: number;
  currentJobTitle?: string;
  education?: string;
}

export interface AnalysisResponse {
  id: string;
  fileName: string;
  score?: number;
  uploadedAt: string;
  analyzedAt?: string;
  status: string;
  candidateInfo?: CandidateInfo;
  suggestions: Suggestion[];
  optimizedContent?: string;
}

export interface Suggestion {
  category: string;
  description: string;
  priority: number;
}
```

---

### 2. Resume Service with Polling

**File**: `frontend/src/app/core/services/resume.service.ts`

**Add status polling method**:
```typescript
import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, interval, switchMap, takeWhile, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import { 
  UploadResponse, 
  ResumeStatus, 
  AnalysisResponse 
} from '../models/resume.model';

@Injectable({ providedIn: 'root' })
export class ResumeService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiUrl}/resumes`;

  // Signal for current polling state
  isPolling = signal<boolean>(false);

  /**
   * Upload resume (returns immediately with 202)
   */
  uploadResume(file: File, userId: string): Observable<UploadResponse> {
    const formData = new FormData();
    formData.append('file', file);
    formData.append('userId', userId);
    
    return this.http.post<UploadResponse>(`${this.apiUrl}/upload`, formData);
  }

  /**
   * Check resume status (single request)
   */
  checkStatus(resumeId: string): Observable<ResumeStatus> {
    return this.http.get<ResumeStatus>(`${this.apiUrl}/${resumeId}/status`);
  }

  /**
   * Poll resume status until complete/failed (every 2 seconds)
   */
  pollResumeStatus(resumeId: string): Observable<ResumeStatus> {
    this.isPolling.set(true);

    return interval(2000).pipe(
      switchMap(() => this.checkStatus(resumeId)),
      tap(status => {
        console.log(`[ResumeService] Status: ${status.status} (${status.progress}%)`);
        
        // Stop polling when complete or failed
        if (status.status === 'complete' || status.status === 'failed') {
          this.isPolling.set(false);
        }
      }),
      takeWhile(
        status => status.status !== 'complete' && status.status !== 'failed',
        true // Include final emission
      )
    );
  }

  /**
   * Get full analysis results (call when status = complete)
   */
  getAnalysis(resumeId: string): Observable<AnalysisResponse> {
    return this.http.get<AnalysisResponse>(`${this.apiUrl}/${resumeId}/analysis`);
  }
}
```

**Key Features**:
- `interval(2000)` polls every 2 seconds
- `switchMap` cancels previous request if still pending
- `takeWhile(..., true)` includes final emission (complete/failed)
- Signal tracks polling state for UI feedback

---

### 3. Candidate Info Card Component

**File**: `frontend/src/app/shared/components/candidate-info-card/candidate-info-card.component.ts`

**Create standalone component**:
```typescript
import { Component, input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { CandidateInfo } from '../../../core/models/resume.model';

@Component({
  selector: 'app-candidate-info-card',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './candidate-info-card.component.html',
  styleUrl: './candidate-info-card.component.scss'
})
export class CandidateInfoCardComponent {
  candidateInfo = input.required<CandidateInfo>();
}
```

**File**: `frontend/src/app/shared/components/candidate-info-card/candidate-info-card.component.html`

**Template with fallbacks**:
```html
<div class="candidate-card">
  <h2 class="candidate-name">{{ candidateInfo().fullName }}</h2>
  
  <div class="contact-info">
    <p class="email">
      <i class="icon-email"></i>
      {{ candidateInfo().email }}
    </p>
    
    @if (candidateInfo().phone) {
      <p class="phone">
        <i class="icon-phone"></i>
        {{ candidateInfo().phone }}
      </p>
    }
    
    @if (candidateInfo().location) {
      <p class="location">
        <i class="icon-location"></i>
        {{ candidateInfo().location }}
      </p>
    }
  </div>

  @if (candidateInfo().currentJobTitle) {
    <div class="job-info">
      <h3>Current Position</h3>
      <p>{{ candidateInfo().currentJobTitle }}</p>
      
      @if (candidateInfo().yearsOfExperience) {
        <p class="experience">
          {{ candidateInfo().yearsOfExperience }} years of experience
        </p>
      }
    </div>
  }

  @if (candidateInfo().skills.length > 0) {
    <div class="skills-section">
      <h3>Skills</h3>
      <div class="skills-badges">
        @for (skill of candidateInfo().skills; track skill) {
          <span class="skill-badge">{{ skill }}</span>
        }
      </div>
    </div>
  }

  @if (candidateInfo().education) {
    <div class="education-section">
      <h3>Education</h3>
      <p>{{ candidateInfo().education }}</p>
    </div>
  }
</div>
```

**File**: `frontend/src/app/shared/components/candidate-info-card/candidate-info-card.component.scss`

**Responsive styles**:
```scss
.candidate-card {
  background: white;
  border-radius: 8px;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
  padding: 24px;
  margin-bottom: 24px;

  .candidate-name {
    font-size: 24px;
    font-weight: 600;
    margin-bottom: 16px;
    color: #2c3e50;
  }

  .contact-info {
    display: flex;
    flex-wrap: wrap;
    gap: 16px;
    margin-bottom: 24px;
    
    p {
      display: flex;
      align-items: center;
      gap: 8px;
      margin: 0;
      color: #666;
      
      i {
        color: #3498db;
      }
    }
  }

  .job-info,
  .skills-section,
  .education-section {
    margin-bottom: 20px;
    
    h3 {
      font-size: 16px;
      font-weight: 600;
      margin-bottom: 12px;
      color: #34495e;
    }
    
    p {
      margin: 0;
      color: #555;
    }
  }

  .skills-badges {
    display: flex;
    flex-wrap: wrap;
    gap: 8px;
    
    .skill-badge {
      background: #ecf0f1;
      color: #2c3e50;
      padding: 6px 12px;
      border-radius: 20px;
      font-size: 14px;
      font-weight: 500;
    }
  }

  .experience {
    font-size: 14px;
    color: #7f8c8d;
    margin-top: 4px;
  }
}

// Mobile responsive
@media (max-width: 768px) {
  .candidate-card {
    padding: 16px;
    
    .candidate-name {
      font-size: 20px;
    }
    
    .contact-info {
      flex-direction: column;
      gap: 8px;
    }
  }
}
```

---

### 4. Resume Upload Component

**File**: `frontend/src/app/features/resume-upload/resume-upload.component.ts`

**Refactor for async workflow**:
```typescript
import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { ResumeService } from '../../core/services/resume.service';

@Component({
  selector: 'app-resume-upload',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './resume-upload.component.html',
  styleUrl: './resume-upload.component.scss'
})
export class ResumeUploadComponent {
  private readonly resumeService = inject(ResumeService);
  private readonly router = inject(Router);

  selectedFile = signal<File | null>(null);
  isUploading = signal<boolean>(false);
  errorMessage = signal<string | null>(null);

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files?.length) {
      const file = input.files[0];
      
      // Validate file type
      const allowedTypes = ['application/pdf', 'application/vnd.openxmlformats-officedocument.wordprocessingml.document'];
      if (!allowedTypes.includes(file.type)) {
        this.errorMessage.set('Only PDF and DOCX files are allowed');
        this.selectedFile.set(null);
        return;
      }
      
      // Validate file size (10MB)
      const maxSizeBytes = 10 * 1024 * 1024;
      if (file.size > maxSizeBytes) {
        this.errorMessage.set('File size must not exceed 10MB');
        this.selectedFile.set(null);
        return;
      }
      
      this.selectedFile.set(file);
      this.errorMessage.set(null);
    }
  }

  async uploadResume(): Promise<void> {
    const file = this.selectedFile();
    if (!file) return;

    this.isUploading.set(true);
    this.errorMessage.set(null);

    try {
      const userId = 'user-123'; // TODO: Get from auth service
      
      // Upload returns immediately (202 Accepted)
      const response = await this.resumeService.uploadResume(file, userId).toPromise();
      
      console.log('Upload successful:', response);
      
      // Navigate to analysis page with resume ID
      await this.router.navigate(['/analysis', response.resumeId]);
      
    } catch (error: any) {
      console.error('Upload failed:', error);
      this.errorMessage.set(error.error?.message || 'Upload failed. Please try again.');
    } finally {
      this.isUploading.set(false);
    }
  }
}
```

**File**: `frontend/src/app/features/resume-upload/resume-upload.component.html`

**Template with validation feedback**:
```html
<div class="upload-container">
  <h1>Upload Your Resume</h1>
  <p class="subtitle">Get AI-powered feedback to improve your CV</p>

  <div class="upload-form">
    <label class="file-input-label">
      <input 
        type="file" 
        (change)="onFileSelected($event)"
        accept=".pdf,.docx"
        [disabled]="isUploading()"
      />
      <span class="file-input-button">
        Choose File
      </span>
    </label>

    @if (selectedFile()) {
      <p class="file-name">
        <i class="icon-file"></i>
        {{ selectedFile()!.name }}
        <span class="file-size">({{ (selectedFile()!.size / 1024 / 1024).toFixed(2) }} MB)</span>
      </p>
    }

    @if (errorMessage()) {
      <div class="error-alert">
        {{ errorMessage() }}
      </div>
    }

    <button 
      class="upload-button"
      [disabled]="!selectedFile() || isUploading()"
      (click)="uploadResume()"
    >
      @if (isUploading()) {
        <span class="spinner"></span>
        Uploading...
      } @else {
        Upload & Analyze
      }
    </button>
  </div>

  <div class="info-section">
    <h3>Supported formats</h3>
    <ul>
      <li>PDF (.pdf)</li>
      <li>Microsoft Word (.docx)</li>
    </ul>
    <p class="note">Maximum file size: 10MB</p>
  </div>
</div>
```

---

### 5. Resume Analysis Component

**File**: `frontend/src/app/features/resume-analysis/resume-analysis.component.ts`

**Add status polling on init**:
```typescript
import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute } from '@angular/router';
import { ResumeService } from '../../core/services/resume.service';
import { CandidateInfoCardComponent } from '../../shared/components/candidate-info-card/candidate-info-card.component';
import { 
  ResumeStatus, 
  AnalysisResponse 
} from '../../core/models/resume.model';

@Component({
  selector: 'app-resume-analysis',
  standalone: true,
  imports: [CommonModule, CandidateInfoCardComponent],
  templateUrl: './resume-analysis.component.html',
  styleUrl: './resume-analysis.component.scss'
})
export class ResumeAnalysisComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly resumeService = inject(ResumeService);

  resumeId = signal<string>('');
  status = signal<ResumeStatus | null>(null);
  analysis = signal<AnalysisResponse | null>(null);
  isLoading = signal<boolean>(true);
  errorMessage = signal<string | null>(null);

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) {
      this.errorMessage.set('Invalid resume ID');
      this.isLoading.set(false);
      return;
    }

    this.resumeId.set(id);
    this.startPolling();
  }

  private startPolling(): void {
    this.resumeService.pollResumeStatus(this.resumeId()).subscribe({
      next: (statusUpdate) => {
        this.status.set(statusUpdate);
        
        // Fetch full analysis when complete
        if (statusUpdate.status === 'complete') {
          this.loadAnalysis();
        }
        
        // Show error if failed
        if (statusUpdate.status === 'failed') {
          this.errorMessage.set(statusUpdate.errorMessage || 'Analysis failed');
          this.isLoading.set(false);
        }
      },
      error: (error) => {
        console.error('Polling error:', error);
        this.errorMessage.set('Failed to check status. Please refresh the page.');
        this.isLoading.set(false);
      }
    });
  }

  private loadAnalysis(): void {
    this.resumeService.getAnalysis(this.resumeId()).subscribe({
      next: (analysisData) => {
        this.analysis.set(analysisData);
        this.isLoading.set(false);
      },
      error: (error) => {
        console.error('Failed to load analysis:', error);
        this.errorMessage.set('Failed to load analysis results');
        this.isLoading.set(false);
      }
    });
  }
}
```

**File**: `frontend/src/app/features/resume-analysis/resume-analysis.component.html`

**Template with loading/error/success states**:
```html
<div class="analysis-container">
  <!-- Loading State -->
  @if (isLoading()) {
    <div class="loading-section">
      <div class="spinner-large"></div>
      <h2>Analyzing Your Resume</h2>
      
      @if (status()) {
        <div class="progress-bar">
          <div 
            class="progress-fill" 
            [style.width.%]="status()!.progress"
          ></div>
        </div>
        <p class="status-text">
          {{ status()!.status | titlecase }} - {{ status()!.progress }}%
        </p>
      }
      
      <p class="loading-message">
        This may take up to 30 seconds. Please don't close this page.
      </p>
    </div>
  }

  <!-- Error State -->
  @if (errorMessage() && !isLoading()) {
    <div class="error-section">
      <div class="error-icon">⚠️</div>
      <h2>Analysis Failed</h2>
      <p>{{ errorMessage() }}</p>
      <button class="retry-button" (click)="startPolling()">
        Retry
      </button>
    </div>
  }

  <!-- Success State -->
  @if (analysis() && !isLoading()) {
    <div class="results-section">
      <h1>Analysis Complete!</h1>
      
      <!-- Score Badge -->
      <div class="score-badge" [class.high-score]="(analysis()!.score || 0) >= 80">
        <span class="score-value">{{ analysis()!.score }}</span>
        <span class="score-label">/100</span>
      </div>

      <!-- Candidate Info Card -->
      @if (analysis()!.candidateInfo) {
        <app-candidate-info-card 
          [candidateInfo]="analysis()!.candidateInfo!"
        />
      }

      <!-- Suggestions -->
      @if (analysis()!.suggestions.length > 0) {
        <div class="suggestions-section">
          <h2>Improvement Suggestions</h2>
          @for (suggestion of analysis()!.suggestions; track suggestion.category) {
            <div class="suggestion-card" [class.priority-high]="suggestion.priority >= 4">
              <h3>{{ suggestion.category }}</h3>
              <p>{{ suggestion.description }}</p>
              <span class="priority-badge">
                Priority: {{ suggestion.priority }}/5
              </span>
            </div>
          }
        </div>
      }

      <!-- Optimized Content -->
      @if (analysis()!.optimizedContent) {
        <div class="optimized-content-section">
          <h2>Optimized Version</h2>
          <div class="content-box">
            <pre>{{ analysis()!.optimizedContent }}</pre>
          </div>
        </div>
      }
    </div>
  }
</div>
```

---

## Routing Configuration

**File**: `frontend/src/app/app.routes.ts`

**Update routes**:
```typescript
import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    redirectTo: '/upload',
    pathMatch: 'full'
  },
  {
    path: 'upload',
    loadComponent: () => import('./features/resume-upload/resume-upload.component')
      .then(m => m.ResumeUploadComponent)
  },
  {
    path: 'analysis/:id',
    loadComponent: () => import('./features/resume-analysis/resume-analysis.component')
      .then(m => m.ResumeAnalysisComponent)
  },
  {
    path: '**',
    redirectTo: '/upload'
  }
];
```

---

## Environment Configuration

**File**: `frontend/src/environments/environment.ts`

**Development**:
```typescript
export const environment = {
  production: false,
  apiUrl: 'http://localhost:5000/api'
};
```

**File**: `frontend/src/environments/environment.prod.ts`

**Production** (with nginx proxy):
```typescript
export const environment = {
  production: true,
  apiUrl: '/api'  // Nginx proxies to backend
};
```

---

## Acceptance Criteria

- [ ] Upload page validates file type and size before upload
- [ ] Upload returns 202 and navigates to analysis page
- [ ] Status polling starts automatically on analysis page load
- [ ] Loading spinner shows during processing (with progress %)
- [ ] Candidate info card displays all extracted fields with fallbacks
- [ ] Skills render as styled badges
- [ ] Suggestions grouped by category with priority indicators
- [ ] Error state shows retry button
- [ ] Mobile responsive (< 768px breakpoint)
- [ ] No console errors during polling

---

## Unit Tests

### ResumeService Tests

**File**: `frontend/src/app/core/services/resume.service.spec.ts`

**Test Cases**:
- ✅ `uploadResume_ValidFile_CallsHttpPost`
- ✅ `checkStatus_ValidId_ReturnsResumeStatus`
- ✅ `pollResumeStatus_CompleteStatus_StopsPolling`
- ✅ `pollResumeStatus_FailedStatus_StopsPolling`

### CandidateInfoCardComponent Tests

**File**: `frontend/src/app/shared/components/candidate-info-card/candidate-info-card.component.spec.ts`

**Test Cases**:
- ✅ `Component_ValidCandidateInfo_RendersAllFields`
- ✅ `Component_MissingOptionalFields_ShowsFallback`
- ✅ `Component_EmptySkills_DoesNotRenderSkillsSection`

---

## Troubleshooting

### Issue: Polling doesn't stop

**Check takeWhile condition**:
```typescript
// Ensure inclusive flag is true
takeWhile(
  status => status.status !== 'complete' && status.status !== 'failed',
  true  // Include final emission
)
```

### Issue: CORS errors

**Verify proxy configuration** (`frontend/proxy.conf.json`):
```json
{
  "/api": {
    "target": "http://localhost:5000",
    "secure": false,
    "changeOrigin": true
  }
}
```

**Start dev server**:
```bash
npm start -- --proxy-config proxy.conf.json
```

---

## Rollback Plan

1. Revert routes to remove `/analysis/:id`
2. Restore old upload component (sync API call)
3. Remove polling service methods
4. No data loss: Backend still supports sync workflow

---

## Next Steps

After Task 5 completion:
- **Task 6**: Testing & Deployment (E2E tests, production deployment, monitoring setup)
