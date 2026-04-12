import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { ErrorService } from '../services/error.service';

export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const errorService = inject(ErrorService);

  return next(req).pipe(
    catchError((error: HttpErrorResponse) => {
      const message = extractErrorMessage(error);

      // No mostrar errores 401 (el auth interceptor los maneja)
      if (error.status !== 401) {
        errorService.showError(message);
      }

      return throwError(() => error);
    })
  );
};

function extractErrorMessage(error: HttpErrorResponse): string {
  if (error.error?.title) return error.error.title;
  if (error.error?.message) return error.error.message;
  if (typeof error.error === 'string') return error.error;

  switch (error.status) {
    case 0:    return 'Sin conexión con el servidor. Verifica tu conexión a internet.';
    case 400:  return 'Solicitud incorrecta.';
    case 403:  return 'No tienes permisos para realizar esta acción.';
    case 404:  return 'El recurso solicitado no existe.';
    case 409:  return 'Conflicto con el estado actual del recurso.';
    case 422:  return 'Los datos enviados no son válidos.';
    case 429:  return 'Demasiadas solicitudes. Espera un momento.';
    case 500:  return 'Error interno del servidor. Inténtalo más tarde.';
    default:   return `Error inesperado (${error.status}).`;
  }
}
