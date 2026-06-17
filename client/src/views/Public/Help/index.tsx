import { GovUKHeaderLogo } from '../../../components/Logos';
import { GovUKBackLink, GovUKSkipLink } from '../../../components/GovUKComponents';
import LayoutFooter from '../../../layouts/General/LayoutFooter';
import usePageTitle from '../../../hooks/usePageTitle';
import './styles.css';

const Help = () => {
  usePageTitle('Help with signing in');
  return (
    <div className="help-page">
      <GovUKSkipLink />
      {/* GOV.UK Header */}
      <header className="govuk-header" data-module="govuk-header">
        <div className="govuk-header__container govuk-width-container">
          <div className="govuk-header__logo">
            <a href="https://www.gov.uk" className="govuk-header__link govuk-header__link--homepage">
              <GovUKHeaderLogo />
            </a>
          </div>
        </div>
      </header>

      {/* Service Banner with Title */}
      <section className="help-page__service-banner" aria-label="Service information">
        <div className="govuk-width-container">
          <span className="help-page__service-name">Report data on Best Start Family Hubs and Healthy Babies</span>
        </div>
      </section>

      {/* Phase Banner */}
      <div className="govuk-phase-banner">
        <div className="govuk-width-container">
          <p className="govuk-phase-banner__content">
            <strong className="govuk-tag govuk-phase-banner__content__tag">Beta</strong>
            <span className="govuk-phase-banner__text">
              This is a new service – your <a className="govuk-link" href="mailto:support@familyhubs.gov.uk">feedback</a> will help us to improve it.
            </span>
          </p>
        </div>
      </div>

      {/* Main Content */}
      <main className="help-page__main" id="main-content" role="main">
        <div className="govuk-width-container">
          <div className="govuk-main-wrapper">
            {/* Back Link */}
            <GovUKBackLink href="/sign-in">
              Back
            </GovUKBackLink>

            <h1 className="govuk-heading-xl">
              Get help with signing in to Report data on Best Start Family Hubs and Healthy Babies
            </h1>

            <p className="govuk-body">
              You can get help with signing in to the Report data on Best Start Family Hubs and Healthy Babies if you're having problems.
            </p>

            <h2 className="govuk-heading-m">If you've forgotten your password</h2>
            <p className="govuk-body">
              You can regain access to your account by <a className="govuk-link" href="https://login.microsoftonline.com/common/oauth2/v2.0/authorize?response_type=code&client_id=your-client-id&redirect_uri=your-redirect-uri&scope=openid%20profile%20email&prompt=login">resetting your password</a> if you've forgotten it.
            </p>
            <p className="govuk-body">
              We will ask you for your email registered with this service. If there's an account linked to that email address, we'll send you a link to set a new password.
            </p>
            <p className="govuk-body">
              Check your spam or junk folder if you do not receive the email. You can also request a new link after 1 minute.
            </p>

            <h2 className="govuk-heading-m">If you've forgotten your registered email address</h2>
            <p className="govuk-body">
              Try signing in with your work email address.
            </p>
            <p className="govuk-body">
              If that does not work, <a className="govuk-link" href="mailto:support@familyhubs.gov.uk">contact us</a> and explain your situation. We'll help you identify your email address registered with this service, and regain access to your account.
            </p>
            <p className="govuk-body">
              You may need to confirm your identity before we can tell you which email address is linked to your account.
            </p>

            <h2 className="govuk-heading-m">If you no longer have access to your registered email address</h2>
            <p className="govuk-body">
              <a className="govuk-link" href="mailto:support@familyhubs.gov.uk">Contact us</a> and explain your situation. We'll help you update your details and regain access to your account.
            </p>
            <p className="govuk-body">
              You may need to confirm your identity before we can make changes to your account.
            </p>

            <h2 className="govuk-heading-m">If you've been locked out of your account</h2>
            <p className="govuk-body">
              You will be temporarily locked out of your account if you enter the wrong sign in details too many times. This is a security measure to help keep your account and the service safe.
            </p>
            <p className="govuk-body">
              Your account will automatically unlock after 2 hours. We cannot unlock your account before that.
            </p>

            <h2 className="govuk-heading-m">If you don't have an account</h2>
            <p className="govuk-body">
              Only eligible staff from DHSC, DfE, and local authorities working on the Best Start Family Hubs and Healthy Babies programme can use this service.
            </p>
            <p className="govuk-body">
              To request an account, <a className="govuk-link" href="mailto:support@familyhubs.gov.uk">contact us</a>.
            </p>

            <h2 className="govuk-heading-m">If you still cannot sign in to your account</h2>
            <p className="govuk-body">
              If the problem continues, <a className="govuk-link" href="mailto:support@familyhubs.gov.uk">contact us</a> and explain what's gone wrong. We'll help you get access to your account.
            </p>
          </div>
        </div>
      </main>

      {/* Footer */}
      <LayoutFooter />
    </div>
  );
};

export default Help;
