import React from 'react';
import { GeneralLayout } from '../../../layouts';
import { H1, H2, Paragraph } from '../../../components/GovUKComponents';
import usePageTitle from '../../../hooks/usePageTitle';

const Help = (): React.ReactElement => {
  usePageTitle('Help');
  return (
    <GeneralLayout>
      <div style={{ paddingTop: '30px' }}>
        <H1>Help and guidance</H1>
        <Paragraph>Find guidance to help you use the Delivery Plan service.</Paragraph>
        <H2>Contact us</H2>
        <Paragraph>If you need help, contact us.</Paragraph>
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

export default Help;
