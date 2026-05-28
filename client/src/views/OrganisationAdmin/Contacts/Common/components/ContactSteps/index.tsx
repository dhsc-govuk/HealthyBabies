import React, { useCallback, useState } from 'react';
import { GovUKStepper } from '../../../../../../components/GovUKComponents';
import useErrorSummaryFocus from '../../../../../../hooks/useErrorSummaryFocus';
import { OrganisationContact, ValidationErrors, validationErrors, ContactReducerAction, isValidEmail } from '../..';
import { GlobalDataDto, getValidationErrorForLookup } from '../../../../../../components/Global/Queries/globalData';
import StepDetails from '../StepDetails';
import StepReview from '../StepReview';

interface Props {
  completeLabel: string;
  contact: OrganisationContact;
  dispatch: React.Dispatch<{ type: ContactReducerAction; value: any }>;
  handleSave: () => void;
  roles: GlobalDataDto[];
  onCancel?: () => void;
}

const ContactSteps = ({ completeLabel, contact, dispatch, handleSave, roles, onCancel }: Props): React.ReactElement => {
  const [step, setStep] = useState<number>(0);

  const [errors, setErrors] = useState<ValidationErrors>({
    name: false,
    email: false,
    role: false,
  });
  const [submitAttempts, setSubmitAttempts] = useState(0);
  useErrorSummaryFocus(submitAttempts, (Object.keys(errors) as Array<keyof typeof errors>).some((k) => errors[k]));

  const stepperSteps = [
    {
      label: 'Details',
      component: <StepDetails contact={contact} errors={errors} setErrors={setErrors} dispatch={dispatch} roles={roles} />,
    },
    {
      label: 'Check your answers',
      component: <StepReview contact={contact} setStep={setStep} />,
    },
  ];

  const validate = useCallback(
    (_step: number) => {
      const errorItems: Record<string, string | boolean> = {};
      if (_step === 0) {
        errorItems.name = !contact.name ? validationErrors.name : false;
        errorItems.email = !contact.email || !isValidEmail(contact.email) ? validationErrors.email : false;
        errorItems.role = getValidationErrorForLookup(contact.role, roles, 'role');
      }
      setErrors({ ...errors, ...errorItems });
      setSubmitAttempts((n) => n + 1);
      return Object.keys(errorItems).some((i) => errorItems[i]);
    },
    [contact, errors, roles]
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

export default ContactSteps;
