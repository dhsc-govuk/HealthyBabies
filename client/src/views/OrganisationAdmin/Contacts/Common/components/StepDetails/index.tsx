import React, { useCallback, useMemo } from 'react';
import { GovUKFieldset, ErrorSummary } from '../../../../../../components/GovUKComponents';
import { OrganisationContact, ValidationErrors, validationErrors, ContactReducerAction, isValidEmail } from '../..';
import { GlobalDataDto, getValidationErrorForLookup } from '../../../../../../components/Global/Queries/globalData';

interface Props {
  contact: OrganisationContact;
  errors: ValidationErrors;
  setErrors: React.Dispatch<React.SetStateAction<ValidationErrors>>;
  dispatch: React.Dispatch<{ type: ContactReducerAction; value: any }>;
  roles: GlobalDataDto[];
}

const StepDetails = ({ contact, errors, setErrors, dispatch, roles }: Props): React.ReactElement => {
  const validate = useCallback(
    (field: string) => {
      const errorItems: Record<string, string | boolean> = {};
      if (field === 'name') {
        errorItems.name = !contact.name ? validationErrors.name : false;
      }
      if (field === 'email') {
        errorItems.email = !contact.email || !isValidEmail(contact.email) ? validationErrors.email : false;
      }
      if (field === 'role') {
        const roleError = getValidationErrorForLookup(contact.role, roles, 'role');
        errorItems.role = roleError;
      }
      setErrors({ ...errors, ...errorItems });
    },
    [contact, errors, setErrors, roles]
  );

  const errorList = useMemo(() => {
    const list: { targetName: string; text: string }[] = [];
    if (errors.name) list.push({ targetName: 'contact-name', text: String(errors.name) });
    if (errors.email) list.push({ targetName: 'contact-email', text: String(errors.email) });
    if (errors.role) list.push({ targetName: 'contact-role', text: String(errors.role) });
    return list;
  }, [errors]);

  return (
    <>
      {errorList.length > 0 && <ErrorSummary errors={errorList} />}
      <GovUKFieldset legend="Contact details" legendSize="m">
        <GovUKFieldset.Input
          id="contact-name"
          name="name"
          label="Contact name"
          hint="Enter the full name of the contact"
          value={contact.name}
          error={errors.name ? String(errors.name) : undefined}
          required
          onChange={(e: React.ChangeEvent<HTMLInputElement>) => dispatch({ type: ContactReducerAction.NAME, value: e.target.value })}
          onBlur={() => validate('name')}
        />
        <GovUKFieldset.Input
          id="contact-email"
          name="email"
          label="Email address"
          hint="Enter the contact's email address"
          value={contact.email}
          error={errors.email ? String(errors.email) : undefined}
          required
          onChange={(e: React.ChangeEvent<HTMLInputElement>) => dispatch({ type: ContactReducerAction.EMAIL, value: e.target.value })}
          onBlur={() => validate('email')}
        />
        <GovUKFieldset.Select
          id="contact-role"
          name="role"
          label="Role"
          hint="Select the contact's role"
          value={contact.role}
          options={[
            { value: '', label: 'Select a role' },
            ...roles.map((role) => ({ value: role.value, label: role.value })),
          ]}
          error={errors.role ? String(errors.role) : undefined}
          required
          onChange={(e: React.ChangeEvent<HTMLSelectElement>) => {
            dispatch({ type: ContactReducerAction.ROLE, value: e.target.value });
            if (e.target.value) {
              setErrors((prev) => ({ ...prev, role: false }));
            }
          }}
        />
      </GovUKFieldset>
    </>
  );
};

export default StepDetails;
