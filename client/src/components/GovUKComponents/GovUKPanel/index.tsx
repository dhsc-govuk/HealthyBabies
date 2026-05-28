import React from 'react';

interface GovUKPanelProps {
  title: string;
  children?: React.ReactNode;
  className?: string;
}

function GovUKPanel({ title, children, className = '' }: GovUKPanelProps): React.ReactElement {
  return (
    <div className={`govuk-panel govuk-panel--confirmation ${className}`.trim()}>
      <h1 className="govuk-panel__title">
        {title}
      </h1>
      {children && (
        <div className="govuk-panel__body">
          {children}
        </div>
      )}
    </div>
  );
}

export default GovUKPanel;
