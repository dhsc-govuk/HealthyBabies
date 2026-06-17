import React from 'react';
import { GovUKHeaderLogo } from '../../Logos';
import { GovUKSkipLink } from '../index';
import LayoutFooter from '../../../layouts/General/LayoutFooter';

interface NotAuthorisedProps {
  goBack: (path: number) => void;
}

const NotAuthorised = ({ goBack }: NotAuthorisedProps): React.ReactElement => {
  return (
    <div className="signin-page">
      <GovUKSkipLink />
      <header className="govuk-header" data-module="govuk-header">
        <div className="govuk-header__container govuk-width-container">
          <div className="govuk-header__logo">
            <a href="https://www.gov.uk" className="govuk-header__link govuk-header__link--homepage">
              <GovUKHeaderLogo />
            </a>
          </div>
        </div>
      </header>
      <main className="govuk-main-wrapper" id="main-content" role="main">
        <div className="govuk-width-container" style={{ textAlign: 'center', display: 'flex', flexDirection: 'column', alignItems: 'center', justifyContent: 'center', minHeight: '40vh' }}>
          <h1 className="govuk-heading-xl" style={{ marginBottom: '10px' }}>403</h1>
          <h2 className="govuk-heading-l">Not Authorised</h2>
          <p className="govuk-body">You do not have permission to access this page.</p>
          <button
            type="button"
            className="govuk-button"
            onClick={() => goBack(-1)}
          >
            Go Back
          </button>
        </div>
      </main>
      <LayoutFooter />
    </div>
  );
};

export default NotAuthorised;
