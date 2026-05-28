import axios, { AxiosRequestConfig, InternalAxiosRequestConfig } from 'axios';

export const STAGING_THRESHOLD_BYTES = 100 * 1024;
export const BODY_STAGING_ID_HEADER = 'X-Body-Staging-Id';
export const STAGING_ENDPOINT_PATH = '/system/request-staging';

// Routes that always go through staging regardless of payload size,
// because their bodies contain embedded JSON that triggers WAF rules.
const FORCE_STAGE_URL_PATTERNS = [
  '/data-collection-form-questions',
  '/services/bulk-upload',
  '/locations/bulk-upload/update',
  'bulk-upload/staged',
];

const SKIP_FLAG = Symbol.for('blum.requestStaging.skip');

export const markRequestAsStagingInternal = <T extends AxiosRequestConfig>(config: T): T => {
  (config as unknown as Record<symbol, boolean>)[SKIP_FLAG] = true;
  return config;
};

const isMarkedInternal = (config: AxiosRequestConfig): boolean =>
  Boolean((config as unknown as Record<symbol, boolean>)[SKIP_FLAG]);

const stagingMethods = new Set(['post', 'put', 'patch']);

const isFormData = (data: unknown): boolean =>
  typeof FormData !== 'undefined' && data instanceof FormData;

const isBlob = (data: unknown): boolean =>
  typeof Blob !== 'undefined' && data instanceof Blob;

const isArrayBufferLike = (data: unknown): boolean =>
  data instanceof ArrayBuffer ||
  (typeof ArrayBuffer !== 'undefined' && ArrayBuffer.isView(data as ArrayBufferView));

const contentTypeIsMultipart = (config: AxiosRequestConfig): boolean => {
  const ct = (config.headers?.['Content-Type'] || config.headers?.['content-type']) as string | undefined;
  return typeof ct === 'string' && ct.toLowerCase().startsWith('multipart/');
};

const measureBytes = (json: string): number => {
  if (typeof Blob !== 'undefined') {
    return new Blob([json]).size;
  }

  if (typeof TextEncoder !== 'undefined') {
    return new TextEncoder().encode(json).length;
  }

  return json.length;
};

const stageBody = async (json: string): Promise<string> => {
  const form = new FormData();
  const blob = new Blob([json], { type: 'application/json' });
  form.append('body', blob, 'body.json');

  const response = await axios.post<{ stagingId: string; expiresAtUtc: string }>(
    STAGING_ENDPOINT_PATH,
    form,
    markRequestAsStagingInternal({
      headers: { 'Content-Type': 'multipart/form-data' },
    }),
  );

  return response.data.stagingId;
};

export const stagingRequestInterceptor = async (
  config: InternalAxiosRequestConfig,
): Promise<InternalAxiosRequestConfig> => {
  if (isMarkedInternal(config)) {
    return config;
  }

  const method = (config.method || 'get').toLowerCase();
  if (!stagingMethods.has(method)) {
    return config;
  }

  const data = config.data;
  if (data == null) {
    return config;
  }

  if (isFormData(data) || isBlob(data) || isArrayBufferLike(data) || contentTypeIsMultipart(config)) {
    return config;
  }

  const url = config.url || '';
  if (url.includes(STAGING_ENDPOINT_PATH)) {
    return config;
  }

  let serialized: string;
  if (typeof data === 'string') {
    serialized = data;
  } else {
    try {
      serialized = JSON.stringify(data);
    } catch {
      return config;
    }
  }

  const shouldForceStage = FORCE_STAGE_URL_PATTERNS.some((pattern) => url.includes(pattern));

  if (!shouldForceStage && measureBytes(serialized) < STAGING_THRESHOLD_BYTES) {
    return config;
  }

  const stagingId = await stageBody(serialized);
  config.data = { _stagingId: stagingId };
  if (config.headers) {
    config.headers[BODY_STAGING_ID_HEADER] = stagingId;
    config.headers['Content-Type'] = 'application/json';
  }

  return config;
};