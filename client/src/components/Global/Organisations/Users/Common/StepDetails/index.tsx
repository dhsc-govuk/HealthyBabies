import React, { useCallback, useMemo } from 'react';
import { GovUKFieldset, ErrorSummary } from '../../../../../../components/GovUKComponents';
import { organisationUserRoleOptions, User, UserReducerAction, validationErrors, ValidationErrors } from '..';
import { validateEmailAddress } from '../../../../../../helpers/validators';

interface Props {
  user: User;
  errors: ValidationErrors;
  setErrors: React.Dispatch<React.SetStateAction<ValidationErrors>>;
  dispatch: React.Dispatch<{ type: UserReducerAction; value: any }>;
  showRoleSelect?: boolean;
  isEdit?: boolean;
}

const StepDetails = ({ user, errors, setErrors, dispatch, showRoleSelect = true, isEdit = false }: Props): React.ReactElement => {
  const validate = useCallback(
    (field: string) => {
      const errorItems: Record<string, string | boolean> = {};
      if (field === 'firstName') {
        errorItems.firstName = !user.firstName ? validationErrors.firstName : false;
      }
      if (field === 'lastName') {
        errorItems.lastName = !user.lastName ? validationErrors.lastName : false;
      }
      if (field === 'email') {
        errorItems.email = !(user.email && validateEmailAddress(user.email)) ? validationErrors.email : false;
      }
      setErrors({ ...errors, ...errorItems });
    },
    [user, errors, setErrors]
  );

  const errorList = useMemo(() => {
    const list: { targetName: string; text: string }[] = [];
    if (errors.firstName) list.push({ targetName: 'first-name', text: String(errors.firstName) });
    if (errors.lastName) list.push({ targetName: 'last-name', text: String(errors.lastName) });
    if (errors.email) list.push({ targetName: 'email', text: String(errors.email) });
    return list;
  }, [errors]);

  return (
    <>
      {errorList.length > 0 && <ErrorSummary errors={errorList} />}
      <GovUKFieldset legend="User details" legendSize="m">
      <GovUKFieldset.Input
        id="first-name"
        name="firstName"
        label="First name"
        value={user.firstName}
        error={errors.firstName ? String(errors.firstName) : undefined}
        required
        onChange={(e: React.ChangeEvent<HTMLInputElement>) => dispatch({ type: UserReducerAction.FIRST_NAME, value: e.target.value })}
        onKeyUp={() => validate('firstName')}
        onBlur={() => validate('firstName')}
      />
      <GovUKFieldset.Input
        id="last-name"
        name="lastName"
        label="Last name"
        value={user.lastName}
        error={errors.lastName ? String(errors.lastName) : undefined}
        required
        onChange={(e: React.ChangeEvent<HTMLInputElement>) => dispatch({ type: UserReducerAction.LAST_NAME, value: e.target.value })}
        onKeyUp={() => validate('lastName')}
        onBlur={() => validate('lastName')}
      />
      <GovUKFieldset.Input
        id="email"
        name="email"
        label="Email"
        type="email"
        value={user.email}
        error={errors.email ? String(errors.email) : undefined}
        required
        disabled={isEdit}
        onChange={(e: React.ChangeEvent<HTMLInputElement>) => dispatch({ type: UserReducerAction.EMAIL, value: e.target.value })}
        onKeyUp={() => validate('email')}
        onBlur={() => validate('email')}
      />
      {showRoleSelect && (
        <GovUKFieldset.Select
          id="role"
          name="role"
          label="Role"
          value={user.role}
          options={organisationUserRoleOptions}
          required
          onChange={(e: React.ChangeEvent<HTMLSelectElement>) => dispatch({ type: UserReducerAction.ROLE, value: e.target.value })}
        />
      )}
      <GovUKFieldset.Checkbox
        id="is-active"
        name="isActive"
        label="Active"
        hint="Select if this user should be active in the system"
        checked={user.isActive}
        onChange={(e: React.ChangeEvent<HTMLInputElement>) => dispatch({ type: UserReducerAction.ACTIVE, value: e.target.checked })}
      />
    </GovUKFieldset>
    </>
  );
};

export default StepDetails;
