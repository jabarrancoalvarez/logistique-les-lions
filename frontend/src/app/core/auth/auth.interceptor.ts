import { HttpInterceptorFn, HttpRequest, HttpHandlerFn, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, switchMap, throwError } from 'rxjs';
import { AuthService } from './auth.service';
import { Router } from '@angular/router';

export const authInterceptor: HttpInterceptorFn = (
  req: HttpRequest<unknown>,
  next: HttpHandlerFn
) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  const token = authService.accessToken();
  const correlationId = crypto.randomUUID().replace(/-/g, '');

  // Añadir headers de autenticación y correlationId
  const authReq = token
    ? req.clone({
        setHeaders: {
          Authorization: `Bearer ${token}`,
          'X-Correlation-Id': correlationId,
          'Accept-Language': navigator.language || 'es'
        }
      })
    : req.clone({
        setHeaders: { 'X-Correlation-Id': correlationId }
      });

  return next(authReq).pipe(
    catchError((error: HttpErrorResponse) => {
      if (error.status === 401 && token) {
        // Token expirado: intentar refresh
        return authService.refreshToken().pipe(
          switchMap(newToken => {
            const retryReq = req.clone({
              setHeaders: {
                Authorization: `Bearer ${newToken}`,
                'X-Correlation-Id': correlationId
              }
            });
            return next(retryReq);
          }),
          catchError(refreshError => {
            // Refresh falló: cerrar sesión
            authService.logout();
            router.navigate(['/']);
            return throwError(() => refreshError);
          })
        );
      }
      return throwError(() => error);
    })
  );
};
