// Numeric comparison operators for conditional display rules. Kept in one place so every
// client-side evaluator (service user form, read view, bulk-upload validation, delivery plan)
// agrees with each other and with the backend ConditionalRuleEvaluator.
export interface NumericOperators {
  gt?: string;
  gte?: string;
  lt?: string;
  lte?: string;
  eq?: string;
  neq?: string;
  greaterThan?: number; // legacy key, kept for backward compatibility with older rules
}

/**
 * Evaluates whichever numeric operator (gt/gte/lt/lte/eq/neq) is present on the condition
 * against the supplied parent value. Returns null when the condition carries no numeric
 * operator, so callers can fall through to option-based (equals/in) checks. Both sides must
 * parse as numbers for the comparison to hold.
 */
export const evaluateNumericOperators = (
  parentValue: string | string[] | null | undefined,
  cond: NumericOperators
): boolean | null => {
  const a = Array.isArray(parentValue) ? NaN : parseFloat(parentValue ?? '');
  const cmp = (raw: string | undefined, fn: (x: number, y: number) => boolean): boolean => {
    if (raw === undefined) return false;
    const b = parseFloat(raw);
    return !Number.isNaN(a) && !Number.isNaN(b) && fn(a, b);
  };

  if (cond.gt !== undefined) return cmp(cond.gt, (x, y) => x > y);
  if (cond.gte !== undefined) return cmp(cond.gte, (x, y) => x >= y);
  if (cond.lt !== undefined) return cmp(cond.lt, (x, y) => x < y);
  if (cond.lte !== undefined) return cmp(cond.lte, (x, y) => x <= y);
  if (cond.eq !== undefined) return cmp(cond.eq, (x, y) => x === y);
  if (cond.neq !== undefined) return cmp(cond.neq, (x, y) => x !== y);
  if (cond.greaterThan !== undefined) return !Number.isNaN(a) && a > cond.greaterThan;
  return null;
};
