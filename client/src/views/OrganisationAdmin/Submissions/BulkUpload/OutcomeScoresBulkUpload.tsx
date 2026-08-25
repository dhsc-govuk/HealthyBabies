import React from 'react';
import { useParams } from 'react-router-dom';
import BulkUpload from '../../../../components/Global/BulkUpload';
import { downloadBulkUploadTemplate, validateBulkUpload, processStagedBulkUpload, checkOutcomeScoreDuplicates } from '../queries';
import { ROUTES } from '../../../../constants/routes';

const OutcomeScoresBulkUpload: React.FC = () => {
  const { submissionId, moduleId } = useParams<{
    submissionId: string;
    moduleId: string;
  }>();

  return (
    <BulkUpload
      moduleType="outcome-scores"
      moduleName="Outcome scores"
      backUrl={ROUTES.organisationAdmin.submissions.outcomeScores.list(submissionId, moduleId)}
      api={{
        downloadTemplate: downloadBulkUploadTemplate,
        validate: validateBulkUpload,
        processStaged: processStagedBulkUpload,
        checkOutcomeScoreDuplicates,
      }}
    />
  );
};

export default OutcomeScoresBulkUpload;
