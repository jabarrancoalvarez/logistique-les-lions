import { Component, ChangeDetectionStrategy, computed, signal, inject } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';
import { AuthService, AccountType } from '@core/auth/auth.service';
import { SENEGAL_REGIONS, citiesOfRegion } from '@shared/data/senegal-geo';

/** Teléfono senegalés en cualquiera de sus formas: 77 123 45 67, +221771234567, 00221... */
const SENEGAL_PHONE = /^(?:\+221|00221|221)?[\s.-]*[0-9](?:[\s.-]*[0-9]){8}$/;

const ACCOUNT_TYPES: readonly { value: AccountType; label: string; hint: string }[] = [
  { value: 'Particulier', label: 'Particulier', hint: 'Je vends ou j\'achète à titre personnel' },
  { value: 'Professionnel', label: 'Professionnel', hint: 'Je représente un garage ou une société' }
];

@Component({
  selector: 'lll-register',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './register.component.html'
})
export class RegisterComponent {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  /**
   * Destino tras crear la cuenta. Lo usan las acciones que exigen registro
   * (Favoris, Comparer…) para devolver al usuario al anuncio del que venía.
   */
  private get returnUrl(): string {
    return this.route.snapshot.queryParamMap.get('returnUrl') ?? '/dashboard';
  }

  readonly loading = signal(false);
  readonly error = signal<string | null>(null);

  readonly accountTypes = ACCOUNT_TYPES;
  readonly regions = SENEGAL_REGIONS;

  /** Región seleccionada, para poblar el desplegable de ciudades. */
  private readonly selectedRegion = signal<string>('');
  readonly cities = computed(() => citiesOfRegion(this.selectedRegion()));

  readonly form: FormGroup = this.fb.group({
    phone:       ['', [Validators.required, Validators.pattern(SENEGAL_PHONE)]],
    displayName: ['', [Validators.required, Validators.maxLength(150)]],
    accountType: ['Particulier' as AccountType, Validators.required],
    region:      [''],
    city:        [''],
    password:    ['', [Validators.required, Validators.minLength(8)]],
    email:       ['', Validators.email]
  });

  onRegionChange(event: Event): void {
    const code = (event.target as HTMLSelectElement).value;
    this.selectedRegion.set(code);
    // La ciudad depende de la región: al cambiarla, la selección previa deja de ser válida.
    this.form.patchValue({ city: '' });
  }

  submit(): void {
    if (this.form.invalid || this.loading()) {
      this.form.markAllAsTouched();
      return;
    }

    this.loading.set(true);
    this.error.set(null);

    const v = this.form.value;
    this.auth.register({
      phone:       v.phone,
      password:    v.password,
      displayName: v.displayName,
      accountType: v.accountType,
      region:      v.region || undefined,
      city:        v.city || undefined,
      email:       v.email || undefined
    }).subscribe({
      next: () => this.router.navigateByUrl(this.returnUrl),
      error: err => {
        this.error.set(this.messageFor(err?.error?.error));
        this.loading.set(false);
      }
    });
  }

  private messageFor(code: string | undefined): string {
    switch (code) {
      case 'Auth.PhoneAlreadyExists':
        return 'Ce numéro de téléphone est déjà utilisé.';
      case 'Auth.EmailAlreadyExists':
        return 'Cette adresse e-mail est déjà utilisée.';
      case 'Auth.InvalidPhone':
        return 'Numéro de téléphone sénégalais invalide.';
      default:
        return 'Impossible de créer le compte. Veuillez réessayer.';
    }
  }
}
