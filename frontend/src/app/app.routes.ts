import { Routes } from '@angular/router';
import { authGuard, guestGuard } from './core/guards/auth.guard';

export const routes: Routes = [
  {
    path: '',
    redirectTo: '/upload',
    pathMatch: 'full'
  },
  {
    path: 'login',
    canActivate: [guestGuard],
    loadComponent: () => import('./features/auth/login/login.component')
      .then(m => m.LoginComponent)
  },
  {
    path: 'register',
    canActivate: [guestGuard],
    loadComponent: () => import('./features/auth/register/register.component')
      .then(m => m.RegisterComponent)
  },
  {
    path: 'dashboard',
    canActivate: [authGuard],
    loadComponent: () => import('./features/dashboard/dashboard.component')
      .then(m => m.DashboardComponent)
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
