import React from 'react';
import { Link } from 'react-router-dom';
import { GeneralLayout } from '../../../layouts';
import { H1, H2, Paragraph } from '../../../components/GovUKComponents';
import usePageTitle from '../../../hooks/usePageTitle';

const Home = (): React.ReactElement => {
  usePageTitle('Home');
  return (
    <GeneralLayout>
      <div style={{ paddingTop: '30px' }}>
        <H1>Report data on Best Start Family Hubs and Healthy Babies</H1>
        <Paragraph>Use this service to report and access data for the Best Start Family Hubs and Healthy Babies programme.</Paragraph>
        <Paragraph>You can:</Paragraph>
        <ul className="govuk-list govuk-list--bullet">
          {/* <li>
            <Link to="/organisation-admin/my-account" className="govuk-link">
              manage your account
            </Link>
          </li> */}
          <li>
            <Link to="/organisation-admin/core-data/delivery-locations" className="govuk-link">
              manage core data
            </Link>
            , like services and delivery locations
          </li>
          <li>
            <Link to="/organisation-admin/submissions" className="govuk-link">
              submit quarterly reports and view previously submitted reports
            </Link>
          </li>
        </ul>
        <H2>Get help using this service</H2>
        <p className="govuk-body">
          <Link to="/organisation-admin/help" className="govuk-link">
            Guidance
          </Link>
          {' '}is available to help you use this service.
        </p>
        <Paragraph>You can also contact us for more help.</Paragraph>
        <p className="govuk-body">
          <strong>Email</strong>
          <br />
          <a href="mailto:healthybabies.dataanddigital@dhsc.gov.uk" className="govuk-link">
            healthybabies.dataanddigital@dhsc.gov.uk
          </a>
        </p>
      </div>
    </GeneralLayout>
  );
};

export default Home;
