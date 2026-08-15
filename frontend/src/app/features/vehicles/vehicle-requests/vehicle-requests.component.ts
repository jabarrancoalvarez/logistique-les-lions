import { Component, ChangeDetectionStrategy, OnInit, signal, computed, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import {
  VehicleRequestService, VehicleRequestSummary, VehicleRequestOrigin,
  REQUEST_STATUS_LABELS, REQUEST_STATUS_CLASSES, REQUEST_ORIGIN_LABELS
} from '@core/services/vehicle-request.service';
import {
  VehicleService, VehicleMake,
  FUEL_LABELS, TRANSMISSION_LABELS, BODY_LABELS
} from '@core/services/vehicle.service';
import { FcfaPipe } from '@shared/pipes/fcfa.pipe';

function optionsFrom<K extends string>(labels: Record<K, string>): { value: K; label: string }[] {
  return (Object.keys(labels) as K[]).map(value => ({ value, label: labels[value] }));
}

@Component({
  selector: 'lll-vehicle-requests',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule, ReactiveFormsModule, RouterLink, FcfaPipe],
  templateUrl: './vehicle-requests.component.html'
})
export class VehicleRequestsComponent implements OnInit {
  private readonly service = inject(VehicleRequestService);
  private readonly vehicleService = inject(VehicleService);
  private readonly fb = inject(FormBuilder);

  readonly requests = signal<VehicleRequestSummary[]>([]);
  readonly loading = signal(true);
  readonly hasError = signal(false);

  readonly makes = signal<VehicleMake[]>([]);

  /** El formulario se despliega bajo el botón «+ Trouvez-moi une voiture». */
  readonly formOpen = signal(false);
  readonly submitting = signal(false);
  readonly submitError = signal<string | null>(null);
  readonly createdReference = signal<string | null>(null);

  readonly fuelTypes = optionsFrom(FUEL_LABELS);
  readonly transmissions = optionsFrom(TRANSMISSION_LABELS);
  readonly bodyTypes = optionsFrom(BODY_LABELS).filter(o => o.value !== 'Autre');
  readonly origins = optionsFrom(REQUEST_ORIGIN_LABELS);

  readonly currentYear = new Date().getFullYear();
  readonly years = Array.from({ length: this.currentYear - 1989 }, (_, i) => this.currentYear - i);

  readonly form: FormGroup = this.fb.group({
    makeId:             [''],
    modelName:          ['', Validators.maxLength(100)],
    version:            ['', Validators.maxLength(100)],
    yearFrom:           [''],
    yearTo:             [''],
    maxMileage:         [''],
    fuelType:           [''],
    transmission:       [''],
    bodyType:           [''],
    color:              ['', Validators.maxLength(50)],
    importantEquipment: ['', Validators.maxLength(1000)],
    maxBudget:          [''],
    // La V1 orienta esta función sobre todo a la importación.
    origin:             ['Importation' as VehicleRequestOrigin, Validators.required],
    notes:              ['', Validators.maxLength(2000)]
  });

  /** La marca es lo único obligatorio del formulario. */
  readonly canSubmit = computed(() => !this.submitting());

  ngOnInit(): void {
    this.vehicleService.getMakes(false).subscribe(m => this.makes.set(m));
    this.load();
  }

  private load(): void {
    this.loading.set(true);
    this.service.getAll().subscribe({
      next: items => {
        this.requests.set(items);
        this.loading.set(false);
      },
      error: () => {
        this.hasError.set(true);
        this.loading.set(false);
      }
    });
  }

  toggleForm(): void {
    this.formOpen.update(v => !v);
    this.submitError.set(null);
  }

  submit(): void {
    const makeId = this.form.value.makeId as string;
    const makeName = this.makes().find(m => m.id === makeId)?.name;

    if (!makeName) {
      this.submitError.set('Choisissez une marque.');
      return;
    }
    if (this.form.invalid || this.submitting()) return;

    this.submitting.set(true);
    this.submitError.set(null);

    const v = this.form.value;
    const num = (value: string) => (value ? +value : undefined);

    this.service.create({
      makeId,
      makeName,
      modelName:          v.modelName || undefined,
      version:            v.version || undefined,
      yearFrom:           num(v.yearFrom),
      yearTo:             num(v.yearTo),
      maxMileage:         num(v.maxMileage),
      fuelType:           v.fuelType || undefined,
      transmission:       v.transmission || undefined,
      bodyType:           v.bodyType || undefined,
      color:              v.color || undefined,
      importantEquipment: v.importantEquipment || undefined,
      maxBudget:          num(v.maxBudget),
      origin:             v.origin,
      notes:              v.notes || undefined
    }).subscribe({
      next: created => {
        this.submitting.set(false);
        this.formOpen.set(false);
        this.createdReference.set(created.publicReference);
        this.form.reset({ origin: 'Importation' });
        this.load();
        setTimeout(() => this.createdReference.set(null), 8000);
      },
      error: err => {
        this.submitting.set(false);
        this.submitError.set(err?.error === 'VehicleRequest.TooManyOpen'
          ? 'Vous avez trop de demandes en cours. Terminez ou annulez-en une avant d\'en créer une nouvelle.'
          : 'Impossible d\'envoyer la demande. Veuillez réessayer.');
      }
    });
  }

  // ─── Etiquetas de la tarjeta ───────────────────────────────────────────
  statusLabel(r: VehicleRequestSummary): string {
    return REQUEST_STATUS_LABELS[r.status];
  }

  statusClass(r: VehicleRequestSummary): string {
    return REQUEST_STATUS_CLASSES[r.status];
  }

  originLabel(r: VehicleRequestSummary): string {
    return REQUEST_ORIGIN_LABELS[r.origin];
  }

  /** "2018–2022 · ≤120.000 km" */
  criteria(r: VehicleRequestSummary): string {
    const parts: string[] = [];
    if (r.yearFrom && r.yearTo) parts.push(`${r.yearFrom}–${r.yearTo}`);
    else if (r.yearFrom) parts.push(`à partir de ${r.yearFrom}`);
    else if (r.yearTo) parts.push(`jusqu'à ${r.yearTo}`);
    if (r.maxMileage) parts.push(`≤${r.maxMileage.toLocaleString('fr-FR')} km`);
    return parts.join(' · ');
  }

  vehicleName(r: VehicleRequestSummary): string {
    return [r.makeName, r.modelName].filter(Boolean).join(' ');
  }
}
