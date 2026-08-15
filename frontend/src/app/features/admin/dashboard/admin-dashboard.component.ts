import { Component, ChangeDetectionStrategy, OnInit, signal, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { AdminService, AdminDashboard } from '@core/services/admin.service';

/**
 * «Tableau de bord» — la pantalla inicial del backoffice.
 *
 * No pretende ser Business Intelligence: es un vistazo a lo que está ocurriendo en la
 * plataforma, agrupado como en la especificación.
 */
@Component({
  selector: 'lll-admin-dashboard',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule, RouterLink],
  templateUrl: './admin-dashboard.component.html'
})
export class AdminDashboardComponent implements OnInit {
  private readonly admin = inject(AdminService);

  readonly data = signal<AdminDashboard | null>(null);
  readonly loading = signal(true);
  readonly error = signal(false);

  ngOnInit(): void {
    this.admin.getDashboard().subscribe({
      next: d => { this.data.set(d); this.loading.set(false); },
      error: () => { this.error.set(true); this.loading.set(false); }
    });
  }
}
