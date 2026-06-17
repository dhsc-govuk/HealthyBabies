import React, { useState } from 'react';
import { Button, LoadingBox } from 'govuk-react';
import { GovUKBackLink } from '../../../GovUKComponents';
import './styles.css';

export interface ContactPerson {
  fullName: string;
  role: string;
  roleTitle?: string;
  email: string;
}

export interface LocalAuthorityFormData {
  name: string;
  onsCode: string;
  isActive: boolean;
  contacts: ContactPerson[];
}

interface ContactValidationErrors {
  fullName?: string;
  role?: string;
  roleTitle?: string;
  email?: string;
}

interface ValidationErrors {
  name?: string;
  onsCode?: string;
  contacts?: ContactValidationErrors[];
}

export interface LocalAuthorityFormProps {
  initialData?: Partial<LocalAuthorityFormData>;
  isLoading?: boolean;
  isSaving?: boolean;
  onSubmit: (data: LocalAuthorityFormData) => void;
  onCancel: () => void;
  submitLabel?: string;
  isEdit?: boolean;
  onStepChange?: (step: 'form' | 'review') => void;
}

const LocalAuthorityForm = ({
  initialData,
  isLoading = false,
  isSaving = false,
  onSubmit,
  onCancel,
  submitLabel = 'Confirm and create',
  isEdit = false,
  onStepChange,
}: LocalAuthorityFormProps): React.ReactElement => {
  const [step, setStep] = useState<'form' | 'review'>('form');

  const [formData, setFormData] = useState<LocalAuthorityFormData>({
    name: initialData?.name ?? '',
    onsCode: initialData?.onsCode ?? '',
    isActive: initialData?.isActive ?? false,
    contacts: initialData?.contacts ?? [{ fullName: '', role: '', email: '' }],
  });

  const [errors, setErrors] = useState<ValidationErrors>({});

  const validate = (): boolean => {
    const newErrors: ValidationErrors = {};

    if (!formData.name.trim()) {
      newErrors.name = 'Enter a local authority name';
    }
    if (!formData.onsCode.trim()) {
      newErrors.onsCode = 'Enter an ONS code';
    }

    const contactErrors: ContactValidationErrors[] = [];
    formData.contacts.forEach((contact, index) => {
      const errors: ContactValidationErrors = {};
      if (!contact.fullName.trim()) {
        errors.fullName = 'Enter a full name';
      }
      if (!contact.role) {
        errors.role = 'Select a role';
      }
      if (contact.role === 'other' && !contact.roleTitle?.trim()) {
        errors.roleTitle = 'Enter a role title';
      }
      if (!contact.email.trim()) {
        errors.email = 'Enter an email address';
      }
      contactErrors[index] = errors;
    });

    if (contactErrors.some(e => Object.keys(e).length > 0)) {
      newErrors.contacts = contactErrors;
    }

    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  const handleContinue = (e: React.FormEvent) => {
    e.preventDefault();
    if (validate()) {
      setStep('review');
      onStepChange?.('review');
    }
  };

  const handleBack = () => {
    setStep('form');
    onStepChange?.('form');
  };

  const handleSubmit = () => {
    onSubmit(formData);
  };

  const handleChange = (field: keyof LocalAuthorityFormData, value: string | boolean) => {
    setFormData((prev) => ({ ...prev, [field]: value }));
    if (errors[field as keyof ValidationErrors]) {
      setErrors((prev) => ({ ...prev, [field]: undefined }));
    }
  };

  const handleContactChange = (index: number, field: keyof ContactPerson, value: string) => {
    setFormData((prev) => {
      const newContacts = [...prev.contacts];
      newContacts[index] = { ...newContacts[index], [field]: value };
      return { ...prev, contacts: newContacts };
    });
    if (errors.contacts?.[index]?.[field]) {
      setErrors((prev) => {
        const newContactErrors = [...(prev.contacts || [])];
        if (newContactErrors[index]) {
          newContactErrors[index] = { ...newContactErrors[index], [field]: undefined };
        }
        return { ...prev, contacts: newContactErrors };
      });
    }
  };

  const addContact = () => {
    setFormData((prev) => ({
      ...prev,
      contacts: [...prev.contacts, { fullName: '', role: '', email: '' }],
    }));
  };

  const removeContact = (index: number) => {
    setFormData((prev) => ({
      ...prev,
      contacts: prev.contacts.filter((_, i) => i !== index),
    }));
  };

  if (isLoading) {
    return <LoadingBox loading={true}><div style={{ minHeight: '200px' }} /></LoadingBox>;
  }

  const getRoleDisplayName = (role: string, roleTitle?: string): string => {
    if (role === 'other' && roleTitle) return roleTitle;
    const roleMap: Record<string, string> = {
      'admin': 'Administrator',
      'manager': 'Manager',
      'data-entry': 'Data Entry',
      'other': roleTitle || 'Other'
    };
    return roleMap[role] || role;
  };

  if (step === 'review') {
    return (
      <LoadingBox loading={isSaving}>
        <div className="la-form-review">
          <GovUKBackLink onClick={handleBack}>Back</GovUKBackLink>

          <h1 className="govuk-heading-l">Add a local authority</h1>

          <h2 className="govuk-heading-m">Check your answers before creating the local authority account</h2>
          <p className="govuk-body">
            Check the information you've entered before creating the local authority account. 
            Make sure the details are accurate and complete. You can go back to make changes if anything needs updating.
          </p>

          <dl className="govuk-summary-list la-form-review-list">
            <div className="govuk-summary-list__row">
              <dt className="govuk-summary-list__key">Name</dt>
              <dd className="govuk-summary-list__value">{formData.name}</dd>
            </div>
            <div className="govuk-summary-list__row">
              <dt className="govuk-summary-list__key">ONS code</dt>
              <dd className="govuk-summary-list__value">{formData.onsCode}</dd>
            </div>
            {formData.contacts.map((contact, index) => (
              <div key={index} className="govuk-summary-list__row">
                <dt className="govuk-summary-list__key">Contact person</dt>
                <dd className="govuk-summary-list__value">
                  <div>{contact.fullName}</div>
                  <div>{getRoleDisplayName(contact.role, contact.roleTitle)}</div>
                  <div><a href={`mailto:${contact.email}`} className="govuk-link">{contact.email}</a></div>
                </dd>
              </div>
            ))}
          </dl>

          <div className="la-form-actions">
            <Button onClick={handleSubmit} disabled={isSaving}>
              Confirm and create
            </Button>
          </div>
        </div>
      </LoadingBox>
    );
  }

  return (
    <form onSubmit={handleContinue} className="la-form">
      <GovUKBackLink onClick={onCancel}>Back</GovUKBackLink>

      <h1 className="govuk-heading-l">{isEdit ? 'Edit local authority' : 'Add a local authority'}</h1>

      <div className="govuk-form-group">
        <label className="govuk-label govuk-label--s" htmlFor="la-name">Name</label>
        {errors.name && <p className="govuk-error-message"><span className="govuk-visually-hidden">Error:</span> {errors.name}</p>}
        <input
          className={`govuk-input ${errors.name ? 'govuk-input--error' : ''}`}
          id="la-name"
          name="name"
          type="text"
          value={formData.name}
          onChange={(e) => handleChange('name', e.target.value)}
        />
      </div>

      <div className="govuk-form-group">
        <label className="govuk-label govuk-label--s" htmlFor="la-ons-code">ONS code</label>
        {errors.onsCode && <p className="govuk-error-message"><span className="govuk-visually-hidden">Error:</span> {errors.onsCode}</p>}
        <input
          className={`govuk-input ${errors.onsCode ? 'govuk-input--error' : ''}`}
          id="la-ons-code"
          name="onsCode"
          type="text"
          value={formData.onsCode}
          onChange={(e) => handleChange('onsCode', e.target.value)}
        />
      </div>

      {formData.contacts.map((contact, index) => (
        <div key={index} className="la-form-contact-group">
          <div className="la-form-contact-header">
            <h2 className="govuk-heading-m">Contact person</h2>
            {formData.contacts.length > 1 && (
              <button
                type="button"
                className="govuk-button govuk-button--secondary la-form-remove-contact"
                onClick={() => removeContact(index)}
              >
                Remove
              </button>
            )}
          </div>

          <div className="govuk-form-group">
            <label className="govuk-label govuk-label--s" htmlFor={`contact-name-${index}`}>Full name</label>
            {errors.contacts?.[index]?.fullName && <p className="govuk-error-message"><span className="govuk-visually-hidden">Error:</span> {errors.contacts[index].fullName}</p>}
            <input
              className={`govuk-input ${errors.contacts?.[index]?.fullName ? 'govuk-input--error' : ''}`}
              id={`contact-name-${index}`}
              name={`contact-name-${index}`}
              type="text"
              value={contact.fullName}
              onChange={(e) => handleContactChange(index, 'fullName', e.target.value)}
            />
          </div>

          <div className="govuk-form-group">
            <label className="govuk-label govuk-label--s" htmlFor={`contact-role-${index}`}>Role</label>
            {errors.contacts?.[index]?.role && <p className="govuk-error-message"><span className="govuk-visually-hidden">Error:</span> {errors.contacts[index].role}</p>}
            <select
              className={`govuk-select ${errors.contacts?.[index]?.role ? 'govuk-select--error' : ''}`}
              id={`contact-role-${index}`}
              name={`contact-role-${index}`}
              value={contact.role}
              onChange={(e) => handleContactChange(index, 'role', e.target.value)}
            >
              <option value="">Select a role...</option>
              <option value="admin">Administrator</option>
              <option value="manager">Manager</option>
              <option value="data-entry">Data Entry</option>
              <option value="other">Other...</option>
            </select>
          </div>

          {contact.role === 'other' && (
            <div className="govuk-form-group la-form-role-title">
              <label className="govuk-label govuk-label--s" htmlFor={`contact-role-title-${index}`}>Role title</label>
              {errors.contacts?.[index]?.roleTitle && <p className="govuk-error-message"><span className="govuk-visually-hidden">Error:</span> {errors.contacts[index].roleTitle}</p>}
              <input
                className={`govuk-input ${errors.contacts?.[index]?.roleTitle ? 'govuk-input--error' : ''}`}
                id={`contact-role-title-${index}`}
                name={`contact-role-title-${index}`}
                type="text"
                value={contact.roleTitle || ''}
                onChange={(e) => handleContactChange(index, 'roleTitle', e.target.value)}
              />
            </div>
          )}

          <div className="govuk-form-group">
            <label className="govuk-label govuk-label--s" htmlFor={`contact-email-${index}`}>Email address</label>
            <p className="govuk-hint">Enter the user's work email address. They will use it to sign in and receive notifications about the service. Do not use a group email address or distribution list.</p>
            {errors.contacts?.[index]?.email && <p className="govuk-error-message"><span className="govuk-visually-hidden">Error:</span> {errors.contacts[index].email}</p>}
            <input
              className={`govuk-input ${errors.contacts?.[index]?.email ? 'govuk-input--error' : ''}`}
              id={`contact-email-${index}`}
              name={`contact-email-${index}`}
              type="email"
              value={contact.email}
              onChange={(e) => handleContactChange(index, 'email', e.target.value)}
            />
          </div>
        </div>
      ))}

      <button type="button" className="govuk-button govuk-button--secondary la-form-add-contact" onClick={addContact}>
        Add another contact person
      </button>

      <div className="govuk-form-group la-form-checkbox-group">
        <div className="govuk-checkboxes" data-module="govuk-checkboxes">
          <div className="govuk-checkboxes__item">
            <input
              className="govuk-checkboxes__input"
              id="la-is-active"
              name="isActive"
              type="checkbox"
              checked={formData.isActive}
              onChange={(e) => handleChange('isActive', e.target.checked)}
            />
            <label className="govuk-label govuk-checkboxes__label" htmlFor="la-is-active">
              Automatically activate the local authority account when it is created
            </label>
          </div>
        </div>
      </div>

      <div className="la-form-actions">
        <Button type="submit">Continue</Button>
      </div>
    </form>
  );
};

export default LocalAuthorityForm;
