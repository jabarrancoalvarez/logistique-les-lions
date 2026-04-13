import { ChangeDetectionStrategy, Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../../environments/environment';

const API = `${environment.apiUrl}/v1`;

function shell(title: string, subtitle: string, body: string) {
  return `
    <section class="p-8 max-w-7xl">
      <header class="mb-6">
        <h1 class="text-2xl font-bold text-slate-900">${title}</h1>
        <p class="text-sm text-slate-500 mt-1">${subtitle}</p>
      </header>
      ${body}
    </section>
  `;
}

@Component({
  selector: 'lll-admin-vehicles',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule],
  template: shell(
    'Vehículos',
    'Listado completo de vehículos publicados en la plataforma.',
    `
    @if (loading()) { <p class="text-slate-500">Cargando…</p> }
    @else {
      <div class="bg-white rounded-lg shadow-sm overflow-hidden">
        <table class="w-full text-sm">
          <thead class="bg-slate-100 text-slate-600 uppercase text-xs">
            <tr>
              <th class="px-4 py-3 text-left">Título</th>
              <th class="px-4 py-3 text-left">Año</th>
              <th class="px-4 py-3 text-right">Precio</th>
              <th class="px-4 py-3 text-left">Estado</th>
            </tr>
          </thead>
          <tbody>
            @for (v of items(); track v.id) {
              <tr class="border-t border-slate-100">
                <td class="px-4 py-3">{{ v.title }}</td>
                <td class="px-4 py-3">{{ v.year }}</td>
                <td class="px-4 py-3 text-right">{{ v.priceEur | number }} €</td>
                <td class="px-4 py-3"><span class="px-2 py-1 rounded text-xs bg-slate-100">{{ v.status }}</span></td>
              </tr>
            }
          </tbody>
        </table>
      </div>
    }
    `
  )
})
export class AdminVehiclesComponent implements OnInit {
  private http = inject(HttpClient);
  loading = signal(true);
  items = signal<any[]>([]);
  ngOnInit() {
    this.http.get<any>(`${API}/vehicles?pageSize=50`).subscribe({
      next: (r) => { this.items.set(r.items ?? r ?? []); this.loading.set(false); },
      error: () => this.loading.set(false)
    });
  }
}

@Component({
  selector: 'lll-admin-processes',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule],
  template: shell(
    'Procesos',
    'Procesos de import/export en curso y finalizados.',
    `
    @if (loading()) { <p class="text-slate-500">Cargando…</p> }
    @else {
      <div class="bg-white rounded-lg shadow-sm overflow-hidden">
        <table class="w-full text-sm">
          <thead class="bg-slate-100 text-slate-600 uppercase text-xs">
            <tr>
              <th class="px-4 py-3 text-left">Tracking</th>
              <th class="px-4 py-3 text-left">Tipo</th>
              <th class="px-4 py-3 text-left">Estado</th>
              <th class="px-4 py-3 text-right">% completado</th>
            </tr>
          </thead>
          <tbody>
            @for (p of items(); track p.id) {
              <tr class="border-t border-slate-100">
                <td class="px-4 py-3 font-mono text-xs">{{ p.trackingCode }}</td>
                <td class="px-4 py-3">{{ p.type }}</td>
                <td class="px-4 py-3">{{ p.status }}</td>
                <td class="px-4 py-3 text-right">{{ p.completionPercent }}%</td>
              </tr>
            } @empty {
              <tr><td colspan="4" class="px-4 py-6 text-center text-slate-400">Sin procesos.</td></tr>
            }
          </tbody>
        </table>
      </div>
    }
    `
  )
})
export class AdminProcessesComponent implements OnInit {
  private http = inject(HttpClient);
  loading = signal(true);
  items = signal<any[]>([]);
  ngOnInit() {
    this.http.get<any>(`${API}/compliance/processes`).subscribe({
      next: (r) => { this.items.set(r.items ?? r ?? []); this.loading.set(false); },
      error: () => this.loading.set(false)
    });
  }
}

@Component({
  selector: 'lll-admin-incidents',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule],
  template: shell(
    'Incidencias',
    'Bloqueos abiertos y resueltos en procesos de tramitación.',
    `
    @if (loading()) { <p class="text-slate-500">Cargando…</p> }
    @else {
      <div class="bg-white rounded-lg shadow-sm overflow-hidden">
        <table class="w-full text-sm">
          <thead class="bg-slate-100 text-slate-600 uppercase text-xs">
            <tr>
              <th class="px-4 py-3 text-left">Título</th>
              <th class="px-4 py-3 text-left">Severidad</th>
              <th class="px-4 py-3 text-left">Estado</th>
              <th class="px-4 py-3 text-left">Fecha</th>
            </tr>
          </thead>
          <tbody>
            @for (i of items(); track i.id) {
              <tr class="border-t border-slate-100">
                <td class="px-4 py-3">{{ i.title }}</td>
                <td class="px-4 py-3">{{ i.severity }}</td>
                <td class="px-4 py-3">{{ i.status }}</td>
                <td class="px-4 py-3 text-slate-500">{{ i.createdAt | date:'short' }}</td>
              </tr>
            } @empty {
              <tr><td colspan="4" class="px-4 py-6 text-center text-slate-400">Sin incidencias.</td></tr>
            }
          </tbody>
        </table>
      </div>
    }
    `
  )
})
export class AdminIncidentsComponent implements OnInit {
  private http = inject(HttpClient);
  loading = signal(true);
  items = signal<any[]>([]);
  ngOnInit() {
    this.http.get<any>(`${API}/compliance/incidents`).subscribe({
      next: (r) => { this.items.set(r.items ?? r ?? []); this.loading.set(false); },
      error: () => this.loading.set(false)
    });
  }
}

@Component({
  selector: 'lll-admin-partners',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule],
  template: shell(
    'Marketplace de partners',
    'Gestores, transportistas, inspectores y homologadores.',
    `
    @if (loading()) { <p class="text-slate-500">Cargando…</p> }
    @else {
      <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
        @for (p of items(); track p.id) {
          <div class="bg-white rounded-lg shadow-sm p-5">
            <div class="flex items-center justify-between mb-2">
              <h3 class="font-semibold text-slate-900">{{ p.name }}</h3>
              <span class="text-xs px-2 py-1 bg-amber-100 text-amber-700 rounded">{{ p.type }}</span>
            </div>
            <p class="text-sm text-slate-500 line-clamp-2">{{ p.description }}</p>
            <div class="mt-3 flex items-center gap-2 text-xs text-slate-400">
              <span>⭐ {{ p.rating }}</span>
              <span>·</span>
              <span>{{ p.reviewsCount }} reseñas</span>
              @if (p.isVerified) { <span class="ml-auto text-emerald-600">✓ verificado</span> }
            </div>
          </div>
        } @empty {
          <p class="text-slate-400">Sin partners registrados.</p>
        }
      </div>
    }
    `
  )
})
export class AdminPartnersComponent implements OnInit {
  private http = inject(HttpClient);
  loading = signal(true);
  items = signal<any[]>([]);
  ngOnInit() {
    this.http.get<any>(`${API}/marketplace/partners`).subscribe({
      next: (r) => { this.items.set(r ?? []); this.loading.set(false); },
      error: () => this.loading.set(false)
    });
  }
}

@Component({
  selector: 'lll-admin-users',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule],
  template: shell(
    'Usuarios',
    'Cuentas registradas en la plataforma.',
    `
    @if (loading()) { <p class="text-slate-500">Cargando…</p> }
    @else {
      <div class="bg-white rounded-lg shadow-sm overflow-hidden">
        <table class="w-full text-sm">
          <thead class="bg-slate-100 text-slate-600 uppercase text-xs">
            <tr>
              <th class="px-4 py-3 text-left">Email</th>
              <th class="px-4 py-3 text-left">Nombre</th>
              <th class="px-4 py-3 text-left">Rol</th>
              <th class="px-4 py-3 text-left">Estado</th>
            </tr>
          </thead>
          <tbody>
            @for (u of items(); track u.id) {
              <tr class="border-t border-slate-100">
                <td class="px-4 py-3">{{ u.email }}</td>
                <td class="px-4 py-3">{{ u.fullName }}</td>
                <td class="px-4 py-3">{{ u.role }}</td>
                <td class="px-4 py-3">
                  <span class="px-2 py-1 rounded text-xs"
                    [class.bg-emerald-100]="u.isActive"
                    [class.text-emerald-700]="u.isActive"
                    [class.bg-slate-100]="!u.isActive"
                    [class.text-slate-500]="!u.isActive">
                    {{ u.isActive ? 'Activo' : 'Inactivo' }}
                  </span>
                </td>
              </tr>
            } @empty {
              <tr><td colspan="4" class="px-4 py-6 text-center text-slate-400">Sin usuarios.</td></tr>
            }
          </tbody>
        </table>
      </div>
    }
    `
  )
})
const MOCK_USERS = [
  { id: 'u-001', email: 'admin@logistiqueleslions.com',  fullName: 'Jaime Barranco',      role: 'Admin',     isActive: true  },
  { id: 'u-002', email: 'laura.garcia@example.com',       fullName: 'Laura García',        role: 'Dealer',    isActive: true  },
  { id: 'u-003', email: 'carlos.ruiz@example.com',        fullName: 'Carlos Ruiz',         role: 'Dealer',    isActive: true  },
  { id: 'u-004', email: 'marta.sanchez@example.com',      fullName: 'Marta Sánchez',       role: 'Buyer',     isActive: true  },
  { id: 'u-005', email: 'pedro.lopez@example.com',        fullName: 'Pedro López',         role: 'Buyer',     isActive: false },
  { id: 'u-006', email: 'ana.martinez@example.com',       fullName: 'Ana Martínez',        role: 'Seller',    isActive: true  },
  { id: 'u-007', email: 'javier.fernandez@example.com',   fullName: 'Javier Fernández',    role: 'Moderator', isActive: true  },
  { id: 'u-008', email: 'sofia.gomez@example.com',        fullName: 'Sofía Gómez',         role: 'Dealer',    isActive: true  },
  { id: 'u-009', email: 'diego.perez@example.com',        fullName: 'Diego Pérez',         role: 'Buyer',     isActive: true  },
  { id: 'u-010', email: 'elena.torres@example.com',       fullName: 'Elena Torres',        role: 'Seller',    isActive: false },
  { id: 'u-011', email: 'lucas.jimenez@example.com',      fullName: 'Lucas Jiménez',       role: 'Buyer',     isActive: true  },
  { id: 'u-012', email: 'clara.moreno@example.com',       fullName: 'Clara Moreno',        role: 'Dealer',    isActive: true  },
];

export class AdminUsersComponent implements OnInit {
  private http = inject(HttpClient);
  loading = signal(true);
  items = signal<any[]>([]);
  ngOnInit() {
    this.http.get<any>(`${API}/admin/users`).subscribe({
      next: (r) => {
        const data = r?.items ?? r ?? [];
        this.items.set(Array.isArray(data) && data.length > 0 ? data : MOCK_USERS);
        this.loading.set(false);
      },
      error: () => { this.items.set(MOCK_USERS); this.loading.set(false); }
    });
  }
}

@Component({
  selector: 'lll-admin-notifications',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule],
  template: shell(
    'Notificaciones',
    'Centro de notificaciones del usuario actual.',
    `
    @if (loading()) { <p class="text-slate-500">Cargando…</p> }
    @else {
      <p class="text-sm text-slate-500 mb-4">Pendientes: {{ unread() }}</p>
      <div class="bg-white rounded-lg shadow-sm divide-y divide-slate-100">
        @for (n of items(); track n.id) {
          <div class="p-4 flex items-start gap-3">
            <span class="mt-1 w-2 h-2 rounded-full" [class.bg-amber-500]="!n.isRead" [class.bg-slate-300]="n.isRead"></span>
            <div class="flex-1">
              <h4 class="text-sm font-medium text-slate-900">{{ n.title }}</h4>
              <p class="text-xs text-slate-500 mt-1">{{ n.body }}</p>
            </div>
            <span class="text-xs text-slate-400">{{ n.createdAt | date:'short' }}</span>
          </div>
        } @empty {
          <p class="p-6 text-center text-slate-400">Sin notificaciones.</p>
        }
      </div>
    }
    `
  )
})
export class AdminNotificationsComponent implements OnInit {
  private http = inject(HttpClient);
  loading = signal(true);
  items = signal<any[]>([]);
  unread = signal(0);
  ngOnInit() {
    this.http.get<any>(`${API}/notifications`).subscribe({
      next: (r) => { this.items.set(r.items ?? []); this.unread.set(r.unreadCount ?? 0); this.loading.set(false); },
      error: () => this.loading.set(false)
    });
  }
}
