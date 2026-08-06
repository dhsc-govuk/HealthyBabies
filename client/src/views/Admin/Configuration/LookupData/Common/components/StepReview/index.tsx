import React from 'react';
import { GovUKSummaryTable } from '../../../../../../../components/GovUKComponents';
import { LookupData } from '../..';

interface Props {
  lookupData: LookupData;
  setStep: React.Dispatch<React.SetStateAction<number>>;
}

const StepReview = ({ lookupData, setStep }: Props): React.ReactElement => {
  return (
    <GovUKSummaryTable
      onChangeAll={() => setStep(0)}
      rows={[
        { label: 'Entity', value: lookupData.entity, changeHiddenText: 'entity' },
        { label: 'Value', value: lookupData.value, changeHiddenText: 'value' },
        { label: 'Description', value: lookupData.description || '-', changeHiddenText: 'description' },
      ]}
    />
  );
};

export default StepReview;
