import React from 'react';

interface Props {
  phase?: 'alpha' | 'beta';
  feedbackUrl?: string;
}

const GovUKPhaseBanner = ({ 
  phase = 'beta', 
  feedbackUrl = 'https://forms.office.com/Pages/ResponsePage.aspx?id=MIwnYaiRMUyMH-9N6Jc6HAIrcSJBJtRJtmrT9QlbA9tUNTdOVVkxWldGVU5aNVRZRDZSMTlLSlhJOCQlQCN0PWcu'
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
          <a className="govuk-link" href={feedbackUrl} target='_blank'>feedback</a>{' '}
          will help us to improve it.
        </span>
      </p>
    </div>
  );
};

export default GovUKPhaseBanner;
