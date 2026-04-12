import { Injectable, signal, computed } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap, map } from 'rxjs';
import { environment } from '@environments/environment';

export interface AuthUser {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  role: string;
  avatarUrl?: string;
  isVerified: boolean;
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

export interface ProfileData {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  role: string;
  phone?: string;
  avatarUrl?: string;
  countryCode?: string;
  city?: string;
  companyName?: string;
  companyVat?: string;
  bio?: string;
  isVerified: boolean;
  lastLoginAt?: string;
  createdAt: string;
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
  readonly isDealer = computed(() => this._user()?.role === 'Dealer');
  readonly isModerator = computed(() => this._user()?.role === 'Moderator');

  /** Devuelve true si el usuario tiene exactamente uno de los roles indicados. */
  hasAnyRole(roles: readonly string[]): boolean {
    const role = this._user()?.role;
    return !!role && roles.includes(role);
  }

  /** Helper de capacidades — alineado con las policies del backend. */
  canModerate       = computed(() => this.hasAnyRole(['Admin', 'Moderator']));
  canPublishVehicle = computed(() => this.hasAnyRole(['Admin', 'Dealer', 'Seller']));
  canManageUsers    = computed(() => this.hasAnyRole(['Admin']));
  canViewAdminPanel = computed(() => this.hasAnyRole(['Admin', 'Moderator']));

  constructor(private http: HttpClient) {}

  register(payload: {
    email: string;
    password: string;
    firstName: string;
    lastName: string;
    role: number;
    phone?: string;
    countryCode?: string;
    companyName?: string;
    companyVat?: string;
  }): Observable<AuthUser> {
    return this.http.post<AuthResponse>(`${this.apiUrl}/register`, payload).pipe(
      tap(r => this.storeTokens(r.value)),
      map(r => r.value.user)
    );
  }

  login(email: string, password: string): Observable<AuthUser> {
    return this.http.post<AuthResponse>(`${this.apiUrl}/login`, { email, password }).pipe(
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
    return stored ? JSON.parse(stored) : null;
  }
}
