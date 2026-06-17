import React, { useState, useMemo } from 'react';
import { Button, LoadingBox, ErrorSummary } from 'govuk-react';
import { GovUKFieldset, GovUKDateField, GovUKButton, GovUKBackLink } from '../../../GovUKComponents';
import useErrorSummaryFocus from '../../../../hooks/useErrorSummaryFocus';
import './styles.css';

export interface DataCollectionFormData {
  name: string;
  description: string;
  startDate: string;
  endDate: string;
}

interface ValidationErrors {
  name?: string;
  startDate?: string;
  endDate?: string;
}

export interface DataCollectionFormProps {
  initialData?: Partial<DataCollectionFormData>;
  isLoading?: boolean;
  isSaving?: boolean;
  onSubmit: (data: DataCollectionFormData, saveAsDraft: boolean) => void;
  onCancel: () => void;
  submitLabel?: string;
  isEdit?: boolean;
}

const formatDateForDisplay = (dateStr: string): string => {
  if (!dateStr) return '';
  const parts = dateStr.split('/');
  if (parts.length === 3) {
    const [day, month, year] = parts;
    const date = new Date(parseInt(year), parseInt(month) - 1, parseInt(day));
    return date.toLocaleDateString('en-GB', {
      day: 'numeric',
      month: 'long',
      year: 'numeric',
    });
  }
  return dateStr;
};

const DataCollectionForm = ({
  initialData,
  isLoading = false,
  isSaving = false,
  onSubmit,
  onCancel,
  submitLabel = 'Confirm and create',
  isEdit = false,
}: DataCollectionFormProps): React.ReactElement => {
  const [step, setStep] = useState<'form' | 'review'>('form');

  const [formData, setFormData] = useState<DataCollectionFormData>({
    name: initialData?.name ?? '',
    description: initialData?.description ?? '',
    startDate: initialData?.startDate ?? '',
    endDate: initialData?.endDate ?? '',
  });

  const [errors, setErrors] = useState<ValidationErrors>({});
  const [submitAttempts, setSubmitAttempts] = useState(0);
  useErrorSummaryFocus(submitAttempts, Object.values(errors).some(Boolean));

  const validate = (): boolean => {
    const newErrors: ValidationErrors = {};

    if (!formData.name.trim()) {
      newErrors.name = 'Enter a data collection name';
    }
    if (!formData.startDate) {
      newErrors.startDate = 'Enter a start date';
    }
    if (!formData.endDate) {
      newErrors.endDate = 'Enter an end date';
    }

    if (formData.startDate && formData.endDate) {
      const startParts = formData.startDate.split('/');
      const endParts = formData.endDate.split('/');
      if (startParts.length === 3 && endParts.length === 3) {
        const start = new Date(parseInt(startParts[2]), parseInt(startParts[1]) - 1, parseInt(startParts[0]));
        const end = new Date(parseInt(endParts[2]), parseInt(endParts[1]) - 1, parseInt(endParts[0]));
        if (start >= end) {
          newErrors.endDate = 'End date must be after start date';
        }
      }
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

  const handleBack = () => {
    setStep('form');
  };

  const handleSubmit = (saveAsDraft: boolean) => {
    onSubmit(formData, saveAsDraft);
  };

  const handleChange = (field: keyof DataCollectionFormData, value: string) => {
    setFormData((prev) => ({ ...prev, [field]: value }));
    if (errors[field as keyof ValidationErrors]) {
      setErrors((prev) => ({ ...prev, [field]: undefined }));
    }
  };

  const errorList = useMemo(() => {
    const list: { targetName: string; text: string }[] = [];
    if (errors.name) list.push({ targetName: 'dc-name', text: errors.name });
    if (errors.startDate) list.push({ targetName: 'startDate-day', text: errors.startDate });
    if (errors.endDate) list.push({ targetName: 'endDate-day', text: errors.endDate });
    return list;
  }, [errors]);

  if (isLoading) {
    return <LoadingBox loading={true}><div style={{ minHeight: '200px' }} /></LoadingBox>;
  }

  if (step === 'review') {
    return (
      <LoadingBox loading={isSaving}>
        <div className="dc-form-review">
          <GovUKBackLink onClick={handleBack}>Back</GovUKBackLink>
          <h2 className="dc-form-review-title">Check your answers</h2>

          <dl className="govuk-summary-list govuk-summary-list--no-outer-border">
            <div className="govuk-summary-list__row">
              <dt className="govuk-summary-list__key">Name</dt>
              <dd className="govuk-summary-list__value">{formData.name}</dd>
              <dd className="govuk-summary-list__actions">
                <button type="button" className="govuk-link" style={{ background: 'none', border: 'none', padding: 0, cursor: 'pointer' }} onClick={handleBack}>
                  Change
                </button>
              </dd>
            </div>
            <div className="govuk-summary-list__row">
              <dt className="govuk-summary-list__key">Description</dt>
              <dd className="govuk-summary-list__value">{formData.description || 'Not provided'}</dd>
              <dd className="govuk-summary-list__actions">
                <button type="button" className="govuk-link" style={{ background: 'none', border: 'none', padding: 0, cursor: 'pointer' }} onClick={handleBack}>
                  Change
                </button>
              </dd>
            </div>
            <div className="govuk-summary-list__row">
              <dt className="govuk-summary-list__key">Start date</dt>
              <dd className="govuk-summary-list__value">{formatDateForDisplay(formData.startDate)}</dd>
              <dd className="govuk-summary-list__actions">
                <button type="button" className="govuk-link" style={{ background: 'none', border: 'none', padding: 0, cursor: 'pointer' }} onClick={handleBack}>
                  Change
                </button>
              </dd>
            </div>
            <div className="govuk-summary-list__row">
              <dt className="govuk-summary-list__key">End date</dt>
              <dd className="govuk-summary-list__value">{formatDateForDisplay(formData.endDate)}</dd>
              <dd className="govuk-summary-list__actions">
                <button type="button" className="govuk-link" style={{ background: 'none', border: 'none', padding: 0, cursor: 'pointer' }} onClick={handleBack}>
                  Change
                </button>
              </dd>
            </div>
          </dl>

          <div className="govuk-button-group">
            <Button onClick={() => handleSubmit(false)} disabled={isSaving}>
              {submitLabel}
            </Button>
            <GovUKButton className="govuk-button govuk-button--secondary" onClick={() => handleSubmit(true)} disabled={isSaving}>
              Save as draft and exit
            </GovUKButton>
          </div>
        </div>
      </LoadingBox>
    );
  }

  return (
    <form onSubmit={handleContinue} className="dc-form" noValidate>
      <GovUKBackLink onClick={onCancel}>Back</GovUKBackLink>

      {errorList.length > 0 && <ErrorSummary errors={errorList} />}

      <GovUKFieldset legend={isEdit ? 'Edit data collection' : 'Data collection details'} legendSize="l">
        <GovUKFieldset.Input
          id="dc-name"
          name="name"
          label="Name"
          hint="Enter the name of the data collection"
          value={formData.name}
          error={errors.name}
          onChange={(e: React.ChangeEvent<HTMLInputElement>) => handleChange('name', e.target.value)}
          onBlur={() => {
            if (!formData.name.trim()) {
              setErrors((prev) => ({ ...prev, name: 'Enter a data collection name' }));
            }
          }}
        />
        <GovUKFieldset.Textarea
          id="dc-description"
          name="description"
          label="Description"
          hint="Optional description for this data collection"
          value={formData.description}
          onChange={(e: React.ChangeEvent<HTMLTextAreaElement>) => handleChange('description', e.target.value)}
        />
      </GovUKFieldset>

      <GovUKDateField
        id="startDate"
        legend="Start date"
        legendSize="s"
        value={formData.startDate}
        onChange={(value) => handleChange('startDate', value)}
        error={errors.startDate}
        hint="For example, 27 3 2024"
        onBlur={() => {
          if (!formData.startDate) {
            setErrors((prev) => ({ ...prev, startDate: 'Enter a start date' }));
          }
        }}
      />

      <GovUKDateField
        id="endDate"
        legend="End date"
        legendSize="s"
        value={formData.endDate}
        onChange={(value) => handleChange('endDate', value)}
        error={errors.endDate}
        hint="For example, 27 6 2024"
        onBlur={() => {
          if (!formData.endDate) {
            setErrors((prev) => ({ ...prev, endDate: 'Enter an end date' }));
          } else if (formData.startDate && formData.endDate) {
            const startParts = formData.startDate.split('/');
            const endParts = formData.endDate.split('/');
            if (startParts.length === 3 && endParts.length === 3) {
              const start = new Date(parseInt(startParts[2]), parseInt(startParts[1]) - 1, parseInt(startParts[0]));
              const end = new Date(parseInt(endParts[2]), parseInt(endParts[1]) - 1, parseInt(endParts[0]));
              if (start >= end) {
                setErrors((prev) => ({ ...prev, endDate: 'End date must be after start date' }));
              }
            }
          }
        }}
      />

      <div className="govuk-button-group">
        <Button type="submit">Continue</Button>
        <GovUKButton className="govuk-button govuk-button--secondary" onClick={() => handleSubmit(true)} type="button">
          Save as draft and exit
        </GovUKButton>
      </div>
    </form>
  );
};

export default DataCollectionForm;
