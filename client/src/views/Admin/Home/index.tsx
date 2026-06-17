import React from 'react';
import { Link } from 'react-router-dom';
import { GeneralLayout } from '../../../layouts';
import { H1, Paragraph } from '../../../components/GovUKComponents';
import usePageTitle from '../../../hooks/usePageTitle';

const Home = (): React.ReactElement => {
  usePageTitle('Home');
  return (
    <GeneralLayout>
      <div style={{ paddingTop: '30px' }}>
        <H1>Report data on Best Start Family Hubs and Healthy Babies</H1>
      <Paragraph>Use this service to manage and access data for the Best Start Family Hubs and Healthy Babies programme.</Paragraph>
      <Paragraph>You can:</Paragraph>
      <ul className="govuk-list govuk-list--bullet">
        <li>
          <Link to="/admin/submissions" className="govuk-link">
            track and access reported data from local authorities
          </Link>
        </li>
        <li>
          <Link to="/admin/core-data" className="govuk-link">
            download core data for sites and services
          </Link>
        </li>
        <li>
          <Link to="/admin/settings" className="govuk-link">
            manage user accounts and data collections
          </Link>
        </li>
      </ul>
      </div>
    </GeneralLayout>
  );
};

export default Home;
