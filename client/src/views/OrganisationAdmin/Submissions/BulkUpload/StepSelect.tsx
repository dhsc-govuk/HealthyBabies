import React from 'react';
import { GovUKButton } from '../../../../components/GovUKComponents';
import { ServiceSelection } from './types';
import './styles.css';

interface StepSelectProps {
  serviceSelections: ServiceSelection[];
  isLoading: boolean;
  onToggleService: (serviceName: string, updateFromFile: boolean) => void;
  onConfirm: () => void;
}

const StepSelect: React.FC<StepSelectProps> = ({
  serviceSelections,
  isLoading,
  onToggleService,
  onConfirm,
}) => {
  return (
    <div className="bulk-upload-step">
      {/* Step caption and heading */}
      <div className="govuk-!-width-two-thirds">
        <span className="govuk-caption-l">Step 4 of 5</span>
        <h1 className="govuk-heading-l">Choose services you want to update</h1>
      </div>

      {/* Description */}
      <div className="govuk-!-width-two-thirds govuk-!-margin-bottom-6">
        <p className="govuk-body">
          The uploaded file includes user data for the services below. Some services may already
          have user data saved for this quarter.
        </p>
        <p className="govuk-body">
          For each service, choose whether to keep the existing data or replace it with the data
          from the uploaded file.
        </p>
      </div>

      {/* Service selection list */}
      <div className="bulk-upload-summary-list">
        {serviceSelections.map((selection, idx) => (
          <div key={selection.serviceName} className="bulk-upload-summary-list__row">
            {/* Service name and status */}
            <div className="bulk-upload-summary-list__key">
              <p className="govuk-body govuk-!-font-weight-bold govuk-!-margin-bottom-1">{selection.serviceName}</p>
              <p className="govuk-body-s govuk-!-margin-bottom-0">{selection.status}</p>
            </div>

            {/* Radio options */}
            <div className="bulk-upload-summary-list__value">
              {/* Update from file option */}
              <div className="bulk-upload-summary-list__option">
                <div className="govuk-radios govuk-radios--small">
                  <div className="govuk-radios__item">
                    <input
                      type="radio"
                      id={`selection-update-${idx}`}
                      name={`selection-${selection.serviceName}`}
                      checked={selection.updateFromFile}
                      onChange={() => onToggleService(selection.serviceName, true)}
                      className="govuk-radios__input"
                    />
                    <label className="govuk-label govuk-radios__label" htmlFor={`selection-update-${idx}`}>
                      Update from this file
                    </label>
                  </div>
                </div>
              </div>

              {/* Keep existing option */}
              <div className="bulk-upload-summary-list__option">
                <div className="govuk-radios govuk-radios--small">
                  <div className="govuk-radios__item">
                    <input
                      type="radio"
                      id={`selection-keep-${idx}`}
                      name={`selection-${selection.serviceName}`}
                      checked={!selection.updateFromFile}
                      onChange={() => onToggleService(selection.serviceName, false)}
                      className="govuk-radios__input"
                    />
                    <label className="govuk-label govuk-radios__label" htmlFor={`selection-keep-${idx}`}>
                      Keep existing data
                    </label>
                  </div>
                </div>
                {selection.lastUpdated && (
                  <div className="govuk-!-margin-top-1 govuk-!-padding-left-7">
                    <p className="govuk-body-s govuk-!-margin-bottom-0">Form draft saved on</p>
                    <p className="govuk-body-s govuk-!-margin-bottom-0">{selection.lastUpdated}</p>
                  </div>
                )}
              </div>
            </div>
          </div>
        ))}
      </div>

      {/* Now save section */}
      <div className="govuk-!-margin-bottom-6">
        <h2 className="govuk-heading-m">Now save your service user data</h2>
        <div className="govuk-!-width-two-thirds">
          <p className="govuk-body">
            By saving user data for the services you selected, you confirm that, to the best of your knowledge,
            the details you are providing are correct. You may return and update these details before you submit
            this quarterly Management Information data collection.
          </p>
        </div>
      </div>

      {/* Submit button */}
      <GovUKButton
        onClick={onConfirm}
        disabled={serviceSelections.filter((s) => s.updateFromFile).length === 0}
        isLoading={isLoading}
      >
        Confirm and save
      </GovUKButton>
    </div>
  );
};

export default StepSelect;
