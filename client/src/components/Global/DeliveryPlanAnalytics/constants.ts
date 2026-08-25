export interface Strand {
  key: string;
  label: string;
  shortLabel: string;
}

/** The seven allocation strands, in the order shown across the profile tabs. */
export const ALLOCATION_STRANDS: Strand[] = [
  { key: 'delivery-grant', label: 'Delivery Grant', shortLabel: 'Delivery grant' },
  { key: 'parenting', label: 'Parenting Support', shortLabel: 'Parenting' },
  { key: 'hle', label: 'Home Learning Environment (HLE)', shortLabel: 'HLE' },
  { key: 'send', label: 'Special Educational Needs and Disability (SEND)', shortLabel: 'SEND' },
  { key: 'pnmh-and-pir', label: 'Perinatal Mental Health and Parent-Infant Relationships', shortLabel: 'PMH/PIR' },
  { key: 'infant-feeding', label: 'Infant Feeding', shortLabel: 'Infant feeding' },
  { key: 'healthy-babies-offer', label: 'Healthy Babies Offer', shortLabel: 'Healthy babies offer' },
];

export const ROLLOVER_STRAND: Strand = {
  key: 'rollover-spend',
  label: 'Rollover spend',
  shortLabel: 'Rollover spend',
};

export const ALLOCATION_YEARS = ['2026-27', '2027-28', '2028-29'];
export const ROLLOVER_YEAR = '2025-26';

export const formatMoney = (value: number): string =>
  `£${value.toLocaleString('en-GB', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`;

/** Whole-pound formatting for chart axes and bar labels. */
export const formatWholeMoney = (value: number): string =>
  `£${Math.round(value).toLocaleString('en-GB')}`;

export const formatPercent = (value: number, of = 'total annual allocation'): string => `${value.toFixed(2)}% of ${of}`;
