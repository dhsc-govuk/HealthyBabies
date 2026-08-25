import React from 'react';
import { SummaryList } from '../../../../../../../components/GovUKComponents';
import { LookupData } from '../..';

interface Props {
  lookupData: LookupData;
  setStep: React.Dispatch<React.SetStateAction<number>>;
}

const StepReview = ({ lookupData, setStep }: Props): React.ReactElement => {
  return (
    // The GOV.UK Check answers pattern uses the Summary list component, which stacks to a
    // single column at narrow widths. SummaryList derives each Change link's visually hidden
    // text from the row label, matching the previous changeHiddenText values.
    <SummaryList
      noOuterBorder
      items={[
        { label: 'Entity', value: lookupData.entity, onAction: () => setStep(0), actionLabel: 'Change' },
        { label: 'Value', value: lookupData.value, onAction: () => setStep(0), actionLabel: 'Change' },
        { label: 'Description', value: lookupData.description || '-', onAction: () => setStep(0), actionLabel: 'Change' },
      ]}
    />
  );
};

export default StepReview;
