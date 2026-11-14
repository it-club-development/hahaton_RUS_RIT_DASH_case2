import { defineConfig } from 'vite';
export default defineConfig({
  server: { proxy: { '/api': 'http://localhost:5126' } },
  build: { target: 'chrome75', cssTarget: 'chrome75' },
});
