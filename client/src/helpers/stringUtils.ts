export const encodeForWaf = (text: string): string => {
  const bytes = new TextEncoder().encode(text);
  let binary = '';
  bytes.forEach((b) => (binary += String.fromCharCode(b)));
  return `b64:${btoa(binary)}`;
};
export const encodeNullableForWaf = (text: string | null | undefined): string | null =>
  text ? encodeForWaf(text) : null;

export const decodeFromWaf = (text: string | null | undefined): string | null => {
  if (!text) return text ?? null;
  if (!text.startsWith('b64:')) return text;
  try {
    return decodeURIComponent(escape(atob(text.slice(4))));
  } catch {
    return text;
  }
};

export function capitaliseFirst(str: string): string {
  if (!str) return '';
  return str.charAt(0).toUpperCase() + str.slice(1);
}

export function booleanToYesNo(value: boolean): string {
  return value ? 'Yes' : 'No';
}

export function stringFromArray(arr: string[], separator = ' '): string {
  return arr.filter(Boolean).join(separator);
}
