import React from 'react';
import { SummaryList } from '../../../../../../components/GovUKComponents';
import { Organisation } from '../..';
import { booleanToYesNo } from '../../../../../../helpers/stringUtils';

interface Props {
  organisation: Organisation;
  setStep: React.Dispatch<React.SetStateAction<number>>;
}

const StepReview = ({ organisation, setStep }: Props): React.ReactElement => (
  <SummaryList
    items={[
      { label: 'Local Authority name', value: organisation.name, onAction: () => setStep(0) },
      { label: 'ONS Code', value: organisation.onsCode, onAction: () => setStep(0) },
      { label: 'Status', value: booleanToYesNo(organisation.isActive) === 'Yes' ? 'Active' : 'Inactive', onAction: () => setStep(0) },
    ]}
  />
);

export default StepReview;
