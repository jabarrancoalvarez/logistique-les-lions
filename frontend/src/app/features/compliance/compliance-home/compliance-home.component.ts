import {
  Component, ChangeDetectionStrategy, signal, inject, OnInit
} from '@angular/core';
import { RouterLink, ActivatedRoute } from '@angular/router';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ComplianceService, CountryRequirement } from '@core/services/compliance.service';

@Component({
  selector: 'lll-compliance-home',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule, RouterLink, FormsModule],
  templateUrl: './compliance-home.component.html'
})
export class ComplianceHomeComponent implements OnInit {
  private readonly complianceService = inject(ComplianceService);
  private readonly route = inject(ActivatedRoute);

  readonly requirement = signal<CountryRequirement | null>(null);
  readonly isLoading = signal(false);
  readonly error = signal<string | null>(null);

  origin = 'ES';
  destination = 'DE';

  readonly countries = [
    { code: 'ES', name: 'España 🇪🇸' },
    { code: 'DE', name: 'Alemania 🇩🇪' },
    { code: 'FR', name: 'Francia 🇫🇷' },
    { code: 'IT', name: 'Italia 🇮🇹' },
    { code: 'JP', name: 'Japón 🇯🇵' },
    { code: 'US', name: 'EE.UU. 🇺🇸' },
    { code: 'GB', name: 'Reino Unido 🇬🇧' },
    { code: 'MA', name: 'Marruecos 🇲🇦' },
  ];

  readonly steps = [
    { icon: '🔍', title: 'Consulta los requisitos', desc: 'Elige origen y destino para ver la normativa aplicable.' },
    { icon: '📋', title: 'Obtén el checklist', desc: 'Lista completa de documentos necesarios para el proceso.' },
    { icon: '💰', title: 'Calcula los costes', desc: 'Aranceles, IVA, homologación y gestión — todo estimado.' },
    { icon: '✅', title: 'Gestiona el proceso', desc: 'Seguimiento en tiempo real de cada documento.' },
  ];

  ngOnInit(): void {
    const o = this.route.snapshot.queryParams['origin'];
    const d = this.route.snapshot.queryParams['destination'];
    if (o) this.origin = o;
    if (d) this.destination = d;
    if (o || d) this.search();
  }

  search(): void {
    if (!this.origin || !this.destination) return;
    this.isLoading.set(true);
    this.error.set(null);

    this.complianceService.getRequirements(this.origin, this.destination).subscribe({
      next: r => {
        this.requirement.set(r);
        this.isLoading.set(false);
      },
      error: () => {
        this.requirement.set(null);
        this.error.set(`No hay datos de normativa para ${this.origin} → ${this.destination} todavía.`);
        this.isLoading.set(false);
      }
    });
  }

  get docLabel(): (d: string) => string {
    const map: Record<string, string> = {
      FichaTecnica: 'Ficha técnica', COC: 'COC europeo', Itv: 'ITV / TÜV',
      TituloPropiedad: 'Título de propiedad', FacturaCompra: 'Factura de compra',
      DeclaracionAduana: 'DUA (Declaración de aduana)', PagoAranceles: 'Pago de aranceles',
      SeguroImportacion: 'Seguro de importación', Homologacion: 'Homologación',
      InspeccionTecnica: 'Inspección técnica destino', Matriculacion: 'Documentación matrícula',
    };
    return (d: string) => map[d] ?? d;
  }
}
