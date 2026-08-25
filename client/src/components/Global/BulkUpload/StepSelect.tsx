import React from 'react';
import { GovUKButton } from '../../../components/GovUKComponents';
import { ServiceSelection, BulkUploadModuleType } from './types';
import './styles.css';

interface StepSelectProps {
  serviceSelections: ServiceSelection[];
  moduleType: BulkUploadModuleType;
  isLoading: boolean;
  onToggleService: (rowIndex: number, updateFromFile: boolean) => void;
  onConfirm: () => void;
  onStartOver: () => void;
}

const StepSelect: React.FC<StepSelectProps> = ({
  serviceSelections,
  moduleType,
  isLoading,
  onToggleService,
  onConfirm,
  onStartOver,
}) => {
  const isOutcomeScores = moduleType === 'outcome-scores';
  const isActivity = moduleType === 'activity';
  const servicesWithExisting = serviceSelections.filter((s) => s.hasExistingData);
  const servicesNew = serviceSelections.filter((s) => !s.hasExistingData);

  const canConfirm = serviceSelections.some((s) => !s.hasExistingData || s.updateFromFile);

  // Nothing in the file could be matched to a record. Guarded against upstream, but this step used
  // to render as a blank list with a permanently disabled Confirm button and no control but the
  // layout back-link, so keep a way out rather than trusting the guards to hold.
  const isEmpty = serviceSelections.length === 0;

  const heading = isOutcomeScores ? 'Review outcome score data' : isActivity ? 'Choose activities you want to update' : 'Choose services you want to update';

  if (isEmpty) {
    return (
      <div className="bulk-upload-step">
        <div className="govuk-!-width-two-thirds">
          <span className="govuk-caption-l">Step 4 of 5</span>
          <h1 className="govuk-heading-l">{heading}</h1>
          <p className="govuk-body">
            We could not match any row in your file to {isActivity ? 'an activity' : 'a service'}, so there is nothing to save. Check that every row names{' '}
            {isActivity ? 'an activity' : 'a service'}, then upload the file again.
          </p>
          <GovUKButton onClick={onStartOver}>Upload a different file</GovUKButton>
        </div>
      </div>
    );
  }

  return (
    <div className="bulk-upload-step">
      <div className="govuk-!-width-two-thirds">
        <span className="govuk-caption-l">Step 4 of 5</span>
        <h1 className="govuk-heading-l">{heading}</h1>
      </div>

      <div className="govuk-!-width-two-thirds govuk-!-margin-bottom-6">
        {isOutcomeScores ? (
          <>
            {servicesWithExisting.length > 0 && (
              <p className="govuk-body">
                The uploaded file includes outcome score data for services that already have records saved for this
                period. For each of these services, choose whether to keep the existing records or add data from the
                uploaded file.
              </p>
            )}
            {servicesNew.length > 0 && (
              <p className="govuk-body">Services with no existing records will have new data added automatically.</p>
            )}
          </>
        ) : isActivity ? (
          <>
            <p className="govuk-body">
              The uploaded file includes activities below. Some activities may already exist with the same name in
              this delivery plan.
            </p>
            <p className="govuk-body">
              For each existing activity, choose whether to keep the existing data or replace it with the data from
              the uploaded file. Activities with no existing match will be added as new.
            </p>
          </>
        ) : (
          <>
            <p className="govuk-body">
              The uploaded file includes user data for the services below. Some services may already have user data
              saved for this quarter.
            </p>
            <p className="govuk-body">
              For each service, choose whether to keep the existing data or replace it with the data from the uploaded
              file.
            </p>
          </>
        )}
      </div>

      <div className="bulk-upload-summary-list">
        {serviceSelections.map((selection, idx) => (
          <div key={`${selection.serviceName}-${selection.rowIndex}`} className="bulk-upload-summary-list__row">
            {/* Row identifier / service name */}
            <div className="bulk-upload-summary-list__key">
              {isOutcomeScores ? (
                <p className="govuk-body govuk-!-font-weight-bold govuk-!-margin-bottom-0">
                  Row {selection.rowIndex + 1}
                </p>
              ) : (
                <p className="govuk-body govuk-!-font-weight-bold govuk-!-margin-bottom-0">{selection.serviceName}</p>
              )}
            </div>

            {/* Radio options or new-data indicator */}
            <div className="bulk-upload-summary-list__value">
              {selection.hasExistingData ? (
                <fieldset className="govuk-fieldset govuk-radios--small bulk-upload-summary-list__fieldset">
                  <legend className="govuk-fieldset__legend">
                    <span className="govuk-visually-hidden">Data update option for {selection.serviceName}</span>
                  </legend>

                  <div className="bulk-upload-summary-list__option">
                    <div className="govuk-radios__item">
                      <input
                        type="radio"
                        id={`selection-update-${idx}`}
                        name={`selection-${idx}`}
                        checked={selection.updateFromFile}
                        onChange={() => onToggleService(selection.rowIndex, true)}
                        className="govuk-radios__input"
                      />
                      <label className="govuk-label govuk-radios__label" htmlFor={`selection-update-${idx}`}>
                        Update from this file
                      </label>
                    </div>
                    {selection.lastUpdated && (
                      <div className="govuk-!-padding-left-7">
                        <p className="govuk-body-s govuk-!-margin-bottom-0">Form draft saved on</p>
                        <p className="govuk-body-s govuk-!-margin-bottom-0">{selection.lastUpdated}</p>
                      </div>
                    )}
                  </div>

                  <div className="bulk-upload-summary-list__option">
                    <div className="govuk-radios__item">
                      <input
                        type="radio"
                        id={`selection-keep-${idx}`}
                        name={`selection-${idx}`}
                        checked={!selection.updateFromFile}
                        onChange={() => onToggleService(selection.rowIndex, false)}
                        className="govuk-radios__input"
                      />
                      <label className="govuk-label govuk-radios__label" htmlFor={`selection-keep-${idx}`}>
                        Keep existing data
                      </label>
                    </div>
                  </div>
                </fieldset>
              ) : (
                <p className="govuk-body-s govuk-!-margin-bottom-0 govuk-colour-secondary">
                  New data will be added from this file
                </p>
              )}
            </div>
          </div>
        ))}
      </div>

      <div className="govuk-!-margin-bottom-6">
        <h2 className="govuk-heading-m">
          {isOutcomeScores ? 'Now save your outcome score data' : isActivity ? 'Now save your activity data' : 'Now save your service user data'}
        </h2>
        <div className="govuk-!-width-two-thirds">
          <p className="govuk-body">
            By saving the data you selected, you confirm that, to the best of your knowledge, the details you are
            providing are correct. You may return and update these details before you submit this quarterly
            Management Information data collection.
          </p>
        </div>
      </div>

      <GovUKButton onClick={onConfirm} disabled={!canConfirm} isLoading={isLoading}>
        Confirm and save
      </GovUKButton>
    </div>
  );
};

export default StepSelect;
