import React, { useState, useEffect, useMemo, useRef } from 'react';
import { Box, Typography } from '@mui/material';
import { GovUKFieldset } from '../../../../../components/GovUKComponents';

// Minimal shape the builder needs from a form question, so it can be reused across the Data
// Collection, Service and Location/Site builders regardless of their full field/question types.
// The Data Collection FormField satisfies this structurally.
export interface ConditionalTriggerField {
  fieldKey: string;
  label: string;
  fieldType: string;
  displayOrder: number;
  options: { value: string; label: string }[];
}

// 'equals'/'in' are the option-based operators (radio/checkbox/select answers): 'equals' for a single
// answer, 'in' when the admin picks two or more (OR). The rest are numeric comparisons for
// number/currency triggers and map 1:1 to the JSON keys the evaluators understand.
type ConditionOperator = 'equals' | 'in' | 'gt' | 'gte' | 'lt' | 'lte' | 'eq' | 'neq';

const NUMERIC_OPERATORS: ConditionOperator[] = ['gt', 'gte', 'lt', 'lte', 'eq', 'neq'];

const NUMERIC_OPERATOR_OPTIONS = [
  { value: 'gt', label: 'Greater than (>)' },
  { value: 'gte', label: 'Greater than or equal to (≥)' },
  { value: 'lt', label: 'Less than (<)' },
  { value: 'lte', label: 'Less than or equal to (≤)' },
  { value: 'eq', label: 'Equal to (=)' },
  { value: 'neq', label: 'Not equal to (≠)' },
];

const OPERATOR_SYMBOL: Record<string, string> = { gt: '>', gte: '≥', lt: '<', lte: '≤', eq: '=', neq: '≠' };

const isNumericOperator = (op: ConditionOperator): boolean => NUMERIC_OPERATORS.includes(op);

interface ConditionEntry {
  fieldKey: string;
  operator: ConditionOperator;
  value: string; // numeric operators only
  values: string[]; // option operators only (equals => 1 selected, in => 2+)
}

interface TopLevelRule {
  displayInline?: boolean;
  parentOption?: string;
}

interface Props {
  value: string;
  onChange: (value: string) => void;
  availableFields: ConditionalTriggerField[];
  currentFieldKey?: string;
  configuration?: string;
  onConfigurationChange?: (value: string) => void;
}

interface FieldConfig {
  group?: string;
  groupLabel?: string;
  groupHint?: string;
  suffix?: string;
  [key: string]: unknown;
}

const MAX_CONDITIONS = 5;
const NUMERIC_FIELD_TYPES = ['number', 'currency'];
const EMPTY_CONDITION: ConditionEntry = { fieldKey: '', operator: 'equals', value: '', values: [] };

interface ParseResult {
  conditions: ConditionEntry[];
  topLevel: TopLevelRule;
  isOrMode: boolean;
}

type RawCondition = { fieldKey?: string; equals?: string; in?: string[] } & Partial<Record<'gt' | 'gte' | 'lt' | 'lte' | 'eq' | 'neq', string>>;

const toConditionEntry = (c: RawCondition): ConditionEntry => {
  const numericOp = NUMERIC_OPERATORS.find((op) => c[op as keyof RawCondition] !== undefined);
  if (numericOp) {
    return { fieldKey: c.fieldKey!, operator: numericOp, value: c[numericOp as keyof RawCondition] as string, values: [] };
  }
  if (c.in !== undefined) {
    return { fieldKey: c.fieldKey!, operator: 'in', value: '', values: c.in };
  }
  return { fieldKey: c.fieldKey!, operator: 'equals', value: '', values: c.equals !== undefined ? [c.equals] : [] };
};

const parseValue = (raw: string): ParseResult => {
  const empty: ParseResult = { conditions: [], topLevel: {}, isOrMode: false };
  if (!raw) return empty;
  try {
    const parsed = JSON.parse(raw) as {
      showWhen?: RawCondition & {
        allOf?: RawCondition[];
        anyOf?: RawCondition[];
      };
      displayInline?: boolean;
      parentOption?: string;
    };

    const { showWhen, displayInline, parentOption } = parsed;
    const topLevel: TopLevelRule = {};
    if (displayInline !== undefined) topLevel.displayInline = displayInline;
    if (parentOption !== undefined) topLevel.parentOption = parentOption;

    if (!showWhen) return { conditions: [], topLevel, isOrMode: false };

    if (showWhen.allOf) {
      return {
        conditions: showWhen.allOf.filter((c) => c.fieldKey).map(toConditionEntry),
        topLevel,
        isOrMode: false,
      };
    }

    if (showWhen.anyOf) {
      return {
        conditions: showWhen.anyOf.filter((c) => c.fieldKey).map(toConditionEntry),
        topLevel,
        isOrMode: true,
      };
    }

    if (showWhen.fieldKey) {
      return {
        conditions: [toConditionEntry(showWhen)],
        topLevel,
        isOrMode: false,
      };
    }

    return { conditions: [], topLevel, isOrMode: false };
  } catch {
    return empty;
  }
};

// An option condition serializes to 'equals' for a single answer and 'in' for two or more (OR).
const toConditionJson = (c: ConditionEntry): Record<string, unknown> => {
  if (isNumericOperator(c.operator)) return { fieldKey: c.fieldKey, [c.operator]: c.value };
  if (c.values.length > 1) return { fieldKey: c.fieldKey, in: c.values };
  return { fieldKey: c.fieldKey, equals: c.values[0] };
};

const isConditionComplete = (c: ConditionEntry): boolean =>
  Boolean(c.fieldKey) && (isNumericOperator(c.operator) ? c.value !== '' : c.values.length > 0);

const serializeValue = (conditions: ConditionEntry[], topLevel: TopLevelRule): string => {
  const complete = conditions.filter(isConditionComplete);
  if (complete.length === 0) return '';

  let showWhen: Record<string, unknown>;

  if (complete.length === 1) {
    showWhen = toConditionJson(complete[0]);
  } else {
    showWhen = { allOf: complete.map(toConditionJson) };
  }

  const rule: Record<string, unknown> = { showWhen };
  if (topLevel.displayInline !== undefined) rule.displayInline = topLevel.displayInline;
  // parentOption only makes sense for a single condition matching a single option (it identifies
  // which discrete option triggers inline display) — never for numeric or multi-answer (in) rules.
  if (complete.length === 1 && topLevel.parentOption) rule.parentOption = topLevel.parentOption;

  return JSON.stringify(rule);
};

// parentOption is meaningful only when the whole rule is a single option condition with exactly one
// answer selected. Numeric thresholds and multi-answer (OR) conditions never carry one.
const parentOptionFor = (conditions: ConditionEntry[]): string | undefined =>
  conditions.length === 1 && conditions[0].values.length === 1 ? conditions[0].values[0] : undefined;

// Shared with Edit/Create validateForm so both use the exact same parsing rules.
export const getConditionFieldKeys = (raw: string): string[] => parseValue(raw).conditions.map((c) => c.fieldKey).filter(Boolean);

const ConditionalRulesBuilder = ({
  value,
  onChange,
  availableFields,
  currentFieldKey,
  configuration,
  onConfigurationChange,
}: Props): React.ReactElement => {
  const [enabled, setEnabled] = useState(false);
  const [conditions, setConditions] = useState<ConditionEntry[]>([EMPTY_CONDITION]);
  const [topLevel, setTopLevel] = useState<TopLevelRule>({});
  const [isOrMode, setIsOrMode] = useState(false);
  const [config, setConfig] = useState<FieldConfig>({});

  // Tracks the last value this component emitted, so the sync effect below only re-parses when
  // `value` changes externally (e.g. loading a different question) and ignores its own echo.
  // Without this, picking the field on an incomplete new condition serialises to a value that omits
  // it, which would re-parse and wipe the in-progress row.
  const lastEmittedRef = useRef<string | null>(null);

  useEffect(() => {
    if (value === lastEmittedRef.current) return;
    const { conditions: parsed, topLevel: parsedTop, isOrMode: orMode } = parseValue(value);
    if (parsed.length > 0) {
      setEnabled(true);
      setConditions(parsed);
      setTopLevel(parsedTop);
      setIsOrMode(orMode);
    } else {
      setEnabled(false);
      setConditions([EMPTY_CONDITION]);
      setTopLevel({});
      setIsOrMode(false);
    }
  }, [value]);

  useEffect(() => {
    if (configuration) {
      try {
        setConfig(JSON.parse(configuration) as FieldConfig);
      } catch {
        setConfig({});
      }
    } else {
      setConfig({});
    }
  }, [configuration]);

  const triggerFields = useMemo(() => {
    return availableFields.filter(
      (f) =>
        f.fieldKey !== currentFieldKey &&
        ['radio', 'checkbox', 'select'].includes(f.fieldType.toLowerCase()) &&
        f.options.length > 0
    );
  }, [availableFields, currentFieldKey]);

  const numericTriggerFields = useMemo(() => {
    return availableFields.filter(
      (f) => f.fieldKey !== currentFieldKey && NUMERIC_FIELD_TYPES.includes(f.fieldType.toLowerCase())
    );
  }, [availableFields, currentFieldKey]);

  const isNumericTrigger = (fieldKey: string) => numericTriggerFields.some((f) => f.fieldKey === fieldKey);

  const currentDisplayOrder = useMemo(
    () => availableFields.find((f) => f.fieldKey === currentFieldKey)?.displayOrder ?? null,
    [availableFields, currentFieldKey]
  );

  const getFieldOptions = (index: number) => {
    const chosenElsewhere = new Set(
      conditions.filter((_, i) => i !== index).map((c) => c.fieldKey).filter(Boolean)
    );
    const labelFor = (f: ConditionalTriggerField) => `${f.fieldKey} - ${f.label.substring(0, 50)}${f.label.length > 50 ? '...' : ''}`;
    return [
      { value: '', label: 'Select a question' },
      ...[...triggerFields, ...numericTriggerFields]
        .filter((f) => !chosenElsewhere.has(f.fieldKey))
        .map((f) => ({ value: f.fieldKey, label: labelFor(f) })),
    ];
  };

  const getAnswerOptions = (fieldKey: string) => {
    const field = triggerFields.find((f) => f.fieldKey === fieldKey);
    return field ? field.options.map((o) => ({ value: o.value, label: o.label })) : [];
  };

  const optionLabel = (fieldKey: string, optionValue: string) =>
    triggerFields.find((f) => f.fieldKey === fieldKey)?.options.find((o) => o.value === optionValue)?.label ?? optionValue;

  const notify = (updated: ConditionEntry[], updatedTop: TopLevelRule) => {
    const serialized = serializeValue(updated, updatedTop);
    lastEmittedRef.current = serialized;
    onChange(serialized);
  };

  const applyUpdate = (updated: ConditionEntry[]) => {
    const newTop: TopLevelRule = { ...topLevel, parentOption: parentOptionFor(updated) };
    setConditions(updated);
    setTopLevel(newTop);
    notify(updated, newTop);
  };

  const handleEnabledChange = (checked: boolean) => {
    setEnabled(checked);
    if (!checked) {
      setConditions([EMPTY_CONDITION]);
      setTopLevel({});
      lastEmittedRef.current = '';
      onChange('');
    }
  };

  const handleFieldChange = (index: number, fieldKey: string) => {
    // Numeric triggers default to ">=" (matches the historical behaviour); option triggers use "equals".
    const operator: ConditionOperator = isNumericTrigger(fieldKey) ? 'gte' : 'equals';
    const updated = conditions.map((c, i) => (i === index ? { fieldKey, operator, value: '', values: [] } : c));
    applyUpdate(updated);
  };

  const handleOperatorChange = (index: number, operator: ConditionOperator) => {
    const updated = conditions.map((c, i) => (i === index ? { ...c, operator } : c));
    setConditions(updated);
    notify(updated, topLevel);
  };

  const handleNumericValueChange = (index: number, value: string) => {
    const updated = conditions.map((c, i) => (i === index ? { ...c, value } : c));
    setConditions(updated);
    notify(updated, topLevel);
  };

  // Toggle one option answer on/off (multi-answer questions). One selected => 'equals'; two or more => 'in' (OR).
  const handleAnswerToggle = (index: number, optionValue: string, checked: boolean) => {
    const updated = conditions.map((c, i) => {
      if (i !== index) return c;
      const values = checked ? [...c.values, optionValue] : c.values.filter((v) => v !== optionValue);
      return { ...c, values, operator: (values.length > 1 ? 'in' : 'equals') as ConditionOperator };
    });
    applyUpdate(updated);
  };

  // Pick a single option answer (questions with 2 or fewer options behave like radios => always 'equals').
  const handleAnswerSelect = (index: number, optionValue: string) => {
    const updated = conditions.map((c, i) => (i === index ? { ...c, values: [optionValue], operator: 'equals' as ConditionOperator } : c));
    applyUpdate(updated);
  };

  const handleAddCondition = () => {
    if (conditions.length >= MAX_CONDITIONS) return;
    setConditions((prev) => [...prev, EMPTY_CONDITION]);
  };

  const handleRemoveCondition = (index: number) => {
    applyUpdate(conditions.filter((_, i) => i !== index));
  };

  const handleDisplayInlineChange = (checked: boolean) => {
    const newTop = { ...topLevel, displayInline: checked };
    setTopLevel(newTop);
    notify(conditions, newTop);
  };

  const updateConfig = (updates: Partial<FieldConfig>) => {
    const newConfig = { ...config, ...updates };
    Object.keys(newConfig).forEach((key) => {
      if (newConfig[key] === '' || newConfig[key] === undefined) delete newConfig[key];
    });
    setConfig(newConfig);
    if (onConfigurationChange) {
      onConfigurationChange(Object.keys(newConfig).length > 0 ? JSON.stringify(newConfig) : '');
    }
  };

  const handleGroupChange = (group: string) => updateConfig({ group: group || undefined });
  const handleSuffixChange = (suffix: string) => updateConfig({ suffix: suffix || undefined });
  const handleGroupLabelChange = (groupLabel: string) => updateConfig({ groupLabel: groupLabel || undefined });
  const handleGroupHintChange = (groupHint: string) => updateConfig({ groupHint: groupHint || undefined });

  const completeConditions = conditions.filter(isConditionComplete);

  const previewText = useMemo(() => {
    if (completeConditions.length === 0) return null;
    return completeConditions
      .map((c) => {
        if (isNumericOperator(c.operator)) return `${c.fieldKey} ${OPERATOR_SYMBOL[c.operator]} ${c.value}`;
        const labels = c.values.map((v) => optionLabel(c.fieldKey, v));
        return `${c.fieldKey} = ${labels.map((l) => `"${l}"`).join(' or ')}`;
      })
      .join(' AND ');
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [completeConditions, triggerFields]);

  return (
    <Box sx={{ border: '1px solid #b1b4b6', p: 3, borderRadius: 1, mb: 3 }}>
      <Typography variant="h6" sx={{ mb: 2, fontSize: '1.1rem' }}>
        Conditional Display Rules
      </Typography>
      <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
        Make this question appear only when specific answers are selected on other questions. Multiple conditions are combined with AND.
      </Typography>

      <div className="govuk-form-group govuk-!-margin-bottom-2">
        <div className="govuk-checkboxes" data-module="govuk-checkboxes">
          <div className="govuk-checkboxes__item">
            <input
              className="govuk-checkboxes__input"
              id="conditional-rules-enabled"
              name="conditionalRulesEnabled"
              type="checkbox"
              checked={enabled}
              onChange={(e) => handleEnabledChange(e.target.checked)}
            />
            <label className="govuk-label govuk-checkboxes__label" htmlFor="conditional-rules-enabled">
              Enable conditional display
            </label>
          </div>
        </div>
      </div>

      {enabled && (
        <Box sx={{ mt: 2, pl: 2, borderLeft: '4px solid #1d70b8' }}>
          {isOrMode && (
            <Box sx={{ p: 2, mb: 2, backgroundColor: '#fef7e5', borderRadius: 1 }}>
              <Typography variant="body2" color="warning.main">
                This question uses OR (anyOf) conditions which cannot be edited in this UI — shown for reference only.
              </Typography>
              {conditions.map((c, i) => (
                <Typography key={i} variant="body2" sx={{ mt: 0.5 }}>
                  {i > 0 && <em>OR </em>}
                  <strong>{c.fieldKey}</strong>{' '}
                  {isNumericOperator(c.operator) ? (
                    <>
                      {OPERATOR_SYMBOL[c.operator]} <strong>{c.value}</strong>
                    </>
                  ) : (
                    <>
                      = <strong>{c.values.map((v) => `"${v}"`).join(' or ')}</strong>
                    </>
                  )}
                </Typography>
              ))}
            </Box>
          )}

          {!isOrMode && availableFields.length === 0 && conditions.some((c) => c.fieldKey) && (
            <Box sx={{ p: 2, mb: 2, backgroundColor: '#f3f2f1', borderRadius: 1 }}>
              <Typography variant="body2" sx={{ fontWeight: 600 }}>
                Current Rules:
              </Typography>
              {conditions.map((c, i) => (
                <Typography key={i} variant="body2">
                  {i > 0 && <strong>AND </strong>}
                  Show when <strong>{c.fieldKey}</strong>{' '}
                  {isNumericOperator(c.operator) ? (
                    <>
                      is <strong>{OPERATOR_SYMBOL[c.operator]} {c.value}</strong>
                    </>
                  ) : (
                    <>
                      equals <strong>{c.values.map((v) => `"${v}"`).join(' or ')}</strong>
                    </>
                  )}
                </Typography>
              ))}
              <Typography variant="body2" color="text.secondary" sx={{ mt: 1 }}>
                Loading available questions to edit...
              </Typography>
            </Box>
          )}

          {!isOrMode && availableFields.length === 0 && !conditions.some((c) => c.fieldKey) && (
            <Box sx={{ p: 2, backgroundColor: '#f3f2f1', borderRadius: 1 }}>
              <Typography variant="body2" color="text.secondary">
                Loading available questions...
              </Typography>
            </Box>
          )}

          {!isOrMode && availableFields.length > 0 && triggerFields.length === 0 && numericTriggerFields.length === 0 && (
            <Box sx={{ p: 2, backgroundColor: '#fef7e5', borderRadius: 1 }}>
              <Typography variant="body2" color="warning.main">
                No questions with options (Radio, Checkbox, or Select) or numeric questions (Number, Currency) are available to use as
                triggers. Add a question of one of these types first.
              </Typography>
            </Box>
          )}

          {!isOrMode && availableFields.length > 0 && (triggerFields.length > 0 || numericTriggerFields.length > 0) && (
            <>
              {conditions.map((condition, index) => {
                const field =
                  triggerFields.find((f) => f.fieldKey === condition.fieldKey) ??
                  numericTriggerFields.find((f) => f.fieldKey === condition.fieldKey);
                const isOrphaned = condition.fieldKey !== '' && !field;
                const isForwardRef =
                  !isOrphaned &&
                  field !== undefined &&
                  currentDisplayOrder !== null &&
                  field.displayOrder > currentDisplayOrder;

                return (
                  <Box key={index}>
                    {index > 0 && (
                      <Box sx={{ display: 'flex', alignItems: 'center', my: 1.5 }}>
                        <Box sx={{ flexGrow: 1, height: '1px', backgroundColor: '#b1b4b6' }} />
                        <Typography
                          variant="body2"
                          sx={{ mx: 2, fontWeight: 700, color: '#505a5f', fontSize: '0.8rem', letterSpacing: '0.08em' }}
                        >
                          AND
                        </Typography>
                        <Box sx={{ flexGrow: 1, height: '1px', backgroundColor: '#b1b4b6' }} />
                      </Box>
                    )}

                    <Box sx={{ display: 'flex', alignItems: 'flex-start', gap: 1 }}>
                      <Box sx={{ flexGrow: 1 }}>
                        {isOrphaned ? (
                          <Box sx={{ p: 2, mb: 1, backgroundColor: '#fef7e5', borderRadius: 1 }}>
                            <Typography variant="body2" color="warning.main">
                              Condition references field &ldquo;{condition.fieldKey}&rdquo; which no longer exists or is not a Radio, Checkbox, Select, Number, or Currency field.
                            </Typography>
                          </Box>
                        ) : (
                          <>
                            <GovUKFieldset.Select
                              id={`conditional-field-${index}`}
                              name={`conditionalField${index}`}
                              label={index === 0 ? 'Show this question when...' : 'AND when...'}
                              hint="Select the question that controls when this one appears"
                              value={condition.fieldKey}
                              options={getFieldOptions(index)}
                              onChange={(e: React.ChangeEvent<HTMLSelectElement>) => handleFieldChange(index, e.target.value)}
                            />

                            {condition.fieldKey && isNumericTrigger(condition.fieldKey) && (
                              <>
                                <GovUKFieldset.Select
                                  id={`conditional-operator-${index}`}
                                  name={`conditionalOperator${index}`}
                                  label="...is"
                                  hint="Choose how the numeric answer is compared"
                                  value={isNumericOperator(condition.operator) ? condition.operator : 'gte'}
                                  options={NUMERIC_OPERATOR_OPTIONS}
                                  onChange={(e: React.ChangeEvent<HTMLSelectElement>) =>
                                    handleOperatorChange(index, e.target.value as ConditionOperator)
                                  }
                                />
                                <GovUKFieldset.Input
                                  id={`conditional-answer-${index}`}
                                  name={`conditionalAnswer${index}`}
                                  type="number"
                                  label="...the value"
                                  hint="Enter the numeric value to compare against"
                                  value={condition.value}
                                  onChange={(e: React.ChangeEvent<HTMLInputElement>) => handleNumericValueChange(index, e.target.value)}
                                />
                              </>
                            )}

                            {condition.fieldKey && !isNumericTrigger(condition.fieldKey) && (() => {
                              const answerOptions = getAnswerOptions(condition.fieldKey);
                              // Only offer multi-answer (OR) when there are more than two options. With two or
                              // fewer (e.g. Yes/No) a single choice is the only sensible rule, so use radios.
                              const allowMultiple = answerOptions.length > 2;

                              if (allowMultiple) {
                                return (
                                  <div className="govuk-form-group">
                                    <label className="govuk-label">...has any of these answers</label>
                                    <div className="govuk-hint" style={{ marginBottom: '0.5rem' }}>
                                      Tick one answer, or tick several to match when the question has any of them (OR).
                                    </div>
                                    <div className="govuk-checkboxes" data-module="govuk-checkboxes">
                                      {answerOptions.map((o) => {
                                        const optionId = `conditional-answer-${index}-${o.value}`;
                                        return (
                                          <div className="govuk-checkboxes__item" key={o.value}>
                                            <input
                                              className="govuk-checkboxes__input"
                                              id={optionId}
                                              name={`conditionalAnswer${index}`}
                                              type="checkbox"
                                              value={o.value}
                                              checked={condition.values.includes(o.value)}
                                              onChange={(e) => handleAnswerToggle(index, o.value, e.target.checked)}
                                            />
                                            <label className="govuk-label govuk-checkboxes__label" htmlFor={optionId}>
                                              {o.label}
                                            </label>
                                          </div>
                                        );
                                      })}
                                    </div>
                                  </div>
                                );
                              }

                              return (
                                <div className="govuk-form-group">
                                  <label className="govuk-label">...has the answer</label>
                                  <div className="govuk-hint" style={{ marginBottom: '0.5rem' }}>
                                    Select which answer triggers this question to appear.
                                  </div>
                                  <div className="govuk-radios govuk-radios--small" data-module="govuk-radios">
                                    {answerOptions.map((o) => {
                                      const optionId = `conditional-answer-${index}-${o.value}`;
                                      return (
                                        <div className="govuk-radios__item" key={o.value}>
                                          <input
                                            className="govuk-radios__input"
                                            id={optionId}
                                            name={`conditionalAnswer${index}`}
                                            type="radio"
                                            value={o.value}
                                            checked={condition.values[0] === o.value}
                                            onChange={() => handleAnswerSelect(index, o.value)}
                                          />
                                          <label className="govuk-label govuk-radios__label" htmlFor={optionId}>
                                            {o.label}
                                          </label>
                                        </div>
                                      );
                                    })}
                                  </div>
                                </div>
                              );
                            })()}

                            {isForwardRef && (
                              <Box sx={{ p: 1, mb: 1, backgroundColor: '#fef7e5', borderRadius: 1 }}>
                                <Typography variant="body2" color="warning.main" sx={{ fontSize: '0.875rem' }}>
                                  This question appears after the current one in the form — check the order.
                                </Typography>
                              </Box>
                            )}
                          </>
                        )}
                      </Box>

                      {conditions.length > 1 && (
                        <button
                          type="button"
                          className="govuk-button govuk-button--secondary"
                          style={{ marginTop: 0, marginBottom: 0, whiteSpace: 'nowrap' }}
                          onClick={() => handleRemoveCondition(index)}
                          aria-label={`Remove condition ${index + 1}`}
                        >
                          Remove
                        </button>
                      )}
                    </Box>
                  </Box>
                );
              })}

              {conditions.length < MAX_CONDITIONS && (
                <button
                  type="button"
                  className="govuk-button govuk-button--secondary"
                  style={{ marginTop: '0.5rem' }}
                  onClick={handleAddCondition}
                >
                  + Add another condition
                </button>
              )}

              {completeConditions.length > 0 && (
                <div className="govuk-form-group govuk-!-margin-top-2">
                  <div className="govuk-checkboxes" data-module="govuk-checkboxes">
                    <div className="govuk-checkboxes__item">
                      <input
                        className="govuk-checkboxes__input"
                        id="conditional-rules-display-inline"
                        name="conditionalRulesDisplayInline"
                        type="checkbox"
                        checked={topLevel.displayInline || false}
                        onChange={(e) => handleDisplayInlineChange(e.target.checked)}
                      />
                      <label className="govuk-label govuk-checkboxes__label" htmlFor="conditional-rules-display-inline">
                        Display inline (nested under the parent question)
                      </label>
                    </div>
                  </div>
                </div>
              )}

              {topLevel.displayInline && (
                <Box sx={{ mt: 2, pl: 2, borderLeft: '4px solid #b1b4b6' }}>
                  <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
                    Group multiple inline fields together by giving them the same Group Name (e.g., &ldquo;QSU12&rdquo; for sex breakdown fields).
                  </Typography>
                  <GovUKFieldset.Input
                    id="field-group"
                    name="fieldGroup"
                    label="Group Name"
                    hint="Fields with the same group name will display together in a table format (e.g., QSU12)"
                    value={config.group || ''}
                    onChange={(e: React.ChangeEvent<HTMLInputElement>) => handleGroupChange(e.target.value)}
                  />
                  <GovUKFieldset.Input
                    id="field-group-label"
                    name="fieldGroupLabel"
                    label="Group Label"
                    hint="The heading text for the group. Only set this on the first field in the group."
                    value={config.groupLabel || ''}
                    onChange={(e: React.ChangeEvent<HTMLInputElement>) => handleGroupLabelChange(e.target.value)}
                  />
                  <GovUKFieldset.Input
                    id="field-group-hint"
                    name="fieldGroupHint"
                    label="Group Hint"
                    hint="Help text shown below the group heading. Only set this on the first field in the group."
                    value={config.groupHint || ''}
                    onChange={(e: React.ChangeEvent<HTMLInputElement>) => handleGroupHintChange(e.target.value)}
                  />
                  <GovUKFieldset.Input
                    id="field-suffix"
                    name="fieldSuffix"
                    label="Input Suffix"
                    hint="Text shown after the input field (e.g., 'parents/carers')"
                    value={config.suffix || ''}
                    onChange={(e: React.ChangeEvent<HTMLInputElement>) => handleSuffixChange(e.target.value)}
                  />
                </Box>
              )}

              {previewText && (
                <Box sx={{ mt: 2, p: 2, backgroundColor: '#f3f2f1', borderRadius: 1 }}>
                  <Typography variant="body2" sx={{ fontWeight: 600 }}>
                    Preview:
                  </Typography>
                  <Typography variant="body2">
                    This question will appear when <strong>{previewText}</strong>
                    {topLevel.displayInline && ' (displayed inline)'}
                    {config.group && ` in group "${config.group}"`}
                  </Typography>
                </Box>
              )}
            </>
          )}
        </Box>
      )}
    </Box>
  );
};

export default ConditionalRulesBuilder;
