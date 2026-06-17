import React from 'react';
import { SummaryList } from '../../../../../../components/GovUKComponents';
import { OrganisationContact } from '../..';

interface Props {
  contact: OrganisationContact;
  setStep: React.Dispatch<React.SetStateAction<number>>;
}

const StepReview = ({ contact, setStep }: Props): React.ReactElement => {
  return (
    <SummaryList
      items={[
        { label: 'Contact name', value: contact.name, onAction: () => setStep(0) },
        { label: 'Email address', value: contact.email, onAction: () => setStep(0) },
        { label: 'Role', value: contact.role, onAction: () => setStep(0) },
      ]}
    />
  );
};

export default StepReview;
