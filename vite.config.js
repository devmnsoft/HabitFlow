import { defineConfig } from "vite";

export default defineConfig({
  root: ".",
  publicDir: false,
  server: { port: 5177, strictPort: true },
  preview: { port: 5177, strictPort: true },
  build: {
    outDir: "dist",
    emptyOutDir: true,
    sourcemap: false,
    minify: "terser",
    cssMinify: true,
    rollupOptions: { input: "index.html" },
    terserOptions: {
      compress: { drop_console: true, drop_debugger: true, passes: 2 },
      mangle: true,
      format: { comments: false }
    }
  }
});
