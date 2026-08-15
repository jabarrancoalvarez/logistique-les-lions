import { Injectable, signal, computed } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap, map } from 'rxjs';
import { environment } from '@environments/environment';

/** Tipo de cuenta declarado por el usuario. Campo informativo: no otorga permisos. */
export type AccountType = 'Particulier' | 'Professionnel';

/** Identidad mínima guardada en sesión. */
export interface AuthUser {
  id: string;
  displayName: string;
  phone?: string;
  email?: string;
  role: string;
  accountType: AccountType;
  avatarUrl?: string;
  phoneVerified: boolean;
}

export interface AuthResponse {
  isSuccess: boolean;
  value: {
    accessToken: string;
    refreshToken: string;
    expiresAt: string;
    user: AuthUser;
  };
}

/** Datos completos de "Mon profil". */
export interface ProfileData {
  id: string;
  displayName: string;
  phone?: string;
  phoneVerified: boolean;
  email?: string;
  role: string;
  accountType: AccountType;
  avatarUrl?: string;
  region?: string;
  city?: string;
  bio?: string;
  allowWhatsAppContact: boolean;
  verifiedSalesCount: number;
  activeListingsCount: number;
  lastLoginAt?: string;
  createdAt: string;
}

export interface RegisterPayload {
  phone: string;
  password: string;
  displayName: string;
  accountType: AccountType;
  region?: string;
  city?: string;
  email?: string;
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly apiUrl = `${environment.apiUrl}/v1/auth`;

  private readonly _user = signal<AuthUser | null>(this.loadUserFromStorage());
  private readonly _accessToken = signal<string | null>(
    typeof localStorage !== 'undefined' ? localStorage.getItem('lll_access_token') : null
  );

  readonly user = this._user.asReadonly();
  readonly accessToken = this._accessToken.asReadonly();
  readonly isAuthenticated = computed(() => !!this._user());
  readonly isAdmin = computed(() => this._user()?.role === 'Admin');

  /**
   * Todas las funcionalidades de usuario son gratuitas y sin límites: publicar,
   * comprar o negociar solo exige estar autenticado. No hay capacidades por rol
   * más allá del acceso al backoffice.
   */
  readonly canPublishVehicle = this.isAuthenticated;
  readonly canViewAdminPanel = this.isAdmin;
  readonly canManageUsers = this.isAdmin;
  readonly canModerate = this.isAdmin;

  /** Devuelve true si el usuario tiene uno de los roles indicados. */
  hasAnyRole(roles: readonly string[]): boolean {
    const role = this._user()?.role;
    return !!role && roles.includes(role);
  }

  constructor(private http: HttpClient) {}

  register(payload: RegisterPayload): Observable<AuthUser> {
    return this.http.post<AuthResponse>(`${this.apiUrl}/register`, payload).pipe(
      tap(r => this.storeTokens(r.value)),
      map(r => r.value.user)
    );
  }

  /** `identifier` admite el teléfono (identificador principal) o el correo. */
  login(identifier: string, password: string): Observable<AuthUser> {
    return this.http.post<AuthResponse>(`${this.apiUrl}/login`, { identifier, password }).pipe(
      tap(r => this.storeTokens(r.value)),
      map(r => r.value.user)
    );
  }

  refreshToken(): Observable<string> {
    const refreshToken = typeof localStorage !== 'undefined'
      ? localStorage.getItem('lll_refresh_token')
      : null;
    return this.http.post<AuthResponse>(`${this.apiUrl}/refresh`, { refreshToken }).pipe(
      tap(r => this.storeTokens(r.value)),
      map(r => r.value.accessToken)
    );
  }

  getProfile(): Observable<ProfileData> {
    return this.http.get<{ isSuccess: boolean; value: ProfileData }>(`${this.apiUrl}/me`).pipe(
      map(r => r.value)
    );
  }

  updateProfile(data: Partial<ProfileData>): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/me`, data);
  }

  logout(): void {
    this.http.post(`${this.apiUrl}/logout`, {}).subscribe({ error: () => {} });
    this.clearSession();
  }

  clearSession(): void {
    if (typeof localStorage !== 'undefined') {
      localStorage.removeItem('lll_access_token');
      localStorage.removeItem('lll_refresh_token');
      localStorage.removeItem('lll_user');
    }
    this._accessToken.set(null);
    this._user.set(null);
  }

  private storeTokens(data: { accessToken: string; refreshToken: string; user: AuthUser }): void {
    if (typeof localStorage !== 'undefined') {
      localStorage.setItem('lll_access_token', data.accessToken);
      localStorage.setItem('lll_refresh_token', data.refreshToken);
      localStorage.setItem('lll_user', JSON.stringify(data.user));
    }
    this._accessToken.set(data.accessToken);
    this._user.set(data.user);
  }

  private loadUserFromStorage(): AuthUser | null {
    if (typeof localStorage === 'undefined') return null;
    const stored = localStorage.getItem('lll_user');
    if (!stored) return null;
    try {
      return JSON.parse(stored) as AuthUser;
    } catch {
      // Sesión guardada con el modelo de usuario anterior: se descarta.
      localStorage.removeItem('lll_user');
      return null;
    }
  }
}
