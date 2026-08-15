import { Component, ChangeDetectionStrategy, signal, inject } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { AuthService } from '@core/auth/auth.service';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'lll-login',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './login.component.html'
})
export class LoginComponent {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  /** Destino tras iniciar sesión; ver el mismo mecanismo en el registro. */
  private get returnUrl(): string {
    return this.route.snapshot.queryParamMap.get('returnUrl') ?? '/dashboard';
  }

  readonly loading = signal(false);
  readonly error = signal<string | null>(null);

  /**
   * `identifier` admite el teléfono (identificador principal de la cuenta) o el
   * correo, para que las cuentas anteriores a la migración sigan entrando.
   */
  readonly form: FormGroup = this.fb.group({
    identifier: ['', Validators.required],
    password:   ['', [Validators.required, Validators.minLength(8)]]
  });

  submit(): void {
    if (this.form.invalid || this.loading()) {
      this.form.markAllAsTouched();
      return;
    }

    this.loading.set(true);
    this.error.set(null);

    const { identifier, password } = this.form.value;
    this.auth.login(identifier, password).subscribe({
      next: () => this.router.navigateByUrl(this.returnUrl),
      error: err => {
        this.error.set(this.messageFor(err?.error?.error));
        this.loading.set(false);
      }
    });
  }

  private messageFor(code: string | undefined): string {
    switch (code) {
      case 'Auth.AccountBlocked':
        return 'Ce compte a été bloqué. Contactez le support.';
      case 'Auth.AccountSuspended':
        return 'Ce compte est temporairement suspendu.';
      default:
        return 'Numéro de téléphone ou mot de passe incorrect.';
    }
  }
}
