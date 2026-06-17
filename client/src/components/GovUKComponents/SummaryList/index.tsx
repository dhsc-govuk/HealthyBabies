import React from 'react';

interface SummaryItem {
  label: string;
  value: string | React.ReactNode;
  onAction?: () => void;
  actionLabel?: string;
  isSubAnswer?: boolean;
}

interface SummaryListProps {
  items: SummaryItem[];
  noBorder?: boolean;
  noOuterBorder?: boolean;
  equalColumns?: boolean;
  halfWidthColumns?: boolean;
}

function SummaryList({ items, noBorder = false, noOuterBorder = false, equalColumns = false, halfWidthColumns = false }: SummaryListProps): React.ReactElement {
  const listClassName = ['govuk-summary-list', noBorder ? 'govuk-summary-list--no-border' : '']
    .filter(Boolean)
    .join(' ');

  const getRowStyle = (index: number, item: SummaryItem): React.CSSProperties => {
    const isLastItem = index === items.length - 1;
    const nextItem = items[index + 1];
    const isFollowedBySubAnswer = nextItem?.isSubAnswer === true;
    const prevItem = items[index - 1];
    const isFirstSubAnswer = item.isSubAnswer && !prevItem?.isSubAnswer;
    
    if (!noOuterBorder) {
      // When not using noOuterBorder, still group sub-answers with parent
      if (isFollowedBySubAnswer) {
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
    
    // Parent followed by sub-answer: no bottom border
    if (isFollowedBySubAnswer) {
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

  const getColumnStyle = (): React.CSSProperties => {
    if (halfWidthColumns) return { width: '50%' };
    if (equalColumns) return { width: '50%' };
    return {};
  };

  const columnStyle = getColumnStyle();

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
