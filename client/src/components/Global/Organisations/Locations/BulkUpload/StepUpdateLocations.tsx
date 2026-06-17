import React, { useState, useMemo } from 'react';
import { LocationUpdateSelection } from './types';
import { BulkUpdateLocationsResult } from './mutations';
import './styles.css';

interface StepUpdateLocationsProps {
  selections: LocationUpdateSelection[];
  updateResult: BulkUpdateLocationsResult | null;
  isLoading: boolean;
  onToggleSelection: (siteName: string, updateFromFile: boolean) => void;
  onBack: () => void;
  onSubmit: () => void;
  onFinish: () => void;
}

const StepUpdateLocations: React.FC<StepUpdateLocationsProps> = ({
  selections,
  updateResult,
  isLoading,
  onToggleSelection,
  onBack,
  onSubmit,
  onFinish,
}) => {
  const [showExistingDataOnly, setShowExistingDataOnly] = useState(false);

  const locationsWithExistingData = useMemo(
    () => selections.filter((s) => s.hasExistingData),
    [selections]
  );

  const displayedSelections = showExistingDataOnly ? locationsWithExistingData : selections;

  const selectedForUpdate = useMemo(
    () => selections.filter((s) => s.updateFromFile),
    [selections]
  );

  const getStatusLabel = (selection: LocationUpdateSelection) => {
    if (selection.isNew) return 'New site';
    return selection.isActive ? 'Active' : 'Inactive';
  };

  // Show error results after submission (success redirects to sites list)
  if (updateResult) {
    return (
      <div>
        <div className="bulk-upload-content">
          <p className="bulk-upload-caption">Step 4 of 4</p>
          <h1 className="bulk-upload-heading">Update completed with errors</h1>
        </div>

        <div className="bulk-upload-error-panel">
          <h2 className="bulk-upload-error-panel__title">
            Some sites failed to update
          </h2>
          <p className="bulk-upload-error-panel__body">
            {updateResult.successCount} site{updateResult.successCount !== 1 ? 's' : ''} updated
            successfully. {updateResult.errorCount} site{updateResult.errorCount !== 1 ? 's' : ''}{' '}
            failed to update.
          </p>
        </div>

        <div className="bulk-upload-section">
          <h3 className="bulk-upload-heading--medium">Results</h3>
          <table className="bulk-upload-table">
            <thead>
              <tr>
                <th>Site name</th>
                <th>Status</th>
                <th>Action</th>
                <th>Error</th>
              </tr>
            </thead>
            <tbody>
              {updateResult.results.map((result, index) => (
                <tr key={result.locationId || index}>
                  <td>{result.siteName}</td>
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
                  <td>{result.isNew ? 'Created' : 'Updated'}</td>
                  <td>{result.errorMessage || '-'}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>

        <button type="button" className="bulk-upload-button" onClick={onFinish}>
          Return to sites list
        </button>
      </div>
    );
  }

  return (
    <div>
      {/* Step caption and heading */}
      <div className="bulk-upload-content">
        <p className="bulk-upload-caption">Step 4 of 4</p>
        <h1 className="bulk-upload-heading">Choose delivery locations you want to update or create</h1>
      </div>

      {/* Description */}
      <div className="bulk-upload-content bulk-upload-section">
        <p className="bulk-upload-body">
          The uploaded file includes data for the sites below. Some sites may already exist in the
          system.
        </p>
        <p className="bulk-upload-body">
          For each site, choose whether to update from the file or keep the existing data. New sites
          will be created automatically.
        </p>
      </div>

      {/* Show only sites with existing data checkbox */}
      {locationsWithExistingData.length > 0 && (
        <div style={{ marginBottom: '30px' }}>
          <label className="bulk-upload-checkbox-small">
            <input
              type="checkbox"
              checked={showExistingDataOnly}
              onChange={(e) => setShowExistingDataOnly(e.target.checked)}
              className="bulk-upload-checkbox-small__input"
            />
            <span className="bulk-upload-checkbox-small__label">
              Show only sites with existing data
            </span>
          </label>
        </div>
      )}

      {/* Summary list of sites */}
      <div className="bulk-upload-summary-list">
        {displayedSelections.map((selection) => (
          <div key={selection.siteName} className="bulk-upload-summary-list__row">
            {/* Site name and status */}
            <div className="bulk-upload-summary-list__key">
              <p className="bulk-upload-summary-list__service-name">{selection.siteName}</p>
              <p className="bulk-upload-summary-list__service-status">
                {getStatusLabel(selection)}
              </p>
            </div>

            {/* Radio options */}
            <div className="bulk-upload-summary-list__value">
              {/* Update from file option */}
              <div className="bulk-upload-summary-list__option">
                <label className="bulk-upload-radio-small">
                  <input
                    type="radio"
                    name={`selection-${selection.siteName}`}
                    checked={selection.updateFromFile}
                    onChange={() => onToggleSelection(selection.siteName, true)}
                    className="bulk-upload-radio-small__input"
                  />
                  <span className="bulk-upload-radio-small__label">
                    {selection.isNew ? 'Create new site' : 'Update from this file'}
                  </span>
                </label>
              </div>

              {/* Keep existing / Do not update option */}
              {!selection.isNew && (
                <div className="bulk-upload-summary-list__option">
                  <label className="bulk-upload-radio-small">
                    <input
                      type="radio"
                      name={`selection-${selection.siteName}`}
                      checked={!selection.updateFromFile}
                      onChange={() => onToggleSelection(selection.siteName, false)}
                      className="bulk-upload-radio-small__input"
                    />
                    <span className="bulk-upload-radio-small__label">
                      {selection.hasExistingData ? 'Keep existing data' : 'Do not update'}
                    </span>
                  </label>
                  <div className="bulk-upload-summary-list__option-info">
                    {selection.hasExistingData ? (
                      <p>Site has existing data</p>
                    ) : (
                      <p>No saved data</p>
                    )}
                  </div>
                </div>
              )}
            </div>
          </div>
        ))}
      </div>

      {/* Now save section */}
      <div className="bulk-upload-section">
        <h2 className="bulk-upload-heading--medium">Now save your site data</h2>
        <div className="bulk-upload-content">
          <p className="bulk-upload-body">
            By saving data for the sites you selected, you are confirming that, to the best of your
            knowledge, the details you are providing are correct.
          </p>
        </div>
      </div>

      {/* Submit button */}
      <div>
        <button
          type="button"
          className="bulk-upload-button"
          onClick={onSubmit}
          disabled={selectedForUpdate.length === 0 || isLoading}
        >
          {isLoading ? 'Saving...' : 'Confirm and save'}
        </button>
      </div>
    </div>
  );
};

export default StepUpdateLocations;
