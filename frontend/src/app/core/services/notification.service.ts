import { Injectable, inject, signal, computed, effect } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import * as signalR from '@microsoft/signalr';
import { environment } from '@environments/environment';
import { AuthService } from '@core/auth/auth.service';

/** Categorías de la campana. Determinan el icono mostrado en la lista. */
export type NotificationCategory =
  | 'price-drop' | 'new-listing' | 'request-proposal' | 'message'
  | 'offer' | 'contract' | 'reminder' | 'admin' | 'system';

export interface AppNotification {
  id: string;
  category: string;
  title: string;
  body: string | null;
  link: string | null;
  isRead: boolean;
  createdAt: string;
  readAt: string | null;
}

interface NotificationListResponse {
  unreadCount: number;
  items: AppNotification[];
}

/** Título en francés de cada categoría, para el texto alternativo del icono. */
export const NOTIFICATION_CATEGORY_LABELS: Record<string, string> = {
  'price-drop': 'Baisse de prix',
  'new-listing': 'Nouvelle annonce',
  'request-proposal': 'Véhicule trouvé',
  'message': 'Nouveau message',
  'offer': 'Nouvelle offre',
  'contract': 'Contrat',
  'reminder': "Rappel d'entretien",
  'admin': 'Administration',
  'system': 'Information'
};

@Injectable({ providedIn: 'root' })
export class NotificationService {
  private readonly http = inject(HttpClient);
  private readonly auth = inject(AuthService);
  private readonly baseUrl = `${environment.apiUrl}/v1/notifications`;

  private readonly _items = signal<AppNotification[]>([]);
  private readonly _unreadCount = signal(0);

  readonly items = this._items.asReadonly();
  readonly unreadCount = this._unreadCount.asReadonly();
  readonly hasUnread = computed(() => this._unreadCount() > 0);

  private hubConnection?: signalR.HubConnection;

  constructor() {
    // El token caduca a los 15 minutos y el interceptor lo renueva solo. Antes, el hub
    // se quedaba con el token viejo: la negociación devolvía 401, no se reintentaba
    // nunca, y el tiempo real moría en silencio hasta recargar la página.
    effect(() => {
      const token = this.auth.accessToken();

      if (!token) { this.disconnect(); return; }

      // Si la conexión no está en pie, se levanta con el token nuevo.
      if (this.hubConnection?.state !== signalR.HubConnectionState.Connected) {
        this.reconnect();
      }
    });
  }

  /** Cierra lo que hubiera y vuelve a conectar con el token vigente. */
  private reconnect(): void {
    const previa = this.hubConnection;
    this.hubConnection = undefined;
    previa?.stop().catch(() => {});
    this.connect();
  }

  /** Carga las notificaciones del usuario y refresca el contador. */
  load(take = 30): Observable<NotificationListResponse> {
    const params = new HttpParams().set('take', take);
    return this.http.get<NotificationListResponse>(this.baseUrl, { params }).pipe(
      tap(r => {
        this._items.set(r.items);
        this._unreadCount.set(r.unreadCount);
      })
    );
  }

  markRead(id: string): Observable<{ updated: number }> {
    // Se actualiza en local antes de responder: la campana debe reaccionar al instante.
    this._items.update(list =>
      list.map(n => (n.id === id && !n.isRead ? { ...n, isRead: true } : n)));
    this._unreadCount.update(c => Math.max(0, c - 1));

    return this.http.post<{ updated: number }>(`${this.baseUrl}/${id}/read`, null);
  }

  markAllRead(): Observable<{ updated: number }> {
    this._items.update(list => list.map(n => ({ ...n, isRead: true })));
    this._unreadCount.set(0);

    return this.http.post<{ updated: number }>(`${this.baseUrl}/read-all`, null);
  }

  // ─── Tiempo real ───────────────────────────────────────────────────────
  /**
   * Se conecta al hub para recibir avisos sin recargar. Si falla, la campana sigue
   * funcionando: las notificaciones están persistidas y se leen al abrirla.
   */
  connect(): void {
    if (this.hubConnection) return;

    if (!this.auth.accessToken()) return;

    const hubUrl = environment.apiUrl.replace('/api', '') + '/hubs/notifications';

    this.hubConnection = new signalR.HubConnectionBuilder()
      // Se lee en cada negociación, no se captura: si el token se ha renovado mientras
      // tanto, SignalR usa el nuevo al reconectar.
      .withUrl(hubUrl, { accessTokenFactory: () => this.auth.accessToken() ?? '' })
      .withAutomaticReconnect()
      .configureLogging(signalR.LogLevel.Warning)
      .build();

    this.hubConnection.on('notification', (payload: Partial<AppNotification>) => {
      const incoming: AppNotification = {
        id: payload.id ?? crypto.randomUUID(),
        category: payload.category ?? 'system',
        title: payload.title ?? '',
        body: payload.body ?? null,
        link: payload.link ?? null,
        isRead: false,
        createdAt: new Date().toISOString(),
        readAt: null
      };

      this._items.update(list => [incoming, ...list]);
      this._unreadCount.update(c => c + 1);
    });

    this.hubConnection.start().catch(() => {
      // Sin tiempo real la campana sigue siendo funcional.
      this.hubConnection = undefined;
    });
  }

  disconnect(): void {
    this.hubConnection?.stop().catch(() => {});
    this.hubConnection = undefined;
    this._items.set([]);
    this._unreadCount.set(0);
  }
}
