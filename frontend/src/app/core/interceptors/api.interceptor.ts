import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';

export const apiInterceptor: HttpInterceptorFn = (req, next) => {
  // Add any global headers here (auth tokens, etc.)
  const clonedRequest = req.clone({
    setHeaders: {
      'Content-Type': req.headers.get('Content-Type') || 'application/json'
    }
  });

  return next(clonedRequest).pipe(
    catchError((error) => {
      // Global error handling
      console.error('HTTP Error:', error);
      return throwError(() => error);
    })
  );
};
