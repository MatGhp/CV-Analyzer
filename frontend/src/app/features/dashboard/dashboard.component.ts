import { Component, OnInit, OnDestroy, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, NavigationEnd } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { Subscription, filter } from 'rxjs';

interface Resume {
  resumeId: string;
  fileName: string;
  score: number | null;
  status: string;
  uploadedAt: string;
  analyzedAt: string | null;
  blobUrl: string | null;
}

interface UserProfile {
  userId: string;
  email: string;
  fullName: string;
  phone: string | null;
  createdAt: string;
  lastLoginAt: string | null;
  isEmailVerified: boolean;
  totalResumes: number;
}

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.scss'
})
export class DashboardComponent implements OnInit, OnDestroy {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);
  readonly authService = inject(AuthService);
  private routerSubscription?: Subscription;
  private isInitialLoad = true;

  resumes = signal<Resume[]>([]);
  profile = signal<UserProfile | null>(null);
  loading = signal(true);
  error = signal<string | null>(null);

  ngOnInit(): void {
    // Load data on initial load
    this.loadUserData();
    
    // Also reload data when navigating back to dashboard (skip initial)
    this.routerSubscription = this.router.events.pipe(
      filter(event => event instanceof NavigationEnd)
    ).subscribe((event) => {
      const navEvent = event as NavigationEnd;
      if (navEvent.urlAfterRedirects === '/dashboard') {
        if (this.isInitialLoad) {
          this.isInitialLoad = false;
        } else {
          this.loadUserData();
        }
      }
    });
  }

  ngOnDestroy(): void {
    this.routerSubscription?.unsubscribe();
  }

  async loadUserData(): Promise<void> {
    try {
      this.loading.set(true);
      this.error.set(null);

      console.log('Loading user data...');

      // Load user profile
      const profileResponse = await this.http.get<UserProfile>(`${environment.apiUrl}/auth/me`).toPromise();
      console.log('Profile response:', profileResponse);
      this.profile.set(profileResponse || null);

      // Load user resumes
      const resumesResponse = await this.http.get<Resume[]>(`${environment.apiUrl}/auth/me/resumes`).toPromise();
      console.log('Resumes response:', resumesResponse);
      this.resumes.set(resumesResponse || []);
      
      console.log('Profile signal value:', this.profile());
      console.log('Resumes signal value:', this.resumes());
    } catch (err: any) {
      console.error('Failed to load dashboard data:', err);
      this.error.set('Failed to load your data. Please try again.');
    } finally {
      this.loading.set(false);
      console.log('Loading complete. loading:', this.loading(), 'error:', this.error(), 'profile:', !!this.profile());
    }
  }

  getAverageScore(): number {
    const validScores = this.resumes().filter(r => r.score !== null && r.score > 0);
    if (validScores.length === 0) return 0;
    const sum = validScores.reduce((acc, r) => acc + (r.score || 0), 0);
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

  async deleteResume(resumeId: string, event: Event): Promise<void> {
    event.stopPropagation(); // Prevent triggering viewResumeDetails
    
    if (!confirm('Are you sure you want to delete this resume and its analysis? This action cannot be undone.')) {
      return;
    }

    try {
      await this.http.delete(`${environment.apiUrl}/resumes/${resumeId}`).toPromise();
      // Remove from local list
      this.resumes.update(resumes => resumes.filter(r => r.resumeId !== resumeId));
    } catch (err: any) {
      console.error('Failed to delete resume:', err);
      this.error.set('Failed to delete resume. Please try again.');
    }
  }
}
