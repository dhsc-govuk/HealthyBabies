import { defineConfig, Plugin, transformWithEsbuild } from 'vite';
import react from '@vitejs/plugin-react-swc';
import eslint from 'vite-plugin-eslint';
import { readFileSync } from 'fs';
import { createRequire } from 'module';
import { dirname, join } from 'path';

const packageJson = JSON.parse(readFileSync('./package.json', 'utf-8'));

// PDFKit ships a self-contained UMD build, and the validation report download is the only thing
// that needs it. Bundling it produced a chunk that statically imported the content-hashed entry
// chunk, so its URL moved on every build and a page opened before a deploy asked for files that
// deploy had already removed — the cause of BLUM-7605. Emitting the UMD verbatim keeps it out of
// the chunk graph entirely: it imports nothing, and the version in the name means the URL only
// changes when the dependency does. Resolve it rather than hardcoding a path, so a hoisted
// install or a build run from another directory still finds it.
const requirePackage = createRequire(import.meta.url);
const pdfkitPackagePath = requirePackage.resolve('pdfkit/package.json');
const pdfkitVersion = JSON.parse(readFileSync(pdfkitPackagePath, 'utf-8')).version;
const pdfkitStandalonePath = join(dirname(pdfkitPackagePath), 'js', 'pdfkit.standalone.js');
const pdfkitAssetPath = `assets/pdfkit-${pdfkitVersion}.js`;

const vendorPdfkit = (): Plugin => ({
  name: 'vendor-pdfkit',
  async generateBundle() {
    // The UMD ships unminified, and it used to be minified as part of the bundle. Keep doing so
    // rather than serving roughly 150 kB more over the wire.
    const standalone = readFileSync(pdfkitStandalonePath, 'utf-8');
    const minified = await transformWithEsbuild(standalone, pdfkitStandalonePath, { minify: true, loader: 'js' });

    this.emitFile({
      type: 'asset',
      fileName: pdfkitAssetPath,
      source: minified.code,
    });
  },
  // The build emits the file, so serve the same URL in dev from the installed package.
  configureServer(server) {
    server.middlewares.use((req, res, next) => {
      if (!req.url?.split('?')[0].endsWith(`/${pdfkitAssetPath}`)) {
        next();
        return;
      }

      res.setHeader('Content-Type', 'application/javascript');
      res.end(readFileSync(pdfkitStandalonePath));
    });
  },
});

// https://vitejs.dev/config/
export default defineConfig({
  base: process.env.VITE_BASE_PATH || '/',
  plugins: [react(), eslint(), vendorPdfkit()],
  server: {
    port: 3000,
  },
  define: {
    'import.meta.env.PACKAGE_VERSION': JSON.stringify(packageJson.version),
    'import.meta.env.PDFKIT_ASSET_PATH': JSON.stringify(pdfkitAssetPath),
  },
});
