import {
  Component, ChangeDetectionStrategy, signal, computed, inject
} from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import {
  VehicleService, VehicleMake, VehicleModelOption, VehicleAiContext, AiDocumentExtraction
} from '@core/services/vehicle.service';
import { AuthService } from '@core/auth/auth.service';
import { YOON_CURRENCY_CODE } from '@shared/pipes/fcfa.pipe';
import { SENEGAL_REGIONS, citiesOfRegion } from '@shared/data/senegal-geo';

const CUSTOMS_STATUSES = [
  { value: 'Dedouane',    label: 'Dédouané' },
  { value: 'NonDedouane', label: 'Non dédouané' },
  { value: 'Passavant',   label: 'Passavant' }
] as const;

type WizardStep = 1 | 2 | 3 | 4;

@Component({
  selector: 'lll-vehicle-form',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './vehicle-form.component.html'
})
export class VehicleFormComponent {
  private readonly fb = inject(FormBuilder);
  private readonly vehicleService = inject(VehicleService);
  private readonly router = inject(Router);
  private readonly auth = inject(AuthService);

  readonly currentStep = signal<WizardStep>(1);
  readonly isSubmitting = signal(false);
  readonly submitError = signal<string | null>(null);
  readonly makes = signal<VehicleMake[]>([]);

  // El modelo importa más de lo que parece: el indicador de precio busca los
  // comparables por modelo, así que un anuncio sin él nunca puede mostrarlo, y
  // tampoco aparece al filtrar por modelo.
  readonly models = signal<VehicleModelOption[]>([]);
  readonly loadingModels = signal(false);

  onMakeChange(event: Event): void {
    const makeId = (event.target as HTMLSelectElement).value;

    this.step1.patchValue({ modelId: '' });
    this.models.set([]);

    if (!makeId) return;

    this.loadingModels.set(true);
    this.vehicleService.getModels(makeId).subscribe({
      next: m => { this.models.set(m); this.loadingModels.set(false); },
      // Sin catálogo de modelos el anuncio se puede publicar igual: el campo es
      // opcional en el dominio.
      error: () => { this.models.set([]); this.loadingModels.set(false); }
    });
  }

  // ─── Estado IA ────────────────────────────────────────────────────────
  readonly isExtractingDoc = signal(false);
  readonly extractError = signal<string | null>(null);
  readonly extractInfo = signal<string | null>(null);

  // ─── Fotos del anuncio (Step 4) ───────────────────────────────────────
  readonly selectedImages = signal<{ file: File; preview: string }[]>([]);
  readonly uploadingImages = signal(false);

  readonly steps = [
    { number: 1, label: 'L’essentiel' },
    { number: 2, label: 'Fiche technique' },
    { number: 3, label: 'Prix et lieu' },
    { number: 4, label: 'Photos et description' },
  ];

  // Step 1: Basic
  readonly step1 = this.fb.group({
    makeId:      ['', Validators.required],
    modelId:     [''],
    year:        ['', [Validators.required, Validators.min(1990), Validators.max(new Date().getFullYear() + 1)]],
    condition:   ['Used', Validators.required],
    vin:         ['', [Validators.minLength(17), Validators.maxLength(17)]],
  });

  // ─── Équipements ──────────────────────────────────────────────────────
  // Catálogo en base de datos, nunca texto libre: es lo que permite filtrar por
  // equipamiento en el Marketplace.
  readonly equipments = signal<{ id: string; code: string; name: string }[]>([]);
  readonly selectedEquipmentIds = signal<string[]>([]);

  toggleEquipment(id: string): void {
    this.selectedEquipmentIds.update(ids =>
      ids.includes(id) ? ids.filter(x => x !== id) : [...ids, id]);
  }

  isEquipmentSelected(id: string): boolean {
    return this.selectedEquipmentIds().includes(id);
  }

  // Step 2: Specs
  readonly step2 = this.fb.group({
    bodyType:      [''],
    fuelType:      [''],
    transmission:  [''],
    mileage:       ['', Validators.min(0)],
    color:         [''],
  });

  // Step 3: Price & Location
  readonly step3 = this.fb.group({
    price:          ['', [Validators.required, Validators.min(1)]],
    // Yoon u Auto opera únicamente en FCFA: el campo queda fijo y sin selector.
    currency:       [YOON_CURRENCY_CODE, Validators.required],
    priceNegotiable:[''],
    // Bloque obligatorio de la ficha en Senegal.
    customsStatus:  ['', Validators.required],
    region:         [''],
    city:           [''],
  });

  readonly customsStatuses = CUSTOMS_STATUSES;
  readonly regions = SENEGAL_REGIONS;
  private readonly selectedRegion = signal<string>('');
  readonly cities = computed(() => citiesOfRegion(this.selectedRegion()));

  onRegionChange(event: Event): void {
    this.selectedRegion.set((event.target as HTMLSelectElement).value);
    this.step3.patchValue({ city: '' });
  }

  // Step 4: Images & Description
  readonly step4 = this.fb.group({
    title:         ['', [Validators.required, Validators.maxLength(200)]],
    description:   ['', Validators.maxLength(5000)],
  });

  readonly currentForm = computed<FormGroup>(() => {
    const forms: Record<WizardStep, FormGroup> = {
      1: this.step1, 2: this.step2, 3: this.step3, 4: this.step4
    };
    return forms[this.currentStep()];
  });

  readonly isStepValid = computed(() => this.currentForm().valid);

  readonly progress = computed(() => ((this.currentStep() - 1) / 3) * 100);

  constructor() {
    this.vehicleService.getMakes(false).subscribe(m => this.makes.set(m));
    this.vehicleService.getFilterOptions().subscribe({
      next: o => this.equipments.set(o.equipments),
      // Sin catálogo el resto del formulario sigue siendo utilizable.
      error: () => this.equipments.set([])
    });
  }

  next(): void {
    if (this.currentForm().invalid) {
      this.currentForm().markAllAsTouched();
      return;
    }
    if (this.currentStep() < 4) {
      this.currentStep.update(s => (s + 1) as WizardStep);
    }
  }

  back(): void {
    if (this.currentStep() > 1) {
      this.currentStep.update(s => (s - 1) as WizardStep);
    }
  }

  submit(): void {
    if (this.step4.invalid) {
      this.step4.markAllAsTouched();
      return;
    }

    this.isSubmitting.set(true);
    this.submitError.set(null);

    const s1 = this.step1.value;
    const s2 = this.step2.value;
    const s3 = this.step3.value;
    const s4 = this.step4.value;
    const nz = (v: unknown) => (v === '' || v === undefined ? null : v);

    const payload = {
      title:          s4.title,
      description:    nz(s4.description),
      makeId:         s1.makeId,
      modelId:        nz(s1.modelId),
      version:        null,
      year:           +s1.year!,
      mileage:        s2.mileage ? +s2.mileage : null,
      condition:      s1.condition,
      bodyType:       nz(s2.bodyType),
      fuelType:       nz(s2.fuelType),
      transmission:   nz(s2.transmission),
      color:          nz(s2.color),
      doors:          null,
      seats:          null,
      vin:            nz(s1.vin),
      powerCv:              null,
      engineDisplacementCc: null,
      drivetrain:           null,
      engineName:           null,
      customsStatus:  s3.customsStatus,
      price:          +s3.price!,
      priceNegotiable: !!s3.priceNegotiable,
      region:         nz(s3.region),
      city:           nz(s3.city),
      district:       null,
      equipmentIds:   this.selectedEquipmentIds(),
      publish:        true,
      // El backend lo sustituye por el usuario del token; se envía solo para
      // cumplir el contrato del comando.
      sellerId:       this.auth.user()?.id,
    };

    this.vehicleService.createVehicle(payload).subscribe({
      next: ({ id }) => {
        const imgs = this.selectedImages();
        if (imgs.length === 0) {
          this.isSubmitting.set(false);
          this.router.navigate(['/vehiculos']);
          return;
        }
        this.uploadingImages.set(true);
        let uploaded = 0;
        const done = () => {
          uploaded++;
          if (uploaded === imgs.length) {
            this.uploadingImages.set(false);
            this.isSubmitting.set(false);
            this.router.navigate(['/vehiculos']);
          }
        };
        imgs.forEach(({ file }) => {
          const form = new FormData();
          form.append('file', file);
          this.vehicleService.uploadImage(id, form).subscribe({
            next: () => done(),
            error: () => done()
          });
        });
      },
      error: (err) => {
        console.error('[VehicleForm] POST /vehicles failed:', err);
        console.error('[VehicleForm] Error body:', JSON.stringify(err?.error, null, 2));
        console.error('[VehicleForm] Payload enviado:', JSON.stringify(payload, null, 2));
        this.isSubmitting.set(false);
        const detail = err?.error?.error ?? err?.error?.title ?? err?.error?.message
          ?? (typeof err?.error === 'string' ? err.error : null);
        const validation = err?.error?.errors
          ? Object.entries(err.error.errors).map(([k, v]) => `${k}: ${(v as string[]).join(', ')}`).join(' · ')
          : null;
        this.submitError.set(
          validation ?? detail ?? `Error ${err?.status ?? '?'} al publicar. Abre F12 → Console para ver el detalle completo.`
        );
      }
    });
  }

  onImagesSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const files = Array.from(input.files ?? []);
    files.forEach(file => {
      const reader = new FileReader();
      reader.onload = e => {
        this.selectedImages.update(imgs => [...imgs, { file, preview: e.target!.result as string }]);
      };
      reader.readAsDataURL(file);
    });
    input.value = '';
  }

  removeImage(index: number): void {
    this.selectedImages.update(imgs => imgs.filter((_, i) => i !== index));
  }

  // Los valores son los del enum del dominio, ni uno más: «Lpg» no existe en FuelType
  // y publicar con él devolvía 400, y «Semiautomatique» tampoco existe en
  // TransmissionType, donde solo hay Manuel y Automatique.
  readonly fuelTypes = [
    { value: 'Diesel', label: 'Diesel' },
    { value: 'Essence', label: 'Essence' },
    { value: 'Hybride', label: 'Hybride' },
    { value: 'HybrideRechargeable', label: 'Hybride rechargeable' },
    { value: 'Electrique', label: 'Électrique' },
    { value: 'Autre', label: 'Autre' },
  ];

  readonly transmissions = [
    { value: 'Manuel', label: 'Manuelle' },
    { value: 'Automatique', label: 'Automatique' },
  ];

  // Pick-up y monospace faltaban: sin ellos no se podía publicar un Hilux, que es
  // justo lo que más se vende en Senegal.
  readonly bodyTypes = [
    { value: 'Citadine', label: 'Citadine' },
    { value: 'Berline', label: 'Berline' },
    { value: 'Break', label: 'Break' },
    { value: 'Suv', label: 'SUV / 4x4' },
    { value: 'Coupe', label: 'Coupé' },
    { value: 'Cabriolet', label: 'Cabriolet' },
    { value: 'Monospace', label: 'Monospace' },
    { value: 'PickUp', label: 'Pick-up' },
    { value: 'Utilitaire', label: 'Utilitaire' },
  ];
  readonly countries = [
    { code: 'ES', name: 'España' }, { code: 'DE', name: 'Alemania' },
    { code: 'FR', name: 'Francia' }, { code: 'IT', name: 'Italia' },
    { code: 'JP', name: 'Japón' }, { code: 'US', name: 'EE.UU.' },
    { code: 'GB', name: 'Reino Unido' }, { code: 'MA', name: 'Marruecos' },
  ];
  readonly years = Array.from({ length: new Date().getFullYear() - 1989 }, (_, i) => new Date().getFullYear() - i);

  // ─── IA: extraer datos de documento (Step 1) ──────────────────────────
  onDocumentSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) return;

    this.isExtractingDoc.set(true);
    this.extractError.set(null);
    this.extractInfo.set(null);

    this.vehicleService.extractDocument(file).subscribe({
      next: (data) => {
        this.isExtractingDoc.set(false);
        this.applyDocumentExtraction(data);
        input.value = '';
      },
      error: () => {
        this.isExtractingDoc.set(false);
        this.extractError.set('No se pudo procesar el documento. Inténtalo de nuevo.');
        input.value = '';
      }
    });
  }

  private applyDocumentExtraction(data: AiDocumentExtraction): void {
    const filled: string[] = [];

    // Step 1: marca por nombre + año + VIN
    if (data.make) {
      const match = this.makes().find(
        m => m.name.toLowerCase() === data.make!.toLowerCase()
      );
      if (match) { this.step1.patchValue({ makeId: match.id }); filled.push('marca'); }
    }
    if (data.year) { this.step1.patchValue({ year: String(data.year) }); filled.push('año'); }
    if (data.vin)  { this.step1.patchValue({ vin: data.vin }); filled.push('VIN'); }

    // Step 2: km, color, combustible
    if (data.mileage  !== null) { this.step2.patchValue({ mileage: String(data.mileage) }); filled.push('km'); }
    if (data.color)             { this.step2.patchValue({ color: data.color }); filled.push('color'); }
    if (data.fuelType)          { this.step2.patchValue({ fuelType: data.fuelType }); filled.push('combustible'); }

    this.extractInfo.set(
      filled.length > 0
        ? `Campos rellenados: ${filled.join(', ')}.`
        : 'No se han extraído campos del documento.'
    );
  }

}
