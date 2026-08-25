export interface ValidationReportError {
  rowNumber: number;
  columnCode: string;
  columnLabel: string;
  currentValue: string;
  message: string;
}

export interface ValidationReportData {
  uploadName: string;
  fileName: string;
  errors: ValidationReportError[];
  generatedAt?: Date;
}

export interface ValidationReportFonts {
  regular: ArrayBuffer;
  bold: ArrayBuffer;
}

const normaliseText = (value: string): string =>
  Array.from(value)
    .filter((character) => {
      const characterCode = character.charCodeAt(0);
      return characterCode === 9 || characterCode === 10 || characterCode === 13 || (characterCode >= 32 && characterCode !== 127);
    })
    .join('')
    .trim();

// PDFKit recommends a trailing space inside every marked text sequence so words do not
// run together when assistive technology reads across structure boundaries.
const taggedText = (value: string): string => `${normaliseText(value)} `;

const displayValue = (value: string): string => {
  const normalised = normaliseText(value);
  return normalised.length > 0 ? normalised : 'Blank';
};

const formatGeneratedAt = (date: Date): string =>
  new Intl.DateTimeFormat('en-GB', {
    dateStyle: 'long',
    timeStyle: 'short',
  }).format(date);

const getDownloadFileName = (sourceFileName: string): string => {
  const baseName = sourceFileName.replace(/\.[^.]+$/, '').trim() || 'csv-upload';
  const safeBaseName = baseName
    .replace(/[^a-zA-Z0-9_-]+/g, '-')
    .replace(/^-+|-+$/g, '')
    .slice(0, 80);

  return `${safeBaseName || 'csv-upload'}-validation-report.pdf`;
};

const loadValidationReportFonts = async (): Promise<ValidationReportFonts> => {
  const baseUrl = import.meta.env.BASE_URL;
  const [regularResponse, boldResponse] = await Promise.all([fetch(`${baseUrl}fonts/light-f591b13f7d-v2.woff`), fetch(`${baseUrl}fonts/bold-affa96571d-v2.woff`)]);

  if (!regularResponse.ok || !boldResponse.ok) {
    throw new Error('Could not load the fonts used to create the validation report.');
  }

  const [regular, bold] = await Promise.all([regularResponse.arrayBuffer(), boldResponse.arrayBuffer()]);
  return { regular, bold };
};

/**
 * Raised when the PDFKit script cannot be fetched, as opposed to the report failing to build. It
 * is only requested once the user clicks download, so a page left open across a deploy is the
 * usual cause. Reloading picks up the current build, which is why this is worth telling apart
 * from a generic failure.
 */
export class ValidationReportUnavailableError extends Error {
  constructor(options?: ErrorOptions) {
    super('The validation report tools could not be loaded.', options);
    this.name = 'ValidationReportUnavailableError';
  }
}

const pdfkitScriptUrl = `${import.meta.env.BASE_URL}${import.meta.env.PDFKIT_ASSET_PATH}`;

let pdfkitScriptRequest: Promise<void> | undefined;

const loadPdfkitScript = (): Promise<void> => {
  if (window.PDFDocument) {
    return Promise.resolve();
  }

  if (!pdfkitScriptRequest) {
    pdfkitScriptRequest = new Promise<void>((resolve, reject) => {
      const script = document.createElement('script');
      script.src = pdfkitScriptUrl;
      script.async = true;
      script.onload = () => resolve();
      script.onerror = () => {
        // Let a later attempt retry rather than caching the rejection for the page's lifetime.
        script.remove();
        pdfkitScriptRequest = undefined;
        reject(new ValidationReportUnavailableError());
      };

      document.head.appendChild(script);
    });
  }

  return pdfkitScriptRequest;
};

const loadPdfDocument = async () => {
  await loadPdfkitScript();

  // Only a failure to fetch the script means the tools are unavailable. Anything else is a fault
  // in PDFKit itself, which reloading would not fix, so it must not be reported as a stale page.
  if (!window.PDFDocument) {
    throw new Error('PDFKit loaded but did not register a PDFDocument constructor.');
  }

  return window.PDFDocument;
};

/**
 * Generates a tagged, text-based PDF in the same reading order as the visual content.
 * Supplying fonts is useful for non-browser verification; the download path loads the
 * service's locally hosted fonts so they are embedded in the PDF.
 */
export const createValidationReportPdf = async (data: ValidationReportData, suppliedFonts?: ValidationReportFonts): Promise<Blob> => {
  const [PDFDocument, fonts] = await Promise.all([loadPdfDocument(), suppliedFonts ? Promise.resolve(suppliedFonts) : loadValidationReportFonts()]);

  // PDFKit supports PDF/UA at runtime, but the DefinitelyTyped subset union has not yet
  // added it. Keep the narrow cast at the library boundary.
  const options = {
    size: 'A4',
    margins: { top: 54, right: 54, bottom: 54, left: 54 },
    pdfVersion: '1.7' as const,
    subset: 'PDF/UA',
    tagged: true,
    displayTitle: true,
    lang: 'en-GB',
    bufferPages: true,
    info: {
      Title: `${data.uploadName} CSV validation report`,
      Author: 'Department for Education',
      Subject: `Validation errors found in ${data.fileName}`,
      CreationDate: data.generatedAt ?? new Date(),
    },
  } as unknown as PDFKit.PDFDocumentOptions;

  const doc = new PDFDocument(options);
  const chunks: Uint8Array[] = [];
  const completed = new Promise<Blob>((resolve, reject) => {
    doc.on('data', (chunk: Uint8Array) => chunks.push(chunk));
    doc.on('end', () =>
      resolve(
        new Blob(
          chunks.map((chunk) => Uint8Array.from(chunk).buffer),
          { type: 'application/pdf' }
        )
      )
    );
    doc.on('error', reject);
  });

  doc.registerFont('ReportRegular', fonts.regular);
  doc.registerFont('ReportBold', fonts.bold);

  const contentWidth = doc.page.width - doc.page.margins.left - doc.page.margins.right;
  const addText = (
    parent: PDFKit.PDFStructureElement,
    type: 'H1' | 'H2' | 'H3' | 'P',
    text: string,
    style: { font: 'ReportRegular' | 'ReportBold'; fontSize: number; color?: string; paragraphGap?: number }
  ) => {
    parent.add(
      doc.struct(type, {}, () => {
        doc
          .font(style.font)
          .fontSize(style.fontSize)
          .fillColor(style.color ?? '#0b0c0c')
          .text(taggedText(text), { width: contentWidth, paragraphGap: style.paragraphGap ?? 0 });
      })
    );
  };

  const root = doc.struct('Document', { title: `${data.uploadName} CSV validation report` });
  doc.addStructure(root);

  const overview = doc.struct('Sect', { title: 'Report overview' });
  root.add(overview);
  addText(overview, 'H1', 'CSV validation report', { font: 'ReportBold', fontSize: 24, color: '#1d70b8', paragraphGap: 12 });
  addText(overview, 'P', `Upload type: ${data.uploadName}`, { font: 'ReportRegular', fontSize: 11, paragraphGap: 4 });
  addText(overview, 'P', `CSV file: ${data.fileName}`, { font: 'ReportRegular', fontSize: 11, paragraphGap: 4 });
  addText(overview, 'P', `Generated: ${formatGeneratedAt(data.generatedAt ?? new Date())}`, { font: 'ReportRegular', fontSize: 11, paragraphGap: 12 });

  const affectedRows = new Set(data.errors.map((error) => error.rowNumber)).size;
  addText(overview, 'H2', 'Summary', { font: 'ReportBold', fontSize: 17, paragraphGap: 6 });
  addText(overview, 'P', `${data.errors.length} validation ${data.errors.length === 1 ? 'error' : 'errors'} across ${affectedRows} CSV ${affectedRows === 1 ? 'row' : 'rows'}.`, {
    font: 'ReportRegular',
    fontSize: 11,
    paragraphGap: 6,
  });
  addText(overview, 'P', 'Use the CSV row number and column code below to find each value in the original file. Correct every error, save the file as CSV, then upload it again.', {
    font: 'ReportRegular',
    fontSize: 11,
    paragraphGap: 18,
  });

  const errorsHeading = doc.struct('Sect', { title: 'Validation errors' });
  root.add(errorsHeading);
  addText(errorsHeading, 'H2', 'Validation errors', { font: 'ReportBold', fontSize: 19, paragraphGap: 10 });

  const sortedErrors = [...data.errors].sort(
    (left, right) => left.rowNumber - right.rowNumber || left.columnCode.localeCompare(right.columnCode) || left.message.localeCompare(right.message)
  );

  sortedErrors.forEach((error, index) => {
    // Keep the item heading with at least the first detail lines where possible.
    if (doc.y > doc.page.height - doc.page.margins.bottom - 125) {
      doc.addPage();
    }

    // Nested inside the errors section rather than alongside it, so the outline stays
    // "Validation errors > Error N". Screen reader users can then skip the whole block by
    // heading level instead of stepping through every error at the same level.
    const errorSection = doc.struct('Sect', { title: `Error ${index + 1} of ${sortedErrors.length}` });
    errorsHeading.add(errorSection);
    addText(errorSection, 'H3', `Error ${index + 1} of ${sortedErrors.length}`, { font: 'ReportBold', fontSize: 14, color: '#d4351c', paragraphGap: 5 });
    addText(errorSection, 'P', `CSV row: ${error.rowNumber}`, { font: 'ReportRegular', fontSize: 11, paragraphGap: 3 });
    addText(errorSection, 'P', `Column: ${error.columnCode} — ${error.columnLabel}`, { font: 'ReportRegular', fontSize: 11, paragraphGap: 3 });
    addText(errorSection, 'P', `Current value: ${displayValue(error.currentValue)}`, { font: 'ReportRegular', fontSize: 11, paragraphGap: 3 });
    addText(errorSection, 'P', `What needs changing: ${error.message}`, { font: 'ReportRegular', fontSize: 11, paragraphGap: 14 });
  });

  const pageRange = doc.bufferedPageRange();
  for (let pageIndex = pageRange.start; pageIndex < pageRange.start + pageRange.count; pageIndex += 1) {
    doc.switchToPage(pageIndex);
    const contentBottomMargin = doc.page.margins.bottom;
    // Footer artifacts sit inside the physical page margin. Temporarily relaxing the
    // automatic-flow boundary prevents PDFKit from creating a new page for the footer.
    doc.page.margins.bottom = 0;
    doc.markContent('Artifact', { type: 'Pagination', attached: ['Bottom'] });
    doc
      .font('ReportRegular')
      .fontSize(9)
      .fillColor('#505a5f')
      .text(`Page ${pageIndex - pageRange.start + 1} of ${pageRange.count}`, doc.page.margins.left, doc.page.height - 35, {
        width: contentWidth,
        align: 'center',
        lineBreak: false,
      });
    doc.endMarkedContent();
    doc.page.margins.bottom = contentBottomMargin;
  }

  doc.end();
  return completed;
};

export const downloadValidationReport = async (data: ValidationReportData): Promise<void> => {
  const blob = await createValidationReportPdf(data);
  const url = window.URL.createObjectURL(blob);
  const link = document.createElement('a');
  link.href = url;
  link.download = getDownloadFileName(data.fileName);
  document.body.appendChild(link);
  link.click();
  link.remove();
  window.URL.revokeObjectURL(url);
};
