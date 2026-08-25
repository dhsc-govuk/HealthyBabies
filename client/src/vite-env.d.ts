/// <reference types="vite/client" />
interface ImportMetaEnv {
  readonly PACKAGE_VERSION: string;
  readonly PDFKIT_ASSET_PATH: string;
}

interface ImportMeta {
  readonly env: ImportMetaEnv
}
