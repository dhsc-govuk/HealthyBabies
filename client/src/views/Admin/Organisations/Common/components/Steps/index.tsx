import React, { useCallback, useState } from 'react';
import { GovUKStepper } from '../../../../../../components/GovUKComponents';
import useErrorSummaryFocus from '../../../../../../hooks/useErrorSummaryFocus';
import { Organisation, validationErrors, ValidationErrors, OrganisationReducerAction } from '../..';
import StepDetails from '../StepDetails';
import StepReview from '../StepReview';

interface Props {
  completeLabel: string;
  organisation: Organisation;
  dispatch: React.Dispatch<{ type: OrganisationReducerAction; value: any }>;
  handleSave: () => void;
  onCancel?: () => void;
}

const OrganisationSteps = ({ completeLabel, organisation, dispatch, handleSave, onCancel }: Props): React.ReactElement => {
  const [step, setStep] = useState<number>(0);

  const [errors, setErrors] = useState<ValidationErrors>({
    name: false,
    onsCode: false,
  });
  const [submitAttempts, setSubmitAttempts] = useState(0);
  useErrorSummaryFocus(submitAttempts, (Object.keys(errors) as Array<keyof typeof errors>).some((k) => errors[k]));

  const stepperSteps = [
    {
      label: 'Details',
      component: <StepDetails organisation={organisation} errors={errors} setErrors={setErrors} dispatch={dispatch} />,
    },
    {
      label: 'Check your answers',
      component: <StepReview organisation={organisation} setStep={setStep} />,
    },
  ];

  const validate = useCallback(
    (_step: number) => {
      const errorItems: Record<string, string | boolean> = {};
      if (_step === 0) {
        errorItems.name = !organisation.name ? validationErrors.name : false;
        errorItems.onsCode = !organisation.onsCode ? validationErrors.onsCode : false;
      }
      setErrors({ ...errors, ...errorItems });
      setSubmitAttempts((n) => n + 1);
      return Object.keys(errorItems).some((i) => errorItems[i]);
    },
    [organisation, errors]
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

export default OrganisationSteps;
