/**
 * Converts a human-readable label into a stable, machine-safe "system-owned" key.
 *
 * Used for two things:
 *  1. Generating the value/code for a NEWLY added option as the user types its label.
 *  2. Normalising values at comparison time, so an existing stored value (e.g.
 *     "PIR and PMH") and a freshly generated slug ("pir_and_pmh") still match
 *     without ever rewriting the existing data.
 *
 * Lowercase, collapse any run of non-alphanumeric characters to a single
 * underscore, and trim leading/trailing underscores.
 */
export const slug = (input: string | null | undefined): string =>
  (input ?? '')
    .trim()
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, '_')
    .replace(/^_+|_+$/g, '');

/**
 * Like {@link slug}, but guarantees the result is unique against an existing set
 * of values by appending a numeric suffix (_2, _3, …) on collision. Used when
 * generating a new option value so two options never share the same key.
 */
export const uniqueSlug = (input: string | null | undefined, taken: Iterable<string>): string => {
  const base = slug(input);
  const takenSet = new Set(taken);
  if (!base || !takenSet.has(base)) {
    return base;
  }
  let suffix = 2;
  while (takenSet.has(`${base}_${suffix}`)) {
    suffix += 1;
  }
  return `${base}_${suffix}`;
};
