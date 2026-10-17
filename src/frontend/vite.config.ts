import 'vitest/config';
import { tanstackRouter } from '@tanstack/router-plugin/vite';
import react from '@vitejs/plugin-react';
import { defineConfig } from 'vite';
import { VitePWA as pwa } from 'vite-plugin-pwa';

import { resolveBuildTargets } from './buildTargets.ts';

const BASE_PUBLIC_PATH = './';

export default defineConfig(() => ({
  plugins: [
    tanstackRouter({
      target: 'react',
      routesDirectory: 'src/app/routes',
      generatedRouteTree: 'src/app/routeTree.gen.ts',
      autoCodeSplitting: true,
    }),
    react(),
    pwa({
      strategies: 'injectManifest',
      srcDir: 'src/app',
      filename: 'serviceWorker.ts',
      manifest: {
        name: 'gymlun',
        short_name: 'gymlun',
        start_url: BASE_PUBLIC_PATH,
        scope: BASE_PUBLIC_PATH,
        icons: [
          {
            src: 'android-chrome-192x192.png',
            sizes: '192x192',
            type: 'image/png',
          },
          {
            src: 'android-chrome-512x512.png',
            sizes: '512x512',
            type: 'image/png',
          },
          {
            src: 'logo-maskable.svg',
            sizes: '192x192 512x512',
            purpose: 'maskable',
          },
        ],
        theme_color: '#1e3a8a',
        background_color: '#ffffff',
        display: 'standalone',
      },
      workbox: {
        navigateFallbackDenylist: [/^\/api\/v1\/auth/, /^\/signin-google/, /^\/authorize/],
      },
    }),
  ],

  base: BASE_PUBLIC_PATH,

  build: {
    target: resolveBuildTargets('production'),
  },

  resolve: {
    alias: {
      src: new URL('./src/', import.meta.url).pathname,
      tests: new URL('./tests/', import.meta.url).pathname,
      '@/': new URL('./src/', import.meta.url).pathname,
      '@tests/': new URL('./tests/', import.meta.url).pathname,
    },
  },

  test: {
    globals: true,
    environment: 'jsdom',
    setupFiles: './tests/setup.ts',
    css: true,
  },
}));
