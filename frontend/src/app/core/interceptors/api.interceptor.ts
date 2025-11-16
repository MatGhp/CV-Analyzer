import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';

export const apiInterceptor: HttpInterceptorFn = (req, next) => {
  let clonedRequest = req;
  
  if (!(req.body instanceof FormData)) {
    clonedRequest = req.clone({
      setHeaders: {
        'Content-Type': req.headers.get('Content-Type') || 'application/json'
      }
    });
  }

  return next(clonedRequest).pipe(
    catchError((error) => {
      return throwError(() => error);
    })
  );
};
