/** @type {import('tailwindcss').Config} */
module.exports = {
  content: [
    './src/**/*.{html,ts}'
  ],
  theme: {
    extend: {
      colors: {
        navy:        '#0B1F3A',
        'navy-light':'#1E3A5F',
        'navy-dark': '#060F1E',
        gold:        '#C9A84C',
        'gold-light':'#E8D5A3',
        'gold-dark': '#9B7A2A',
        ivory:       '#F8F6F0',
        'ivory-dark':'#EDE9DF',
        success:     '#22C55E',
        warning:     '#F59E0B',
        error:       '#EF4444',
        info:        '#3B82F6',
      },
      fontFamily: {
        heading: ['"Playfair Display"', 'Georgia', 'serif'],
        body:    ['Inter', 'system-ui', 'sans-serif'],
        mono:    ['"JetBrains Mono"', '"Fira Code"', 'monospace'],
      },
      boxShadow: {
        gold:        '0 4px 20px rgba(201, 168, 76, 0.3)',
        navy:        '0 4px 20px rgba(11, 31, 58, 0.4)',
        card:        '0 2px 12px rgba(11, 31, 58, 0.08)',
        'card-hover':'0 8px 32px rgba(11, 31, 58, 0.16)',
      },
      borderRadius: {
        card: '0.75rem',
        btn:  '0.5rem',
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
