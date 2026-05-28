import React from 'react';

export interface SummaryCardAction {
  label: string;
  onClick: () => void;
  visuallyHiddenText?: string;
}

interface GovUKSummaryCardProps {
  title?: React.ReactNode;
  actions?: SummaryCardAction[];
  children: React.ReactNode;
}

/**
 * GOV.UK Summary card component.
 * Uses native govuk-frontend classes (.govuk-summary-card) so styling comes
 * directly from the Design System. See https://design-system.service.gov.uk/components/summary-list/#summary-cards
 */
function GovUKSummaryCard({ title, actions, children }: GovUKSummaryCardProps): React.ReactElement {
  return (
    <div className="govuk-summary-card">
      <div className="govuk-summary-card__title-wrapper">
        {title !== undefined && <h2 className="govuk-summary-card__title">{title}</h2>}
        {actions && actions.length > 0 && (
          <ul className="govuk-summary-card__actions">
            {actions.map((action, index) => (
              <li className="govuk-summary-card__action" key={index}>
                <a
                  href="#"
                  className="govuk-link"
                  onClick={(e) => {
                    e.preventDefault();
                    action.onClick();
                  }}>
                  {action.label}
                  {action.visuallyHiddenText && <span className="govuk-visually-hidden"> {action.visuallyHiddenText}</span>}
                </a>
              </li>
            ))}
          </ul>
        )}
      </div>
      <div className="govuk-summary-card__content">{children}</div>
    </div>
  );
}

export default GovUKSummaryCard;
