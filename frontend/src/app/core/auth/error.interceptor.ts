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
    case 0:    return 'Pas de connexion au serveur. Vérifiez votre connexion Internet.';
    case 400:  return 'Requête incorrecte.';
    case 403:  return "Vous n'avez pas les droits pour effectuer cette action.";
    case 404:  return "La ressource demandée n'existe pas.";
    case 409:  return "Conflit avec l'état actuel de la ressource.";
    case 422:  return 'Les données envoyées ne sont pas valides.';
    case 429:  return 'Trop de requêtes. Patientez un instant.';
    case 500:  return 'Erreur interne du serveur. Réessayez plus tard.';
    default:   return `Erreur inattendue (${error.status}).`;
  }
}
