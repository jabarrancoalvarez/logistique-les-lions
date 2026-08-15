/** @type {import('tailwindcss').Config} */
module.exports = {
  content: [
    './src/**/*.{html,ts}'
  ],
  theme: {
    extend: {
      colors: {
        /* ── Azul profundo (base de marca, tomado del logo Yoon U Auto) ── */
        navy:        '#0A2E4D',
        'navy-light':'#1F588F',
        'navy-dark': '#061F36',

        /* ── Azul brillante (acento del logo) ── */
        azure:       '#22A7D2',
        'azure-light':'#7FD3EC',
        'azure-dark':'#157FA8',

        /* ── Blancos y plata (parte "blanca" del logo) ── */
        frost:       '#F4F8FB',
        'frost-dark':'#E3EDF5',
        silver:      '#C7D5E0',
        steel:       '#5B7185',

        success:     '#16A34A',
        warning:     '#D97706',
        error:       '#DC2626',
        info:        '#22A7D2',
      },
      fontFamily: {
        heading: ['Montserrat', '"Segoe UI"', 'system-ui', 'sans-serif'],
        body:    ['Inter', 'system-ui', 'sans-serif'],
        mono:    ['"JetBrains Mono"', '"Fira Code"', 'monospace'],
      },
      boxShadow: {
        azure:       '0 6px 24px rgba(34, 167, 210, 0.35)',
        navy:        '0 8px 28px rgba(10, 46, 77, 0.35)',
        card:        '0 2px 12px rgba(10, 46, 77, 0.07)',
        'card-hover':'0 12px 32px rgba(10, 46, 77, 0.16)',
        glass:       '0 1px 0 rgba(255,255,255,0.06) inset, 0 8px 24px rgba(6, 31, 54, 0.28)',
      },
      backgroundImage: {
        'gradient-brand': 'linear-gradient(135deg, #0A2E4D 0%, #1F588F 55%, #22A7D2 100%)',
        'gradient-azure': 'linear-gradient(135deg, #22A7D2 0%, #157FA8 100%)',
        'gradient-frost': 'linear-gradient(180deg, #FFFFFF 0%, #F4F8FB 100%)',
      },
      borderRadius: {
        card: '1rem',
        btn:  '0.625rem',
      },
      transitionDuration: {
        fast:   '150ms',
        normal: '250ms',
        slow:   '400ms',
      },
      spacing: {
        18:  '4.5rem',
        22:  '5.5rem',
        88:  '22rem',
        112: '28rem',
        128: '32rem',
      },
    },
  },
  plugins: [],
};
