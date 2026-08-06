import React from 'react';
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
  rows: GovUKSummaryTableRow[];
  onChangeAll?: () => void;
  className?: string;
  variant?: 'card';
  actions?: GovUKSummaryTableAction[];
}

const GovUKSummaryTable = ({ caption, captionSize = 'm', rows, onChangeAll, className, variant, actions }: GovUKSummaryTableProps): React.ReactElement => {
  const table = (
    <table className={`govuk-table govuk-summary-table${!variant && className ? ` ${className}` : ''}`}>
      {caption && !variant && (
        <caption className={`govuk-table__caption govuk-table__caption--${captionSize}`}>{caption}</caption>
      )}
      <tbody className="govuk-table__body">
        {rows.map((row, index) => (
          <tr key={index} className="govuk-table__row">
            <th scope="row" className="govuk-table__header">{row.label}</th>
            <td className={`govuk-table__cell${onChangeAll ? ' govuk-table__cell--numeric' : ''}`}>
              {onChangeAll ? (
                <>
                  <span className="govuk-!-margin-right-4">{row.value}</span>
                  <a className="govuk-link" href="#" onClick={(e) => { e.preventDefault(); onChangeAll(); }}>
                    Change{row.changeHiddenText && <span className="govuk-visually-hidden"> {row.changeHiddenText}</span>}
                  </a>
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
          {caption && <span>{caption}</span>}
          {actions && actions.length > 0 && (
            <div className="govuk-summary-table-card__actions">
              {actions.map((action, i) => (
                <React.Fragment key={i}>
                  {i > 0 && <span className="govuk-summary-table-card__actions-separator" aria-hidden="true">|</span>}
                  <a className="govuk-link" href="#" onClick={(e) => { e.preventDefault(); action.onClick(); }}>
                    {action.label}
                  </a>
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
