import { Component, ChangeDetectionStrategy, OnInit, computed, signal, inject } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { AuthService, ProfileData, AccountType } from '@core/auth/auth.service';
import { CommonModule } from '@angular/common';

import { SENEGAL_REGIONS, citiesOfRegion } from '@shared/data/senegal-geo';

const ACCOUNT_TYPES: readonly { value: AccountType; label: string }[] = [
  { value: 'Particulier', label: 'Particulier' },
  { value: 'Professionnel', label: 'Professionnel' }
];

@Component({
  selector: 'lll-profile',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './profile.component.html'
})
export class ProfileComponent implements OnInit {
  private readonly auth = inject(AuthService);
  private readonly fb = inject(FormBuilder);

  readonly profile = signal<ProfileData | null>(null);
  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly saved = signal(false);
  readonly error = signal<string | null>(null);

  readonly accountTypes = ACCOUNT_TYPES;
  readonly regions = SENEGAL_REGIONS;

  private readonly selectedRegion = signal<string>('');
  readonly cities = computed(() => citiesOfRegion(this.selectedRegion()));

  /** "Membre depuis 2026" — solo el año, como en la especificación. */
  readonly memberSince = computed(() => {
    const p = this.profile();
    return p ? new Date(p.createdAt).getFullYear() : null;
  });

  readonly form: FormGroup = this.fb.group({
    displayName:          ['', [Validators.required, Validators.maxLength(150)]],
    accountType:          ['Particulier' as AccountType, Validators.required],
    region:               [''],
    city:                 [''],
    email:                ['', Validators.email],
    bio:                  [''],
    allowWhatsAppContact: [true]
  });

  ngOnInit(): void {
    this.auth.getProfile().subscribe({
      next: p => {
        this.profile.set(p);
        this.selectedRegion.set(p.region ?? '');
        this.form.patchValue({
          displayName:          p.displayName,
          accountType:          p.accountType,
          region:               p.region ?? '',
          city:                 p.city ?? '',
          email:                p.email ?? '',
          bio:                  p.bio ?? '',
          allowWhatsAppContact: p.allowWhatsAppContact
        });
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Impossible de charger votre profil.');
        this.loading.set(false);
      }
    });
  }

  onRegionChange(event: Event): void {
    const code = (event.target as HTMLSelectElement).value;
    this.selectedRegion.set(code);
    this.form.patchValue({ city: '' });
  }

  save(): void {
    if (this.form.invalid || this.saving()) {
      this.form.markAllAsTouched();
      return;
    }

    this.saving.set(true);
    this.error.set(null);

    const v = this.form.value;
    this.auth.updateProfile({
      displayName:          v.displayName,
      accountType:          v.accountType,
      region:               v.region || undefined,
      city:                 v.city || undefined,
      email:                v.email || undefined,
      bio:                  v.bio || undefined,
      allowWhatsAppContact: v.allowWhatsAppContact
    }).subscribe({
      next: () => {
        this.saving.set(false);
        this.saved.set(true);
        setTimeout(() => this.saved.set(false), 3000);
      },
      error: err => {
        this.error.set(err?.error?.error === 'Auth.EmailAlreadyExists'
          ? 'Cette adresse e-mail est déjà utilisée.'
          : 'Impossible d\'enregistrer les modifications.');
        this.saving.set(false);
      }
    });
  }
}
