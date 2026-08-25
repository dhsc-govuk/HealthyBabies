import React from 'react';
import GovUKLinkButton from '../GovUKLinkButton';
import './styles.css';

export interface GovUKSummaryTableRow {
  label: string;
  value: React.ReactNode;
  changeHiddenText?: string;
}

export interface GovUKSummaryTableAction {
  label: string;
  onClick: () => void;
}

export interface GovUKSummaryTableProps {
  caption?: string;
  captionSize?: 's' | 'm' | 'l' | 'xl';
  /**
   * Exposes the caption to screen readers only. Use when the page already shows an equivalent
   * heading above the table, so the caption names the table without duplicating visible text.
   */
  captionVisuallyHidden?: boolean;
  rows: GovUKSummaryTableRow[];
  onChangeAll?: () => void;
  className?: string;
  variant?: 'card';
  actions?: GovUKSummaryTableAction[];
}

const GovUKSummaryTable = ({
  caption,
  captionSize = 'm',
  captionVisuallyHidden = false,
  rows,
  onChangeAll,
  className,
  variant,
  actions,
}: GovUKSummaryTableProps): React.ReactElement => {
  const showVisibleCaption = Boolean(caption) && !variant && !captionVisuallyHidden;

  const table = (
    <table className={`govuk-table govuk-summary-table${!variant && className ? ` ${className}` : ''}`}>
      <caption className={showVisibleCaption ? `govuk-table__caption govuk-table__caption--${captionSize}` : 'govuk-visually-hidden'}>{caption ?? 'Summary information'}</caption>
      <tbody className="govuk-table__body">
        {rows.map((row, index) => (
          <tr key={index} className="govuk-table__row">
            <th scope="row" className="govuk-table__header">
              {row.label}
            </th>
            <td className={`govuk-table__cell${onChangeAll ? ' govuk-table__cell--numeric' : ''}`}>
              {onChangeAll ? (
                <>
                  <span className="govuk-!-margin-right-4">{row.value}</span>
                  <GovUKLinkButton onClick={onChangeAll}>Change{row.changeHiddenText && <span className="govuk-visually-hidden"> {row.changeHiddenText}</span>}</GovUKLinkButton>
                </>
              ) : (
                row.value
              )}
            </td>
          </tr>
        ))}
      </tbody>
    </table>
  );

  if (variant === 'card') {
    return (
      <div className={`govuk-summary-table-card${className ? ` ${className}` : ''}`}>
        <div className="govuk-summary-table-card__header">
          {caption && !captionVisuallyHidden && <span>{caption}</span>}
          {actions && actions.length > 0 && (
            <div className="govuk-summary-table-card__actions">
              {actions.map((action, i) => (
                <React.Fragment key={i}>
                  {i > 0 && (
                    <span className="govuk-summary-table-card__actions-separator" aria-hidden="true">
                      |
                    </span>
                  )}
                  <GovUKLinkButton onClick={action.onClick}>{action.label}</GovUKLinkButton>
                </React.Fragment>
              ))}
            </div>
          )}
        </div>
        <div className="govuk-summary-table-card__content">{table}</div>
      </div>
    );
  }

  return table;
};

export default GovUKSummaryTable;
