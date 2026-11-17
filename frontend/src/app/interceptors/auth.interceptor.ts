
import { inject } from '@angular/core';
import {
  HttpInterceptorFn,
  HttpErrorResponse
} from '@angular/common/http';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';

import { AuthService } from '../services/auth.service';
import { environment } from '../../environments/environment';

export const authInterceptor: HttpInterceptorFn = (req, next) => {

  const authService = inject(AuthService);
  const router = inject(Router);

  // Only intercept requests to our backend API
  const isApiRequest = req.url.startsWith(`${environment.apiUrl}/`);

  // Login must work without a token
  const isLoginRequest = req.url === `${environment.apiUrl}/auth/login`;

  if (!isApiRequest || isLoginRequest) {
    return next(req);
  }

  // Get the JWT saved after login
  const token = authService.getToken();

  // Add the token to the HTTP request
  const request = token
    ? req.clone({
        setHeaders: {
          Authorization: `Bearer ${token}`
        }
      })
    : req;

  return next(request).pipe(
    catchError((error: HttpErrorResponse) => {

      // Token is missing, invalid, or expired
      if (error.status === 401) {
        authService.logout();
        router.navigate(['/login']);
      }

      // Let the component handle the error as usual
      return throwError(() => error);
    })
  );
};
