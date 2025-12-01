import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';

interface Resume {
  id: string;
  fileName: string;
  uploadDate: string;
  analysisScore: number;
  status: string;
}

interface UserProfile {
  id: string;
  email: string;
  fullName: string;
  phone: string;
  createdAt: string;
  isEmailVerified: boolean;
}

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.scss'
})
export class DashboardComponent implements OnInit {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);
  readonly authService = inject(AuthService);

  resumes = signal<Resume[]>([]);
  profile = signal<UserProfile | null>(null);
  loading = signal(true);
  error = signal<string | null>(null);

  ngOnInit(): void {
    this.loadUserData();
  }

  async loadUserData(): Promise<void> {
    try {
      this.loading.set(true);
      this.error.set(null);

      // Load user profile
      const profileResponse = await this.http.get<UserProfile>(`${environment.apiUrl}/auth/me`).toPromise();
      this.profile.set(profileResponse || null);

      // Load user resumes
      const resumesResponse = await this.http.get<Resume[]>(`${environment.apiUrl}/auth/me/resumes`).toPromise();
      this.resumes.set(resumesResponse || []);
    } catch (err: any) {
      console.error('Failed to load dashboard data:', err);
      this.error.set('Failed to load your data. Please try again.');
    } finally {
      this.loading.set(false);
    }
  }

  getAverageScore(): number {
    const validScores = this.resumes().filter(r => r.analysisScore > 0);
    if (validScores.length === 0) return 0;
    const sum = validScores.reduce((acc, r) => acc + r.analysisScore, 0);
    return Math.round(sum / validScores.length);
  }

  getScoreClass(score: number): string {
    if (score >= 80) return 'score-excellent';
    if (score >= 60) return 'score-good';
    if (score >= 40) return 'score-fair';
    return 'score-poor';
  }

  formatDate(dateString: string): string {
    const date = new Date(dateString);
    return date.toLocaleDateString('en-US', { 
      year: 'numeric', 
      month: 'short', 
      day: 'numeric' 
    });
  }

  logout(): void {
    this.authService.logout();
    this.router.navigate(['/']);
  }

  viewResumeDetails(resumeId: string): void {
    this.router.navigate(['/analysis', resumeId]);
  }

  uploadNewResume(): void {
    this.router.navigate(['/']);
  }
}
