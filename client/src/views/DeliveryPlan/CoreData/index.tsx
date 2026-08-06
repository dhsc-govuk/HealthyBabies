import React from 'react';
import { GeneralLayout } from '../../../layouts';
import { H1, Paragraph } from '../../../components/GovUKComponents';
import usePageTitle from '../../../hooks/usePageTitle';

const CoreData = (): React.ReactElement => {
  usePageTitle('Core Data');
  return (
    <GeneralLayout>
      <div style={{ paddingTop: '30px' }}>
        <H1>Core Data</H1>
        <Paragraph>Core data management for the Delivery Plan is coming soon.</Paragraph>
      </div>
    </GeneralLayout>
  );
};

export default CoreData;
