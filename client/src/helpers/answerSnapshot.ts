// Submitted answers are self-describing (BLUM-7178): alongside the value, the server stores the
// question label and the option labels as they were when the answer was saved, and sends them back
// as `questionLabel` and `displayValue`. Prefer those wherever a screen is showing a stored answer,
// so a later relabel in Admin Configuration cannot reach back and change what a historic submission
// says (BLUM-7614). The live field still wins while the user is editing, and for legacy rows that
// have no snapshot at all.

import { decodeFromWaf } from './stringUtils';

export interface SnapshotAnswerField {
  label: string;
  value: string | null;
  questionLabel?: string | null;
  displayValue?: string | null;
}

/**
 * True when the row being rendered still shows the answer the server has stored, rather than an
 * edit the user has made since. Values arrive from the form state as a string, or as an array for
 * multi-selects, which the API stores comma-separated.
 *
 * Both sides are run through `decodeFromWaf` because screens disagree about it: the Delivery Plan
 * activity form seeds its state already decoded while the rest seed the raw `field.value`. Decoding
 * is a no-op on anything without the `b64:` prefix, so this compares like with like either way —
 * without it, a `b64:` answer never matched and the snapshot silently fell back to the live label.
 */
export const isStoredAnswer = (field: Pick<SnapshotAnswerField, 'value'>, current: string | string[] | null | undefined): boolean => {
  const currentAsString = Array.isArray(current) ? current.join(',') : current ?? '';
  return decodeFromWaf(currentAsString) === decodeFromWaf(field.value ?? '');
};

/** The question label this answer was given under, falling back to the live label. */
export const resolveSnapshotLabel = (field: SnapshotAnswerField, current: string | string[] | null | undefined): string =>
  field.questionLabel && isStoredAnswer(field, current) ? field.questionLabel : field.label;
