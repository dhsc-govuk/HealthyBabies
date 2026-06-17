import React, { useCallback, useMemo } from 'react';
import { GovUKFieldset, ErrorSummary } from '../../../../../../components/GovUKComponents';
import { Organisation, ValidationErrors, validationErrors, OrganisationReducerAction } from '../..';

interface Props {
  organisation: Organisation;
  errors: ValidationErrors;
  setErrors: React.Dispatch<React.SetStateAction<ValidationErrors>>;
  dispatch: React.Dispatch<{ type: OrganisationReducerAction; value: any }>;
}

const StepDetails = ({ organisation, errors, setErrors, dispatch }: Props): React.ReactElement => {
  const validate = useCallback(
    (field: string) => {
      const errorItems: Record<string, string | boolean> = {};
      if (field === 'name') {
        errorItems.name = !organisation.name ? validationErrors.name : false;
      }
      if (field === 'onsCode') {
        errorItems.onsCode = !organisation.onsCode ? validationErrors.onsCode : false;
      }
      setErrors({ ...errors, ...errorItems });
    },
    [organisation, errors, setErrors]
  );

  const errorList = useMemo(() => {
    const list: { targetName: string; text: string }[] = [];
    if (errors.name) list.push({ targetName: 'organisation-name', text: String(errors.name) });
    if (errors.onsCode) list.push({ targetName: 'organisation-ons-code', text: String(errors.onsCode) });
    return list;
  }, [errors]);

  return (
    <>
      {errorList.length > 0 && <ErrorSummary errors={errorList} />}
      <GovUKFieldset legend="Local Authority details" legendSize="m">
        <GovUKFieldset.Input
          id="organisation-name"
          name="name"
          label="Local Authority name"
          hint="Enter the full name of the local authority"
          value={organisation.name}
          error={errors.name ? String(errors.name) : undefined}
          required
          onChange={(e: React.ChangeEvent<HTMLInputElement>) => dispatch({ type: OrganisationReducerAction.NAME, value: e.target.value })}
          onBlur={() => validate('name')}
        />
        <GovUKFieldset.Input
          id="organisation-ons-code"
          name="onsCode"
          label="ONS Code"
          hint="Enter the Office for National Statistics code for this local authority"
          value={organisation.onsCode}
          error={errors.onsCode ? String(errors.onsCode) : undefined}
          required
          onChange={(e: React.ChangeEvent<HTMLInputElement>) => dispatch({ type: OrganisationReducerAction.ONS_CODE, value: e.target.value })}
          onBlur={() => validate('onsCode')}
        />
        <GovUKFieldset.Checkbox
          id="is-active"
          name="isActive"
          label="Active"
          hint="Select if this local authority should be active in the system"
          checked={organisation.isActive}
          onChange={(e: React.ChangeEvent<HTMLInputElement>) => dispatch({ type: OrganisationReducerAction.ACTIVE, value: e.target.checked })}
        />
      </GovUKFieldset>
    </>
  );
};

export default StepDetails;
