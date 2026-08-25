import React from 'react';
import GovUKFieldset from '../GovUKFieldset';
import GovUKCheckbox from '../GovUKCheckbox';
import GovUKInputWithSuffix from '../GovUKInputWithSuffix';
import GovUKFileUpload from '../GovUKFileUpload';
import GovUKGroupedInputs from '../GovUKGroupedInputs';
import GovUKDateField from '../GovUKDateField';
import { FormModuleFieldDto, ConditionalRule, FieldConfiguration } from '../../../views/OrganisationAdmin/Submissions/queries';

interface FormFieldRendererProps {
  field: FormModuleFieldDto;
  value: string | string[] | null;
  allFields?: FormModuleFieldDto[];
  allValues?: Record<string, string | string[] | null>;
  allErrors?: Record<string, string>;
  showQuestionCode?: boolean;
  isConditionalChild?: boolean;
  onChange: (code: string, value: string | string[] | null) => void;
  onFileUpload?: (code: string, file: File) => Promise<void>;
}

const parseConditionalRules = (rulesJson: string | null | undefined): ConditionalRule | null => {
  if (!rulesJson) return null;
  try {
    return JSON.parse(rulesJson) as ConditionalRule;
  } catch {
    return null;
  }
};

const parseConfiguration = (configJson: string | null | undefined): FieldConfiguration | null => {
  if (!configJson) return null;
  try {
    return JSON.parse(configJson) as FieldConfiguration;
  } catch {
    return null;
  }
};

// Evaluates simple arithmetic formulas like "DG09+DG11" or "DG11-(DG13+DG14)"
// Field codes are substituted with their numeric values from allValues
export const computeFormula = (formula: string, allValues: Record<string, string | string[] | null>): number | null => {
  if (!formula) return null;
  try {
    // Replace field codes (uppercase letters + digits) with their values
    const expression = formula.replace(/[A-Z]{1,4}\d+(?:\.\d+)?[a-z]*/g, (code) => {
      const val = parseFloat((allValues[code] as string) || '0');
      return isNaN(val) ? '0' : String(val);
    });
    // Only allow safe arithmetic characters
    if (!/^[\d\s+\-*/.()]+$/.test(expression)) return null;
    // eslint-disable-next-line no-new-func
    const result = Function(`"use strict"; return (${expression})`)() as number;
    return isNaN(result) ? null : result;
  } catch {
    return null;
  }
};

const FormFieldRenderer: React.FC<FormFieldRendererProps> = ({
  field,
  value,
  allFields = [],
  allValues = {},
  allErrors = {},
  showQuestionCode = true,
  isConditionalChild = false,
  onChange,
  onFileUpload,
}) => {
  const stringValue = typeof value === 'string' ? value : '';
  const arrayValue = Array.isArray(value) ? value : [];
  const config = parseConfiguration(field.configuration);
  const suffix = config?.suffix || field.placeholder || undefined;
  const prefix = config?.prefix || undefined;
  const error = allErrors[field.code];

  const handleFieldChange = (code: string, newValue: string | string[] | null) => {
    onChange(code, newValue);
  };

  const renderInlineField = (inlineField: FormModuleFieldDto) => {
    const inlineValue = allValues[inlineField.code] ?? null;
    return (
      <FormFieldRenderer
        field={inlineField}
        value={inlineValue}
        allFields={allFields}
        allValues={allValues}
        allErrors={allErrors}
        showQuestionCode={true}
        isConditionalChild={true}
        onChange={onChange}
        onFileUpload={onFileUpload}
      />
    );
  };

  switch (field.fieldType.toLowerCase()) {
    case 'text':
    case 'postcode':
    case 'address':
      if (suffix || prefix) {
        return (
          <GovUKInputWithSuffix
            id={field.code}
            name={field.code}
            label={field.label}
            suffix={suffix}
            prefix={prefix}
            hint={field.helpText ?? undefined}
            error={error}
            value={stringValue}
            width={config?.width as '5' | '10' | '20' || '5'}
            questionCode={showQuestionCode ? field.code : undefined}
            onChange={(e: React.ChangeEvent<HTMLInputElement>) => handleFieldChange(field.code, e.target.value)}
          />
        );
      }
      return (
        <GovUKFieldset.Input
          id={field.code}
          name={field.code}
          label={field.label}
          labelSize="s"
          hint={field.helpText ?? undefined}
          error={error}
          value={stringValue}
          questionCode={showQuestionCode ? field.code : undefined}
          onChange={(e: React.ChangeEvent<HTMLInputElement>) => handleFieldChange(field.code, e.target.value)}
        />
      );

    case 'date':
      return (
        <GovUKDateField
          id={field.code}
          legend={field.label}
          questionCode={showQuestionCode ? field.code : undefined}
          value={stringValue}
          hint={field.helpText ?? undefined}
          error={error}
          required={field.isRequired}
          allowFuture={config?.dateRule !== 'not_in_future'}
          disallowPast={config?.dateRule === 'not_in_past'}
          onChange={(dateString: string) => handleFieldChange(field.code, dateString)}
        />
      );

    case 'number':
      if (suffix || prefix) {
        return (
          <GovUKInputWithSuffix
            id={field.code}
            name={field.code}
            label={field.label}
            suffix={suffix}
            prefix={prefix}
            hint={field.helpText ?? undefined}
            error={error}
            value={stringValue}
            type="number"
            width={config?.width as '5' | '10' | '20' || '5'}
            questionCode={showQuestionCode ? field.code : undefined}
            onChange={(e: React.ChangeEvent<HTMLInputElement>) => handleFieldChange(field.code, e.target.value)}
          />
        );
      }
      return (
        <GovUKFieldset.Input
          id={field.code}
          name={field.code}
          label={field.label}
          labelSize="s"
          type="number"
          hint={field.helpText ?? undefined}
          error={error}
          value={stringValue}
          questionCode={showQuestionCode ? field.code : undefined}
          width="5"
          onChange={(e: React.ChangeEvent<HTMLInputElement>) => handleFieldChange(field.code, e.target.value)}
        />
      );

    case 'textarea':
      return (
        <GovUKFieldset.Textarea
          id={field.code}
          name={field.code}
          label={field.label}
          labelSize="s"
          hint={field.helpText ?? undefined}
          error={error}
          value={stringValue}
          questionCode={showQuestionCode ? field.code : undefined}
          onChange={(e: React.ChangeEvent<HTMLTextAreaElement>) => handleFieldChange(field.code, e.target.value)}
        />
      );

    case 'radio': {
      // Find inline conditional fields for this radio and sort by displayOrder
      const inlineFields = allFields
        .filter((f) => {
          const rules = parseConditionalRules(f.conditionalRules);
          if (!rules?.displayInline) return false;
          
          // Check simple fieldKey condition
          if (rules.showWhen?.fieldKey === field.code) return true;
          
          // Check allOf conditions - find fields where the last condition matches this field
          if (rules.showWhen?.allOf && Array.isArray(rules.showWhen.allOf)) {
            const lastCondition = rules.showWhen.allOf[rules.showWhen.allOf.length - 1];
            return lastCondition?.fieldKey === field.code;
          }
          
          return false;
        })
        .sort((a, b) => a.displayOrder - b.displayOrder);

      // Group inline fields by their group configuration
      // Only group fields if there are 2+ fields in the same group
      const groupInlineFieldsWithMinTwo = (fields: FormModuleFieldDto[]) => {
        const tempGroups: Record<string, FormModuleFieldDto[]> = {};
        const ungrouped: FormModuleFieldDto[] = [];
        
        // First pass: collect all fields by group
        fields.forEach((f) => {
          const fieldConfig = parseConfiguration(f.configuration);
          if (fieldConfig?.group) {
            if (!tempGroups[fieldConfig.group]) {
              tempGroups[fieldConfig.group] = [];
            }
            tempGroups[fieldConfig.group].push(f);
          } else {
            ungrouped.push(f);
          }
        });
        
        // Second pass: only keep groups with 2+ fields, move single-field groups to ungrouped
        const groups: Record<string, FormModuleFieldDto[]> = {};
        Object.entries(tempGroups).forEach(([groupName, groupFields]) => {
          if (groupFields.length >= 2) {
            groups[groupName] = groupFields;
          } else {
            ungrouped.push(...groupFields);
          }
        });
        
        return { groups, ungrouped };
      };

      const renderGroupedFields = (groupFields: FormModuleFieldDto[], groupName: string) => {
        const firstField = groupFields[0];
        const firstConfig = parseConfiguration(firstField.configuration);
        const groupLabel = firstConfig?.groupLabel || `Enter values for ${groupName}`;
        const groupHint = firstConfig?.groupHint;

        // Number fields (MI breakdowns e.g. ethnicity/sex/age) use compact table layout
        if (groupFields.every((f) => f.fieldType.toLowerCase() === 'number')) {
          return (
            <GovUKGroupedInputs
              key={groupName}
              legend={groupLabel}
              hint={groupHint}
              questionCode={showQuestionCode ? groupName : undefined}
              items={groupFields.map((f) => {
                const fConfig = parseConfiguration(f.configuration);
                return {
                  code: f.code,
                  label: f.label,
                  value: (allValues[f.code] as string) ?? '',
                  suffix: fConfig?.suffix,
                };
              })}
              onChange={(code, val) => onChange(code, val)}
            />
          );
        }

        // All other types (currency, text, select, textarea, etc.) render full-width
        return (
          <div key={groupName}>
            <div style={{ marginBottom: '8px' }}>
              <p className="govuk-label govuk-label--s" style={{ margin: 0 }}>
                {showQuestionCode && <span className="govuk-visually-hidden">Question ID: {groupName}: </span>}
                {groupLabel}
              </p>
            </div>
            {groupHint && <div className="govuk-hint">{groupHint}</div>}
            {groupFields.map((f) => (
              <div key={f.id}>{renderInlineField(f)}</div>
            ))}
          </div>
        );
      };


      return (
        <GovUKFieldset.RadioGroup
          name={field.code}
          legend={field.label}
          legendSize="s"
          hint={field.helpText ?? undefined}
          error={error}
          size={config?.size}
          options={field.options.map((opt) => {
            // Find all inline fields for this option
            const optionInlineFields = inlineFields.filter((f) => {
              const rules = parseConditionalRules(f.conditionalRules);
              return rules?.parentOption === opt.value;
            });

            // Group the inline fields
            const { groups, ungrouped } = groupInlineFieldsWithMinTwo(optionInlineFields);

            // Render grouped and ungrouped fields
            const conditionalContent = optionInlineFields.length > 0 && stringValue === opt.value ? (
              <div>
                {/* Render grouped fields */}
                {Object.entries(groups).map(([groupName, groupFields]) => 
                  renderGroupedFields(groupFields, groupName)
                )}
                {/* Render ungrouped fields */}
                {ungrouped.map((inlineField) => (
                  <div key={inlineField.id}>
                    {renderInlineField(inlineField)}
                  </div>
                ))}
              </div>
            ) : undefined;

            return {
              value: opt.value,
              text: opt.label,
              conditionalContent,
            };
          })}
          value={stringValue}
          questionCode={showQuestionCode ? field.code : undefined}
          onChange={(val: string) => handleFieldChange(field.code, val)}
        />
      );
    }

    case 'checkbox': {
      const exclusiveOptions = config?.exclusiveOptions || [];
      
      // Don't render inline fields inside checkbox options - they will be rendered
      // as regular conditional children in the main field loop with full width
      // This ensures question codes align with the parent field's question code
      const inlineFields: FormModuleFieldDto[] = [];

      // Group inline fields by their group configuration
      // Only group fields if there are 2+ fields in the same group
      const groupInlineFieldsForCheckbox = (fields: FormModuleFieldDto[]) => {
        const tempGroups: Record<string, FormModuleFieldDto[]> = {};
        const ungrouped: FormModuleFieldDto[] = [];
        
        // First pass: collect all fields by group
        fields.forEach((f) => {
          const fieldConfig = parseConfiguration(f.configuration);
          if (fieldConfig?.group) {
            if (!tempGroups[fieldConfig.group]) {
              tempGroups[fieldConfig.group] = [];
            }
            tempGroups[fieldConfig.group].push(f);
          } else {
            ungrouped.push(f);
          }
        });
        
        // Second pass: only keep groups with 2+ fields, move single-field groups to ungrouped
        const groups: Record<string, FormModuleFieldDto[]> = {};
        Object.entries(tempGroups).forEach(([groupName, groupFields]) => {
          if (groupFields.length >= 2) {
            groups[groupName] = groupFields;
          } else {
            ungrouped.push(...groupFields);
          }
        });
        
        return { groups, ungrouped };
      };

      const renderGroupedFieldsForCheckbox = (groupFields: FormModuleFieldDto[], groupName: string) => {
        const firstField = groupFields[0];
        const firstConfig = parseConfiguration(firstField.configuration);
        const groupLabel = firstConfig?.groupLabel || `Enter values for ${groupName}`;
        const groupHint = firstConfig?.groupHint;
        
        return (
          <GovUKGroupedInputs
            key={groupName}
            legend={groupLabel}
            hint={groupHint}
            questionCode={groupName}
            items={groupFields.map((f) => {
              const fConfig = parseConfiguration(f.configuration);
              return {
                code: f.code,
                label: f.label,
                value: (allValues[f.code] as string) ?? '',
                suffix: fConfig?.suffix,
              };
            })}
            onChange={(code, val) => onChange(code, val)}
          />
        );
      };

      return (
        <GovUKCheckbox
          id={field.code}
          name={field.code}
          legend={field.label}
          legendSize="s"
          hint={field.helpText ?? undefined}
          error={error}
          size={config?.size}
          options={field.options.map((opt) => {
            // Find all inline fields for this option
            const optionInlineFields = inlineFields.filter((f) => {
              const rules = parseConditionalRules(f.conditionalRules);
              return rules?.parentOption === opt.value;
            });

            // Group the inline fields
            const { groups, ungrouped } = groupInlineFieldsForCheckbox(optionInlineFields);

            // Render grouped and ungrouped fields
            const conditionalContent = optionInlineFields.length > 0 && arrayValue.includes(opt.value) ? (
              <div>
                {/* Render grouped fields */}
                {Object.entries(groups).map(([groupName, groupFields]) => 
                  renderGroupedFieldsForCheckbox(groupFields as FormModuleFieldDto[], groupName)
                )}
                {/* Render ungrouped fields */}
                {ungrouped.map((inlineField: FormModuleFieldDto) => (
                  <div key={inlineField.id}>
                    {renderInlineField(inlineField)}
                  </div>
                ))}
              </div>
            ) : undefined;

            return {
              value: opt.value,
              label: opt.label,
              exclusive: exclusiveOptions.includes(opt.value),
              conditionalContent,
            };
          })}
          value={arrayValue}
          questionCode={showQuestionCode ? field.code : undefined}
          onChange={(values: (string | number)[]) => handleFieldChange(field.code, values.map(String))}
        />
      );
    }

    case 'select':
      return (
        <GovUKFieldset.Select
          id={field.code}
          name={field.code}
          label={field.label}
          labelSize="s"
          hint={field.helpText ?? undefined}
          error={error}
          options={[
            { value: '', label: 'Select an option' },
            ...field.options.map((opt) => ({
              value: opt.value,
              label: opt.label,
            })),
          ]}
          value={stringValue}
          questionCode={showQuestionCode ? field.code : undefined}
          onChange={(e: React.ChangeEvent<HTMLSelectElement>) => handleFieldChange(field.code, e.target.value)}
        />
      );

    case 'currency':
      return (
        <GovUKInputWithSuffix
          id={field.code}
          name={field.code}
          label={field.label}
          prefix={prefix ?? '£'}
          hint={field.helpText ?? undefined}
          error={error}
          value={stringValue}
          type="number"
          formatThousands
          width={config?.width as '5' | '10' | '20' || '10'}
          questionCode={showQuestionCode ? field.code : undefined}
          onChange={(e: React.ChangeEvent<HTMLInputElement>) => handleFieldChange(field.code, e.target.value)}
        />
      );

    case 'read_only':
      return (
        <div className="govuk-form-group" id={field.code}>
          <p className="govuk-label govuk-label--s" style={{ marginBottom: '4px' }}>
            {showQuestionCode && <span className="govuk-visually-hidden">Question ID: {field.code}: </span>}
            {field.label}
          </p>
          {field.helpText && <div className="govuk-hint">{field.helpText}</div>}
          <div className="govuk-inset-text" style={{ marginTop: '8px', padding: '10px 15px' }}>
            <span className="govuk-body">{stringValue || '—'}</span>
          </div>
        </div>
      );

    case 'calculation': {
      const formula = config?.formula ?? '';
      const computedValue = computeFormula(formula, allValues);
      const displayValue = computedValue !== null ? `£${computedValue.toLocaleString('en-GB', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}` : '—';
      return (
        <div className="govuk-form-group" id={field.code}>
          <p className="govuk-label govuk-label--s" style={{ marginBottom: '4px' }}>
            {showQuestionCode && <span className="govuk-visually-hidden">Question ID: {field.code}: </span>}
            {field.label}
            {field.helpText && <span className="govuk-visually-hidden"> ({field.helpText})</span>}
          </p>
          <div className="govuk-inset-text" style={{ marginTop: '8px', padding: '10px 15px', background: '#f3f2f1' }}>
            <span className="govuk-body-l" style={{ fontWeight: 700 }}>
              {displayValue}
            </span>
            <span className="govuk-caption-s" style={{ display: 'block', marginTop: '4px' }}>
              Calculated automatically
            </span>
          </div>
        </div>
      );
    }

    case 'file':
      return (
        <GovUKFileUpload
          id={field.code}
          name={field.code}
          label={field.label}
          hint={field.helpText ?? undefined}
          fileName={stringValue}
          questionCode={showQuestionCode ? field.code : undefined}
          onChange={onFileUpload ? (file: File | null) => {
            if (file) {
              onFileUpload(field.code, file);
            }
          } : undefined}
        />
      );

    default:
      return (
        <GovUKFieldset.Input
          id={field.code}
          name={field.code}
          label={field.label}
          labelSize="s"
          hint={field.helpText ?? undefined}
          value={stringValue}
          questionCode={showQuestionCode ? field.code : undefined}
          onChange={(e: React.ChangeEvent<HTMLInputElement>) => handleFieldChange(field.code, e.target.value)}
        />
      );
  }
};

export default FormFieldRenderer;
