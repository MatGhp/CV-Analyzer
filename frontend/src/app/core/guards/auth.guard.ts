import { inject } from '@angular/core';
import { Router, type CanActivateFn } from '@angular/router';
import { AuthService } from '../services/auth.service';

/**
 * Auth guard that protects routes requiring authentication
 * Redirects to login page if user is not authenticated
 */
export const authGuard: CanActivateFn = (route, state) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  if (authService.isAuthenticated()) {
    return true;
  }

  // Store the attempted URL for redirecting after login
  const returnUrl = state.url;
  console.log('Auth guard: User not authenticated, redirecting to login');
  
  return router.createUrlTree(['/login'], {
    queryParams: { returnUrl }
  });
};

/**
 * Guest guard that redirects authenticated users away from login/register pages
 */
export const guestGuard: CanActivateFn = (route, state) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  if (!authService.isAuthenticated()) {
    return true;
  }

  // User is already authenticated, redirect to home/dashboard
  console.log('Guest guard: User already authenticated, redirecting to home');
  return router.createUrlTree(['/']);
};
