import React from 'react';

export type StatCardVariant = 'default' | 'highlight' | 'green' | 'yellow';

interface StatCardProps {
  value: string;
  label: string;
  percent?: string;
  sublabel?: string;
  variant?: StatCardVariant;
}

/**
 * A summary statistic tile used on the Financial data and Delivery data tabs.
 * GOV.UK has no native stat card, so this is a lightweight wrapper styled with
 * the GOV.UK colour palette (see tabs/styles.css).
 */
const StatCard = ({ value, label, percent, sublabel, variant = 'default' }: StatCardProps): React.ReactElement => (
  <div className={`la-stat-card la-stat-card--${variant}`}>
    <p className="la-stat-card__value">{value}</p>
    {percent && <p className="la-stat-card__percent">{percent}</p>}
    <p className="la-stat-card__label">{label}</p>
    {sublabel && <p className="la-stat-card__sublabel">{sublabel}</p>}
  </div>
);

export default StatCard;
