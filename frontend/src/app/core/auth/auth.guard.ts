import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from './auth.service';

/** Solo existen dos roles: usuario general y administrador de la plataforma. */
export type AppRole = 'User' | 'Admin';

export const authGuard: CanActivateFn = (_route, state) => {
  const auth   = inject(AuthService);
  const router = inject(Router);

  if (auth.isAuthenticated()) return true;

  // Se conserva a dónde iba: tras identificarse vuelve ahí y no al panel general.
  // El menú público lleva a acciones que exigen cuenta —vender, pedir un vehículo—
  // y perder el destino convertiría cada una en un callejón sin salida.
  return router.createUrlTree(['/auth/login'], {
    queryParams: { returnUrl: state.url }
  });
};

export const adminGuard: CanActivateFn = () => {
  const auth   = inject(AuthService);
  const router = inject(Router);

  if (auth.isAuthenticated() && auth.isAdmin()) return true;
  return router.createUrlTree(['/']);
};

export const guestGuard: CanActivateFn = () => {
  const auth   = inject(AuthService);
  const router = inject(Router);

  if (!auth.isAuthenticated()) return true;
  return router.createUrlTree(['/']);
};

/**
 * Factory de guard parametrizado por roles permitidos. Uso en routes:
 *   { path: 'admin', canActivate: [roleGuard('Admin')], ... }
 */
export const roleGuard = (...allowedRoles: AppRole[]): CanActivateFn => () => {
  const auth   = inject(AuthService);
  const router = inject(Router);

  if (!auth.isAuthenticated()) {
    return router.createUrlTree(['/auth/login']);
  }
  if (auth.hasAnyRole(allowedRoles)) return true;
  return router.createUrlTree(['/']);
};
