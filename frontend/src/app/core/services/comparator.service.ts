import { Injectable, signal, computed, effect, inject } from '@angular/core';
import { PlatformService } from './platform.service';

const STORAGE_KEY = 'lll_comparator';

/**
 * Respaldo mientras la configuración no ha llegado del servidor.
 *
 * El límite real vive en `platform_settings` y lo edita el administrador: aquí solo se
 * necesita un número con el que arrancar en el primer render.
 */
export const DEFAULT_MAX_COMPARED_VEHICLES = 3;

/**
 * Tope de seguridad al leer de localStorage, muy por encima de cualquier límite
 * razonable: solo evita que un valor manipulado a mano crezca sin fin.
 */
const HARD_CAP = 20;

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

  constructor() {
    // La configuración llega después del primer render. Cuando lo hace, se recorta a lo
    // que de verdad cabe: así una selección heredada de un límite mayor no se queda
    // por encima del actual.
    effect(() => {
      const limite = this.max();
      const actuales = this._ids();
      if (actuales.length > limite) this.persist(actuales.slice(0, limite));
    });
  }

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
      // ❌ Ya no se recorta con el respaldo. Lo hacía porque al arrancar todavía no ha
      // llegado la configuración del servidor, pero con el límite en 4 el usuario perdía
      // un vehículo en cada recarga sin que nada se lo dijera. Se guarda lo que hay,
      // acotado por seguridad, y se ajusta al límite real cuando la configuración llega.
      return Array.isArray(parsed)
        ? parsed
            .filter((v): v is string => typeof v === 'string')
            .slice(0, HARD_CAP)
        : [];
    } catch {
      localStorage.removeItem(STORAGE_KEY);
      return [];
    }
  }
}
