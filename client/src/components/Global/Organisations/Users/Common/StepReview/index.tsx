import React from 'react';
import { SummaryList } from '../../../../../../components/GovUKComponents';
import { booleanToYesNo, stringFromArray } from '../../../../../../helpers/stringUtils';
import { User } from '..';

interface Props {
  user: User;
  setStep: React.Dispatch<React.SetStateAction<number>>;
}

const StepReview = ({ user, setStep }: Props): React.ReactElement => {
  return (
    <SummaryList
      items={[
        { label: 'Full Name', value: stringFromArray([user.firstName, user.lastName]), onAction: () => setStep(0) },
        { label: 'Email', value: user.email, onAction: () => setStep(0) },
        { label: 'Active', value: booleanToYesNo(user.isActive), onAction: () => setStep(0) },
        { label: 'Role', value: user.role === 'organisation admin' ? 'Admin' : user.role, onAction: () => setStep(0) },
      ]}
    />
  );
};

export default StepReview;
