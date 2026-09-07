import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
  },
  // Bundled into the WebApi's own wwwroot so the production container serves API + client from
  // a single origin (Program.cs's UseDefaultFiles/UseStaticFiles/MapFallbackToFile) - same
  // pattern later copied into the PokerLive project when it was cloned from this one.
  build: {
    outDir: '../src/Presentation/SicBoLive.WebApi/wwwroot',
    emptyOutDir: true,
  },
})
