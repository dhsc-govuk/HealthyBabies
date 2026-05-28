import React from 'react';
import { useNavigate } from 'react-router-dom';
import { GeneralLayout } from '../../../../layouts';
import BulkUploadComponent from '../../../../components/Global/Organisations/Locations/BulkUpload';
import { useAuthProvider } from '../../../../components/AuthProvider';

interface NotificationData {
  type: 'success' | 'important';
  title: string;
  message: string;
}

const LocationBulkUpload = (): React.ReactElement => {
  const navigate = useNavigate();
  const { organisationId } = useAuthProvider();

  const handleFinish = (notification?: NotificationData) => {
    navigate(`/organisation-admin/core-data/delivery-locations`, {
      state: notification ? { notification } : undefined,
    });
  };

  return (
    <GeneralLayout
      breadcrumbs={[
        { label: 'Home', link: '/organisation-admin/home' },
        { label: 'Delivery locations', link: `/organisation-admin/core-data/delivery-locations` },
        { label: 'Bulk Upload', link: '' },
      ]}
      currentPage="Bulk Upload">
      <BulkUploadComponent organisationId={organisationId!} handleFinish={handleFinish} />
    </GeneralLayout>
  );
};

export default LocationBulkUpload;
