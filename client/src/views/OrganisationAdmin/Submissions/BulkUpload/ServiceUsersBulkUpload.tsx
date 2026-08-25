import React from 'react';
import { useParams } from 'react-router-dom';
import BulkUpload from '../../../../components/Global/BulkUpload';
import { downloadBulkUploadTemplate, validateBulkUpload, processStagedBulkUpload, checkServiceUserDuplicates } from '../queries';
import { ROUTES } from '../../../../constants/routes';

const ServiceUsersBulkUpload: React.FC = () => {
  const { submissionId, moduleId } = useParams<{
    submissionId: string;
    moduleId: string;
  }>();

  return (
    <BulkUpload
      moduleType="service-users"
      moduleName="Service users"
      backUrl={ROUTES.organisationAdmin.submissions.serviceUsers.list(submissionId, moduleId)}
      api={{
        downloadTemplate: downloadBulkUploadTemplate,
        validate: validateBulkUpload,
        processStaged: processStagedBulkUpload,
        checkServiceUserDuplicates,
      }}
    />
  );
};

export default ServiceUsersBulkUpload;
