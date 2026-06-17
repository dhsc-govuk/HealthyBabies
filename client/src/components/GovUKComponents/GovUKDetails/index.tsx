import React from 'react';

interface GovUKDetailsProps {
  summary: string;
  children: React.ReactNode;
  open?: boolean;
  className?: string;
}

function GovUKDetails({ 
  summary, 
  children, 
  open = false,
  className = '' 
}: GovUKDetailsProps): React.ReactElement {
  return (
    <details className={`govuk-details ${className}`.trim()} open={open}>
      <summary className="govuk-details__summary">
        <span className="govuk-details__summary-text">
          {summary}
        </span>
      </summary>
      <div className="govuk-details__text">
        {children}
      </div>
    </details>
  );
}

export default GovUKDetails;
