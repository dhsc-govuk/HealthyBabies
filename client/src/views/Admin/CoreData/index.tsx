import React from 'react';
import { useMutation } from 'react-query';

import { SettingsLayout } from '../../../layouts';
import { useGovUKNotification } from '../../../components/GovUKComponents';
import { processError } from '../../../helpers/axiosErrorFallback';
import { downloadServicesCsv, downloadSitesCsv, triggerCsvDownload } from './queries';
import usePageTitle from '../../../hooks/usePageTitle';

function CoreData(): React.ReactElement {
  usePageTitle('Core data downloads');
  const { setNotification } = useGovUKNotification();

  const { mutate: downloadSites, isLoading: downloadingSites } = useMutation({
    mutationFn: downloadSitesCsv,
    onSuccess: (response) => {
      triggerCsvDownload(response.data, 'sites.csv');
    },
    onError: (err) => {
      processError(err, (e) => setNotification({ type: 'important', title: 'Error', message: e }));
    },
  });

  const { mutate: downloadServices, isLoading: downloadingServices } = useMutation({
    mutationFn: downloadServicesCsv,
    onSuccess: (response) => {
      triggerCsvDownload(response.data, 'services.csv');
    },
    onError: (err) => {
      processError(err, (e) => setNotification({ type: 'important', title: 'Error', message: e }));
    },
  });

  return (
    <SettingsLayout>
      <h1 className="govuk-heading-l">Core data downloads</h1>

      <div className="govuk-grid-row govuk-!-margin-top-6" style={{ display: 'flex' }}>
        <div className="govuk-grid-column-one-half" style={{ display: 'flex', flexDirection: 'column' }}>
          <div className="govuk-summary-card" style={{ flex: 1 }}>
            <div className="govuk-summary-card__title-wrapper">
              <h2 className="govuk-summary-card__title">Sites</h2>
            </div>
            <div className="govuk-summary-card__content">
              <p className="govuk-body">Download all service delivery locations across all local authorities, including their site form question answers.</p>
              <button
                type="button"
                className="govuk-button govuk-button--secondary"
                disabled={downloadingSites}
                onClick={() => downloadSites()}
              >
                {downloadingSites ? 'Downloading...' : 'Download sites (CSV)'}
              </button>
            </div>
          </div>
        </div>

        <div className="govuk-grid-column-one-half" style={{ display: 'flex', flexDirection: 'column' }}>
          <div className="govuk-summary-card" style={{ flex: 1 }}>
            <div className="govuk-summary-card__title-wrapper">
              <h2 className="govuk-summary-card__title">Services</h2>
            </div>
            <div className="govuk-summary-card__content">
              <p className="govuk-body">Download all services across all local authorities, including their service form question answers.</p>
              <button
                type="button"
                className="govuk-button govuk-button--secondary"
                disabled={downloadingServices}
                onClick={() => downloadServices()}
              >
                {downloadingServices ? 'Downloading...' : 'Download services (CSV)'}
              </button>
            </div>
          </div>
        </div>
      </div>
    </SettingsLayout>
  );
}

export default CoreData;
