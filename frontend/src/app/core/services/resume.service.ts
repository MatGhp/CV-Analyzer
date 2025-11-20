import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, timer } from 'rxjs';
import { switchMap, takeWhile, tap } from 'rxjs/operators';
import { environment } from '../../../environments/environment';
import {
  Resume,
  UploadResumeRequest,
  UploadResumeResponse,
  UploadResponse,
  ResumeStatusResponse,
  AnalysisResponse,
  FileValidationResult,
  FILE_CONSTRAINTS
} from '../models/resume.model';

@Injectable({
  providedIn: 'root'
})
export class ResumeService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiUrl}/resumes`;

  isPolling = signal<boolean>(false);

  validateFile(file: File): FileValidationResult {
    if (file.size > FILE_CONSTRAINTS.maxSizeBytes) {
      return {
        valid: false,
        error: `File size must be less than ${FILE_CONSTRAINTS.maxSizeBytes / 1024 / 1024}MB`
      };
    }

    const fileExtension = ('.' + file.name.split('.').pop()?.toLowerCase()) as string;
    const allowedExtensions = FILE_CONSTRAINTS.allowedExtensions as readonly string[];
    if (!allowedExtensions.includes(fileExtension)) {
      return {
        valid: false,
        error: 'Only PDF and DOCX files are supported'
      };
    }

    const allowedTypes = FILE_CONSTRAINTS.allowedTypes as readonly string[];
    if (file.type && !allowedTypes.includes(file.type)) {
      return {
        valid: false,
        error: 'Only PDF and DOCX files are supported'
      };
    }

    return { valid: true };
  }

  /**
   * Uploads resume file - returns 202 Accepted for async processing.
   * UserId is optional and will be extracted from resume content if not provided.
   */
  uploadResume(request: UploadResumeRequest): Observable<UploadResponse> {
    const formData = new FormData();
    formData.append('file', request.file);
    
    if (request.userId) {
      formData.append('userId', request.userId);
    }

    return this.http.post<UploadResponse>(
      `${this.apiUrl}/upload`,
      formData
    );
  }

  checkStatus(resumeId: string): Observable<ResumeStatusResponse> {
    return this.http.get<ResumeStatusResponse>(`${this.apiUrl}/${resumeId}/status`);
  }

  /**
   * Polls resume status immediately, then every 2 seconds until complete or failed.
   * Emits final status before completing the observable.
   */
  pollResumeStatus(resumeId: string): Observable<ResumeStatusResponse> {
    this.isPolling.set(true);

    return timer(0, 2000).pipe(
      switchMap(() => this.checkStatus(resumeId)),
      tap(status => {
        if (status.status === 'complete' || status.status === 'failed') {
          this.isPolling.set(false);
        }
      }),
      takeWhile(
        status => status.status !== 'complete' && status.status !== 'failed',
        true
      )
    );
  }

  getAnalysis(resumeId: string): Observable<AnalysisResponse> {
    return this.http.get<AnalysisResponse>(`${this.apiUrl}/${resumeId}/analysis`);
  }

  getResume(id: string): Observable<Resume> {
    return this.http.get<Resume>(`${this.apiUrl}/${id}`);
  }

  getUserResumes(userId: string): Observable<Resume[]> {
    return this.http.get<Resume[]>(`${this.apiUrl}/user/${userId}`);
  }

  analyzeResume(id: string): Observable<AnalysisResponse> {
    return this.http.post<AnalysisResponse>(
      `${this.apiUrl}/${id}/analyze`,
      {}
    );
  }
}
