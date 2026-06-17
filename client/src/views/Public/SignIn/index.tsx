import { useState, useEffect } from 'react';
import usePageTitle from '../../../hooks/usePageTitle';
import { useNavigate, Link } from 'react-router-dom';
import { roleToAreaMap, useAuthProvider } from '../../../components/AuthProvider';
import { GovUKHeaderLogo } from '../../../components/Logos';
import { GovUKBreadcrumbs, GovUKButton, GovUKSkipLink } from '../../../components/GovUKComponents';
import LayoutFooter from '../../../layouts/General/LayoutFooter';
import './styles.css';

const ArrowIcon = () => (
  <svg className="govuk-button__start-icon" xmlns="http://www.w3.org/2000/svg" width="17.5" height="19" viewBox="0 0 33 40" aria-hidden="true" focusable="false">
    <path fill="currentColor" d="M0 0h13l20 20-20 20H0l20-20z" />
  </svg>
);

const SignIn = () => {
  usePageTitle('Sign in');
  const navigate = useNavigate();
  const { signIn, userRole } = useAuthProvider();
  const [signingIn, setSigningIn] = useState<boolean>(false);

  useEffect(() => {
    if (userRole) {
      const area = roleToAreaMap(userRole);
      return navigate(`/${area}/home`);
    }
  }, [userRole, navigate]);

  const handleSignIn = () => {
    setSigningIn(true);
    signIn();
  };

  const breadcrumbItems = [
    { label: 'Home', href: 'https://www.gov.uk' },
    { label: 'Childcare and parenting', href: 'https://www.gov.uk/childcare-parenting' },
  ];

  return (
    <div className="signin-page">
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

      {/* Main Content */}
      <main className="signin-page__main" id="main-content" role="main">
        <div className="govuk-width-container">
          {/* Breadcrumbs */}
          <GovUKBreadcrumbs items={breadcrumbItems} />
          <div className="govuk-main-wrapper" style={{ paddingTop: '20px' }}>
            <h1 className="govuk-heading-xl">Report data on Best Start Family Hubs and Healthy Babies</h1>

            <p className="govuk-body">
              Use this service to report and access data for the Best Start Family Hubs and Healthy Babies programme. You need an account to use this service.
            </p>

            <p className="govuk-body">If you work for a local authority, sign in to:</p>

            <ul className="govuk-list govuk-list--bullet">
              <li>manage your account</li>
              <li>report data</li>
              <li>view trends in your local authority</li>
            </ul>

            <p className="govuk-body">If you work for the DHSC or DfE, sign in to:</p>

            <ul className="govuk-list govuk-list--bullet">
              <li>manage user accounts, data and guidance</li>
              <li>track data collection from local authorities</li>
              <li>access reported data and view trends</li>
            </ul>

            <p className="govuk-body">
              By using this service, you acknowledge and agree with the{' '}
              <Link to="/acceptable-use-policy" className="govuk-link">
                acceptable use policy
              </Link>
              .
            </p>

            <GovUKButton start isLoading={signingIn} onClick={handleSignIn} data-qa="signin-button">
              Sign in
              <ArrowIcon />
            </GovUKButton>

            <h2 className="govuk-heading-m">If you cannot sign in to your account</h2>
            <p className="govuk-body">
              If you have problems signing in to your account,{' '}
              <Link to="/help" className="govuk-link">
                read our help guidance
              </Link>
              .
            </p>

            <h2 className="govuk-heading-m">If you don't have an account</h2>
            <p className="govuk-body">
              Only eligible staff from DHSC, DfE, and local authorities working on the Best Start Family Hubs and Healthy Babies programme can use this service.
            </p>
            <p className="govuk-body">
              To request an account,{' '}
              <a href="mailto:support@familyhubs.gov.uk" className="govuk-link">
                contact us
              </a>
              .
            </p>
          </div>
        </div>
      </main>

      {/* Footer */}
      <LayoutFooter />
    </div>
  );
};

export default SignIn;
