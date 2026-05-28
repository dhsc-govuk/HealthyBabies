import React from 'react';
import { useParams } from 'react-router-dom';
import BulkUpload from './index';

const ServiceUsersBulkUpload: React.FC = () => {
  const { submissionId, moduleId } = useParams<{
    submissionId: string;
    moduleId: string;
  }>();

  return (
    <BulkUpload
      moduleType="service-users"
      moduleName="Service users"
      backUrl={`/organisation-admin/submissions/${submissionId}/modules/${moduleId}/services`}
    />
  );
};

export default ServiceUsersBulkUpload;
