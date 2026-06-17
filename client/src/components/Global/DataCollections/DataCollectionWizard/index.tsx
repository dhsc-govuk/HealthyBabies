import React, { useState, useMemo } from 'react';
import { LoadingBox, ErrorSummary } from 'govuk-react';
import { GovUKDateField, GovUKStepper } from '../../../GovUKComponents';
import SearchIcon from '../../../Logos/SearchIcon';
import useErrorSummaryFocus from '../../../../hooks/useErrorSummaryFocus';
import './styles.css';

export interface LocalAuthorityOption {
  id: string;
  name: string;
}

export interface FormModuleOption {
  id: string;
  sectionNumber: number;
  name: string;
  lastChangedOn: string;
}

export interface DataCollectionWizardData {
  name: string;
  description: string;
  startDate: string;
  endDate: string;
  formModuleIds: string[];
  isSubmittedByAllLocalAuthorities: boolean;
  localAuthorityIds: string[];
}

interface ValidationErrors {
  name?: string;
  startDate?: string;
  endDate?: string;
  formModules?: string;
  localAuthorities?: string;
}

export interface DataCollectionWizardProps {
  initialData?: Partial<DataCollectionWizardData>;
  availableLocalAuthorities?: LocalAuthorityOption[];
  availableFormModules?: FormModuleOption[];
  isLoading?: boolean;
  isSaving?: boolean;
  onSubmit: (data: DataCollectionWizardData) => void;
  onSaveAsDraft: (data: DataCollectionWizardData) => void;
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

const DataCollectionWizard = ({
  initialData,
  availableLocalAuthorities = [],
  availableFormModules = [],
  isLoading = false,
  isSaving = false,
  onSubmit,
  onSaveAsDraft,
  submitLabel = 'Confirm and create',
  isEdit = false,
}: DataCollectionWizardProps): React.ReactElement => {
  const [step, setStep] = useState(0);

  const [formData, setFormData] = useState<DataCollectionWizardData>({
    name: initialData?.name ?? '',
    description: initialData?.description ?? '',
    startDate: initialData?.startDate ?? '',
    endDate: initialData?.endDate ?? '',
    formModuleIds: initialData?.formModuleIds ?? [],
    isSubmittedByAllLocalAuthorities: initialData?.isSubmittedByAllLocalAuthorities ?? true,
    localAuthorityIds: initialData?.localAuthorityIds ?? [],
  });

  const [errors, setErrors] = useState<ValidationErrors>({});
  const [submitAttempts, setSubmitAttempts] = useState(0);
  useErrorSummaryFocus(submitAttempts, Object.values(errors).some(Boolean));
  const [laSearchTerm, setLaSearchTerm] = useState('');

  const validateStep = (currentStep: number): boolean => {
    const newErrors: ValidationErrors = {};

    if (currentStep === 0) {
      if (!formData.name.trim()) {
        newErrors.name = 'Enter a data collection name';
      }
      if (!formData.startDate) {
        newErrors.startDate = 'Enter a start date';
      }
      if (!formData.endDate) {
        newErrors.endDate = 'Enter a due date';
      }
      if (formData.startDate && formData.endDate) {
        const startParts = formData.startDate.split('/');
        const endParts = formData.endDate.split('/');
        if (startParts.length === 3 && endParts.length === 3) {
          const start = new Date(parseInt(startParts[2]), parseInt(startParts[1]) - 1, parseInt(startParts[0]));
          const end = new Date(parseInt(endParts[2]), parseInt(endParts[1]) - 1, parseInt(endParts[0]));
          if (start >= end) {
            newErrors.endDate = 'Due date must be after start date';
          }
        }
      }
    }

    if (currentStep === 1) {
      if (formData.formModuleIds.length === 0) {
        newErrors.formModules = 'Select at least one form';
      }
    }

    if (currentStep === 2) {
      if (!formData.isSubmittedByAllLocalAuthorities && formData.localAuthorityIds.length === 0) {
        newErrors.localAuthorities = 'Select at least one local authority';
      }
    }

    setErrors(newErrors);
    setSubmitAttempts((n) => n + 1);
    return Object.keys(newErrors).length > 0;
  };

  const handleComplete = () => {
    onSubmit(formData);
  };

  const handleSaveAsDraft = () => {
    onSaveAsDraft(formData);
  };

  const handleChange = <K extends keyof DataCollectionWizardData>(field: K, value: DataCollectionWizardData[K]) => {
    setFormData((prev) => ({ ...prev, [field]: value }));
    if (errors[field as keyof ValidationErrors]) {
      setErrors((prev) => ({ ...prev, [field]: undefined }));
    }
  };

  const handleLaToggle = (laId: string) => {
    setFormData((prev) => ({
      ...prev,
      localAuthorityIds: prev.localAuthorityIds.includes(laId) ? prev.localAuthorityIds.filter((id) => id !== laId) : [...prev.localAuthorityIds, laId],
    }));
    if (errors.localAuthorities) {
      setErrors((prev) => ({ ...prev, localAuthorities: undefined }));
    }
  };

  const handleSelectAllLas = () => {
    const allSelected = availableLocalAuthorities.every((la) => formData.localAuthorityIds.includes(la.id));
    if (allSelected) {
      setFormData((prev) => ({ ...prev, localAuthorityIds: [] }));
    } else {
      setFormData((prev) => ({
        ...prev,
        localAuthorityIds: availableLocalAuthorities.map((la) => la.id),
      }));
    }
    if (errors.localAuthorities) {
      setErrors((prev) => ({ ...prev, localAuthorities: undefined }));
    }
  };

  const handleFormModuleToggle = (moduleId: string) => {
    setFormData((prev) => ({
      ...prev,
      formModuleIds: prev.formModuleIds.includes(moduleId) ? prev.formModuleIds.filter((id) => id !== moduleId) : [...prev.formModuleIds, moduleId],
    }));
    if (errors.formModules) {
      setErrors((prev) => ({ ...prev, formModules: undefined }));
    }
  };

  const selectedFormModulesDisplay = useMemo(() => {
    return availableFormModules.filter((fm) => formData.formModuleIds.includes(fm.id)).sort((a, b) => a.sectionNumber - b.sectionNumber);
  }, [availableFormModules, formData.formModuleIds]);

  const filteredLocalAuthorities = useMemo(() => {
    if (!laSearchTerm.trim()) return availableLocalAuthorities;
    return availableLocalAuthorities.filter((la) => la.name.toLowerCase().includes(laSearchTerm.toLowerCase()));
  }, [availableLocalAuthorities, laSearchTerm]);

  const errorList = useMemo(() => {
    const list: { targetName: string; text: string }[] = [];
    if (errors.name) list.push({ targetName: 'dc-name', text: errors.name });
    if (errors.startDate) list.push({ targetName: 'startDate-day', text: errors.startDate });
    if (errors.endDate) list.push({ targetName: 'endDate-day', text: errors.endDate });
    if (errors.formModules) list.push({ targetName: 'form-modules-fieldset', text: errors.formModules });
    if (errors.localAuthorities) list.push({ targetName: 'la-fieldset', text: errors.localAuthorities });
    return list;
  }, [errors]);

  if (isLoading) {
    return (
      <LoadingBox loading={true}>
        <div style={{ minHeight: '200px' }} />
      </LoadingBox>
    );
  }

  const step1Content = (
    <div className="dc-wizard">
      {errorList.length > 0 && <ErrorSummary errors={errorList} />}

      <div className="govuk-form-group">
        <label className="govuk-label govuk-label--s" htmlFor="dc-name">
          Data collection name
        </label>
        <div className="govuk-hint">This is how the data collection will be shown to users.</div>
        {errors.name && (
          <span className="govuk-error-message">
            <span className="govuk-visually-hidden">Error:</span> {errors.name}
          </span>
        )}
        <input
          className={`govuk-input ${errors.name ? 'govuk-input--error' : ''}`}
          id="dc-name"
          name="name"
          type="text"
          value={formData.name}
          onChange={(e) => handleChange('name', e.target.value)}
        />
      </div>

      <GovUKDateField
        id="startDate"
        legend="Start date"
        legendSize="s"
        value={formData.startDate}
        onChange={(value) => handleChange('startDate', value)}
        error={errors.startDate}
        hint="This is when data collection will open and submissions will be accepted."
      />

      <GovUKDateField
        id="endDate"
        legend="Due date"
        legendSize="s"
        value={formData.endDate}
        onChange={(value) => handleChange('endDate', value)}
        error={errors.endDate}
        hint="This is when data collection will close. No further submissions will be possible after this date unless an extension is granted."
      />
    </div>
  );

  const step2Content = (
    <div className="dc-wizard">
      {errorList.length > 0 && <ErrorSummary errors={errorList} />}

      <fieldset className="govuk-fieldset" id="form-modules-fieldset">
        <legend className="govuk-fieldset__legend govuk-fieldset__legend--m">Forms</legend>
        <div className="govuk-hint">Select the forms to include in this data collection.</div>

        {errors.formModules && (
          <span className="govuk-error-message">
            <span className="govuk-visually-hidden">Error:</span> {errors.formModules}
          </span>
        )}

        <div className="govuk-checkboxes">
          {availableFormModules
            .sort((a, b) => a.sectionNumber - b.sectionNumber)
            .map((fm) => (
              <div className="govuk-checkboxes__item" key={fm.id}>
                <input
                  className="govuk-checkboxes__input"
                  id={`fm-${fm.id}`}
                  name="formModules"
                  type="checkbox"
                  value={fm.id}
                  checked={formData.formModuleIds.includes(fm.id)}
                  onChange={() => handleFormModuleToggle(fm.id)}
                />
                <label className="govuk-label govuk-checkboxes__label" htmlFor={`fm-${fm.id}`}>
                  Section {fm.sectionNumber}: {fm.name}
                  <br />
                  <span className="govuk-hint govuk-!-margin-bottom-0" style={{ marginTop: '4px' }}>
                    Last changed on {new Date(fm.lastChangedOn).toLocaleDateString('en-GB', { day: 'numeric', month: 'long', year: 'numeric' })}
                  </span>
                </label>
              </div>
            ))}
        </div>
      </fieldset>
    </div>
  );

  const step3Content = (
    <div className="dc-wizard">
      {errorList.length > 0 && <ErrorSummary errors={errorList} />}

      <fieldset className="govuk-fieldset" id="la-fieldset">
        <legend className="govuk-fieldset__legend govuk-fieldset__legend--m">Is this data collection submitted by all local authorities?</legend>

        <div className="govuk-radios" data-module="govuk-radios">
          <div className="govuk-radios__item">
            <input
              className="govuk-radios__input"
              id="la-all-yes"
              name="isSubmittedByAllLocalAuthorities"
              type="radio"
              value="yes"
              checked={formData.isSubmittedByAllLocalAuthorities}
              onChange={() => handleChange('isSubmittedByAllLocalAuthorities', true)}
            />
            <label className="govuk-label govuk-radios__label" htmlFor="la-all-yes">
              Yes
            </label>
          </div>
          <div className="govuk-radios__item">
            <input
              className="govuk-radios__input"
              id="la-all-no"
              name="isSubmittedByAllLocalAuthorities"
              type="radio"
              value="no"
              checked={!formData.isSubmittedByAllLocalAuthorities}
              onChange={() => handleChange('isSubmittedByAllLocalAuthorities', false)}
            />
            <label className="govuk-label govuk-radios__label" htmlFor="la-all-no">
              No
            </label>
          </div>
        </div>

        {!formData.isSubmittedByAllLocalAuthorities && (
          <div className="dc-wizard__conditional-section">
            <h3 className="govuk-heading-s govuk-!-margin-bottom-1">Local authorities</h3>
            <p className="govuk-hint">Select the local authorities to assign this data collection to.</p>

            {errors.localAuthorities && (
              <span className="govuk-error-message">
                <span className="govuk-visually-hidden">Error:</span> {errors.localAuthorities}
              </span>
            )}

            <div className="govuk-form-group">
              <label className="govuk-label" htmlFor="la-search">
                Search local authority by name
              </label>
              <div className="dc-wizard__search-container">
                <input
                  className="govuk-input dc-wizard__search-input"
                  id="la-search"
                  name="la-search"
                  type="text"
                  value={laSearchTerm}
                  onChange={(e) => setLaSearchTerm(e.target.value)}
                />
                <button type="button" className="dc-wizard__search-button" aria-label="Search">
                  <SearchIcon />
                </button>
              </div>
            </div>

            {filteredLocalAuthorities.length > 0 && (
              <div className="govuk-checkboxes govuk-!-margin-top-4 govuk-!-margin-bottom-7">
                <div className="govuk-checkboxes__item">
                  <input
                    className="govuk-checkboxes__input"
                    id="la-select-all"
                    name="la-select-all"
                    type="checkbox"
                    checked={availableLocalAuthorities.length > 0 && availableLocalAuthorities.every((la) => formData.localAuthorityIds.includes(la.id))}
                    ref={(el) => {
                      if (el) {
                        const totalSelected = formData.localAuthorityIds.length;
                        const allCount = availableLocalAuthorities.length;
                        el.indeterminate = totalSelected > 0 && totalSelected < allCount;
                      }
                    }}
                    onChange={handleSelectAllLas}
                  />
                  <label className="govuk-label govuk-checkboxes__label" htmlFor="la-select-all">
                    All
                  </label>
                </div>
              </div>
            )}

            <div className="govuk-checkboxes">
              {filteredLocalAuthorities.length === 0 ? (
                <p className="govuk-body">No local authorities found matching your search.</p>
              ) : (
                filteredLocalAuthorities.map((la) => (
                  <div className="govuk-checkboxes__item" key={la.id}>
                    <input
                      className="govuk-checkboxes__input"
                      id={`la-${la.id}`}
                      name="localAuthorities"
                      type="checkbox"
                      value={la.id}
                      checked={formData.localAuthorityIds.includes(la.id)}
                      onChange={() => handleLaToggle(la.id)}
                    />
                    <label className="govuk-label govuk-checkboxes__label" htmlFor={`la-${la.id}`}>
                      {la.name}
                    </label>
                  </div>
                ))
              )}
            </div>
          </div>
        )}
      </fieldset>
    </div>
  );

  const step4Content = (
    <LoadingBox loading={isSaving}>
      <div className="dc-wizard">
        <h2 className="govuk-heading-m">Check your answers before saving the data collection</h2>
        <p className="govuk-body">
          Check the information you've entered before saving the data collection. Make sure the details are accurate and complete. You can go back to make changes if anything needs
          updating.
        </p>
        <dl className="govuk-summary-list">
          <div className="govuk-summary-list__row">
            <dt className="govuk-summary-list__key">Data collection name</dt>
            <dd className="govuk-summary-list__value">{formData.name}</dd>
            <dd className="govuk-summary-list__actions">
              <button type="button" className="govuk-link" style={{ background: 'none', border: 'none', padding: 0, cursor: 'pointer' }} onClick={() => setStep(0)}>
                Change<span className="govuk-visually-hidden"> name</span>
              </button>
            </dd>
          </div>
          <div className="govuk-summary-list__row">
            <dt className="govuk-summary-list__key">Start date</dt>
            <dd className="govuk-summary-list__value">{formatDateForDisplay(formData.startDate)}</dd>
            <dd className="govuk-summary-list__actions">
              <button type="button" className="govuk-link" style={{ background: 'none', border: 'none', padding: 0, cursor: 'pointer' }} onClick={() => setStep(0)}>
                Change<span className="govuk-visually-hidden"> start date</span>
              </button>
            </dd>
          </div>
          <div className="govuk-summary-list__row">
            <dt className="govuk-summary-list__key">Due date</dt>
            <dd className="govuk-summary-list__value">{formatDateForDisplay(formData.endDate)}</dd>
            <dd className="govuk-summary-list__actions">
              <button type="button" className="govuk-link" style={{ background: 'none', border: 'none', padding: 0, cursor: 'pointer' }} onClick={() => setStep(0)}>
                Change<span className="govuk-visually-hidden"> due date</span>
              </button>
            </dd>
          </div>
          <div className="govuk-summary-list__row">
            <dt className="govuk-summary-list__key">Forms</dt>
            <dd className="govuk-summary-list__value">
              {selectedFormModulesDisplay.length > 0
                ? selectedFormModulesDisplay.map((fm) => (
                    <div key={fm.id}>
                      Section {fm.sectionNumber}: {fm.name}
                    </div>
                  ))
                : 'None selected'}
            </dd>
            <dd className="govuk-summary-list__actions">
              <button type="button" className="govuk-link" style={{ background: 'none', border: 'none', padding: 0, cursor: 'pointer' }} onClick={() => setStep(1)}>
                Change<span className="govuk-visually-hidden"> forms</span>
              </button>
            </dd>
          </div>
          <div className="govuk-summary-list__row">
            <dt className="govuk-summary-list__key">Is this data collection submitted by all local authorities?</dt>
            <dd className="govuk-summary-list__value">{formData.isSubmittedByAllLocalAuthorities ? 'Yes' : 'No'}</dd>
            <dd className="govuk-summary-list__actions">
              <button type="button" className="govuk-link" style={{ background: 'none', border: 'none', padding: 0, cursor: 'pointer' }} onClick={() => setStep(2)}>
                Change<span className="govuk-visually-hidden"> local authorities</span>
              </button>
            </dd>
          </div>
        </dl>

        <h2 className="govuk-heading-m">Now save the data collection</h2>
        <p className="govuk-body">When you save this data collection, it will automatically open on the start date and begin accepting submissions.</p>
        <p className="govuk-body">Until the start date, its status will be 'Planned'.</p>
        <p className="govuk-body">The collection will automatically close on the due date, unless it is closed manually before then.</p>
      </div>
    </LoadingBox>
  );

  const stepperSteps = [
    { label: isEdit ? 'Edit data collection' : 'Add a data collection', component: step1Content },
    { label: isEdit ? 'Edit data collection' : 'Add a data collection', component: step2Content },
    { label: isEdit ? 'Edit data collection' : 'Add a data collection', component: step3Content },
    { label: isEdit ? 'Edit data collection' : 'Add a data collection', component: step4Content },
  ];

  return (
    <GovUKStepper
      step={step}
      setStep={setStep}
      stepperSteps={stepperSteps}
      completeLabel={submitLabel}
      handleComplete={handleComplete}
      validate={validateStep}
      isNextDisabled={isSaving}
      title={stepperSteps[step].label}
      onSaveAsDraft={handleSaveAsDraft}
      showSaveAsDraft={!isEdit}
    />
  );
};

export default DataCollectionWizard;
