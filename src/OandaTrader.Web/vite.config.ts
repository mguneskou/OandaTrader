import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    strictPort: true,
    proxy: {
      '/api': {
        target: 'http://localhost:5047',
        changeOrigin: true,
      },
      // ws: true so SignalR can negotiate up to a WebSocket transport rather than
      // silently falling back to long polling.
      '/hubs': {
        target: 'http://localhost:5047',
        changeOrigin: true,
        ws: true,
      },
    },
  },
})
