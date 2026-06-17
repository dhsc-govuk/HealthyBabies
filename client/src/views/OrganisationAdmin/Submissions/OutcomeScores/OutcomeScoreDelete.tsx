import React, { useMemo } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { useMutation, useQuery, useQueryClient } from 'react-query';
import { LoadingBox, Button } from 'govuk-react';
import { GeneralLayout } from '../../../../layouts';
import { getOutcomeScoreRecord, getOutcomeScoresModule, deleteOutcomeScoreRecord } from '../queries';
import { WarningPanel, WarningPanelTitle, WarningPanelBody, WarningPanelActions, WarningPanelAnchor } from '../../../../styles/govuk-global';

const OutcomeScoreDelete = (): React.ReactElement => {
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const { submissionId, moduleId, recordId } = useParams<{
    submissionId: string;
    moduleId: string;
    recordId: string;
  }>();

  const { data: formData, isLoading: isLoadingRecord } = useQuery({
    queryKey: ['outcome-score-record', submissionId, moduleId, recordId],
    queryFn: () => getOutcomeScoreRecord(submissionId!, moduleId!, recordId!),
    enabled: !!submissionId && !!moduleId && !!recordId,
  });

  const { data: moduleData, isLoading: isLoadingModule } = useQuery({
    queryKey: ['outcome-scores-module', submissionId, moduleId],
    queryFn: () => getOutcomeScoresModule(submissionId!, moduleId!),
    enabled: !!submissionId && !!moduleId,
  });

  const isLoading = isLoadingRecord || isLoadingModule;

  const deleteMutation = useMutation({
    mutationFn: () => deleteOutcomeScoreRecord(submissionId!, moduleId!, recordId!),
    onSuccess: () => {
      queryClient.invalidateQueries(['outcome-scores-module', submissionId, moduleId]);
      navigate(`/organisation-admin/submissions/${submissionId}/modules/${moduleId}/outcome-scores`);
    },
  });

  const outcomeScoreForm = formData?.data;

  // Get anonymisedId from module data
  const anonymisedId = useMemo(() => {
    if (!moduleData?.data?.records || !recordId) return '';
    const record = moduleData.data.records.find((r) => r.recordId === recordId);
    return record?.anonymisedId || '';
  }, [moduleData, recordId]);

  const handleDelete = () => {
    deleteMutation.mutate();
  };

  const serviceName = outcomeScoreForm?.serviceName || 'this service';

  return (
    <LoadingBox loading={isLoading || deleteMutation.isLoading}>
      <GeneralLayout currentPage="" backLink={{ href: `/organisation-admin/submissions/${submissionId}/modules/${moduleId}/outcome-scores` }}>
        <WarningPanel>
          <WarningPanelTitle>Are you sure you want to delete the outcome scores record?</WarningPanelTitle>
          <WarningPanelBody>
            Deleting the outcome scores record {anonymisedId} will remove all saved outcome score information for one of the users who used &apos;{serviceName}&apos; in the past 3
            months.
          </WarningPanelBody>
          <WarningPanelBody>This will not remove outcome scores for other service users or from previous Management Information data collections.</WarningPanelBody>
          <WarningPanelBody>
            <strong>This cannot be undone.</strong>
          </WarningPanelBody>
          <WarningPanelBody>If you want to keep this service&apos;s outcome scores for the past quarter, you can go back.</WarningPanelBody>
          <WarningPanelActions>
            <Button onClick={handleDelete} disabled={deleteMutation.isLoading} buttonColour="#ffffff" buttonTextColour="#1d70b8">
              Yes, I want to delete
            </Button>
            <WarningPanelAnchor href={`/organisation-admin/submissions/${submissionId}/modules/${moduleId}/outcome-scores`}>Go back</WarningPanelAnchor>
          </WarningPanelActions>
        </WarningPanel>
      </GeneralLayout>
    </LoadingBox>
  );
};

export default OutcomeScoreDelete;
