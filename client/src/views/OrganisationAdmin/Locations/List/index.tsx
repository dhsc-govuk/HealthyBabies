import React, { useEffect } from 'react';
import { useNavigate, useLocation } from 'react-router-dom';
import { useMutation } from 'react-query';
import { GovUKButton, GovUKNotificationBanner, H1, Paragraph, useGovUKNotification } from '../../../../components/GovUKComponents';
import { CoreDataLayout } from '../../../../layouts';
import ListLocationsComponent from '../../../../components/Global/Organisations/Locations/List';
import { downloadSitesExport } from '../../../../components/Global/Organisations/Locations/List/queries';
import { useAuthProvider } from '../../../../components/AuthProvider';
import { processError } from '../../../../helpers/axiosErrorFallback';
import usePageTitle from '../../../../hooks/usePageTitle';
import { ROUTES } from '../../../../constants/routes';

interface LocationState {
  notification?: {
    type: 'success' | 'important';
    title: string;
    message: string;
  };
}

const ListLocations = (): React.ReactElement => {
  usePageTitle('Delivery locations');
  const { organisationId } = useAuthProvider();

  const navigate = useNavigate();
  const location = useLocation();
  const locationState = location.state as LocationState | null;
  const notification = locationState?.notification;

  // Scroll to top and clear the location state when showing notification
  useEffect(() => {
    if (notification) {
      window.scrollTo(0, 0);
      window.history.replaceState({}, document.title);
    }
  }, [notification]);

  const handleView = (locationId: string) => {
    navigate(ROUTES.organisationAdmin.locations.view(locationId));
  };

  const handleEdit = (locationId: string) => {
    navigate(ROUTES.organisationAdmin.locations.edit(locationId));
  };

  const handleCreate = () => {
    navigate(ROUTES.organisationAdmin.locations.create);
  };

  const handleBulkUpload = () => {
    navigate(ROUTES.organisationAdmin.locations.bulkUpload);
  };

  const { setNotification } = useGovUKNotification();

  // BLUM-7251: the LA's own submitted sites data as CSV, scoped server-side to this organisation.
  const { mutate: downloadExport, isLoading: downloadingExport } = useMutation({
    mutationKey: ['download-sites-export'],
    mutationFn: downloadSitesExport,
    onSuccess: (blob) => {
      const url = window.URL.createObjectURL(blob);
      const link = document.createElement('a');
      link.href = url;
      link.download = 'sites.csv';
      document.body.appendChild(link);
      link.click();
      document.body.removeChild(link);
      window.URL.revokeObjectURL(url);
    },
    onError: (error) => {
      processError(error, (message) => setNotification({ type: 'important', title: 'Error', message }));
    },
  });

  return (
    <CoreDataLayout breadcrumbs={[{ label: 'Home', link: ROUTES.organisationAdmin.home }]}>
      {notification && (
        <GovUKNotificationBanner type={notification.type} title={notification.title} autoDismiss>
          <p>{notification.message}</p>
        </GovUKNotificationBanner>
      )}
      <H1>Manage delivery locations</H1>
      <Paragraph>This page lets you add, view, change or delete delivery location in your local authority.</Paragraph>
      <Paragraph>You can update or remove delivery location at any time to keep your information accurate.</Paragraph>
      {/* Labels describe the action rather than the mechanism (WCAG 2.4.6). "Add delivery
          location" is the primary action, "Upload with CSV" the secondary one — govuk-react's
          Button has no `variant` prop, so the GOV.UK modifier class is applied directly. */}
      <div style={{ display: 'flex', flexWrap: 'wrap', gap: '8px', marginBottom: '20px' }}>
        <GovUKButton onClick={handleCreate}>Add delivery location</GovUKButton>
        <GovUKButton onClick={handleBulkUpload} className="govuk-button govuk-button--secondary">
          Upload with CSV
        </GovUKButton>
        <GovUKButton onClick={() => downloadExport()} disabled={downloadingExport} className="govuk-button govuk-button--secondary">
          Download copy in CSV
        </GovUKButton>
      </div>
      <ListLocationsComponent organisationId={organisationId!} handleView={handleView} handleEdit={handleEdit} handleCreate={handleCreate} />
    </CoreDataLayout>
  );
};

export default ListLocations;
