import React, { useEffect, useRef } from 'react';

interface ErrorItem {
  targetName: string;
  text: string;
}

interface GovUKErrorSummaryProps {
  errors: ErrorItem[];
  heading?: string;
}

const GovUKErrorSummary = ({ errors, heading = 'There is a problem' }: GovUKErrorSummaryProps): React.ReactElement | null => {
  const ref = useRef<HTMLDivElement>(null);

  useEffect(() => {
    ref.current?.focus();
  }, []);

  if (errors.length === 0) return null;

  return (
    <div className="govuk-error-summary" tabIndex={-1} ref={ref} data-module="govuk-error-summary">
      <div role="alert">
        <h2 className="govuk-error-summary__title">{heading}</h2>
        <div className="govuk-error-summary__body">
          <ul className="govuk-list govuk-error-summary__list">
            {errors.map((error) => (
              <li key={error.targetName}>
                <a href={`#${error.targetName}`}>{error.text}</a>
              </li>
            ))}
          </ul>
        </div>
      </div>
    </div>
  );
};

export default GovUKErrorSummary;
