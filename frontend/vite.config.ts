import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import path from 'node:path'
import fs from 'node:fs'
import { fileURLToPath } from 'node:url'

const __dirname = path.dirname(fileURLToPath(import.meta.url))

function fixHashPathPlugin() {
  return {
    name: 'fix-hash-path',
    enforce: 'pre' as const,
    resolveId(source: string, importer?: string) {
      if (importer && source.startsWith('.')) {
        const cleanImporter = importer.split('?')[0];
        const dir = path.dirname(cleanImporter);
        const candidate = path.resolve(dir, source);
        for (const ext of ['', '.tsx', '.ts', '.jsx', '.js', '/index.tsx', '/index.ts']) {
          const file = candidate + ext;
          if (fs.existsSync(file) && fs.statSync(file).isFile()) {
            return file;
          }
        }
      }
      return null;
    }
  }
}

export default defineConfig({
  plugins: [fixHashPathPlugin(), react()],
  server: {
    proxy: {
      '/api': {
        target: 'http://localhost:5000',
        changeOrigin: true,
      },
    },
  },
  resolve: {
    alias: {
      "@": path.resolve(__dirname, "./src"),
    },
  },
})
