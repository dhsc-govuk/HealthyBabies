/// <reference types="pdfkit" />

// The PDFKit UMD build is served as a plain asset and loaded with a script tag, so it arrives as
// a global rather than a module. See the vendor-pdfkit plugin in vite.config.mts.
interface Window {
  PDFDocument?: PDFKit.PDFDocument;
}
