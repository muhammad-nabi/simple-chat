import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, switchMap, throwError } from 'rxjs';
import { AuthService } from './auth.service';

const AUTH_URLS = ['/api/auth/login', '/api/auth/register', '/api/auth/refresh'];

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const authService = inject(AuthService);
  const router = inject(Router);
  const token = authService.accessToken;
  const isAuthUrl = AUTH_URLS.some(url => req.url.endsWith(url));

  // Add Bearer token to non-auth requests
  if (token && !isAuthUrl) {
    req = req.clone({
      setHeaders: { Authorization: `Bearer ${token}` },
    });
  }

  return next(req).pipe(
    catchError((error: HttpErrorResponse) => {
      // Only handle 401s for non-auth endpoints (avoid infinite loop on /api/auth/refresh)
      if (error.status === 401 && !isAuthUrl) {
        return authService.refresh().pipe(
          switchMap(response => {
            // Retry the original request with the new token
            const retryReq = req.clone({
              setHeaders: { Authorization: `Bearer ${response.accessToken}` },
            });
            return next(retryReq);
          }),
          catchError(refreshError => {
            // Refresh failed — session is invalid, redirect to login
            authService.logout();
            router.navigate(['/login']);
            return throwError(() => refreshError);
          }),
        );
      }

      return throwError(() => error);
    }),
  );
};
