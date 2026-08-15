import { Pipe, PipeTransform } from '@angular/core';

/**
 * Formatea un importe en francos CFA con el formato del documento funcional:
 * `8900000` → `8.900.000 FCFA`
 *
 * Yoon u Auto trabaja exclusivamente en FCFA (XOF), por lo que el pipe no admite
 * otras divisas: no existe conversión ni símbolo alternativo.
 *
 * @example {{ vehicle.price | fcfa }}          → 8.900.000 FCFA
 * @example {{ vehicle.price | fcfa:false }}    → 8.900.000
 */
@Pipe({ name: 'fcfa', standalone: true })
export class FcfaPipe implements PipeTransform {
  transform(value: number | string | null | undefined, withSuffix = true): string {
    if (value === null || value === undefined || value === '') return '';

    const amount = typeof value === 'string' ? Number(value) : value;
    if (!Number.isFinite(amount)) return '';

    // Separador de millares "." según el formato usado en el documento.
    const formatted = Math.round(amount)
      .toString()
      .replace(/\B(?=(\d{3})+(?!\d))/g, '.');

    return withSuffix ? `${formatted} FCFA` : formatted;
  }
}

/** Constante de divisa de la plataforma. Código ISO 4217 del franco CFA (BCEAO). */
export const YOON_CURRENCY_CODE = 'XOF';
/** Etiqueta visible de la divisa. */
export const YOON_CURRENCY_LABEL = 'FCFA';
