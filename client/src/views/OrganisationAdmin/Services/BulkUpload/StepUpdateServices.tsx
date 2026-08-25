import React, { useState, useMemo } from 'react';
import { ServiceUpdateSelection } from './types';
import { GovUKTableScroll } from '../../../../components/GovUKComponents';
import { BulkUploadServicesResultDto } from '../../../../components/Global/Services/mutations';
import './styles.css';

interface StepUpdateServicesProps {
  selections: ServiceUpdateSelection[];
  updateResult: BulkUploadServicesResultDto | null;
  isLoading: boolean;
  onToggleSelection: (serviceName: string, updateFromFile: boolean) => void;
  onSubmit: () => void;
  onFinish: () => void;
}

const StepUpdateServices: React.FC<StepUpdateServicesProps> = ({
  selections,
  updateResult,
  isLoading,
  onToggleSelection,
  onSubmit,
  onFinish,
}) => {
  const [showExistingDataOnly, setShowExistingDataOnly] = useState(false);

  const servicesWithExistingData = useMemo(
    () => selections.filter((s) => s.hasExistingData),
    [selections]
  );

  const displayedSelections = showExistingDataOnly ? servicesWithExistingData : selections;

  const selectedForUpdate = useMemo(
    () => selections.filter((s) => s.updateFromFile),
    [selections]
  );

  const formatDateTime = (dateString?: string) => {
    if (!dateString) return '';
    const date = new Date(dateString);
    return date.toLocaleDateString('en-GB', {
      day: 'numeric',
      month: 'short',
      year: 'numeric',
    }) + ' ' + date.toLocaleTimeString('en-GB', {
      hour: '2-digit',
      minute: '2-digit',
    });
  };

  const getStatusLabel = (selection: ServiceUpdateSelection) => {
    if (selection.isNew) return 'New service';
    if (selection.status === undefined) return 'Not started';
    switch (selection.status) {
      case 0:
        return 'Not started';
      case 1:
        return 'In progress';
      case 2:
        return 'Completed';
      default:
        return 'Not started';
    }
  };

  const getDataSourceLabel = (selection: ServiceUpdateSelection) => {
    if (!selection.hasExistingData) return 'No saved data';
    // Determine if data was from form or CSV based on status
    if (selection.status === 2) {
      return 'Form submitted on';
    }
    if (selection.status === 1) {
      return 'Form draft saved on';
    }
    return 'CSV uploaded on';
  };

  // Show error results after submission (success redirects to services list)
  if (updateResult) {
    return (
      <div>
        <div className="bulk-upload-content">
          <p className="bulk-upload-caption">Step 4 of 4</p>
          <h1 className="bulk-upload-heading">Update completed with errors</h1>
        </div>

        <div
          style={{
            border: '4px solid #d5281b',
            padding: '20px',
            marginBottom: '30px',
          }}
        >
          <h2
            style={{
              fontSize: '19px',
              fontWeight: 700,
              color: '#d5281b',
              margin: '0 0 10px 0',
            }}
          >
            Some services failed to update
          </h2>
          <p style={{ fontSize: '19px', color: '#0b0c0c', margin: 0 }}>
            {updateResult.successCount} service{updateResult.successCount !== 1 ? 's' : ''} updated
            successfully. {updateResult.errorCount} service{updateResult.errorCount !== 1 ? 's' : ''}{' '}
            failed to update.
          </p>
        </div>

        <div className="bulk-upload-section">
          <h3 className="bulk-upload-heading--medium">Results</h3>
          <GovUKTableScroll label="Services to update table">
          <table className="bulk-upload-table">
            <caption className="govuk-visually-hidden">Service update results</caption>
            <thead>
              <tr>
                <th>Service name</th>
                <th>Action</th>
                <th>Status</th>
                <th>Error</th>
              </tr>
            </thead>
            <tbody>
              {updateResult.results.map((result, index) => (
                <tr key={result.serviceId || index}>
                  <td>{result.serviceName}</td>
                  <td>{result.isNew ? 'Created' : 'Updated'}</td>
                  <td>
                    <span
                      style={{
                        color: result.isSuccess ? '#007f3b' : '#d5281b',
                        fontWeight: 700,
                      }}
                    >
                      {result.isSuccess ? 'Success' : 'Failed'}
                    </span>
                  </td>
                  <td>{result.errorMessage || '-'}</td>
                </tr>
              ))}
            </tbody>
          </table>
          </GovUKTableScroll>
        </div>

        <button type="button" className="govuk-button" onClick={onFinish}>
          Return to services list
        </button>
      </div>
    );
  }

  return (
    <div>
      {/* Step caption and heading */}
      <div className="bulk-upload-content">
        <p className="bulk-upload-caption">Step 4 of 4</p>
        <h1 className="bulk-upload-heading">Choose services you want to update</h1>
      </div>

      {/* Description */}
      <div className="bulk-upload-content bulk-upload-section">
        <p className="bulk-upload-body">
          The uploaded file includes user data for the services below. Some services may already
          have user data saved for this quarter.
        </p>
        <p className="bulk-upload-body">
          For each service, choose whether to keep the existing data or replace it with the data
          from the uploaded file.
        </p>
      </div>

      {/* Show only services with existing data checkbox */}
      {servicesWithExistingData.length > 0 && (
        <div style={{ marginBottom: '30px' }}>
          <label className="bulk-upload-checkbox-small">
            <input
              type="checkbox"
              checked={showExistingDataOnly}
              onChange={(e) => setShowExistingDataOnly(e.target.checked)}
              className="bulk-upload-checkbox-small__input"
            />
            <span className="bulk-upload-checkbox-small__label">
              Show only services with existing data
            </span>
          </label>
        </div>
      )}

      {/* Summary list of services */}
      <div className="bulk-upload-summary-list">
        {displayedSelections.map((selection, selectionIndex) => (
          <div key={selection.serviceName} className="bulk-upload-summary-list__row">
            {/* Service name and status */}
            <div className="bulk-upload-summary-list__key">
              <p className="bulk-upload-summary-list__service-name">{selection.serviceName}</p>
              <p className="bulk-upload-summary-list__service-status">
                {getStatusLabel(selection)}
                {selection.isNew && <span className="bulk-upload-tag bulk-upload-tag--new">NEW</span>}
              </p>
            </div>

            {/* Radio options */}
            {/* Radio options. The fieldset + legend are what associate the two radios with the
                service they belong to; without them a screen reader announces "Update from this
                file" with no indication of which service, since the name is only in an adjacent
                cell. The legend is visually hidden because the name and status are already
                shown in the row. */}
            <fieldset className="govuk-fieldset govuk-radios--small bulk-upload-summary-list__value">
              <legend className="govuk-fieldset__legend">
                <span className="govuk-visually-hidden">
                  Data update option for {selection.serviceName}, {getStatusLabel(selection)}
                </span>
              </legend>
              {/* Update from file option */}
              <div className="bulk-upload-summary-list__option">
                <label className="bulk-upload-radio-small" htmlFor={`service-selection-update-${selectionIndex}`}>
                  <input
                    id={`service-selection-update-${selectionIndex}`}
                    type="radio"
                    name={`selection-${selection.serviceName}`}
                    checked={selection.updateFromFile}
                    onChange={() => onToggleSelection(selection.serviceName, true)}
                    className="bulk-upload-radio-small__input"
                  />
                  <span className="bulk-upload-radio-small__label">
                    {selection.isNew ? 'Create this service' : 'Update from this file'}
                  </span>
                </label>
              </div>

              {/* Keep existing / Do not update option */}
              <div className="bulk-upload-summary-list__option">
                <label className="bulk-upload-radio-small" htmlFor={`service-selection-keep-${selectionIndex}`}>
                  <input
                    id={`service-selection-keep-${selectionIndex}`}
                    type="radio"
                    name={`selection-${selection.serviceName}`}
                    checked={!selection.updateFromFile}
                    onChange={() => onToggleSelection(selection.serviceName, false)}
                    className="bulk-upload-radio-small__input"
                  />
                  <span className="bulk-upload-radio-small__label">
                    {selection.hasExistingData ? 'Keep existing data' : 'Do not update'}
                  </span>
                </label>
                <div className="bulk-upload-summary-list__option-info">
                  {selection.hasExistingData ? (
                    <>
                      <p>{getDataSourceLabel(selection)}</p>
                      <p>{formatDateTime(selection.lastUpdated)}</p>
                    </>
                  ) : (
                    <p>No saved data</p>
                  )}
                </div>
              </div>
            </fieldset>
          </div>
        ))}
      </div>

      {/* Now save section */}
      <div className="bulk-upload-section">
        <h2 className="bulk-upload-heading--medium">Now save your service user data</h2>
        <div className="bulk-upload-content">
          <p className="bulk-upload-body">
            By saving user data for the services you selected, you confirm that, you are confirming
            that, to the best of your knowledge, the details you are providing are correct. You may
            return and update these details before you submit this quarterly Management Information
            data collection.
          </p>
        </div>
      </div>

      {/* Submit button */}
      <div>
        <button
          type="button"
          className="govuk-button"
          onClick={onSubmit}
          disabled={selectedForUpdate.length === 0 || isLoading}
        >
          {isLoading ? 'Saving...' : 'Confirm and save'}
        </button>
      </div>
    </div>
  );
};

export default StepUpdateServices;
