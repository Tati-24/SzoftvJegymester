import { defineConfig } from 'vite'
import { svelte } from '@sveltejs/vite-plugin-svelte'

export default defineConfig({
  plugins: [svelte()],
  server: {
    host: true,
    strictPort: false,
    allowedHosts: ['localhost', '127.0.0.1', 'jegymester.local'],
  },
})
