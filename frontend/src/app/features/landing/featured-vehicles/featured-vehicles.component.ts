import { Component, OnInit, ChangeDetectionStrategy, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';
import { VehicleService, FeaturedVehicle } from '../../../core/services/vehicle.service';
import { VehicleCardComponent } from '../../../shared/components/vehicle-card/vehicle-card.component';

// Datos de ejemplo para cuando la API no devuelve vehículos destacados
const now = Date.now();
const ago = (days: number) => new Date(now - days * 86_400_000).toISOString();

const MOCK_VEHICLES: FeaturedVehicle[] = [
  {
    id: '1', slug: 'bmw-320d-2022', title: 'BMW Serie 3 320d Sport Line',
    makeName: 'BMW', modelName: 'Serie 3', year: 2022, mileage: 28400,
    price: 34900, currency: 'EUR', countryOrigin: 'DE', countryFlagEmoji: '🇩🇪',
    condition: 'Used', fuelType: 'Diesel', transmission: 'Automatic',
    primaryImageUrl: 'https://images.unsplash.com/photo-1555215695-3004980ad54e?w=800&h=500&fit=crop&q=80',
    thumbnailUrl:    'https://images.unsplash.com/photo-1555215695-3004980ad54e?w=400&h=250&fit=crop&q=80',
    favoritesCount: 12, viewsCount: 340, createdAt: ago(3)
  },
  {
    id: '2', slug: 'mercedes-c300-2023', title: 'Mercedes-Benz C 300 AMG Line',
    makeName: 'Mercedes-Benz', modelName: 'Clase C', year: 2023, mileage: 7800,
    price: 52500, currency: 'EUR', countryOrigin: 'DE', countryFlagEmoji: '🇩🇪',
    condition: 'Used', fuelType: 'Gasoline', transmission: 'Automatic',
    primaryImageUrl: 'https://images.unsplash.com/photo-1618843479313-40f8afb4b4d8?w=800&h=500&fit=crop&q=80',
    thumbnailUrl:    'https://images.unsplash.com/photo-1618843479313-40f8afb4b4d8?w=400&h=250&fit=crop&q=80',
    favoritesCount: 28, viewsCount: 720, createdAt: ago(1)
  },
  {
    id: '3', slug: 'tesla-model3-2023', title: 'Tesla Model 3 Long Range AWD',
    makeName: 'Tesla', modelName: 'Model 3', year: 2023, mileage: 14500,
    price: 42000, currency: 'EUR', countryOrigin: 'US', countryFlagEmoji: '🇺🇸',
    condition: 'Used', fuelType: 'Electric', transmission: 'Automatic',
    primaryImageUrl: 'https://images.unsplash.com/photo-1560958089-b8a1929cea89?w=800&h=500&fit=crop&q=80',
    thumbnailUrl:    'https://images.unsplash.com/photo-1560958089-b8a1929cea89?w=400&h=250&fit=crop&q=80',
    favoritesCount: 45, viewsCount: 1200, createdAt: ago(0)
  },
  {
    id: '4', slug: 'audi-q5-2022', title: 'Audi Q5 40 TDI quattro S tronic',
    makeName: 'Audi', modelName: 'Q5', year: 2022, mileage: 34200,
    price: 43800, currency: 'EUR', countryOrigin: 'DE', countryFlagEmoji: '🇩🇪',
    condition: 'Used', fuelType: 'Diesel', transmission: 'Automatic',
    primaryImageUrl: 'https://images.unsplash.com/photo-1606664515524-ed2f786a0bd6?w=800&h=500&fit=crop&q=80',
    thumbnailUrl:    'https://images.unsplash.com/photo-1606664515524-ed2f786a0bd6?w=400&h=250&fit=crop&q=80',
    favoritesCount: 19, viewsCount: 480, createdAt: ago(5)
  },
  {
    id: '5', slug: 'porsche-cayenne-2021', title: 'Porsche Cayenne E-Hybrid Coupé',
    makeName: 'Porsche', modelName: 'Cayenne', year: 2021, mileage: 41800,
    price: 78000, currency: 'EUR', countryOrigin: 'FR', countryFlagEmoji: '🇫🇷',
    condition: 'Used', fuelType: 'PluginHybrid', transmission: 'Automatic',
    primaryImageUrl: 'https://images.unsplash.com/photo-1503376780353-7e6692767b70?w=800&h=500&fit=crop&q=80',
    thumbnailUrl:    'https://images.unsplash.com/photo-1503376780353-7e6692767b70?w=400&h=250&fit=crop&q=80',
    favoritesCount: 31, viewsCount: 890, createdAt: ago(7)
  },
  {
    id: '6', slug: 'toyota-land-cruiser-2022', title: 'Toyota Land Cruiser 300 GX-R V6',
    makeName: 'Toyota', modelName: 'Land Cruiser', year: 2022, mileage: 21500,
    price: 89000, currency: 'EUR', countryOrigin: 'JP', countryFlagEmoji: '🇯🇵',
    condition: 'Used', fuelType: 'Diesel', transmission: 'Automatic',
    primaryImageUrl: 'https://images.unsplash.com/photo-1543465077-db45d34b88a5?w=800&h=500&fit=crop&q=80',
    thumbnailUrl:    'https://images.unsplash.com/photo-1543465077-db45d34b88a5?w=400&h=250&fit=crop&q=80',
    favoritesCount: 67, viewsCount: 2100, createdAt: ago(2)
  }
];

@Component({
  selector: 'lll-featured-vehicles',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, CommonModule, VehicleCardComponent],
  templateUrl: './featured-vehicles.component.html'
})
export class FeaturedVehiclesComponent implements OnInit {
  private readonly vehicleService = inject(VehicleService);

  // Cargados inmediatamente con ejemplos — la API los reemplazará si responde
  readonly vehicles = signal<FeaturedVehicle[]>(MOCK_VEHICLES);

  ngOnInit(): void {
    this.vehicleService.getFeaturedVehicles(6).subscribe({
      next: list => { if (list.length > 0) this.vehicles.set(list); },
      error: () => { /* conservamos los ejemplos */ }
    });
  }
}
