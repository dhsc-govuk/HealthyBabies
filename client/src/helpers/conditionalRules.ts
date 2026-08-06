// Shared evaluator for the rich multi-condition visibility rules (showWhen / allOf / anyOf) used by
// the Data Collection (MI) forms and — via this helper — the Service and Location/Site form builders.
// It mirrors the MI form's isFieldVisible logic so every builder agrees with the backend
// ConditionalRuleEvaluator. Conditions reference other questions in the same form by their code.
import { evaluateNumericOperators, NumericOperators } from './conditionalOperators';

export type ConditionalAnswerMap = Record<string, string | string[] | null | undefined>;

interface RuleCondition extends NumericOperators {
  fieldKey?: string;
  // Some MI rules reference a service answer via serviceFieldKey; for same-form builders it is looked
  // up in the same answer map, so either key works.
  serviceFieldKey?: string;
  equals?: string;
  notEquals?: string;
  in?: string[];
}

interface ShowWhen extends RuleCondition {
  allOf?: RuleCondition[];
  anyOf?: RuleCondition[];
}

// A stored answer may be a single value, an array, or a comma-joined string (checkbox answers).
// Normalise to a list so equals/in/notEquals all compare against the selected set.
const toValues = (value: string | string[] | null | undefined): string[] => {
  if (value === null || value === undefined) return [];
  if (Array.isArray(value)) return value;
  return value
    .split(',')
    .map((v) => v.trim())
    .filter((v) => v !== '');
};

const evaluateCondition = (condition: RuleCondition, answers: ConditionalAnswerMap): boolean => {
  const key = condition.fieldKey ?? condition.serviceFieldKey;
  if (!key) return true;

  const raw = answers[key];

  const numeric = evaluateNumericOperators(raw, condition);
  if (numeric !== null) return numeric;

  const selected = toValues(raw);
  if (condition.equals !== undefined) return selected.includes(condition.equals);
  if (condition.notEquals !== undefined) return !selected.includes(condition.notEquals);
  if (condition.in !== undefined && Array.isArray(condition.in)) return condition.in.some((v) => selected.includes(v));

  return true;
};

/**
 * Returns true when a question with the given conditional_rules JSON should be visible for the
 * supplied answers. No rules (null/empty/invalid) means always visible.
 */
export const evaluateConditionalRules = (
  rulesJson: string | null | undefined,
  answers: ConditionalAnswerMap
): boolean => {
  if (!rulesJson) return true;

  let parsed: { showWhen?: ShowWhen };
  try {
    parsed = JSON.parse(rulesJson) as { showWhen?: ShowWhen };
  } catch {
    return true;
  }

  const { showWhen } = parsed;
  if (!showWhen) return true;

  if (showWhen.allOf) return showWhen.allOf.every((c) => evaluateCondition(c, answers));
  if (showWhen.anyOf) return showWhen.anyOf.some((c) => evaluateCondition(c, answers));

  return evaluateCondition(showWhen, answers);
};
