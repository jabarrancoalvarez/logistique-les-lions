import { Injectable, signal } from '@angular/core';

export interface Toast {
  id: string;
  type: 'error' | 'success' | 'warning' | 'info';
  message: string;
  duration: number;
}

@Injectable({ providedIn: 'root' })
export class ErrorService {
  private readonly _toasts = signal<Toast[]>([]);
  readonly toasts = this._toasts.asReadonly();

  showError(message: string, duration = 5000): void {
    this.addToast({ type: 'error', message, duration });
  }

  showSuccess(message: string, duration = 3000): void {
    this.addToast({ type: 'success', message, duration });
  }

  showWarning(message: string, duration = 4000): void {
    this.addToast({ type: 'warning', message, duration });
  }

  private addToast(options: Omit<Toast, 'id'>): void {
    const id = crypto.randomUUID();
    this._toasts.update(toasts => [...toasts, { ...options, id }]);
    setTimeout(() => this.remove(id), options.duration);
  }

  remove(id: string): void {
    this._toasts.update(toasts => toasts.filter(t => t.id !== id));
  }
}
