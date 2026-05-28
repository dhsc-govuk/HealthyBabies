import React from 'react';
import { useMutation } from 'react-query';

import { GeneralLayout } from '../../../layouts';
import { useGovUKNotification } from '../../../components/GovUKComponents';
import { processError } from '../../../helpers/axiosErrorFallback';
import { downloadServicesCsv, downloadSitesCsv, triggerCsvDownload } from './queries';

function CoreData(): React.ReactElement {
  const { setNotification } = useGovUKNotification();

  const breadcrumbItems = [{ label: 'Home', link: '/admin/home' }];

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
    <GeneralLayout breadcrumbs={breadcrumbItems} currentPage="Core data downloads">
      <h1 className="govuk-heading-xl">Core data downloads</h1>
      <p className="govuk-body-l">Download sites and services data as CSV files. Each file includes a LA Name column to allow filtering by local authority.</p>

      <div className="govuk-grid-row govuk-!-margin-top-6">
        <div className="govuk-grid-column-one-half">
          <div className="govuk-summary-card">
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

        <div className="govuk-grid-column-one-half">
          <div className="govuk-summary-card">
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
    </GeneralLayout>
  );
}

export default CoreData;
