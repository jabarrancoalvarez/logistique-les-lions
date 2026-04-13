import {
  Component, ChangeDetectionStrategy, signal, computed, inject
} from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { VehicleService, VehicleMake, VehicleAiContext, AiDocumentExtraction } from '@core/services/vehicle.service';
import { AuthService } from '@core/auth/auth.service';

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

  // ─── Estado IA ────────────────────────────────────────────────────────
  readonly isExtractingDoc = signal(false);
  readonly extractError = signal<string | null>(null);
  readonly extractInfo = signal<string | null>(null);
  readonly isGeneratingDesc = signal(false);
  readonly aiError = signal<string | null>(null);
  readonly aiDescriptionEn = signal<string>('');

  // ─── Fotos del anuncio (Step 4) ───────────────────────────────────────
  readonly selectedImages = signal<{ file: File; preview: string }[]>([]);
  readonly uploadingImages = signal(false);

  readonly steps = [
    { number: 1, label: 'Datos básicos' },
    { number: 2, label: 'Especificaciones' },
    { number: 3, label: 'Precio y ubicación' },
    { number: 4, label: 'Fotos y descripción' },
  ];

  // Step 1: Basic
  readonly step1 = this.fb.group({
    makeId:      ['', Validators.required],
    modelId:     [''],
    year:        ['', [Validators.required, Validators.min(1990), Validators.max(new Date().getFullYear() + 1)]],
    condition:   ['Used', Validators.required],
    vin:         ['', [Validators.minLength(17), Validators.maxLength(17)]],
  });

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
    currency:       ['EUR', Validators.required],
    priceNegotiable:[''],
    countryOrigin:  ['', Validators.required],
    city:           [''],
    isExportReady:  [''],
  });

  // Step 4: Images & Description
  readonly step4 = this.fb.group({
    title:         ['', [Validators.required, Validators.maxLength(200)]],
    descriptionEs: ['', Validators.maxLength(5000)],
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
      descriptionEs:  nz(s4.descriptionEs),
      descriptionEn:  null,
      makeId:         s1.makeId,
      modelId:        nz(s1.modelId),
      year:           +s1.year!,
      mileage:        s2.mileage ? +s2.mileage : null,
      condition:      s1.condition,
      bodyType:       nz(s2.bodyType),
      fuelType:       nz(s2.fuelType),
      transmission:   nz(s2.transmission),
      color:          nz(s2.color),
      vin:            nz(s1.vin),
      price:          +s3.price!,
      currency:       s3.currency,
      priceNegotiable: !!s3.priceNegotiable,
      countryOrigin:  s3.countryOrigin,
      city:           nz(s3.city),
      postalCode:     null,
      isExportReady:  !!s3.isExportReady,
      sellerId:       this.auth.user()?.id,
      dealerId:       null,
      specs:          null,
      features:       null,
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

  readonly currencies = ['EUR', 'USD', 'GBP', 'MAD', 'JPY'];
  readonly fuelTypes = [
    { value: 'Gasoline', label: 'Gasolina' },
    { value: 'Diesel', label: 'Diésel' },
    { value: 'Electric', label: 'Eléctrico' },
    { value: 'Hybrid', label: 'Híbrido' },
    { value: 'PluginHybrid', label: 'Híbrido enchufable' },
    { value: 'Lpg', label: 'GLP' },
  ];
  readonly transmissions = [
    { value: 'Manual', label: 'Manual' },
    { value: 'Automatic', label: 'Automático' },
    { value: 'SemiAutomatic', label: 'Semiautomático' },
  ];
  readonly bodyTypes = [
    { value: 'Sedan', label: 'Sedán' },
    { value: 'Hatchback', label: 'Hatchback' },
    { value: 'Suv', label: 'SUV' },
    { value: 'Coupe', label: 'Coupé' },
    { value: 'Convertible', label: 'Descapotable' },
    { value: 'Wagon', label: 'Familiar' },
    { value: 'Van', label: 'Furgoneta' },
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

  // ─── IA: generar descripción (Step 4) ─────────────────────────────────
  generateDescription(): void {
    const makeId = this.step1.value.makeId;
    const make   = this.makes().find(m => m.id === makeId)?.name;
    const year   = this.step1.value.year ? +this.step1.value.year : null;
    const price  = this.step3.value.price ? +this.step3.value.price : null;

    if (!make || !year || !price || !this.step3.value.countryOrigin) {
      this.aiError.set('Completa los pasos anteriores (marca, año, precio, país) antes de generar la descripción.');
      return;
    }

    const context: VehicleAiContext = {
      make,
      model:        null,
      year,
      mileage:      this.step2.value.mileage ? +this.step2.value.mileage : null,
      fuelType:     this.step2.value.fuelType || null,
      transmission: this.step2.value.transmission || null,
      bodyType:     this.step2.value.bodyType || null,
      color:        this.step2.value.color || null,
      condition:    this.step1.value.condition || 'Used',
      price,
      currency:     this.step3.value.currency || 'EUR',
      countryOrigin: this.step3.value.countryOrigin || '',
      isExportReady: !!this.step3.value.isExportReady
    };

    this.isGeneratingDesc.set(true);
    this.aiError.set(null);
    this.aiDescriptionEn.set('');

    this.vehicleService.previewAiDescription(context).subscribe({
      next: (res) => {
        this.isGeneratingDesc.set(false);
        this.step4.patchValue({ descriptionEs: res.descriptionEs });
        this.aiDescriptionEn.set(res.descriptionEn);
      },
      error: () => {
        this.isGeneratingDesc.set(false);
        this.aiError.set('No se pudo generar la descripción. Inténtalo de nuevo.');
      }
    });
  }
}
