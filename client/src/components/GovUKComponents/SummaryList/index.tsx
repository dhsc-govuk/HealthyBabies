import React from 'react';
import './styles.css';

interface SummaryItem {
  label: string;
  value: string | React.ReactNode;
  onAction?: () => void;
  actionLabel?: string;
  isSubAnswer?: boolean;
  /**
   * Heading for a group of sub-answers — a breakdown's groupLabel, standing in for the question
   * the wizard asks as one fieldset. Carries no value of its own; the sub-answers follow it.
   */
  isGroupHeader?: boolean;
}

interface SummaryListProps {
  items: SummaryItem[];
  noBorder?: boolean;
  noOuterBorder?: boolean;
  equalColumns?: boolean;
  halfWidthColumns?: boolean;
}

function SummaryList({ items, noBorder = false, noOuterBorder = false, equalColumns = false, halfWidthColumns = false }: SummaryListProps): React.ReactElement {
  // The 50% column width is applied via this class (see styles.css) rather than an inline
  // style, so govuk-frontend's mobile stacking still wins at 320px / 400% zoom.
  const listClassName = ['govuk-summary-list', noBorder ? 'govuk-summary-list--no-border' : '', equalColumns || halfWidthColumns ? 'app-summary-list--equal-columns' : '']
    .filter(Boolean)
    .join(' ');

  const getRowStyle = (index: number, item: SummaryItem): React.CSSProperties => {
    const isLastItem = index === items.length - 1;
    const nextItem = items[index + 1];
    // A group heading opens the block its sub-answers close, so it never draws a bottom border of
    // its own. Checked explicitly rather than inferred from the next row being a sub-answer, which
    // only happens to agree while every heading is followed by at least one.
    const opensAGroup = item.isGroupHeader === true || nextItem?.isSubAnswer === true;
    const prevItem = items[index - 1];
    const isFirstSubAnswer = item.isSubAnswer && !prevItem?.isSubAnswer;

    if (!noOuterBorder) {
      // When not using noOuterBorder, still group sub-answers with parent
      if (opensAGroup) {
        return { borderBottom: 'none', paddingBottom: 0 };
      }
      if (item.isSubAnswer) {
        return { 
          borderBottom: isLastItem || !nextItem?.isSubAnswer ? undefined : 'none',
          paddingTop: isFirstSubAnswer ? '10px' : '5px',
          paddingBottom: isLastItem || !nextItem?.isSubAnswer ? undefined : '5px'
        };
      }
      return {};
    }
    
    // noOuterBorder mode
    const baseStyle: React.CSSProperties = {
      borderTop: index === 0 ? 'none' : undefined,
      borderLeft: 'none',
      borderRight: 'none',
      paddingLeft: 0,
      paddingRight: 0,
    };
    
    // Parent or group heading followed by sub-answers: no bottom border
    if (opensAGroup) {
      return { ...baseStyle, borderBottom: 'none', paddingBottom: 0 };
    }
    
    // Sub-answer styling
    if (item.isSubAnswer) {
      const isLastSubAnswer = !nextItem?.isSubAnswer;
      return {
        ...baseStyle,
        borderBottom: isLastItem ? 'none' : (isLastSubAnswer ? '1px solid #b1b4b6' : 'none'),
        paddingTop: isFirstSubAnswer ? '10px' : '5px',
        paddingBottom: isLastSubAnswer ? undefined : '5px',
      };
    }
    
    return {
      ...baseStyle,
      borderBottom: isLastItem ? 'none' : '1px solid #b1b4b6',
    };
  };

  // Intentionally empty: column widths come from app-summary-list--equal-columns so they
  // can be overridden by the responsive stacking rules.
  const columnStyle: React.CSSProperties = {};

  return (
    <dl className={listClassName} style={noOuterBorder ? { border: 'none', marginTop: 0 } : {}}>
      {items.map((item, index) => (
        <div className="govuk-summary-list__row" key={index} style={{ ...getRowStyle(index, item), paddingLeft: 0, paddingRight: 0 }}>
          <dt 
            className="govuk-summary-list__key" 
            style={{ 
              ...columnStyle, 
              fontWeight: item.isSubAnswer ? 400 : 700 
            }}
          >
            {item.isSubAnswer ? `— ${item.label}` : item.label}
          </dt>
          <dd className="govuk-summary-list__value" style={columnStyle}>{item.value}</dd>
          {item.onAction && (
            <dd className="govuk-summary-list__actions">
              <button
                type="button"
                className="govuk-link"
                style={{
                  background: 'none',
                  border: 'none',
                  padding: 0,
                  cursor: 'pointer',
                  font: 'inherit',
                  color: 'inherit',
                  textDecoration: 'underline',
                }}
                onClick={() => item.onAction?.()}
              >
                {item.actionLabel || 'Change'}
                <span className="govuk-visually-hidden"> {item.label?.toLowerCase()}</span>
              </button>
            </dd>
          )}
        </div>
      ))}
    </dl>
  );
}

export default SummaryList;
