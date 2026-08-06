export const FINANCIAL_DATA_ALLOCATION_MODULE_ENTITY = 'FINANCIAL_DATA_ALLOCATION_MODULE';
export const FINANCIAL_DATA_ALLOCATION_YEAR_ENTITY = 'FINANCIAL_DATA_ALLOCATION_YEAR';
export const FINANCIAL_DATA_ROLLOVER_MODULE_ENTITY = 'FINANCIAL_DATA_ROLLOVER_MODULE';
export const FINANCIAL_DATA_ROLLOVER_YEAR_ENTITY = 'FINANCIAL_DATA_ROLLOVER_YEAR';

export interface FinancialModule {
  key: string;
  label: string;
}

export type FormValues = Record<string, Record<string, string>>;

export const formatCurrency = (value: number): string =>
  `£${value.toLocaleString('en-GB', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`;

export const parseAmount = (value: string): number => {
  const cleaned = value.replace(/[^0-9.]/g, '');
  const parsed = parseFloat(cleaned);
  return isNaN(parsed) ? 0 : parsed;
};