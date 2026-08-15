import { Component, ChangeDetectionStrategy, OnInit, signal, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { NegotiationService, ContractVerification } from '@core/services/negotiation.service';
import { FcfaPipe } from '@shared/pipes/fcfa.pipe';

/**
 * Página pública del QR de un contrato.
 *
 * No exige cuenta: quien la abre suele tener el contrato en papel delante y solo quiere
 * saber si la venta es real. Muestra únicamente lo que ya figura en ese papel.
 */
@Component({
  selector: 'lll-contract-verification',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule, FormsModule, RouterLink, FcfaPipe],
  templateUrl: './contract-verification.component.html'
})
export class ContractVerificationComponent implements OnInit {
  private readonly service = inject(NegotiationService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  readonly result = signal<ContractVerification | null>(null);
  readonly loading = signal(false);
  readonly notFound = signal(false);

  code = '';

  ngOnInit(): void {
    const code = this.route.snapshot.paramMap.get('code');
    if (code) {
      this.code = code;
      this.verify();
    }
  }

  verify(): void {
    const code = this.code.trim();
    if (!code || this.loading()) return;

    this.loading.set(true);
    this.notFound.set(false);

    this.service.verifyContract(code).subscribe({
      next: r => {
        this.result.set(r);
        this.loading.set(false);
      },
      error: () => {
        this.result.set(null);
        this.notFound.set(true);
        this.loading.set(false);
      }
    });
  }

  /** Consultar otro código deja la URL en su sitio para poder compartirla. */
  search(): void {
    const code = this.code.trim().toUpperCase();
    if (!code) return;
    void this.router.navigate(['/verification', code]);
    this.verify();
  }
}
