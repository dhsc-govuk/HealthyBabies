export type FileUploadProfile = 'submissionAttachment' | 'bulkUploadCsv' | 'bulkUploadCsvOrExcel';

interface ProfileSettings {
  allowedExtensions: string[];
  maxSizeBytes: number;
  allowedMagicBytes: ReadonlyArray<ReadonlyArray<number>> | null;
}

const MEGABYTE = 1024 * 1024;

const PROFILES: Record<FileUploadProfile, ProfileSettings> = {
  submissionAttachment: {
    allowedExtensions: ['.pdf', '.csv', '.xlsx', '.docx', '.png', '.jpg', '.jpeg'],
    maxSizeBytes: 25 * MEGABYTE,
    allowedMagicBytes: [
      [0x25, 0x50, 0x44, 0x46], // %PDF
      [0x89, 0x50, 0x4e, 0x47], // PNG
      [0xff, 0xd8, 0xff], // JPEG
      [0x50, 0x4b, 0x03, 0x04], // ZIP / OOXML
      [0x50, 0x4b, 0x05, 0x06],
      [0x50, 0x4b, 0x07, 0x08],
    ],
  },
  bulkUploadCsv: {
    allowedExtensions: ['.csv'],
    maxSizeBytes: 5 * MEGABYTE,
    allowedMagicBytes: null,
  },
  bulkUploadCsvOrExcel: {
    allowedExtensions: ['.csv', '.xlsx'],
    maxSizeBytes: 5 * MEGABYTE,
    allowedMagicBytes: [
      [0x50, 0x4b, 0x03, 0x04],
      [0x50, 0x4b, 0x05, 0x06],
      [0x50, 0x4b, 0x07, 0x08],
    ],
  },
};

const ALWAYS_BLOCKED_EXTENSIONS = new Set([
  '.svg', '.html', '.htm', '.xml', '.xhtml',
  '.php', '.phtml', '.pht', '.php3', '.php4', '.php5', '.phps',
  '.asp', '.aspx', '.cer', '.asa',
  '.jsp', '.jspx',
  '.exe', '.dll', '.bat', '.cmd', '.com', '.msi', '.ps1', '.sh', '.vbs', '.vbe',
  '.js', '.jse', '.wsf', '.wsh',
  '.xlsm', '.docm', '.pptm', '.dotm', '.xltm',
  '.jar', '.class',
  '.lnk', '.scr', '.reg',
]);

const FILENAME_MAX_LENGTH = 200;

export type FileValidationResult = { ok: true } | { ok: false; message: string };

const lastExtension = (fileName: string): string => {
  const idx = fileName.lastIndexOf('.');
  return idx >= 0 ? fileName.slice(idx).toLowerCase() : '';
};

const allExtensions = (fileName: string): string[] => {
  const dots = fileName.toLowerCase().split('.').slice(1);
  return dots.map((d) => `.${d}`);
};

const isUnsafeFilename = (fileName: string): boolean => {
  if (!fileName || fileName.length > FILENAME_MAX_LENGTH) return true;
  if (fileName.includes('\0') || fileName.includes('/') || fileName.includes('\\') || fileName.includes('..')) return true;
  for (let i = 0; i < fileName.length; i += 1) {
    if (fileName.charCodeAt(i) < 0x20) return true;
  }
  return false;
};

const matchesAnyMagicByte = (
  bytes: Uint8Array,
  patterns: ReadonlyArray<ReadonlyArray<number>>
): boolean =>
  patterns.some((pattern) => {
    if (bytes.length < pattern.length) return false;
    for (let i = 0; i < pattern.length; i += 1) {
      if (bytes[i] !== pattern[i]) return false;
    }
    return true;
  });

export const validateUploadedFile = async (
  file: File,
  profile: FileUploadProfile
): Promise<FileValidationResult> => {
  const settings = PROFILES[profile];

  if (file.size === 0) {
    return { ok: false, message: 'File is empty.' };
  }

  if (file.size > settings.maxSizeBytes) {
    const limit = Math.round(settings.maxSizeBytes / MEGABYTE);
    return { ok: false, message: `File is too large. Maximum size is ${limit} MB.` };
  }

  if (isUnsafeFilename(file.name)) {
    return { ok: false, message: 'File name is not allowed.' };
  }

  const innerExtensions = allExtensions(file.name);
  const blocked = innerExtensions.find((ext) => ALWAYS_BLOCKED_EXTENSIONS.has(ext));
  if (blocked) {
    return { ok: false, message: `File type ${blocked} is not allowed for security reasons.` };
  }

  const extension = lastExtension(file.name);
  if (!extension) {
    return { ok: false, message: 'File must have an extension.' };
  }

  if (!settings.allowedExtensions.includes(extension)) {
    return {
      ok: false,
      message: `File type ${extension} is not allowed. Allowed types: ${settings.allowedExtensions.join(', ')}.`,
    };
  }

  if (settings.allowedMagicBytes) {
    const headerBuffer = await file.slice(0, 8).arrayBuffer();
    const header = new Uint8Array(headerBuffer);
    if (!matchesAnyMagicByte(header, settings.allowedMagicBytes)) {
      return { ok: false, message: 'File contents do not match the declared file type.' };
    }
  }

  return { ok: true };
};