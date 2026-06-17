import React, { useCallback, useState } from 'react';
import { GovUKStepper } from '../../../../../../components/GovUKComponents';
import useErrorSummaryFocus from '../../../../../../hooks/useErrorSummaryFocus';
import { User, validationErrors, ValidationErrors, UserReducerAction } from '..';
import StepDetails from '../StepDetails';
import StepReview from '../StepReview';

interface Props {
  completeLabel: string;
  user: User;
  dispatch: React.Dispatch<{ type: UserReducerAction; value: any }>;
  handleSave: () => void;
  showRoleSelect?: boolean;
  isEdit?: boolean;
  onCancel?: () => void;
}

const UserSteps = ({ completeLabel, user, dispatch, handleSave, showRoleSelect = true, isEdit = false, onCancel }: Props): React.ReactElement => {
  const [step, setStep] = useState<number>(0);

  const [errors, setErrors] = useState<ValidationErrors>({
    firstName: false,
    lastName: false,
    email: false,
  });
  const [submitAttempts, setSubmitAttempts] = useState(0);
  useErrorSummaryFocus(submitAttempts, (Object.keys(errors) as Array<keyof typeof errors>).some((k) => errors[k]));

  const stepperSteps = [
    {
      label: 'Details',
      component: <StepDetails user={user} errors={errors} setErrors={setErrors} dispatch={dispatch} showRoleSelect={showRoleSelect} isEdit={isEdit} />,
    },
    {
      label: 'Check your answers',
      component: <StepReview user={user} setStep={setStep} />,
    },
  ];

  const validate = useCallback(
    (_step: number) => {
      const errorItems: Record<string, string | boolean> = {};
      if (_step === 0) {
        errorItems.firstName = !user.firstName ? validationErrors.firstName : false;
        errorItems.lastName = !user.lastName ? validationErrors.lastName : false;
        errorItems.email = !user.email ? validationErrors.email : false;
      }
      setErrors({ ...errors, ...errorItems });
      setSubmitAttempts((n) => n + 1);
      return Object.keys(errorItems).some((i) => errorItems[i]);
    },
    [user, errors]
  );

  const hasErrors = (Object.keys(errors) as Array<keyof typeof errors>).some((i) => errors[i]);

  return (
    <GovUKStepper
      step={step}
      setStep={setStep}
      stepperSteps={stepperSteps}
      completeLabel={completeLabel}
      handleComplete={handleSave}
      validate={validate}
      isNextDisabled={hasErrors}
      onCancel={onCancel}
    />
  );
};

export default UserSteps;
