import { Component, OnInit, signal } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { AuthService, ProfileData } from '@core/auth/auth.service';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'lll-profile',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './profile.component.html'
})
export class ProfileComponent implements OnInit {
  profile = signal<ProfileData | null>(null);
  loading = signal(true);
  saving  = signal(false);
  saved   = signal(false);
  form!: FormGroup;

  constructor(private auth: AuthService, private fb: FormBuilder) {}

  ngOnInit(): void {
    this.auth.getProfile().subscribe({
      next: (p) => {
        this.profile.set(p);
        this.loading.set(false);
        this.form = this.fb.group({
          firstName:   [p.firstName],
          lastName:    [p.lastName],
          phone:       [p.phone ?? ''],
          countryCode: [p.countryCode ?? ''],
          city:        [p.city ?? ''],
          companyName: [p.companyName ?? ''],
          companyVat:  [p.companyVat ?? ''],
          bio:         [p.bio ?? '']
        });
      },
      error: () => this.loading.set(false)
    });
  }

  save(): void {
    if (!this.form || this.saving()) return;
    this.saving.set(true);
    this.auth.updateProfile(this.form.value).subscribe({
      next: () => {
        this.saving.set(false);
        this.saved.set(true);
        setTimeout(() => this.saved.set(false), 3000);
      },
      error: () => this.saving.set(false)
    });
  }
}
