export interface DateValidationResult {
  error?: string;
  errorFields: { day: boolean; month: boolean; year: boolean };
}

export interface DateValidationOptions {
  allowFuture?: boolean;
  disallowPast?: boolean;
  legendText?: string;
}

function capitalise(str: string): string {
  return str.charAt(0).toUpperCase() + str.slice(1);
}

export function validateDateParts(
  day: string,
  month: string,
  year: string,
  options: DateValidationOptions = {},
): DateValidationResult {
  const { allowFuture = false, disallowPast = false, legendText = 'date' } = options;
  const clear: DateValidationResult = { errorFields: { day: false, month: false, year: false } };

  if (!day && !month && !year) return clear;

  const missing: string[] = [];
  if (!day) missing.push('a day');
  if (!month) missing.push('a month');
  if (!year) missing.push('a year');
  if (missing.length > 0) {
    return {
      error: `${capitalise(legendText)} must include ${missing.join(' and ')}`,
      errorFields: { day: !day, month: !month, year: !year },
    };
  }

  if (!/^\d+$/.test(day)) return { error: 'Enter a day as a number', errorFields: { day: true, month: false, year: false } };
  if (!/^\d+$/.test(month)) return { error: 'Enter a month as a number', errorFields: { day: false, month: true, year: false } };
  if (!/^\d+$/.test(year)) return { error: 'Enter a year as a number', errorFields: { day: false, month: false, year: true } };

  const dayNum = parseInt(day, 10);
  const monthNum = parseInt(month, 10);
  const yearNum = parseInt(year, 10);

  if (monthNum < 1 || monthNum > 12) {
    return { error: 'Month must be between 1 and 12', errorFields: { day: false, month: true, year: false } };
  }
  if (dayNum < 1 || dayNum > 31) {
    return { error: 'Day must be between 1 and 31', errorFields: { day: true, month: false, year: false } };
  }
  if (year.length !== 4 || yearNum < 1000) {
    return { error: 'Year must include 4 numbers', errorFields: { day: false, month: false, year: true } };
  }

  // Calendar validity — uses JS Date rollover detection to catch invalid combos (e.g. Feb 30, leap-year Feb 29)
  const date = new Date(yearNum, monthNum - 1, dayNum);
  if (date.getFullYear() !== yearNum || date.getMonth() !== monthNum - 1 || date.getDate() !== dayNum) {
    return { error: `Enter a real ${legendText}`, errorFields: { day: true, month: true, year: false } };
  }

  if (!allowFuture) {
    const today = new Date();
    today.setHours(0, 0, 0, 0);
    if (date > today) {
      return {
        error: `${capitalise(legendText)} must be today or in the past`,
        errorFields: { day: false, month: false, year: false },
      };
    }
  }

  if (disallowPast) {
    const today = new Date();
    today.setHours(0, 0, 0, 0);
    if (date < today) {
      return {
        error: `${capitalise(legendText)} must be today or in the future`,
        errorFields: { day: false, month: false, year: false },
      };
    }
  }

  return clear;
}
