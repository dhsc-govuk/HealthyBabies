import React, { useEffect } from 'react';
import { useNavigate, useLocation } from 'react-router-dom';
import { Button, GovUKNotificationBanner, H1, Paragraph } from '../../../../components/GovUKComponents';
import { CoreDataLayout } from '../../../../layouts';
import ListLocationsComponent from '../../../../components/Global/Organisations/Locations/List';
import { useAuthProvider } from '../../../../components/AuthProvider';
import usePageTitle from '../../../../hooks/usePageTitle';

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
    navigate(`/organisation-admin/core-data/delivery-locations/${locationId}`);
  };

  const handleEdit = (locationId: string) => {
    navigate(`/organisation-admin/core-data/delivery-locations/${locationId}/edit`);
  };

  const handleCreate = () => {
    navigate(`/organisation-admin/core-data/delivery-locations/create`);
  };

  const handleBulkUpload = () => {
    navigate(`/organisation-admin/core-data/delivery-locations/bulk-upload`);
  };

  return (
    <CoreDataLayout breadcrumbs={[{ label: 'Home', link: '/organisation-admin/home' }]}>
      {notification && (
        <GovUKNotificationBanner type={notification.type} title={notification.title} autoDismiss>
          <p>{notification.message}</p>
        </GovUKNotificationBanner>
      )}
      <H1>Manage delivery locations</H1>
      <Paragraph>This page lets you add, view, change or delete delivery location in your local authority.</Paragraph>
      <Paragraph>You can update or remove delivery location at any time to keep your information accurate.</Paragraph>
      <div style={{ display: 'flex', gap: '8px', marginBottom: '20px' }}>
        <Button onClick={handleBulkUpload} variant="secondary">
          Bulk Upload
        </Button>
        <Button onClick={handleCreate} variant="secondary">
          Create
        </Button>
      </div>
      <ListLocationsComponent organisationId={organisationId!} handleView={handleView} handleEdit={handleEdit} handleCreate={handleCreate} />
    </CoreDataLayout>
  );
};

export default ListLocations;
