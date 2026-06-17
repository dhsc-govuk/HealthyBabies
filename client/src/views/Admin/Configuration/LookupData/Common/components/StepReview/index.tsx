import React from 'react';
import { SummaryList } from '../../../../../../../components/GovUKComponents';
import { LookupData } from '../..';

interface Props {
  lookupData: LookupData;
  setStep: React.Dispatch<React.SetStateAction<number>>;
}

const StepReview = ({ lookupData, setStep }: Props): React.ReactElement => {
  return (
    <SummaryList
      items={[
        { label: 'Entity', value: lookupData.entity, onAction: () => setStep(0) },
        { label: 'Value', value: lookupData.value, onAction: () => setStep(0) },
        { label: 'Description', value: lookupData.description || '-', onAction: () => setStep(0) },
      ]}
    />
  );
};

export default StepReview;
