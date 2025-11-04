import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Resume, UploadResumeRequest, AnalyzeResumeResponse } from '../models/resume.model';

@Injectable({
  providedIn: 'root'
})
export class ResumeService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiUrl}/resumes`;

  uploadResume(request: UploadResumeRequest): Observable<{ id: string }> {
    const formData = new FormData();
    formData.append('file', request.file);
    formData.append('userId', request.userId);

    return this.http.post<{ id: string }>(
      `${this.apiUrl}/upload`,
      formData
    );
  }

  getResume(id: string): Observable<Resume> {
    return this.http.get<Resume>(`${this.apiUrl}/${id}`);
  }

  getUserResumes(userId: string): Observable<Resume[]> {
    return this.http.get<Resume[]>(`${this.apiUrl}/user/${userId}`);
  }

  analyzeResume(id: string): Observable<AnalyzeResumeResponse> {
    return this.http.post<AnalyzeResumeResponse>(
      `${this.apiUrl}/${id}/analyze`,
      {}
    );
  }
}
