import {
  Component, ChangeDetectionStrategy, signal, inject,
  input, output, OnInit
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { VehicleFilters, VehicleMake, VehicleService } from '@core/services/vehicle.service';

@Component({
  selector: 'lll-filter-panel',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormsModule],
  templateUrl: './filter-panel.component.html'
})
export class FilterPanelComponent implements OnInit {
  private readonly vehicleService = inject(VehicleService);

  readonly filters = input<VehicleFilters>({});
  readonly filtersChange = output<Partial<VehicleFilters>>();

  readonly makes = signal<VehicleMake[]>([]);

  // Local editable state (not yet applied)
  search = '';
  makeId = '';
  countryOrigin = '';
  priceFrom = '';
  priceTo = '';
  yearFrom = '';
  yearTo = '';
  condition = '';
  fuelType = '';
  transmission = '';
  bodyType = '';
  isExportReady = false;

  readonly countries = [
    { code: 'ES', name: 'España' },
    { code: 'DE', name: 'Alemania' },
    { code: 'FR', name: 'Francia' },
    { code: 'IT', name: 'Italia' },
    { code: 'JP', name: 'Japón' },
    { code: 'US', name: 'Estados Unidos' },
    { code: 'GB', name: 'Reino Unido' },
    { code: 'MA', name: 'Marruecos' },
  ];

  readonly conditions = [
    { value: 'New', label: 'Nuevo' },
    { value: 'Used', label: 'Segunda mano' },
    { value: 'Km0', label: 'Km 0' },
  ];

  readonly fuelTypes = [
    { value: 'Gasoline', label: 'Gasolina' },
    { value: 'Diesel', label: 'Diésel' },
    { value: 'Electric', label: 'Eléctrico' },
    { value: 'Hybrid', label: 'Híbrido' },
    { value: 'PluginHybrid', label: 'Híbrido enchufable' },
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

  ngOnInit(): void {
    this.vehicleService.getMakes(false).subscribe(makes => this.makes.set(makes));
    const f = this.filters();
    this.search = f.search ?? '';
    this.makeId = f.makeId ?? '';
    this.countryOrigin = f.countryOrigin ?? '';
    this.priceFrom = f.priceFrom?.toString() ?? '';
    this.priceTo = f.priceTo?.toString() ?? '';
    this.yearFrom = f.yearFrom?.toString() ?? '';
    this.yearTo = f.yearTo?.toString() ?? '';
    this.condition = f.condition ?? '';
    this.fuelType = f.fuelType ?? '';
    this.transmission = f.transmission ?? '';
    this.bodyType = f.bodyType ?? '';
    this.isExportReady = f.isExportReady ?? false;
  }

  apply(): void {
    this.filtersChange.emit({
      search: this.search || undefined,
      makeId: this.makeId || undefined,
      countryOrigin: this.countryOrigin || undefined,
      priceFrom: this.priceFrom ? +this.priceFrom : undefined,
      priceTo: this.priceTo ? +this.priceTo : undefined,
      yearFrom: this.yearFrom ? +this.yearFrom : undefined,
      yearTo: this.yearTo ? +this.yearTo : undefined,
      condition: (this.condition as any) || undefined,
      fuelType: (this.fuelType as any) || undefined,
      transmission: (this.transmission as any) || undefined,
      bodyType: (this.bodyType as any) || undefined,
      isExportReady: this.isExportReady || undefined
    });
  }

  reset(): void {
    this.search = '';
    this.makeId = '';
    this.countryOrigin = '';
    this.priceFrom = '';
    this.priceTo = '';
    this.yearFrom = '';
    this.yearTo = '';
    this.condition = '';
    this.fuelType = '';
    this.transmission = '';
    this.bodyType = '';
    this.isExportReady = false;
    this.filtersChange.emit({});
  }

  currentYear = new Date().getFullYear();
  years = Array.from({ length: this.currentYear - 1990 + 1 }, (_, i) => this.currentYear - i);
}
