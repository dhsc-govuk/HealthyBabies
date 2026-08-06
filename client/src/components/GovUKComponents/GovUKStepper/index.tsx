import React, { useEffect, useRef } from 'react';
import { Details } from 'govuk-react';
import GovUKButton from '../GovUKButton';
import GovUKBackLink from '../GovUKBackLink';

interface StepperStep {
  label: string;
  component: React.ReactNode;
}

interface GovUKStepperProps {
  step: number;
  setStep: React.Dispatch<React.SetStateAction<number>>;
  stepperSteps: StepperStep[];
  completeLabel: string;
  handleComplete: () => void;
  validate?: (step: number) => boolean;
  isNextDisabled?: boolean;
  title?: string;
  description?: string;
  helpSummary?: string;
  helpContent?: React.ReactNode;
  onSaveAsDraft?: () => void;
  showSaveAsDraft?: boolean;
  onCancel?: () => void;
}

const GovUKStepper = ({
  step,
  setStep,
  stepperSteps,
  completeLabel,
  handleComplete,
  validate,
  isNextDisabled = false,
  title,
  description,
  helpSummary,
  helpContent,
  onSaveAsDraft,
  showSaveAsDraft = true,
  onCancel,
}: GovUKStepperProps): React.ReactElement => {
  const isLastStep = step === stepperSteps.length - 1;
  const stepCaptionRef = useRef<HTMLParagraphElement>(null);

  useEffect(() => {
    window.scrollTo(0, 0);
    stepCaptionRef.current?.focus({ preventScroll: true });
  }, [step]);

  const handleNext = () => {
    if (validate && validate(step)) {
      return;
    }
    if (isLastStep) {
      handleComplete();
    } else {
      setStep((prev) => prev + 1);
    }
  };

  const handleBack = () => {
    setStep((prev) => prev - 1);
  };

  return (
    <div>
        {step > 0 ? (
        <GovUKBackLink onClick={handleBack}>Back</GovUKBackLink>
      ) : (
        onCancel && <GovUKBackLink onClick={onCancel}>Back</GovUKBackLink>
      )}

      <div>
        <p ref={stepCaptionRef} tabIndex={-1} className="govuk-caption-l" style={{ outline: 'none' }}>
          Step {step + 1} of {stepperSteps.length}
        </p>
        {title && <h1 className="govuk-heading-l">{title}</h1>}
        {description && <p className="govuk-body">{description}</p>}
        {helpSummary && helpContent && <Details summary={helpSummary}>{helpContent}</Details>}
      </div>

      <div>{stepperSteps[step].component}</div>

      <div className="govuk-button-group govuk-!-margin-top-6">
        <GovUKButton className="govuk-button" disabled={isNextDisabled} onClick={handleNext}>
          {isLastStep ? completeLabel : 'Continue'}
        </GovUKButton>
        {showSaveAsDraft && onSaveAsDraft && (
          <GovUKButton className="govuk-button govuk-button--secondary" onClick={onSaveAsDraft}>
            Save as draft and exit
          </GovUKButton>
        )}
        {onCancel && !showSaveAsDraft && (
          <GovUKButton className="govuk-button govuk-button--secondary" onClick={onCancel}>
            Cancel
          </GovUKButton>
        )}
      </div>
    </div>
  );
};

export default GovUKStepper;
