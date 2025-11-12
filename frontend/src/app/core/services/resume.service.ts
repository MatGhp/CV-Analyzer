import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  Resume,
  UploadResumeRequest,
  UploadResumeResponse,
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
   * Uploads a resume file
   */
  uploadResume(request: UploadResumeRequest): Observable<UploadResumeResponse> {
    const formData = new FormData();
    formData.append('file', request.file);
    formData.append('userId', request.userId);

    return this.http.post<UploadResumeResponse>(
      `${this.apiUrl}/upload`,
      formData
    );
  }

  /**
   * Gets a single resume by ID
   */
  getResume(id: string): Observable<Resume> {
    return this.http.get<Resume>(`${this.apiUrl}/${id}`);
  }

  /**
   * Gets all resumes for a user
   */
  getUserResumes(userId: string): Observable<Resume[]> {
    return this.http.get<Resume[]>(`${this.apiUrl}/user/${userId}`);
  }

  /**
   * Triggers AI analysis for a resume
   */
  analyzeResume(id: string): Observable<AnalysisResponse> {
    return this.http.post<AnalysisResponse>(
      `${this.apiUrl}/${id}/analyze`,
      {}
    );
  }
}
