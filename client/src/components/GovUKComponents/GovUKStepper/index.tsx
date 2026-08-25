import React, { useEffect, useRef } from 'react';
import { Details } from 'govuk-react';
import GovUKButton from '../GovUKButton';
import GovUKBackLink from '../GovUKBackLink';
import usePageTitle from '../../../hooks/usePageTitle';
import useStepFocus from '../../../hooks/useStepFocus';

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
  /** When provided, manages the document page title with step position (e.g. "Add a service - Step 1 of 3: Details"). */
  pageTitle?: string;
  /** Pass true when the current step has validation errors so the "Error: " prefix is applied. */
  pageHasErrors?: boolean;
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
  pageTitle,
  pageHasErrors = false,
}: GovUKStepperProps): React.ReactElement => {
  const isLastStep = step === stepperSteps.length - 1;
  const headingRef = useRef<HTMLDivElement>(null);
  // Focus lands on the first interactive element of the new step's content (WCAG 2.4.3);
  // the aria-live page announcer reads the new "Step X of Y" title for orientation.
  const stepContentRef = useStepFocus<HTMLDivElement>(step);

  const stepLabel = pageTitle
    ? `Step ${step + 1} of ${stepperSteps.length}: ${stepperSteps[step]?.label ?? ''}`
    : undefined;
  usePageTitle(pageTitle, pageHasErrors, stepLabel);

  // Disabling the Continue button while it holds focus makes the browser drop focus to
  // document.body, which restarts NVDA from the top of the page. Catch that and park focus
  // on the step heading instead.
  useEffect(() => {
    if (!isNextDisabled) return;
    if (document.activeElement === document.body || document.activeElement === null) {
      headingRef.current?.focus({ preventScroll: true });
    }
  }, [isNextDisabled]);

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

      <div ref={headingRef} tabIndex={-1} style={{ outline: 'none' }}>
        <p className="govuk-caption-l">
          Step {step + 1} of {stepperSteps.length}
        </p>
        {title && <h1 className="govuk-heading-l">{title}</h1>}
        {description && <p className="govuk-body">{description}</p>}
        {helpSummary && helpContent && <Details summary={helpSummary}>{helpContent}</Details>}
      </div>

      <div ref={stepContentRef}>{stepperSteps[step].component}</div>

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
