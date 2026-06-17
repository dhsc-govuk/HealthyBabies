import React from 'react';

interface GovUKWarningTextProps {
  children: React.ReactNode;
  iconFallbackText?: string;
  className?: string;
}

function GovUKWarningText({ 
  children, 
  iconFallbackText = 'Warning',
  className = '' 
}: GovUKWarningTextProps): React.ReactElement {
  return (
    <div className={`govuk-warning-text ${className}`.trim()}>
      <span className="govuk-warning-text__icon" aria-hidden="true">!</span>
      <strong className="govuk-warning-text__text">
        <span className="govuk-visually-hidden">{iconFallbackText}</span>
        {children}
      </strong>
    </div>
  );
}

export default GovUKWarningText;
