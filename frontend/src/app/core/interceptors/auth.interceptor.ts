import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { AuthService } from '../services/auth.service';
import { catchError, throwError } from 'rxjs';

/**
 * HTTP interceptor that adds JWT token to outgoing requests
 * and handles 401 unauthorized responses
 */
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const authService = inject(AuthService);
  const token = authService.token();

  // Clone request and add Authorization header if token exists
  if (token) {
    req = req.clone({
      setHeaders: {
        Authorization: `Bearer ${token}`
      }
    });
  }

  // Handle response and catch 401 errors
  return next(req).pipe(
    catchError(error => {
      if (error.status === 401) {
        // Token expired or invalid, logout user
        console.warn('Unauthorized request detected, logging out user');
        authService.logout();
      }
      return throwError(() => error);
    })
  );
};
