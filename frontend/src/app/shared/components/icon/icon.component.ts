import { ChangeDetectionStrategy, Component, Input } from '@angular/core';

/**
 * Iconos de línea de la marca. Un único trazo, heredan el color (azul) y sustituyen a los
 * emojis, que se veían distintos en cada dispositivo y rompían la paleta de solo azules y
 * blanco. Todos comparten viewBox 24×24 y grosor de trazo, para que el conjunto sea
 * homogéneo.
 */
@Component({
  selector: 'lll-icon',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <svg [attr.width]="size" [attr.height]="size" viewBox="0 0 24 24" fill="none"
         stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round"
         aria-hidden="true" xmlns="http://www.w3.org/2000/svg">
      @switch (name) {
        @case ('search') {
          <circle cx="11" cy="11" r="7"/><path d="M21 21l-4.3-4.3"/>
        }
        @case ('bell') {
          <path d="M18 8a6 6 0 1 0-12 0c0 7-3 9-3 9h18s-3-2-3-9"/><path d="M13.7 21a2 2 0 0 1-3.4 0"/>
        }
        @case ('chat') {
          <path d="M21 11.5a8.5 8.5 0 0 1-12.4 7.6L3 21l1.9-5.6A8.5 8.5 0 1 1 21 11.5z"/>
        }
        @case ('offer') {
          <path d="M20.6 13.4 12 22l-8-8V4h10z"/><circle cx="8.5" cy="8.5" r="1.3"/>
        }
        @case ('clipboard-check') {
          <rect x="6" y="4" width="12" height="17" rx="2"/><path d="M9 4V3h6v1"/><path d="M9 13l2 2 4-4"/>
        }
        @case ('contract') {
          <path d="M14 3H7a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h10a2 2 0 0 0 2-2V8z"/><path d="M14 3v5h5"/><path d="M9 15l2 2 4-4"/>
        }
        @case ('camera') {
          <path d="M4 7h3l1.5-2h7L17 7h3a1 1 0 0 1 1 1v10a1 1 0 0 1-1 1H4a1 1 0 0 1-1-1V8a1 1 0 0 1 1-1z"/><circle cx="12" cy="13" r="3.3"/>
        }
        @case ('chart') {
          <path d="M4 4v16h16"/><path d="M8 15l3-4 3 3 4-6"/>
        }
        @case ('star') {
          <path d="M12 3l2.6 5.3 5.9.9-4.3 4.1 1 5.8L12 16.9 6.8 19.2l1-5.8L3.5 9.2l5.9-.9z"/>
        }
        @case ('handshake') {
          <path d="M8 12l2-2 3 3 3-3 2 2"/><path d="M3 10l3-3 4 1"/><path d="M21 10l-3-3-4 1"/><path d="M10 16l2 2 2-2"/>
        }
        @case ('medal') {
          <circle cx="12" cy="14" r="6"/><path d="M12 11.5l1.2 2.4 2.6.4-1.9 1.8.4 2.6-2.3-1.2-2.3 1.2.4-2.6-1.9-1.8 2.6-.4z"/><path d="M9 3l3 5 3-5"/>
        }
        @case ('cart') {
          <circle cx="9" cy="20" r="1.4"/><circle cx="17" cy="20" r="1.4"/><path d="M3 4h2l2.2 11.2a1 1 0 0 0 1 .8h8.6a1 1 0 0 0 1-.8L20.5 8H6"/>
        }
        @case ('tag') {
          <path d="M20.6 13.4 12 22l-8-8V4h10z"/><circle cx="8.5" cy="8.5" r="1.3"/>
        }
        @case ('location') {
          <path d="M12 21s-7-5.8-7-11a7 7 0 1 1 14 0c0 5.2-7 11-7 11z"/><circle cx="12" cy="10" r="2.6"/>
        }
        @case ('gift') {
          <rect x="4" y="9" width="16" height="12" rx="1"/><path d="M2 9h20"/><path d="M12 9v12"/><path d="M12 9S10.5 4 8 5s.5 4 4 4c3.5 0 4.5-3 2-4s-4 4-4 4z"/>
        }
        @case ('document') {
          <path d="M14 3H7a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h10a2 2 0 0 0 2-2V8z"/><path d="M14 3v5h5"/><path d="M9 13h6M9 17h6"/>
        }
        @default {
          <circle cx="12" cy="12" r="9"/>
        }
      }
    </svg>
  `
})
export class IconComponent {
  @Input({ required: true }) name!: string;
  @Input() size = 24;
}
