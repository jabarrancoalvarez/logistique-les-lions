import {
  Component, ChangeDetectionStrategy, signal, computed, inject
} from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { VehicleService, VehicleMake } from '@core/services/vehicle.service';
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

    const payload = {
      ...this.step1.value,
      ...this.step2.value,
      ...this.step3.value,
      ...this.step4.value,
      mileage: this.step2.value.mileage ? +this.step2.value.mileage : null,
      price: +this.step3.value.price!,
      year: +this.step1.value.year!,
      priceNegotiable: !!this.step3.value.priceNegotiable,
      isExportReady: !!this.step3.value.isExportReady,
      sellerId: this.auth.user()?.id,
    };

    this.vehicleService.createVehicle(payload).subscribe({
      next: ({ id }) => {
        this.isSubmitting.set(false);
        this.router.navigate(['/vehiculos']);
      },
      error: () => {
        this.isSubmitting.set(false);
        this.submitError.set('Error al publicar el vehículo. Inténtalo de nuevo.');
      }
    });
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
}
