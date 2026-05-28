import { useNavigate } from 'react-router-dom';
import usePageTitle from '../../../hooks/usePageTitle';
import { GovUKHeaderLogo } from '../../../components/Logos';
import { GovUKBackLink, GovUKSkipLink } from '../../../components/GovUKComponents';
import LayoutFooter from '../../../layouts/General/LayoutFooter';
import './styles.css';

const AcceptableUsePolicy = () => {
  usePageTitle('Acceptable use policy');
  const navigate = useNavigate();

  const goToSignIn = (e: React.MouseEvent) => {
    e.preventDefault();
    navigate('/sign-in');
  };

  return (
    <div className="acceptable-use-policy-page">
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

      <main className="acceptable-use-policy-page__main" id="main-content" role="main">
        <div className="govuk-width-container">
          <div className="govuk-main-wrapper">
            <GovUKBackLink href="/sign-in">
              Back
            </GovUKBackLink>

            <span className="govuk-caption-xl">Report data on Best Start Family Hubs and Healthy Babies</span>
            <h1 className="govuk-heading-l">Acceptable use policy</h1>

            <div className="govuk-inset-text">By using this service, you acknowledge and agree with this acceptable use policy.</div>

            <h2 className="govuk-heading-m">Applicability</h2>
            <p className="govuk-body">This policy applies to all users of the Report Data on Best Start Family Hubs &amp; Healthy Babies platform.</p>
            <p className="govuk-body">Users include:</p>
            <ul className="govuk-list govuk-list--bullet">
              <li>Local authority staff who log-in to the platform to provide data and understand reporting requirements</li>
              <li>Analysts and data scientists: end users of the data who make use of the platform for analysis and application building.</li>
              <li>
                Engineers and developers: data and platform engineers, software developers and other technical roles (e.g. solution architects) who build data pipelines, platform
                infrastructure and data products.
              </li>
            </ul>
            <p className="govuk-body">Users are either:</p>
            <ul className="govuk-list govuk-list--bullet">
              <li>Internal to DHSC, being staff using DXC laptops and DHSC email addresses.</li>
              <li>External to DHSC, including DfE analysts and local authority users.</li>
            </ul>

            <h2 className="govuk-heading-m">Standard principles</h2>
            <p className="govuk-body">All users MUST:</p>
            <ul className="govuk-list govuk-list--bullet">
              <li>
                Comply with the headline principles of DHSC&rsquo;s Acceptable use of IT policy, namely:
                <ul className="govuk-list govuk-list--bullet">
                  <li>the use of DHSC IT must not break the law, risk bringing the department into disrepute, or place it in a position of liability</li>
                  <li>the use of DHSC IT facilities must not violate any provision set out in this policy, or policy set out by a user&rsquo;s home organization</li>
                  <li>line managers are responsible for ensuring that their staff use IT appropriately, however you remain personally accountable for adhering to the policy</li>
                </ul>
              </li>
              <li>
                Acknowledge that their data access and usage will be monitored for information governance, security and audit purposes. Excessive or misuse of the platform will
                result in access being revoked.
              </li>
              <li>Access the platform within the UK only.</li>
              <li>Only log in with their individual email account and not share account details with others.</li>
            </ul>

            <h2 className="govuk-heading-m">Data governance and security</h2>
            <p className="govuk-body">All users MUST:</p>
            <ul className="govuk-list govuk-list--bullet">
              <li>
                Ensure that data brought onto the platform, via any method, does NOT fall into the following categories:
                <ul className="govuk-list govuk-list--bullet">
                  <li>Data containing individual service user&rsquo;s directly identifiable information such as name, address, postcodes, photos, or bank details.</li>
                  <li>Standard business contact details (e.g. local authority work email addresses, job titles) can be included where necessary for operational purposes.</li>
                </ul>
              </li>
              <li>Any Data classified as SECRET or TOP SECRET.</li>
              <li>
                Take responsibility for notifying DHSC when there are changes to job roles resulting in changes needed to access controls via{' '}
                <a className="govuk-link" href="mailto:healthybabies.dataanddigital@dhsc.gov.uk">
                  healthybabies.dataanddigital@dhsc.gov.uk
                </a>
              </li>
              <li>
                Report any concerns regarding incorrect access controls immediately to{' '}
                <a className="govuk-link" href="mailto:healthybabies.dataanddigital@dhsc.gov.uk">
                  healthybabies.dataanddigital@dhsc.gov.uk
                </a>{' '}
                and engage promptly with the team if they have raised data protection issues with you.
              </li>
              <li>
                Ensure that unpublished data brought onto the platform are:
                <ul className="govuk-list govuk-list--bullet">
                  <li>Covered by an appropriate Data Sharing Agreement, Data Protection Impact Assessment or equivalent</li>
                  <li>Restricted in access to only those with a live project need.</li>
                </ul>
              </li>
            </ul>

            <h2 className="govuk-heading-m">Data sharing and breaches</h2>
            <p className="govuk-body">All users MUST:</p>
            <ol className="govuk-list govuk-list--number">
              <li>
                Ensure that the sharing of data from the platform meets all requirements relating to:
                <ol className="govuk-list govuk-list--number">
                  <li>Relevant DSAs/DPIAs/etc.</li>
                  <li>Data protection legislation.</li>
                  <li>Official statistics requirements.</li>
                </ol>
              </li>
              <li>Take action in the event of a suspected data breach:</li>
              <li>
                Contact DHSC immediately in the event that a data breach is suspected using the{' '}
                <a className="govuk-link" href="mailto:startforlife.dataanddigital@dhsc.gov.uk">
                  startforlife.dataanddigital@dhsc.gov.uk
                </a>{' '}
                mailbox.
              </li>
              <li>Immediately stop work on the affected project until further guidance is received.</li>
            </ol>

            <p className="govuk-body">
              <a href="/sign-in" className="govuk-link" onClick={goToSignIn}>
                Return to the start page
              </a>
            </p>
          </div>
        </div>
      </main>

      <LayoutFooter />
    </div>
  );
};

export default AcceptableUsePolicy;
