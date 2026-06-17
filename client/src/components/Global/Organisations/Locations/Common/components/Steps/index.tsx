import React, { useCallback, useState } from 'react';
import { GovUKStepper } from '../../../../../../../components/GovUKComponents';
import useErrorSummaryFocus from '../../../../../../../hooks/useErrorSummaryFocus';
import { GlobalDataDto } from '../../../../../../../components/Global/Queries/globalData';
import StepDetails from '../StepDetails';
import StepReview from '../StepReview';
import { LocationReducerAction, ValidationErrors, Location, validationErrors } from '../types';

interface Props {
  completeLabel: string;
  location: Location;
  dispatch: React.Dispatch<{ type: LocationReducerAction; value: any }>;
  handleSave: () => void;
  locationTypes: GlobalDataDto[];
  siteTypes: GlobalDataDto[];
  onCancel?: () => void;
}

const LocationSteps = ({ completeLabel, location, dispatch, handleSave, locationTypes, siteTypes, onCancel }: Props): React.ReactElement => {
  const [step, setStep] = useState<number>(0);

  const [errors, setErrors] = useState<ValidationErrors>({
    name: false,
    deliverySiteName: false,
    postCode: false,
    referenceNumber: false,
    statusOfSite: false,
    typeOfSite: false,
    dateOpened: false,
    bsfhBranding: false,
    locationType: false,
  });
  const [submitAttempts, setSubmitAttempts] = useState(0);
  useErrorSummaryFocus(submitAttempts, (Object.keys(errors) as Array<keyof typeof errors>).some((k) => errors[k]));

  const stepperSteps = [
    {
      label: 'Delivery Location Details',
      component: <StepDetails location={location} errors={errors} setErrors={setErrors} dispatch={dispatch} locationTypes={locationTypes} siteTypes={siteTypes} />,
    },
    {
      label: 'Check your answers',
      component: <StepReview location={location} setStep={setStep} locationTypes={locationTypes} siteTypes={siteTypes} />,
    },
  ];

  const validate = useCallback(
    (_step: number) => {
      const errorItems: Record<string, string | boolean> = {};

      if (_step === 0) {
        // Validate required fields
        errorItems.deliverySiteName = !location.deliverySiteName ? validationErrors.deliverySiteName : false;
        errorItems.postCode = !location.postCode ? validationErrors.postCode : false;
        errorItems.referenceNumber = !location.referenceNumber ? validationErrors.referenceNumber : false;
        errorItems.statusOfSite = !location.statusOfSite ? validationErrors.statusOfSite : false;
        errorItems.typeOfSite = !location.typeOfSite ? validationErrors.typeOfSite : false;
        errorItems.dateOpened = !location.dateOpened ? validationErrors.dateOpened : false;
        errorItems.bsfhBranding = !location.bsfhBranding ? validationErrors.bsfhBranding : false;
        errorItems.locationType = !location.locationType || location.locationType.length === 0 ? validationErrors.locationType : false;
      }

      setErrors({ ...errors, ...errorItems });
      setSubmitAttempts((n) => n + 1);
      return Object.keys(errorItems).some((i) => errorItems[i]);
    },
    [location, errors]
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

export default LocationSteps;
