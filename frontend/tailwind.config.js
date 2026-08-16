/** @type {import('tailwindcss').Config} */
module.exports = {
  content: [
    './src/**/*.{html,ts}'
  ],
  theme: {
    extend: {
      colors: {
        /* ── Azul océano (base de marca, medido del logo Yoon U Auto) ──
           El azul más profundo del propio logo es ~#185484: la marca nunca
           llega al casi-negro anterior. Toda la gama se sube para parecerse al
           logo. Contraste verificado: blanco sobre navy = 7.9:1 (AAA). */
        navy:        '#14567F',
        'navy-light':'#1E7BA8',
        'navy-dark': '#0E3E5C',

        /* ── Celeste brillante (acento del logo, ~#24A8CC) ── */
        azure:       '#26AEE0',
        'azure-light':'#7FD3EC',
        'azure-dark':'#157FA8',

        /* ── Blancos y plata (parte "blanca" del logo) ── */
        frost:       '#F5FAFD',
        'frost-dark':'#E6EFF6',
        silver:      '#C7D5E0',
        steel:       '#5B7185',

        success:     '#16A34A',
        warning:     '#D97706',
        error:       '#DC2626',
        info:        '#26AEE0',
      },
      fontFamily: {
        heading: ['Montserrat', '"Segoe UI"', 'system-ui', 'sans-serif'],
        body:    ['Inter', 'system-ui', 'sans-serif'],
        mono:    ['"JetBrains Mono"', '"Fira Code"', 'monospace'],
      },
      boxShadow: {
        azure:       '0 6px 24px rgba(38, 174, 224, 0.35)',
        navy:        '0 8px 28px rgba(20, 86, 127, 0.30)',
        card:        '0 2px 12px rgba(20, 86, 127, 0.08)',
        'card-hover':'0 12px 32px rgba(20, 86, 127, 0.18)',
        glass:       '0 1px 0 rgba(255,255,255,0.06) inset, 0 8px 24px rgba(14, 62, 92, 0.26)',
      },
      backgroundImage: {
        'gradient-brand': 'linear-gradient(135deg, #0E3E5C 0%, #1E7BA8 55%, #26AEE0 100%)',
        'gradient-azure': 'linear-gradient(135deg, #26AEE0 0%, #157FA8 100%)',
        'gradient-frost': 'linear-gradient(180deg, #FFFFFF 0%, #F5FAFD 100%)',
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
