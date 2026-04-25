/** @type {import('tailwindcss').Config} */
export default {
  content: [
    "./index.html",
    "./src/**/*.{js,ts,jsx,tsx}",
  ],
  theme: {
    extend: {
      fontFamily: {
        'sans': ['DM Sans', 'ui-sans-serif', 'system-ui'],
      },
      colors: {
        primary: '#1C1917',
        secondary: '#44403C',
        accent: '#CA8A04',
        'bg-base': '#FAFAF9',
        'text-main': '#0C0A09',
        black: '#1C1917',
      }
    },
  },
  plugins: [],
}
