import { Injectable, signal, computed, inject } from '@angular/core';
import { PlatformService } from './platform.service';

const STORAGE_KEY = 'lll_comparator';

/**
 * Respaldo mientras la configuración no ha llegado del servidor.
 *
 * El límite real vive en `platform_settings` y lo edita el administrador: aquí solo se
 * necesita un número con el que arrancar en el primer render.
 */
export const DEFAULT_MAX_COMPARED_VEHICLES = 3;

export type AddToComparatorResult = 'added' | 'removed' | 'full';

/**
 * Selección del comparador.
 *
 * Guarda **únicamente los identificadores** de los anuncios, nunca sus datos: el precio,
 * el estado o el equipamiento pueden cambiar, y el comparador debe mostrar siempre la
 * información actual. Persiste entre sesiones hasta que el usuario la vacíe.
 */
@Injectable({ providedIn: 'root' })
export class ComparatorService {
  private readonly platform = inject(PlatformService);

  private readonly _ids = signal<string[]>(this.load());

  readonly ids = this._ids.asReadonly();
  readonly count = computed(() => this._ids().length);

  /** Cuántos caben, según la configuración de la plataforma. */
  readonly max = computed(() => this.platform.settings().comparatorMaxVehicles);

  readonly isFull = computed(() => this._ids().length >= this.max());

  /** "Comparer (2/3)" */
  readonly label = computed(() => `Comparer (${this.count()}/${this.max()})`);

  has(vehicleId: string): boolean {
    return this._ids().includes(vehicleId);
  }

  /**
   * Alterna la presencia de un vehículo. Devuelve `'full'` sin modificar nada cuando ya
   * está lleno: para añadir otro hay que retirar uno antes.
   */
  toggle(vehicleId: string): AddToComparatorResult {
    if (this.has(vehicleId)) {
      this.persist(this._ids().filter(id => id !== vehicleId));
      return 'removed';
    }

    if (this.isFull()) return 'full';

    this.persist([...this._ids(), vehicleId]);
    return 'added';
  }

  remove(vehicleId: string): void {
    this.persist(this._ids().filter(id => id !== vehicleId));
  }

  clear(): void {
    this.persist([]);
  }

  private persist(ids: string[]): void {
    this._ids.set(ids);
    if (typeof localStorage === 'undefined') return;
    localStorage.setItem(STORAGE_KEY, JSON.stringify(ids));
  }

  private load(): string[] {
    if (typeof localStorage === 'undefined') return [];
    const raw = localStorage.getItem(STORAGE_KEY);
    if (!raw) return [];
    try {
      const parsed = JSON.parse(raw);
      return Array.isArray(parsed)
        // Se recorta con el respaldo y no con el límite configurado: al cargar desde
        // localStorage la configuración todavía no ha llegado del servidor.
        ? parsed
            .filter((v): v is string => typeof v === 'string')
            .slice(0, DEFAULT_MAX_COMPARED_VEHICLES)
        : [];
    } catch {
      localStorage.removeItem(STORAGE_KEY);
      return [];
    }
  }
}
