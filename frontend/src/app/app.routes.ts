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
