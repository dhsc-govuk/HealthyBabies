import { FormModuleFieldDto, FieldConfiguration } from './queries';

export interface ValidationError {
  fieldCode: string;
  message: string;
}

export interface ValidationResult {
  isValid: boolean;
  errors: Record<string, string>;
  errorSummary: ValidationError[];
}

const parseConfiguration = (configJson: string | null | undefined): FieldConfiguration | null => {
  if (!configJson) return null;
  try {
    return JSON.parse(configJson) as FieldConfiguration;
  } catch {
    return null;
  }
};

/**
 * Validates a single field value against its configuration rules
 */
export const validateFieldValue = (
  field: FormModuleFieldDto,
  value: string | string[] | null,
  allValues: Record<string, string | string[] | null>,
  allFields: FormModuleFieldDto[]
): string | null => {
  const config = parseConfiguration(field.configuration);
  const stringValue = typeof value === 'string' ? value : '';
  const numValue = parseFloat(stringValue);

  // Skip validation if field is empty and not required
  if (!stringValue && !field.isRequired) {
    return null;
  }

  // For number fields, validate min/max
  if (field.fieldType.toLowerCase() === 'number' && stringValue) {
    if (isNaN(numValue)) {
      return 'Enter a valid number';
    }

    if (config?.min !== undefined && numValue < config.min) {
      return `Enter a number ${config.min} or more`;
    }

    if (config?.max !== undefined && numValue > config.max) {
      return `Enter a number ${config.max} or less`;
    }
  }

  return null;
};

/**
 * Validates that grouped fields don't exceed a maximum sum
 */
export const validateGroupSum = (
  fields: FormModuleFieldDto[],
  values: Record<string, string | string[] | null>,
  sumGroup: string,
  maxSumField: string
): ValidationError[] => {
  const errors: ValidationError[] = [];
  
  // Get the max value from the reference field
  const maxValue = parseFloat(values[maxSumField] as string || '0');
  if (isNaN(maxValue) || maxValue === 0) {
    return errors; // Can't validate if max value is not set
  }

  // Find all fields in this sum group
  const groupFields = fields.filter((f) => {
    const config = parseConfiguration(f.configuration);
    return config?.sumGroup === sumGroup;
  });

  // Calculate the sum
  let sum = 0;
  groupFields.forEach((f) => {
    const val = parseFloat(values[f.code] as string || '0');
    if (!isNaN(val)) {
      sum += val;
    }
  });

  // Check if sum exceeds max
  if (sum > maxValue) {
    const firstField = groupFields[0];
    if (firstField) {
      const config = parseConfiguration(firstField.configuration);
      const groupLabel = config?.groupLabel || sumGroup;
      errors.push({
        fieldCode: sumGroup,
        message: `The total for '${groupLabel}' (${sum}) cannot exceed the value entered for the total (${maxValue})`,
      });
    }
  }

  return errors;
};

/**
 * Validates that grouped fields equal a specific field value
 */
export const validateGroupEquality = (
  fields: FormModuleFieldDto[],
  values: Record<string, string | string[] | null>,
  sumGroup: string,
  mustEqualField: string
): ValidationError[] => {
  const errors: ValidationError[] = [];
  
  // Get the expected value from the reference field
  const expectedValue = parseFloat(values[mustEqualField] as string || '0');
  if (isNaN(expectedValue) || expectedValue === 0) {
    return errors; // Can't validate if expected value is not set
  }

  // Find all fields in this sum group
  const groupFields = fields.filter((f) => {
    const config = parseConfiguration(f.configuration);
    return config?.sumGroup === sumGroup;
  });

  // Calculate the sum
  let sum = 0;
  groupFields.forEach((f) => {
    const val = parseFloat(values[f.code] as string || '0');
    if (!isNaN(val)) {
      sum += val;
    }
  });

  // Check if sum equals expected
  if (sum !== expectedValue) {
    const firstField = groupFields[0];
    if (firstField) {
      const config = parseConfiguration(firstField.configuration);
      const groupLabel = config?.groupLabel || sumGroup;
      errors.push({
        fieldCode: sumGroup,
        message: `The total for '${groupLabel}' (${sum}) must equal the value entered for the total (${expectedValue})`,
      });
    }
  }

  return errors;
};

/**
 * Validates all fields and returns a validation result
 */
export const validateAllFields = (
  fields: FormModuleFieldDto[],
  values: Record<string, string | string[] | null>,
  visibleFieldCodes?: Set<string>
): ValidationResult => {
  const errors: Record<string, string> = {};
  const errorSummary: ValidationError[] = [];
  const processedSumGroups = new Set<string>();

  fields.forEach((field) => {
    // Skip validation for hidden fields
    if (visibleFieldCodes && !visibleFieldCodes.has(field.code)) {
      return;
    }

    const value = values[field.code];
    const config = parseConfiguration(field.configuration);

    // Validate individual field
    const fieldError = validateFieldValue(field, value, values, fields);
    if (fieldError) {
      errors[field.code] = fieldError;
      errorSummary.push({ fieldCode: field.code, message: fieldError });
    }

    // Check for sum group validation
    if (config?.sumGroup && !processedSumGroups.has(config.sumGroup)) {
      processedSumGroups.add(config.sumGroup);

      if (config.maxSumField) {
        const sumErrors = validateGroupSum(fields, values, config.sumGroup, config.maxSumField);
        sumErrors.forEach((err) => {
          errors[err.fieldCode] = err.message;
          errorSummary.push(err);
        });
      }

      if (config.mustEqualField) {
        const equalityErrors = validateGroupEquality(fields, values, config.sumGroup, config.mustEqualField);
        equalityErrors.forEach((err) => {
          errors[err.fieldCode] = err.message;
          errorSummary.push(err);
        });
      }
    }
  });

  return {
    isValid: errorSummary.length === 0,
    errors,
    errorSummary,
  };
};

/**
 * Outcome score validation ranges based on the data map
 */
export const OUTCOME_SCORE_RANGES: Record<string, { min: number; max: number }> = {
  'PHQ-9': { min: 0, max: 27 },
  'GAD-7': { min: 0, max: 21 },
  'SWEMWBS': { min: 7, max: 35 },
  'MORS-SF': { min: 12, max: 60 },
  'ASQ-3': { min: 0, max: 60 },
  'KPCS': { min: 0, max: 45 },
  'PSS': { min: 0, max: 40 },
  'HLE': { min: 0, max: 999999 }, // No validation per data map
  'CPRS-SF': { min: 0, max: 999999 }, // No validation per data map
  'Other': { min: 0, max: 999999 }, // No validation per data map
};

/**
 * Get outcome score range for a specific measure
 */
export const getOutcomeScoreRange = (measureCode: string): { min: number; max: number } | null => {
  const measureMap: Record<string, string> = {
    'PPS06': 'ASQ-3',
    'PPS07': 'CPRS-SF',
    'PPS08': 'GAD-7',
    'PPS09': 'HLE',
    'PPS10': 'KPCS',
    'PPS11': 'MORS-SF',
    'PPS12': 'PHQ-9',
    'PPS13': 'PSS',
    'PPS14': 'SWEMWBS',
    'PPS15': 'Other',
  };

  // Extract the base code (e.g., PPS06 from PPS06_pre)
  const baseCode = measureCode.split('_')[0];
  const measureName = measureMap[baseCode];
  
  if (measureName) {
    return OUTCOME_SCORE_RANGES[measureName] || null;
  }
  
  return null;
};

/**
 * Validates outcome score fields using field configuration from database
 * Falls back to hardcoded ranges if configuration is not available
 */
export const validateOutcomeScore = (
  fieldCode: string,
  value: string | null,
  field?: FormModuleFieldDto
): string | null => {
  if (!value) return null;

  const numValue = parseFloat(value);
  if (isNaN(numValue)) {
    return 'Enter a valid number';
  }

  // First try to get min/max from field configuration (from database)
  if (field) {
    const config = parseConfiguration(field.configuration);
    if (config?.min !== undefined && numValue < config.min) {
      return `Enter a number ${config.min} or more`;
    }
    if (config?.max !== undefined && numValue > config.max) {
      return `Enter a number ${config.max} or less`;
    }
    // If config has min/max defined, we've validated - return null
    if (config?.min !== undefined || config?.max !== undefined) {
      return null;
    }
  }

  // Fallback to hardcoded ranges if no configuration
  const range = getOutcomeScoreRange(fieldCode);
  if (range) {
    if (numValue < range.min) {
      return `Enter a number ${range.min} or more`;
    }
    if (numValue > range.max) {
      return `Enter a number ${range.max} or less`;
    }
  }

  return null;
};

/**
 * Validates breastfeeding rate (percentage 0-100)
 */
export const validateBreastfeedingRate = (value: string | null): string | null => {
  if (!value) return null;

  const numValue = parseFloat(value);
  if (isNaN(numValue)) {
    return 'Enter a valid number';
  }

  if (numValue < 0) {
    return 'Enter a number 0 or more';
  }

  if (numValue > 100) {
    return 'Enter a number 100 or less';
  }

  return null;
};
