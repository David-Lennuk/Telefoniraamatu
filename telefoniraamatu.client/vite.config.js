import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

export default defineConfig({
  plugins: [react()],
  server: {
    port: 54554,
    https: false,
    proxy: {
      '/api': {
        target: 'http://localhost:5089',
        changeOrigin: true,
        secure: false,
      },
    },
  },
})
