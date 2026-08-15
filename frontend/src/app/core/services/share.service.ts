import { Injectable } from '@angular/core';

export interface ShareTarget {
  /** Título del anuncio, para el asunto del correo y el texto del mensaje. */
  title: string;
  /** URL pública absoluta del anuncio. */
  url: string;
  /** Precio ya formateado, si se quiere incluir en el mensaje. */
  price?: string;
}

/**
 * Compartir un anuncio. Según la especificación, esta función **no requiere registro**.
 */
@Injectable({ providedIn: 'root' })
export class ShareService {
  /** URL absoluta de un anuncio a partir de su slug. */
  vehicleUrl(slug: string): string {
    if (typeof window === 'undefined') return `/vehiculos/${slug}`;
    return `${window.location.origin}/vehiculos/${slug}`;
  }

  private message(target: ShareTarget): string {
    const price = target.price ? ` — ${target.price}` : '';
    return `${target.title}${price}\n${target.url}`;
  }

  whatsapp(target: ShareTarget): void {
    const text = encodeURIComponent(this.message(target));
    this.open(`https://wa.me/?text=${text}`);
  }

  email(target: ShareTarget): void {
    const subject = encodeURIComponent(target.title);
    const body = encodeURIComponent(this.message(target));
    this.open(`mailto:?subject=${subject}&body=${body}`);
  }

  facebook(target: ShareTarget): void {
    const url = encodeURIComponent(target.url);
    this.open(`https://www.facebook.com/sharer/sharer.php?u=${url}`);
  }

  /** Copia el enlace al portapapeles. Devuelve false si el navegador no lo permite. */
  async copyLink(target: ShareTarget): Promise<boolean> {
    try {
      await navigator.clipboard.writeText(target.url);
      return true;
    } catch {
      return false;
    }
  }

  /** `true` si el dispositivo ofrece su propio menú de compartir. */
  get supportsNativeShare(): boolean {
    return typeof navigator !== 'undefined' && typeof navigator.share === 'function';
  }

  /** Menú nativo del dispositivo. Devuelve false si no está disponible o se cancela. */
  async native(target: ShareTarget): Promise<boolean> {
    if (!this.supportsNativeShare) return false;
    try {
      await navigator.share({ title: target.title, text: target.title, url: target.url });
      return true;
    } catch {
      // El usuario canceló el diálogo: no es un error que deba mostrarse.
      return false;
    }
  }

  private open(url: string): void {
    if (typeof window === 'undefined') return;
    window.open(url, '_blank', 'noopener,noreferrer');
  }
}
