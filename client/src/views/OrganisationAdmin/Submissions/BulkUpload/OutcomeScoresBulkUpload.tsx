import React from 'react';
import { useParams } from 'react-router-dom';
import BulkUpload from './index';

const OutcomeScoresBulkUpload: React.FC = () => {
  const { submissionId, moduleId } = useParams<{
    submissionId: string;
    moduleId: string;
  }>();

  return (
    <BulkUpload
      moduleType="outcome-scores"
      moduleName="Outcome scores"
      backUrl={`/organisation-admin/submissions/${submissionId}/modules/${moduleId}/outcome-scores`}
    />
  );
};

export default OutcomeScoresBulkUpload;
