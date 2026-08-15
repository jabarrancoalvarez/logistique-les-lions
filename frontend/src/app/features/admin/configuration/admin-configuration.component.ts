import { Component, ChangeDetectionStrategy, OnInit, signal, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import {
  AdminService, AdminSettings, Catalogs, CatalogMake, CatalogEquipment, CatalogFeature,
  ActivityLog, FeatureInterestReport, ADMIN_ACTION_LABELS
} from '@core/services/admin.service';

type Tab = 'parametres' | 'catalogues' | 'interet' | 'journal';

/**
 * «Configuration générale».
 *
 * Cuatro pestañas porque son cuatro trabajos distintos que casi nunca se hacen a la vez:
 * mover un parámetro, mantener los catálogos, leer qué pide la gente y revisar quién ha
 * hecho qué.
 */
@Component({
  selector: 'lll-admin-configuration',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule, FormsModule],
  templateUrl: './admin-configuration.component.html'
})
export class AdminConfigurationComponent implements OnInit {
  private readonly admin = inject(AdminService);

  readonly tab = signal<Tab>('parametres');

  readonly tabs: { id: Tab; label: string }[] = [
    { id: 'parametres', label: 'Paramètres' },
    { id: 'catalogues', label: 'Catalogues' },
    { id: 'interet',    label: "Intérêt des utilisateurs" },
    { id: 'journal',    label: "Journal d'activité" }
  ];
  readonly busy = signal(false);
  readonly error = signal<string | null>(null);
  readonly saved = signal(false);

  /** El servidor manda el nombre del enum; si aparece uno nuevo se muestra tal cual. */
  actionLabel = (type: string) =>
    (ADMIN_ACTION_LABELS as Record<string, string>)[type] ?? type;

  ngOnInit(): void {
    this.loadSettings();
  }

  changeTab(tab: Tab): void {
    this.tab.set(tab);
    this.error.set(null);

    if (tab === 'catalogues' && !this.catalogs()) this.loadCatalogs();
    if (tab === 'interet' && !this.interest()) this.loadInterest(null);
    if (tab === 'journal' && !this.log()) this.loadLog();
  }

  // ─── Paramètres ──────────────────────────────────────────────────────────
  readonly settings = signal<AdminSettings | null>(null);

  loadSettings(): void {
    this.admin.getSettings().subscribe({
      next: s => this.settings.set(s),
      error: () => this.error.set('Impossible de charger les paramètres.')
    });
  }

  saveSettings(): void {
    const s = this.settings();
    if (!s || this.busy()) return;

    this.busy.set(true);
    this.error.set(null);
    this.saved.set(false);

    this.admin.updateSettings({
      platform: s.platform, priceIndicator: s.priceIndicator, valuation: s.valuation
    }).subscribe({
      next: () => { this.busy.set(false); this.saved.set(true); },
      error: err => {
        this.busy.set(false);
        this.error.set(SETTINGS_ERRORS[err?.error] ?? 'Enregistrement impossible.');
      }
    });
  }

  toggleFlag(id: string, isEnabled: boolean): void {
    this.admin.toggleFeatureFlag(id, isEnabled).subscribe({
      next: () => {
        const s = this.settings();
        if (!s) return;
        this.settings.set({
          ...s,
          flags: s.flags.map(f => f.id === id ? { ...f, isEnabled } : f)
        });
      },
      error: () => this.error.set('Impossible de changer cet interrupteur.')
    });
  }

  // ─── Catalogues ──────────────────────────────────────────────────────────
  readonly catalogs = signal<Catalogs | null>(null);
  readonly openMakeId = signal<string | null>(null);

  loadCatalogs(): void {
    this.admin.getCatalogs().subscribe({
      next: c => this.catalogs.set(c),
      error: () => this.error.set('Impossible de charger les catalogues.')
    });
  }

  toggleMake(id: string): void {
    this.openMakeId.set(this.openMakeId() === id ? null : id);
  }

  // Formularios de alta, uno por catálogo.
  newMake = { name: '', country: '' };
  newModel = { makeId: '', name: '', category: '' };
  newEquipment = { code: '', name: '', displayOrder: 0 };
  newFeature = { code: '', name: '', description: '', displayOrder: 0 };

  addMake(): void {
    if (!this.newMake.name.trim()) return;

    this.admin.saveMake({
      id: null, name: this.newMake.name.trim(),
      country: this.newMake.country.trim() || null, isPopular: false
    }).subscribe({
      next: () => { this.newMake = { name: '', country: '' }; this.loadCatalogs(); },
      error: err => this.error.set(CATALOG_ERRORS[err?.error] ?? 'Enregistrement impossible.')
    });
  }

  addModel(make: CatalogMake): void {
    if (!this.newModel.name.trim()) return;

    this.admin.saveModel({
      id: null, makeId: make.id, name: this.newModel.name.trim(),
      category: this.newModel.category.trim() || null
    }).subscribe({
      next: () => {
        this.newModel = { makeId: '', name: '', category: '' };
        this.loadCatalogs();
      },
      error: err => this.error.set(CATALOG_ERRORS[err?.error] ?? 'Enregistrement impossible.')
    });
  }

  addEquipment(): void {
    if (!this.newEquipment.code.trim() || !this.newEquipment.name.trim()) return;

    this.admin.saveEquipment({
      id: null, code: this.newEquipment.code.trim(), name: this.newEquipment.name.trim(),
      displayOrder: this.newEquipment.displayOrder, isActive: true
    }).subscribe({
      next: () => {
        this.newEquipment = { code: '', name: '', displayOrder: 0 };
        this.loadCatalogs();
      },
      error: err => this.error.set(CATALOG_ERRORS[err?.error] ?? 'Enregistrement impossible.')
    });
  }

  /** Retirar no borra: esconde del formulario y deja intactos los anuncios. */
  toggleEquipment(e: CatalogEquipment): void {
    this.admin.saveEquipment({
      id: e.id, code: e.code, name: e.name,
      displayOrder: e.displayOrder, isActive: !e.isActive
    }).subscribe({
      next: () => this.loadCatalogs(),
      error: () => this.error.set('Modification impossible.')
    });
  }

  addFeature(): void {
    if (!this.newFeature.code.trim() || !this.newFeature.name.trim()) return;

    this.admin.saveUpcomingFeature({
      id: null, code: this.newFeature.code.trim(), name: this.newFeature.name.trim(),
      description: this.newFeature.description.trim() || null,
      displayOrder: this.newFeature.displayOrder, isActive: true
    }).subscribe({
      next: () => {
        this.newFeature = { code: '', name: '', description: '', displayOrder: 0 };
        this.loadCatalogs();
      },
      error: err => this.error.set(CATALOG_ERRORS[err?.error] ?? 'Enregistrement impossible.')
    });
  }

  toggleFeature(f: CatalogFeature): void {
    this.admin.saveUpcomingFeature({
      id: f.id, code: f.code, name: f.name, description: f.description,
      displayOrder: f.displayOrder, isActive: !f.isActive
    }).subscribe({
      next: () => { this.loadCatalogs(); this.interest.set(null); },
      error: () => this.error.set('Modification impossible.')
    });
  }

  // ─── Intérêt ─────────────────────────────────────────────────────────────
  readonly interest = signal<FeatureInterestReport | null>(null);
  selectedFeatureId: string | null = null;

  loadInterest(featureId: string | null): void {
    this.selectedFeatureId = featureId;

    this.admin.getFeatureInterest(featureId).subscribe({
      next: r => this.interest.set(r),
      error: () => this.error.set('Impossible de charger les intérêts.')
    });
  }

  // ─── Journal ─────────────────────────────────────────────────────────────
  readonly log = signal<ActivityLog | null>(null);
  logFilters = { adminId: '', type: '', from: '', to: '', page: 1 };

  readonly actionTypes = Object.keys(ADMIN_ACTION_LABELS);

  loadLog(): void {
    this.admin.getActivityLog({
      adminId: this.logFilters.adminId || null,
      type: this.logFilters.type || null,
      from: this.logFilters.from || null,
      to: this.logFilters.to || null,
      page: this.logFilters.page
    }).subscribe({
      next: l => this.log.set(l),
      error: () => this.error.set('Impossible de charger le journal.')
    });
  }

  applyLogFilters(): void {
    this.logFilters.page = 1;
    this.loadLog();
  }

  changeLogPage(delta: number): void {
    const l = this.log();
    if (!l) return;

    const last = Math.max(1, Math.ceil(l.totalCount / l.pageSize));
    const next = Math.min(last, Math.max(1, this.logFilters.page + delta));

    if (next === this.logFilters.page) return;

    this.logFilters.page = next;
    this.loadLog();
  }
}

/** Los errores del servidor, dichos en francés y con el número que los provoca. */
const SETTINGS_ERRORS: Record<string, string> = {
  'Settings.ComparatorOutOfRange': 'Le comparateur doit accepter entre 2 et 6 véhicules.',
  'Settings.PointsOutOfRange': 'Les points par vente doivent être entre 0 et 10 000.',
  'Settings.MaxImagesOutOfRange': 'Le nombre de photos doit être entre 1 et 50.',
  'Settings.FreshnessOutOfRange': 'La fraîcheur d\'une annonce doit être entre 7 et 365 jours.',
  'Settings.LegalVersionRequired': 'La version des conditions est obligatoire.',
  'Settings.MinComparablesOutOfRange': 'Il faut au moins un véhicule comparable.',
  'Settings.MarginOutOfRange': 'Les marges doivent être comprises entre 0 et 1 (0,10 = 10 %).',
  'Settings.SpreadOutOfRange': 'La fourchette doit être comprise entre 0 et 1.',
  'Settings.SnapshotIntervalOutOfRange': 'L\'intervalle doit être d\'au moins un jour.'
};

const CATALOG_ERRORS: Record<string, string> = {
  'Catalog.NameRequired': 'Le nom est obligatoire.',
  'Catalog.CodeRequired': 'Le code est obligatoire.',
  'Catalog.MakeAlreadyExists': 'Cette marque existe déjà.',
  'Catalog.ModelAlreadyExists': 'Ce modèle existe déjà pour cette marque.',
  'Catalog.EquipmentAlreadyExists': 'Ce code d\'équipement existe déjà.',
  'Catalog.FeatureAlreadyExists': 'Ce code de fonctionnalité existe déjà.',
  'Catalog.MakeNotFound': 'Marque introuvable.'
};
