import {
  Component, ChangeDetectionStrategy, OnInit, OnDestroy, signal, inject, HostListener, ElementRef
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import {
  NotificationService, AppNotification, NOTIFICATION_CATEGORY_LABELS
} from '@core/services/notification.service';
import { AuthService } from '@core/auth/auth.service';

/** Trazo SVG del icono de cada categoría. */
const CATEGORY_ICONS: Record<string, string> = {
  'price-drop':       'M13 17h8m0 0V9m0 8l-8-8-4 4-6-6',
  'new-listing':      'M12 6v6m0 0v6m0-6h6m-6 0H6',
  'request-proposal': 'M5 13l4 4L19 7',
  'message':          'M8 12h.01M12 12h.01M16 12h.01M21 12c0 4.418-4.03 8-9 8a9.86 9.86 0 01-4.255-.949L3 20l1.395-3.72C3.512 15.042 3 13.574 3 12c0-4.418 4.03-8 9-8s9 3.582 9 8z',
  'offer':            'M12 8c-1.657 0-3 .895-3 2s1.343 2 3 2 3 .895 3 2-1.343 2-3 2m0-8c1.11 0 2.08.402 2.599 1M12 8V7m0 1v8m0 0v1m0-1c-1.11 0-2.08-.402-2.599-1',
  'contract':         'M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z',
  'reminder':         'M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z',
  'admin':            'M12 6V4m0 2a2 2 0 100 4m0-4a2 2 0 110 4m-6 8a2 2 0 100-4m0 4a2 2 0 110-4m0 4v2m0-6V4m6 6v10m6-2a2 2 0 100-4m0 4a2 2 0 110-4m0 4v2m0-6V4',
  'system':           'M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z'
};

@Component({
  selector: 'lll-notification-bell',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule],
  templateUrl: './notification-bell.component.html'
})
export class NotificationBellComponent implements OnInit, OnDestroy {
  readonly notifications = inject(NotificationService);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly host = inject(ElementRef<HTMLElement>);

  readonly open = signal(false);

  ngOnInit(): void {
    if (!this.auth.isAuthenticated()) return;
    this.notifications.load().subscribe({ error: () => {} });
    this.notifications.connect();
  }

  /**
   * La campana solo se renderiza para usuarios autenticados, así que al cerrar sesión
   * Angular destruye el componente: es el momento de soltar la conexión y vaciar el
   * estado, para que la siguiente cuenta no herede las notificaciones de la anterior.
   */
  ngOnDestroy(): void {
    this.notifications.disconnect();
  }

  toggle(): void {
    const next = !this.open();
    this.open.set(next);
    // Al abrir se relee: entre visitas pueden haber llegado avisos sin conexión activa.
    if (next) this.notifications.load().subscribe({ error: () => {} });
  }

  /** Un clic fuera cierra el panel. */
  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    if (!this.open()) return;
    if (!this.host.nativeElement.contains(event.target as Node)) this.open.set(false);
  }

  @HostListener('document:keydown.escape')
  onEscape(): void {
    this.open.set(false);
  }

  /** Abrir una notificación la marca como leída y navega a su destino. */
  openNotification(n: AppNotification): void {
    if (!n.isRead) this.notifications.markRead(n.id).subscribe({ error: () => {} });
    this.open.set(false);
    if (n.link) this.router.navigateByUrl(n.link);
  }

  markAllRead(event: Event): void {
    event.stopPropagation();
    this.notifications.markAllRead().subscribe({ error: () => {} });
  }

  categoryIcon(category: string): string {
    return CATEGORY_ICONS[category] ?? CATEGORY_ICONS['system'];
  }

  categoryLabel(category: string): string {
    return NOTIFICATION_CATEGORY_LABELS[category] ?? 'Information';
  }

  /** «Il y a 3 heures» */
  timeAgo(iso: string): string {
    const minutes = Math.floor((Date.now() - new Date(iso).getTime()) / 60000);
    if (minutes < 1) return "À l'instant";
    if (minutes < 60) return `Il y a ${minutes} min`;

    const hours = Math.floor(minutes / 60);
    if (hours < 24) return `Il y a ${hours} h`;

    const days = Math.floor(hours / 24);
    if (days === 1) return 'Hier';
    if (days < 30) return `Il y a ${days} jours`;

    return new Date(iso).toLocaleDateString('fr-FR');
  }
}
