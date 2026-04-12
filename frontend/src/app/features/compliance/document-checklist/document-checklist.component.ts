import {
  Component, ChangeDetectionStrategy, signal, input, computed
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { ProcessDocumentItem } from '@core/services/compliance.service';

@Component({
  selector: 'lll-document-checklist',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule],
  templateUrl: './document-checklist.component.html'
})
export class DocumentChecklistComponent {
  readonly documents = input<ProcessDocumentItem[]>([]);
  readonly processId = input<string>('');
  readonly completionPercent = input(0);

  readonly buyerDocs = computed(() =>
    this.documents().filter(d => d.responsibleParty === 'buyer'));
  readonly sellerDocs = computed(() =>
    this.documents().filter(d => d.responsibleParty === 'seller'));

  readonly statusLabel: Record<string, string> = {
    Pending:     'Pendiente',
    Submitted:   'Enviado',
    UnderReview: 'En revisión',
    Verified:    'Verificado ✓',
    Rejected:    'Rechazado',
    NotRequired: 'No requerido',
    Expired:     'Caducado',
  };

  readonly statusClass: Record<string, string> = {
    Pending:     'bg-amber-100 text-amber-800',
    Submitted:   'bg-blue-100 text-blue-800',
    UnderReview: 'bg-purple-100 text-purple-800',
    Verified:    'bg-green-100 text-green-800',
    Rejected:    'bg-red-100 text-red-800',
    NotRequired: 'bg-navy/10 text-navy/50',
    Expired:     'bg-red-50 text-red-600',
  };

  readonly docLabels: Record<string, string> = {
    FichaTecnica:     'Ficha técnica del vehículo',
    COC:              'Certificado de conformidad (COC)',
    Itv:              'Inspección técnica (ITV/TÜV)',
    TituloPropiedad:  'Título de propiedad',
    FacturaCompra:    'Factura de compra',
    DeclaracionAduana:'Declaración de aduana (DUA)',
    PagoAranceles:    'Justificante de pago de aranceles',
    SeguroImportacion:'Seguro de importación',
    Homologacion:     'Certificado de homologación',
    InspeccionTecnica:'Inspección técnica destino',
    Matriculacion:    'Documentación de matriculación',
  };
}
