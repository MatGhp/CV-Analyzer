import { Component, signal, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { FileUploadComponent } from '../../shared/components/file-upload.component';
import { ResumeService } from '../../core/services/resume.service';
import { ERROR_MESSAGES } from '../../core/constants/ui.constants';

type UploadState = 'idle' | 'uploading' | 'success' | 'error';

@Component({
  selector: 'app-resume-upload',
  standalone: true,
  imports: [CommonModule, FileUploadComponent],
  templateUrl: './resume-upload.component.html',
  styleUrl: './resume-upload.component.scss'
})
export class ResumeUploadComponent {
  private readonly resumeService = inject(ResumeService);
  private readonly router = inject(Router);

  selectedFile = signal<File | null>(null);
  uploadState = signal<UploadState>('idle');
  errorMessage = signal<string | null>(null);

  isProcessing = () => this.uploadState() === 'uploading';

  onFileSelected(file: File): void {
    this.selectedFile.set(file);
    this.errorMessage.set(null);
    this.uploadState.set('idle');
  }

  onFileRemoved(): void {
    this.selectedFile.set(null);
    this.errorMessage.set(null);
    this.uploadState.set('idle');
  }

  async analyzeResume(): Promise<void> {
    const file = this.selectedFile();
    if (!file) return;

    this.errorMessage.set(null);
    this.uploadState.set('uploading');

    try {
      // TODO: Replace with actual user ID from authentication
      const userId = this.getUserId();
      
      const response = await firstValueFrom(
        this.resumeService.uploadResume({ file, userId })
      );
      
      if (!response) {
        throw new Error('No response from server');
      }

      this.uploadState.set('success');
      await this.router.navigate(['/analysis', response.resumeId]);
      
    } catch (error: any) {
      console.error('Upload failed:', error);
      this.uploadState.set('error');
      this.errorMessage.set(error?.error?.message || ERROR_MESSAGES.ANALYSIS_FAILED);
    }
  }

  /**
   * Get or generate user ID for resume upload
   * TODO: Replace with actual authentication once implemented
   * For now, generates a session-based ID stored in localStorage
   */
  private getUserId(): string {
    const storageKey = 'cv-analyzer-session-id';
    let userId = localStorage.getItem(storageKey);
    
    if (!userId) {
      // Generate a simple session ID (in production, use proper auth)
      userId = `guest-${Date.now()}-${Math.random().toString(36).substring(2, 9)}`;
      localStorage.setItem(storageKey, userId);
    }
    
    return userId;
  }
}
