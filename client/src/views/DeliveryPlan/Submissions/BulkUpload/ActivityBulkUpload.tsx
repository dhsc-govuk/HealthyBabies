import React from 'react';
import { useParams, useLocation } from 'react-router-dom';
import { useQuery } from 'react-query';
import BulkUpload from '../../../../components/Global/BulkUpload';
import { LoadingSpinner } from '../../../../components/GovUKComponents';
import { getActivitiesModule, downloadBulkUploadTemplate, validateBulkUpload, checkActivityDuplicates, processStagedBulkUpload } from '../queries';
import { getModuleContent } from '../Module/moduleContent';
import { ROUTES } from '../../../../constants/routes';

// Shared between the /delivery-plan and /organisation-admin route prefixes, same as ActivitiesList/
// ActivityFormView/ActivityDelete — organisation-admins can also browse into Delivery Plan strand
// submissions (SubmissionsController.GetAll returns all data collection types, not just MI).
const ActivityBulkUpload: React.FC = () => {
  const routerLocation = useLocation();
  const { submissionId, moduleId } = useParams<{
    submissionId: string;
    moduleId: string;
  }>();

  const apiBase = routerLocation.pathname.startsWith(ROUTES.organisationAdmin.root) ? 'organisation-admin' : 'delivery-plan';

  const { data, isLoading } = useQuery({
    queryKey: ['activities-module', submissionId, moduleId],
    queryFn: () => getActivitiesModule(submissionId!, moduleId!, apiBase),
    enabled: !!submissionId && !!moduleId,
  });

  const moduleData = data?.data;
  const content = getModuleContent(moduleData?.code);

  return (
    <LoadingSpinner loading={isLoading} label="Loading">
      <BulkUpload
        moduleType="activity"
        moduleName={moduleData?.name ?? content.listHeading}
        backUrl={ROUTES.areaSubmissions.activities.list(apiBase, submissionId, moduleId)}
        api={{
          downloadTemplate: (submissionId, moduleId, format) => downloadBulkUploadTemplate(submissionId, moduleId, format, apiBase),
          validate: (submissionId, moduleId, file) => validateBulkUpload(submissionId, moduleId, file, apiBase),
          checkActivityDuplicates: (submissionId, moduleId, rows) => checkActivityDuplicates(submissionId, moduleId, rows, apiBase),
          processStaged: (submissionId, moduleId, payload) => processStagedBulkUpload(submissionId, moduleId, payload, apiBase),
        }}
      />
    </LoadingSpinner>
  );
};

export default ActivityBulkUpload;
