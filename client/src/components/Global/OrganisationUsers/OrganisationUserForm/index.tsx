import React, { useState, useMemo } from 'react';
import { Button, LoadingBox } from 'govuk-react';
import { GovUKFieldset, GovUKAutocomplete, GovUKBackLink, H4 } from '../../../../components/GovUKComponents';
import { validateEmailAddress } from '../../../../helpers/validators';
import useErrorSummaryFocus from '../../../../hooks/useErrorSummaryFocus';
import './styles.css';

export interface OrganisationUserFormData {
  firstName: string;
  lastName: string;
  organisationId: string;
  role: string;
  email: string;
  autoActivate: boolean;
}

interface Organisation {
  id: string;
  name: string;
}

interface ValidationErrors {
  firstName?: string;
  lastName?: string;
  organisationId?: string;
  role?: string;
  email?: string;
}

export interface OrganisationUserFormProps {
  initialData?: Partial<OrganisationUserFormData>;
  organisations: Organisation[];
  isLoading?: boolean;
  isSaving?: boolean;
  onSubmit: (data: OrganisationUserFormData) => void;
  onCancel: () => void;
  submitLabel?: string;
  isEdit?: boolean;
  emailReadOnly?: boolean;
  showOrganisation?: boolean;
  showRole?: boolean;
  hideBackLink?: boolean;
}

const roles = [
  { value: 'organisation admin', label: 'LA Admin' },
];

const OrganisationUserForm = ({
  initialData,
  organisations,
  isLoading = false,
  isSaving = false,
  onSubmit,
  onCancel,
  submitLabel = 'Confirm and create',
  isEdit = false,
  emailReadOnly = false,
  showOrganisation = true,
  showRole = true,
  hideBackLink = false,
}: OrganisationUserFormProps): React.ReactElement => {
  const [step, setStep] = useState<'form' | 'review'>('form');

  const [formData, setFormData] = useState<OrganisationUserFormData>({
    firstName: initialData?.firstName ?? '',
    lastName: initialData?.lastName ?? '',
    organisationId: initialData?.organisationId ?? '',
    role: initialData?.role ?? '',
    email: initialData?.email ?? '',
    autoActivate: initialData?.autoActivate ?? true,
  });

  const [errors, setErrors] = useState<ValidationErrors>({});
  const [submitAttempts, setSubmitAttempts] = useState(0);
  useErrorSummaryFocus(submitAttempts, Object.values(errors).some(Boolean));

  const validate = (): boolean => {
    const newErrors: ValidationErrors = {};

    if (!formData.firstName.trim()) {
      newErrors.firstName = 'Enter a first name';
    }
    if (!formData.lastName.trim()) {
      newErrors.lastName = 'Enter a last name';
    }
    if (showOrganisation && !formData.organisationId) {
      newErrors.organisationId = 'Select a local authority';
    }
    if (showRole && !formData.role) {
      newErrors.role = 'Select a role';
    }
    if (!formData.email.trim()) {
      newErrors.email = 'Enter an email address';
    } else if (!validateEmailAddress(formData.email)) {
      newErrors.email = 'Enter a valid email address';
    }

    setErrors(newErrors);
    setSubmitAttempts((n) => n + 1);
    return Object.keys(newErrors).length === 0;
  };

  const handleContinue = (e: React.FormEvent) => {
    e.preventDefault();
    if (validate()) {
      setStep('review');
    }
  };

  const handleConfirm = () => {
    onSubmit(formData);
  };

  const handleBack = () => {
    if (step === 'review') {
      setStep('form');
    } else {
      onCancel();
    }
  };

  const getOrganisationName = () => {
    const org = organisations.find(o => o.id === formData.organisationId);
    return org?.name ?? '-';
  };

  const getRoleName = () => {
    const role = roles.find(r => r.value === formData.role);
    return role?.label ?? '-';
  };

  const errorList = useMemo(() => {
    const list: { targetName: string; text: string }[] = [];
    if (errors.firstName) list.push({ targetName: 'first-name', text: errors.firstName });
    if (errors.lastName) list.push({ targetName: 'last-name', text: errors.lastName });
    if (errors.organisationId) list.push({ targetName: 'organisation', text: errors.organisationId });
    if (errors.role) list.push({ targetName: 'role', text: errors.role });
    if (errors.email) list.push({ targetName: 'email', text: errors.email });
    return list;
  }, [errors]);

  return (
    <>
      {!hideBackLink && (
        <GovUKBackLink onClick={handleBack}>Back</GovUKBackLink>
      )}

      <LoadingBox loading={isLoading || isSaving}>
        {step === 'form' && (
          <>
            {errorList.length > 0 && (
              <div className="govuk-error-summary" aria-labelledby="error-summary-title" role="alert" tabIndex={-1}>
                <h2 className="govuk-error-summary__title" id="error-summary-title">
                  There is a problem
                </h2>
                <div className="govuk-error-summary__body">
                  <ul className="govuk-error-summary__list">
                    {errorList.map((error) => (
                      <li key={error.targetName}>
                        <a href={`#${error.targetName}`}>{error.text}</a>
                      </li>
                    ))}
                  </ul>
                </div>
              </div>
            )}

            <form onSubmit={handleContinue}>
              <GovUKFieldset.Input
                id="first-name"
                name="firstName"
                label="First name"
                value={formData.firstName}
                error={errors.firstName}
                onChange={(e: React.ChangeEvent<HTMLInputElement>) =>
                  setFormData({ ...formData, firstName: e.target.value })
                }
              />

              <GovUKFieldset.Input
                id="last-name"
                name="lastName"
                label="Last name"
                value={formData.lastName}
                error={errors.lastName}
                onChange={(e: React.ChangeEvent<HTMLInputElement>) =>
                  setFormData({ ...formData, lastName: e.target.value })
                }
              />

              {showOrganisation && (
                <GovUKAutocomplete
                  id="organisation"
                  name="organisationId"
                  label="Local authority"
                  hint="Select a local authority the user belongs to. You must create a local authority before you can add users to it."
                  value={formData.organisationId}
                  error={errors.organisationId}
                  placeholder="Type to search a local authority..."
                  options={[
                    { value: '', label: '' },
                    ...organisations.map((org) => ({ value: org.id, label: org.name })),
                  ]}
                  onChange={(value) => setFormData({ ...formData, organisationId: value })}
                />
              )}

              {showRole && (
                <GovUKFieldset.Select
                  id="role"
                  name="role"
                  label="Role"
                  value={formData.role}
                  error={errors.role}
                  options={[
                    { value: '', label: 'Select a role...' },
                    ...roles,
                  ]}
                  onChange={(e) => setFormData({ ...formData, role: e.target.value })}
                />
              )}

              <GovUKFieldset.Input
                id="email"
                name="email"
                label="Email address"
                type="email"
                hint="Enter the user's work email address. They will use it to sign in and receive notifications about the service. Do not use a group email address or distribution list."
                value={formData.email}
                error={errors.email}
                readOnly={emailReadOnly}
                onChange={(e: React.ChangeEvent<HTMLInputElement>) =>
                  setFormData({ ...formData, email: e.target.value })
                }
              />

              <GovUKFieldset.Checkbox
                id="auto-activate"
                name="autoActivate"
                label="Automatically activate the user account when it is created"
                checked={formData.autoActivate}
                onChange={(e: React.ChangeEvent<HTMLInputElement>) =>
                  setFormData({ ...formData, autoActivate: e.target.checked })
                }
              />

              <Button type="submit">Continue</Button>
            </form>
          </>
        )}

        {step === 'review' && (
          <>
            <H4>Check your answers before {isEdit ? 'saving changes' : 'creating the user account'}</H4>
            <p className="govuk-body">
              Check the information you've entered before {isEdit ? 'saving' : 'creating the user account'}. Make sure the details are accurate and complete. You can go back to make changes if anything needs updating.
            </p>

            <dl className="govuk-summary-list govuk-summary-list--no-outer-border">
              <div className="govuk-summary-list__row">
                <dt className="govuk-summary-list__key">First name</dt>
                <dd className="govuk-summary-list__value">{formData.firstName}</dd>
              </div>
              <div className="govuk-summary-list__row">
                <dt className="govuk-summary-list__key">Last name</dt>
                <dd className="govuk-summary-list__value">{formData.lastName}</dd>
              </div>
              {showOrganisation && (
                <div className="govuk-summary-list__row">
                  <dt className="govuk-summary-list__key">Local authority</dt>
                  <dd className="govuk-summary-list__value">{getOrganisationName()}</dd>
                </div>
              )}
              {showRole && (
                <div className="govuk-summary-list__row">
                  <dt className="govuk-summary-list__key">Role</dt>
                  <dd className="govuk-summary-list__value">{getRoleName()}</dd>
                </div>
              )}
              <div className="govuk-summary-list__row">
                <dt className="govuk-summary-list__key">Email address</dt>
                <dd className="govuk-summary-list__value">{formData.email}</dd>
              </div>
              <div className="govuk-summary-list__row">
                <dt className="govuk-summary-list__key">Active upon creation</dt>
                <dd className="govuk-summary-list__value">{formData.autoActivate ? 'Yes' : 'No'}</dd>
              </div>
            </dl>

            <Button onClick={handleConfirm}>{submitLabel}</Button>
          </>
        )}
      </LoadingBox>
    </>
  );
};

export default OrganisationUserForm;
