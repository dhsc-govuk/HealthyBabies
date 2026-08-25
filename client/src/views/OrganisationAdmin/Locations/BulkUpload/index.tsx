import React from 'react';
import { useNavigate } from 'react-router-dom';
import { GeneralLayout } from '../../../../layouts';
import BulkUploadComponent from '../../../../components/Global/Organisations/Locations/BulkUpload';
import { useAuthProvider } from '../../../../components/AuthProvider';
import { ROUTES } from '../../../../constants/routes';

interface NotificationData {
  type: 'success' | 'important';
  title: string;
  message: string;
}

const LocationBulkUpload = (): React.ReactElement => {
  const navigate = useNavigate();
  const { organisationId } = useAuthProvider();

  const handleFinish = (notification?: NotificationData) => {
    navigate(ROUTES.organisationAdmin.locations.list, {
      state: notification ? { notification } : undefined,
    });
  };

  return (
    <GeneralLayout>
      <BulkUploadComponent organisationId={organisationId!} handleFinish={handleFinish} />
    </GeneralLayout>
  );
};

export default LocationBulkUpload;
