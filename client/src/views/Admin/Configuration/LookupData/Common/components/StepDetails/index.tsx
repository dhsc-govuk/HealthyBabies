import React, { useCallback, useMemo } from 'react';
import { GovUKFieldset } from '../../../../../../../components/GovUKComponents';
import { ErrorSummary } from 'govuk-react';
import { useEntityTypesQuery } from '../../../../../../../components/Global/Queries/globalData';
import { LookupData, LookupDataReducerAction, ValidationErrors, validationErrors } from '../..';

interface Props {
  lookupData: LookupData;
  errors: ValidationErrors;
  setErrors: React.Dispatch<React.SetStateAction<ValidationErrors>>;
  dispatch: React.Dispatch<{ type: LookupDataReducerAction; value: any }>;
  isEdit?: boolean;
}

const StepDetails = ({ lookupData, errors, setErrors, dispatch, isEdit = false }: Props): React.ReactElement => {
  const { data: entityTypesData } = useEntityTypesQuery();

  const entityTypeOptions = useMemo(() => {
    const options = [{ value: '', label: 'Select an entity type' }];
    if (entityTypesData?.data) {
      entityTypesData.data.forEach((et) => {
        options.push({ value: et.name, label: `${et.name} (${et.description})` });
      });
    }
    return options;
  }, [entityTypesData]);

  const validate = useCallback(
    (field: string) => {
      const errorItems: Record<string, string | boolean> = {};
      if (field === 'entity') {
        errorItems.entity = !lookupData.entity ? validationErrors.entity : false;
      }
      if (field === 'value') {
        errorItems.value = !lookupData.value ? validationErrors.value : false;
      }
      setErrors({ ...errors, ...errorItems });
    },
    [lookupData, errors, setErrors]
  );

  const errorList = useMemo(() => {
    const list: { targetName: string; text: string }[] = [];
    if (errors.entity) list.push({ targetName: 'lookup-entity', text: String(errors.entity) });
    if (errors.value) list.push({ targetName: 'lookup-value', text: String(errors.value) });
    return list;
  }, [errors]);

  return (
    <>
      {errorList.length > 0 && <ErrorSummary errors={errorList} />}
      <GovUKFieldset legend="Lookup Data Details" legendSize="m">
        <GovUKFieldset.Select
          id="lookup-entity"
          name="entity"
          label="Entity"
          hint="Select the entity type for this lookup value"
          value={lookupData.entity}
          options={entityTypeOptions}
          error={errors.entity ? String(errors.entity) : undefined}
          width="two-thirds"
          required
          disabled={isEdit}
          onChange={(e: React.ChangeEvent<HTMLSelectElement>) => {
            dispatch({ type: LookupDataReducerAction.ENTITY, value: e.target.value });
            if (e.target.value) {
              setErrors((prev: ValidationErrors) => ({ ...prev, entity: false }));
            }
          }}
        />
        <GovUKFieldset.Input
          id="lookup-value"
          name="value"
          label="Value"
          hint="Enter the lookup value"
          value={lookupData.value}
          error={errors.value ? String(errors.value) : undefined}
          width="two-thirds"
          required
          onChange={(e: React.ChangeEvent<HTMLInputElement>) => dispatch({ type: LookupDataReducerAction.VALUE, value: e.target.value })}
          onBlur={() => validate('value')}
        />
        <GovUKFieldset.Input
          id="lookup-description"
          name="description"
          label="Description"
          hint="Enter an optional description"
          value={lookupData.description}
          width="two-thirds"
          onChange={(e: React.ChangeEvent<HTMLInputElement>) => dispatch({ type: LookupDataReducerAction.DESCRIPTION, value: e.target.value })}
        />
      </GovUKFieldset>
    </>
  );
};

export default StepDetails;
