import { Injectable, inject, signal, computed, effect } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { Observable, tap, catchError, throwError } from 'rxjs';
import { environment } from '../../../environments/environment';
import { User, RegisterRequest, RegisterResponse, LoginRequest, LoginResponse } from '../models/auth.model';
import { ERROR_MESSAGES } from '../constants/ui.constants';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);
  private readonly apiUrl = `${environment.apiUrl}/auth`;

  // Signals for reactive state management
  private readonly tokenSignal = signal<string | null>(this.getStoredToken());
  private readonly userSignal = signal<User | null>(null);

  // Public computed signals
  readonly token = this.tokenSignal.asReadonly();
  readonly user = this.userSignal.asReadonly();
  readonly isAuthenticated = computed(() => !!this.tokenSignal());
  
  // Backwards compatibility
  readonly currentUser = this.userSignal;

  constructor() {
    // Auto-load user profile if token exists
    effect(() => {
      const token = this.tokenSignal();
      if (token && !this.userSignal()) {
        this.loadUserProfile().subscribe({
          error: (error) => {
            console.error('Failed to load user profile:', error);
            // Token might be invalid/expired, clear it
            if (error.status === 401) {
              this.logout();
            }
          }
        });
      }
    });
  }

  /**
   * Register a new user account
   */
  register(request: RegisterRequest): Observable<RegisterResponse> {
    return this.http.post<RegisterResponse>(`${this.apiUrl}/register`, request).pipe(
      tap(response => {
        this.setToken(response.token);
        // Load user profile after registration
        this.loadUserProfile().subscribe();
      }),
      catchError(error => {
        console.error('Registration failed:', error);
        return throwError(() => error);
      })
    );
  }

  /**
   * Login with email and password
   */
  login(request: LoginRequest): Observable<LoginResponse> {
    return this.http.post<LoginResponse>(`${this.apiUrl}/login`, request).pipe(
      tap(response => {
        this.setToken(response.token);
        // Load full user profile after login
        this.loadUserProfile().subscribe();
      }),
      catchError(error => {
        console.error('Login failed:', error);
        return throwError(() => error);
      })
    );
  }

  /**
   * Logout current user
   */
  logout(): void {
    this.clearToken();
    this.userSignal.set(null);
    this.router.navigate(['/login']);
  }

  /**
   * Load current user profile
   */
  loadUserProfile(): Observable<User> {
    return this.http.get<User>(`${this.apiUrl}/me`).pipe(
      tap(user => this.userSignal.set(user)),
      catchError(error => {
        console.error('Failed to load user profile:', error);
        return throwError(() => error);
      })
    );
  }

  /**
   * Get user's resumes
   */
  getUserResumes(): Observable<any[]> {
    return this.http.get<any[]>(`${this.apiUrl}/me/resumes`);
  }

  /**
   * Set authentication token
   */
  private setToken(token: string): void {
    localStorage.setItem('auth_token', token);
    this.tokenSignal.set(token);
  }

  /**
   * Clear authentication token
   */
  private clearToken(): void {
    localStorage.removeItem('auth_token');
    this.tokenSignal.set(null);
  }

  /**
   * Get stored token from localStorage
   */
  private getStoredToken(): string | null {
    if (typeof window !== 'undefined' && window.localStorage) {
      return localStorage.getItem('auth_token');
    }
    return null;
  }

  /**
   * Get guest session token (if exists from previous guest upload)
   */
  getGuestSessionToken(): string | null {
    if (typeof window !== 'undefined' && window.localStorage) {
      return localStorage.getItem('guest_session_token');
    }
    return null;
  }

  /**
   * Clear guest session token after migration
   */
  clearGuestSessionToken(): void {
    if (typeof window !== 'undefined' && window.localStorage) {
      localStorage.removeItem('guest_session_token');
    }
  }

  // Backwards compatibility methods
  getCurrentUserId(): string {
    const user = this.userSignal();
    if (!user) {
      throw new Error(ERROR_MESSAGES.NOT_AUTHENTICATED);
    }
    return user.userId;
  }

  setUser(user: User | null): void {
    this.userSignal.set(user);
  }

  signOut(): void {
    this.logout();
  }
}
