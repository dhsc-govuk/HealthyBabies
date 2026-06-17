import React, { useState, useEffect, useMemo } from 'react';
import { Box, Typography, FormControlLabel, Switch } from '@mui/material';
import { GovUKFieldset } from '../../../../../components/GovUKComponents';
import { FormField } from './types';

interface ConditionalRule {
  showWhen?: {
    fieldKey: string;
    equals: string;
  };
  displayInline?: boolean;
  parentOption?: string;
}

interface Props {
  value: string;
  onChange: (value: string) => void;
  availableFields: FormField[];
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

const ConditionalRulesBuilder = ({ value, onChange, availableFields, currentFieldKey, configuration, onConfigurationChange }: Props): React.ReactElement => {
  const [enabled, setEnabled] = useState(false);
  const [rule, setRule] = useState<ConditionalRule>({});
  const [config, setConfig] = useState<FieldConfig>({});

  // Parse existing JSON value when it changes
  useEffect(() => {
    if (value) {
      try {
        const parsed = JSON.parse(value) as ConditionalRule;
        setRule(parsed);
        setEnabled(!!parsed.showWhen);
      } catch {
        setEnabled(false);
        setRule({});
      }
    } else {
      setEnabled(false);
      setRule({});
    }
  }, [value]);

  // Parse configuration JSON
  useEffect(() => {
    if (configuration) {
      try {
        const parsed = JSON.parse(configuration) as FieldConfig;
        setConfig(parsed);
      } catch {
        setConfig({});
      }
    } else {
      setConfig({});
    }
  }, [configuration]);

  // Filter out current field and only show fields that can have options (radio, checkbox, select)
  const triggerFields = useMemo(() => {
    return availableFields.filter(
      (f) =>
        f.fieldKey !== currentFieldKey &&
        ['radio', 'checkbox', 'select'].includes(f.fieldType.toLowerCase()) &&
        f.options.length > 0
    );
  }, [availableFields, currentFieldKey]);

  const selectedField = useMemo(() => {
    return triggerFields.find((f) => f.fieldKey === rule.showWhen?.fieldKey);
  }, [triggerFields, rule.showWhen?.fieldKey]);

  const fieldOptions = useMemo(() => {
    return [
      { value: '', label: 'Select a question' },
      ...triggerFields.map((f) => ({ value: f.fieldKey, label: `${f.fieldKey} - ${f.label.substring(0, 50)}${f.label.length > 50 ? '...' : ''}` })),
    ];
  }, [triggerFields]);

  const answerOptions = useMemo(() => {
    if (!selectedField) return [{ value: '', label: 'Select an answer' }];
    return [
      { value: '', label: 'Select an answer' },
      ...selectedField.options.map((opt) => ({ value: opt.value, label: opt.label })),
    ];
  }, [selectedField]);

  const updateRule = (updates: Partial<ConditionalRule>) => {
    const newRule = { ...rule, ...updates };
    setRule(newRule);

    // Convert to JSON and notify parent
    if (newRule.showWhen?.fieldKey && newRule.showWhen?.equals) {
      onChange(JSON.stringify(newRule));
    } else if (!enabled) {
      onChange('');
    }
  };

  const handleEnabledChange = (checked: boolean) => {
    setEnabled(checked);
    if (!checked) {
      setRule({});
      onChange('');
    }
  };

  const handleFieldChange = (fieldKey: string) => {
    const field = triggerFields.find((f) => f.fieldKey === fieldKey);
    updateRule({
      showWhen: fieldKey ? { fieldKey, equals: '' } : undefined,
      parentOption: field?.options[0]?.value,
    });
  };

  const handleAnswerChange = (equals: string) => {
    updateRule({
      showWhen: rule.showWhen ? { ...rule.showWhen, equals } : undefined,
      parentOption: equals,
    });
  };

  const handleDisplayInlineChange = (checked: boolean) => {
    updateRule({ displayInline: checked });
  };

  const updateConfig = (updates: Partial<FieldConfig>) => {
    const newConfig = { ...config, ...updates };
    // Remove empty values
    Object.keys(newConfig).forEach((key) => {
      if (newConfig[key] === '' || newConfig[key] === undefined) {
        delete newConfig[key];
      }
    });
    setConfig(newConfig);
    if (onConfigurationChange) {
      onConfigurationChange(Object.keys(newConfig).length > 0 ? JSON.stringify(newConfig) : '');
    }
  };

  const handleGroupChange = (group: string) => {
    updateConfig({ group: group || undefined });
  };

  const handleSuffixChange = (suffix: string) => {
    updateConfig({ suffix: suffix || undefined });
  };

  const handleGroupLabelChange = (groupLabel: string) => {
    updateConfig({ groupLabel: groupLabel || undefined });
  };

  const handleGroupHintChange = (groupHint: string) => {
    updateConfig({ groupHint: groupHint || undefined });
  };

  return (
    <Box sx={{ border: '1px solid #b1b4b6', p: 3, borderRadius: 1, mb: 3 }}>
      <Typography variant="h6" sx={{ mb: 2, fontSize: '1.1rem' }}>
        Conditional Display Rules
      </Typography>
      <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
        Make this question appear only when a specific answer is selected on another question.
      </Typography>

      <FormControlLabel
        control={
          <Switch
            checked={enabled}
            onChange={(e) => handleEnabledChange(e.target.checked)}
          />
        }
        label="Enable conditional display"
        sx={{ mb: 2 }}
      />

      {enabled && (
        <Box sx={{ mt: 2, pl: 2, borderLeft: '4px solid #1d70b8' }}>
          {/* Show current rule value if fields haven't loaded yet */}
          {availableFields.length === 0 && rule.showWhen?.fieldKey && (
            <Box sx={{ p: 2, mb: 2, backgroundColor: '#f3f2f1', borderRadius: 1 }}>
              <Typography variant="body2" sx={{ fontWeight: 600 }}>
                Current Rule:
              </Typography>
              <Typography variant="body2">
                Show when <strong>{rule.showWhen.fieldKey}</strong> equals <strong>"{rule.showWhen.equals}"</strong>
                {rule.displayInline && ' (displayed inline)'}
              </Typography>
              <Typography variant="body2" color="text.secondary" sx={{ mt: 1 }}>
                Loading available questions to edit...
              </Typography>
            </Box>
          )}

          {availableFields.length === 0 ? (
            !rule.showWhen?.fieldKey && (
              <Box sx={{ p: 2, backgroundColor: '#f3f2f1', borderRadius: 1 }}>
                <Typography variant="body2" color="text.secondary">
                  Loading available questions...
                </Typography>
              </Box>
            )
          ) : triggerFields.length === 0 ? (
            <Box sx={{ p: 2, backgroundColor: '#fef7e5', borderRadius: 1 }}>
              <Typography variant="body2" color="warning.main">
                No questions with options (Radio, Checkbox, or Select) are available to use as triggers.
                Add questions with options first.
              </Typography>
            </Box>
          ) : (
            <>
              <GovUKFieldset.Select
                id="conditional-field"
                name="conditionalField"
                label="Show this question when..."
                hint="Select the question that controls when this one appears"
                value={rule.showWhen?.fieldKey || ''}
                options={fieldOptions}
                onChange={(e) => handleFieldChange(e.target.value)}
              />

              {rule.showWhen?.fieldKey && (
                <GovUKFieldset.Select
                  id="conditional-answer"
                  name="conditionalAnswer"
                  label="...has the answer"
                  hint="Select which answer triggers this question to appear"
                  value={rule.showWhen?.equals || ''}
                  options={answerOptions}
                  onChange={(e) => handleAnswerChange(e.target.value)}
                />
              )}

              {rule.showWhen?.fieldKey && rule.showWhen?.equals && (
                <FormControlLabel
                  control={
                    <Switch
                      checked={rule.displayInline || false}
                      onChange={(e) => handleDisplayInlineChange(e.target.checked)}
                    />
                  }
                  label="Display inline (nested under the parent question)"
                  sx={{ mt: 2 }}
                />
              )}

              {rule.displayInline && (
                <Box sx={{ mt: 2, pl: 2, borderLeft: '4px solid #b1b4b6' }}>
                  <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
                    Group multiple inline fields together by giving them the same Group Name (e.g., "QSU12" for sex breakdown fields).
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
                    hint="The heading text for the group (e.g., 'How many parents or carers have used this service, broken down by sex?'). Only set this on the first field in the group."
                    value={config.groupLabel || ''}
                    onChange={(e: React.ChangeEvent<HTMLInputElement>) => handleGroupLabelChange(e.target.value)}
                  />
                  <GovUKFieldset.Input
                    id="field-group-hint"
                    name="fieldGroupHint"
                    label="Group Hint"
                    hint="Help text shown below the group heading (e.g., 'For each sex, give a figure for the last 3 months...'). Only set this on the first field in the group."
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

              {rule.showWhen?.fieldKey && rule.showWhen?.equals && (
                <Box sx={{ mt: 2, p: 2, backgroundColor: '#f3f2f1', borderRadius: 1 }}>
                  <Typography variant="body2" sx={{ fontWeight: 600 }}>
                    Preview:
                  </Typography>
                  <Typography variant="body2">
                    This question will appear when <strong>{rule.showWhen.fieldKey}</strong> equals{' '}
                    <strong>"{selectedField?.options.find((o) => o.value === rule.showWhen?.equals)?.label || rule.showWhen.equals}"</strong>
                    {rule.displayInline && ' (displayed inline)'}
                    {config.group && ` in group "${config.group}"`}
                  </Typography>
                </Box>
              )}

              {/* Show existing rule info if the trigger field isn't in the dropdown (e.g., different field type) */}
              {rule.showWhen?.fieldKey && !selectedField && (
                <Box sx={{ mt: 2, p: 2, backgroundColor: '#fef7e5', borderRadius: 1 }}>
                  <Typography variant="body2" color="warning.main">
                    Current rule references field "{rule.showWhen.fieldKey}" which is not available in the dropdown
                    (it may not be a Radio/Checkbox/Select field or may have been deleted).
                  </Typography>
                  <Typography variant="body2" sx={{ mt: 1 }}>
                    Current value: <code>{value}</code>
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
