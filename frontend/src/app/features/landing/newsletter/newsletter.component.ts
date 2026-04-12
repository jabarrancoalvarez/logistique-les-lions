import { Component, ChangeDetectionStrategy, signal, inject } from '@angular/core';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { environment } from '@environments/environment';

@Component({
  selector: 'lll-newsletter',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule],
  templateUrl: './newsletter.component.html'
})
export class NewsletterComponent {
  private readonly fb = new FormBuilder();
  private readonly http = inject(HttpClient);

  readonly form: FormGroup = this.fb.group({
    email: ['', [Validators.required, Validators.email]],
    acceptPrivacy: [false, Validators.requiredTrue]
  });

  readonly state = signal<'idle' | 'loading' | 'success' | 'error'>('idle');

  onSubmit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.state.set('loading');

    this.http.post(`${environment.apiUrl}/v1/newsletter/subscribe`, {
      email: this.form.value.email
    }).subscribe({
      next: () => { this.state.set('success'); this.form.reset(); },
      error: () => this.state.set('error')
    });
  }

  get emailError(): string | null {
    const ctrl = this.form.get('email');
    if (!ctrl?.touched || !ctrl?.errors) return null;
    if (ctrl.errors['required']) return 'El email es obligatorio.';
    if (ctrl.errors['email']) return 'Introduce un email válido.';
    return null;
  }
}
