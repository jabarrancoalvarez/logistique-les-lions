import { Component, ChangeDetectionStrategy, OnInit, signal, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { SavedSearchService, SavedSearch } from '@core/services/saved-search.service';
import { summarizeFilters } from '@shared/data/search-summary';

@Component({
  selector: 'lll-saved-searches',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './saved-searches.component.html'
})
export class SavedSearchesComponent implements OnInit {
  private readonly service = inject(SavedSearchService);
  private readonly router = inject(Router);

  readonly searches = signal<SavedSearch[]>([]);
  readonly loading = signal(true);
  readonly hasError = signal(false);

  /** Búsqueda en edición de nombre, si la hay. */
  readonly editingId = signal<string | null>(null);
  editingName = '';

  ngOnInit(): void {
    this.load();
  }

  private load(): void {
    this.loading.set(true);
    this.service.getAll().subscribe({
      next: items => {
        this.searches.set(items);
        this.loading.set(false);
      },
      error: () => {
        this.hasError.set(true);
        this.loading.set(false);
      }
    });
  }

  /** «2017–2022 · ≤150.000 km · ≤12.000.000 FCFA · Dakar» */
  summary(search: SavedSearch): string {
    return summarizeFilters(search.filters);
  }

  /**
   * Estado de la pantalla, no criterios de la búsqueda: se omiten para no ensuciar
   * la URL con parámetros que el Marketplace ya establece por su cuenta.
   */
  private static readonly NON_CRITERIA = new Set([
    'page', 'pageSize', 'sortBy', 'sortDesc', 'includeNonPublic',
    'sellerId', 'status', 'isFeatured'
  ]);

  /** Voir les résultats: reabre el Marketplace con los filtros guardados. */
  viewResults(search: SavedSearch): void {
    const queryParams: Record<string, unknown> = {};

    for (const [key, value] of Object.entries(search.filters)) {
      if (value === undefined || value === null || value === '') continue;
      if (SavedSearchesComponent.NON_CRITERIA.has(key)) continue;
      queryParams[key] = value;
    }

    this.router.navigate(['/vehiculos'], { queryParams });
  }

  // ─── Modifier: por ahora, el nombre ────────────────────────────────────
  startEditing(search: SavedSearch): void {
    this.editingId.set(search.id);
    this.editingName = search.name;
  }

  cancelEditing(): void {
    this.editingId.set(null);
  }

  saveName(search: SavedSearch): void {
    const name = this.editingName.trim();
    if (!name) return;

    this.patch(search.id, { name });
    this.editingId.set(null);

    this.service.update(search.id, name, search.filters).subscribe({
      error: () => this.patch(search.id, { name: search.name })
    });
  }

  // ─── Alerte nouveaux véhicules ─────────────────────────────────────────
  toggleAlert(search: SavedSearch): void {
    const next = !search.alertEnabled;
    this.patch(search.id, { alertEnabled: next });

    this.service.setAlert(search.id, next).subscribe({
      error: () => this.patch(search.id, { alertEnabled: !next })
    });
  }

  remove(search: SavedSearch): void {
    const previous = this.searches();
    this.searches.update(list => list.filter(s => s.id !== search.id));

    this.service.remove(search.id).subscribe({
      error: () => this.searches.set(previous)
    });
  }

  private patch(id: string, changes: Partial<SavedSearch>): void {
    this.searches.update(list => list.map(s => (s.id === id ? { ...s, ...changes } : s)));
  }
}
