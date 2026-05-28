import React from 'react';

interface Props {
  phase?: 'alpha' | 'beta';
  feedbackUrl?: string;
}

const GovUKPhaseBanner = ({ 
  phase = 'beta', 
  feedbackUrl = '#'
}: Props): React.ReactElement => {
  const phaseLabel = phase.charAt(0).toUpperCase() + phase.slice(1);
  
  return (
    <div className="govuk-phase-banner">
      <p className="govuk-phase-banner__content">
        <strong className="govuk-tag govuk-phase-banner__content__tag">
          {phaseLabel}
        </strong>
        <span className="govuk-phase-banner__text">
          This is a new service – your{' '}
          <a className="govuk-link" href={feedbackUrl}>feedback</a>{' '}
          will help us to improve it.
        </span>
      </p>
    </div>
  );
};

export default GovUKPhaseBanner;
