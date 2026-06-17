import React, { useCallback, useState } from 'react';
import { GovUKStepper } from '../../../../../../../components/GovUKComponents';
import useErrorSummaryFocus from '../../../../../../../hooks/useErrorSummaryFocus';
import { LookupData, validationErrors, ValidationErrors, LookupDataReducerAction } from '../..';
import StepDetails from '../StepDetails';
import StepReview from '../StepReview';

interface Props {
  completeLabel: string;
  lookupData: LookupData;
  dispatch: React.Dispatch<{ type: LookupDataReducerAction; value: any }>;
  handleSave: () => void;
  isEdit?: boolean;
  onCancel?: () => void;
}

const LookupDataSteps = ({ completeLabel, lookupData, dispatch, handleSave, isEdit = false, onCancel }: Props): React.ReactElement => {
  const [step, setStep] = useState<number>(0);

  const [errors, setErrors] = useState<ValidationErrors>({
    entity: false,
    value: false,
  });
  const [submitAttempts, setSubmitAttempts] = useState(0);
  useErrorSummaryFocus(submitAttempts, (Object.keys(errors) as Array<keyof typeof errors>).some((k) => errors[k]));

  const stepperSteps = [
    {
      label: 'Details',
      component: <StepDetails lookupData={lookupData} errors={errors} setErrors={setErrors} dispatch={dispatch} isEdit={isEdit} />,
    },
    {
      label: 'Check your answers',
      component: <StepReview lookupData={lookupData} setStep={setStep} />,
    },
  ];

  const validate = useCallback(
    (_step: number) => {
      const errorItems: Record<string, string | boolean> = {};
      if (_step === 0) {
        errorItems.entity = !lookupData.entity ? validationErrors.entity : false;
        errorItems.value = !lookupData.value ? validationErrors.value : false;
      }
      setErrors({ ...errors, ...errorItems });
      setSubmitAttempts((n) => n + 1);
      return Object.keys(errorItems).some((i) => errorItems[i]);
    },
    [lookupData, errors]
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

export default LookupDataSteps;
