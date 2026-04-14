import { Component, signal } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '@core/auth/auth.service';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'lll-register',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './register.component.html'
})
export class RegisterComponent {
  form: FormGroup;
  loading = signal(false);
  error   = signal<string | null>(null);

  readonly roles = [
    { value: 0, label: 'Comprador particular' },
    { value: 1, label: 'Vendedor particular' },
    { value: 2, label: 'Concesionario / Dealer' }
  ];

  constructor(
    private fb: FormBuilder,
    private auth: AuthService,
    private router: Router
  ) {
    this.form = this.fb.group({
      email:       ['', [Validators.required, Validators.email]],
      password:    ['', [Validators.required, Validators.minLength(8)]],
      firstName:   ['', [Validators.required, Validators.maxLength(100)]],
      lastName:    ['', [Validators.required, Validators.maxLength(100)]],
      role:        [0, Validators.required],
      phone:       [''],
      countryCode: [''],
      companyName: [''],
      companyVat:  ['']
    });
  }

  get isDealer(): boolean { return Number(this.form.value.role) === 2; }

  submit(): void {
    if (this.form.invalid || this.loading()) return;
    this.loading.set(true);
    this.error.set(null);

    const v = this.form.value;
    this.auth.register({
      email:       v.email,
      password:    v.password,
      firstName:   v.firstName,
      lastName:    v.lastName,
      role:        Number(v.role),
      phone:       v.phone || undefined,
      countryCode: v.countryCode || undefined,
      companyName: v.companyName || undefined,
      companyVat:  v.companyVat || undefined
    }).subscribe({
      next: () => this.router.navigate(['/dashboard']),
      error: (err) => {
        this.error.set(err?.error?.error ?? 'Error al registrarse. Inténtalo de nuevo.');
        this.loading.set(false);
      }
    });
  }
}
