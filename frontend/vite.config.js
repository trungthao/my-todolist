import vue from '@vitejs/plugin-vue'
import { defineConfig } from 'vite'

// https://vite.dev/config/
export default defineConfig({
  plugins: [vue()],
  server: {
    proxy: {
      '/api': {
        target: 'http://localhost:5108',
        changeOrigin: true
      },
      '/taskHub': {
        target: 'http://localhost:5108',
        changeOrigin: true,
        ws: true
      }
    }
  }
})
