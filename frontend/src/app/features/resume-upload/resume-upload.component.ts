import { Component, signal, inject, output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FileUploadComponent } from '../../shared/components/file-upload.component';
import { ResumeService } from '../../core/services/resume.service';
import { AuthService } from '../../core/services/auth.service';
import { AnalysisResponse } from '../../core/models/resume.model';
import { ERROR_MESSAGES } from '../../core/constants/ui.constants';
import { catchError, finalize, of, switchMap } from 'rxjs';

type UploadState = 'idle' | 'uploading' | 'analyzing' | 'success' | 'error';

@Component({
  selector: 'app-resume-upload',
  standalone: true,
  imports: [CommonModule, FileUploadComponent],
  templateUrl: './resume-upload.component.html',
  styleUrl: './resume-upload.component.scss'
})
export class ResumeUploadComponent {
  private readonly resumeService = inject(ResumeService);
  private readonly authService = inject(AuthService);

  analysisComplete = output<AnalysisResponse>();

  selectedFile = signal<File | null>(null);
  uploadState = signal<UploadState>('idle');
  errorMessage = signal<string | null>(null);
  resumeId = signal<string | null>(null);

  isProcessing = () => {
    const state = this.uploadState();
    return state === 'uploading' || state === 'analyzing';
  };

  onFileSelected(file: File): void {
    this.selectedFile.set(file);
    this.errorMessage.set(null);
    this.uploadState.set('idle');
  }

  onFileRemoved(): void {
    this.selectedFile.set(null);
    this.errorMessage.set(null);
    this.uploadState.set('idle');
    this.resumeId.set(null);
  }

  analyzeResume(): void {
    const file = this.selectedFile();
    if (!file) return;

    this.errorMessage.set(null);
    this.uploadState.set('uploading');

    const userId = this.authService.getCurrentUserId();
    this.resumeService.uploadResume({ userId, file })
      .pipe(
        takeUntilDestroyed(),
        switchMap(response => {
          this.resumeId.set(response.id);
          this.uploadState.set('analyzing');
          return this.resumeService.analyzeResume(response.id);
        }),
        catchError(error => {
          console.error('Analysis failed:', error);
          this.uploadState.set('error');
          this.errorMessage.set(
            error?.error?.message || ERROR_MESSAGES.ANALYSIS_FAILED
          );
          return of(null);
        }),
        finalize(() => {
          if (this.uploadState() === 'analyzing') {
            this.uploadState.set('success');
          }
        })
      )
      .subscribe(result => {
        if (result) {
          this.uploadState.set('success');
          this.analysisComplete.emit(result);
        }
      });
  }
}
