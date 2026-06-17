import React from 'react';

interface GovUKInsetTextProps {
  children: React.ReactNode;
  className?: string;
}

function GovUKInsetText({ children, className = '' }: GovUKInsetTextProps): React.ReactElement {
  return (
    <div className={`govuk-inset-text ${className}`.trim()}>
      {children}
    </div>
  );
}

export default GovUKInsetText;
