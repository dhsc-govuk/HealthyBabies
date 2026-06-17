import React from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { useQuery, useMutation, useQueryClient } from 'react-query';
import { LoadingBox, Button } from 'govuk-react';
import { GeneralLayout } from '../../../../../layouts';
import { Paragraph } from '../../../../../components/GovUKComponents';
import { getFormModule, deleteFormModule } from '../../queries';
import { WarningPanel, WarningPanelTitle, WarningPanelBody, WarningPanelActions, WarningPanelLink } from '../../../../../styles/govuk-global';
import usePageTitle from '../../../../../hooks/usePageTitle';

const formatDate = (dateString: string): string => {
  const date = new Date(dateString);
  return date.toLocaleDateString('en-GB', {
    day: 'numeric',
    month: 'long',
    year: 'numeric',
  });
};

const DeleteModule = (): React.ReactElement => {
  usePageTitle('Delete module');
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const { submissionId, moduleId } = useParams<{
    submissionId: string;
    moduleId: string;
  }>();

  const {
    data,
    isLoading: isLoadingModule,
    isError,
  } = useQuery({
    queryKey: ['organisation-admin-module', submissionId, moduleId],
    queryFn: () => getFormModule(submissionId!, moduleId!),
    enabled: !!submissionId && !!moduleId,
  });

  const formModule = data?.data;

  const deleteMutation = useMutation({
    mutationFn: () => deleteFormModule(submissionId!, moduleId!),
    onSuccess: () => {
      queryClient.invalidateQueries(['organisation-admin-submission', submissionId]);
      queryClient.invalidateQueries(['organisation-admin-module', submissionId, moduleId]);
      navigate(`/organisation-admin/submissions/${submissionId}`, {
        state: {
          successTitle: `${formModule?.name} data deleted`,
          successMessage: `${formModule?.name} data has been successfully deleted.`,
        },
      });
    },
  });

  const handleDelete = async () => {
    await deleteMutation.mutateAsync();
  };

  const handleCancel = () => {
    navigate(`/organisation-admin/submissions/${submissionId}/modules/${moduleId}`);
  };

  const isLoading = isLoadingModule || deleteMutation.isLoading;

  if (isLoadingModule) {
    return (
      <LoadingBox loading>
        <GeneralLayout>
          <Paragraph>Loading...</Paragraph>
        </GeneralLayout>
      </LoadingBox>
    );
  }

  if (isError || !formModule) {
    return (
      <GeneralLayout>
        <Paragraph>Form module not found.</Paragraph>
        <a href={`/organisation-admin/submissions/${submissionId}`} className="govuk-link">
          Back to submission
        </a>
      </GeneralLayout>
    );
  }

  return (
    <LoadingBox loading={isLoading}>
      <GeneralLayout backLink={{ href: `/organisation-admin/submissions/${submissionId}/modules/${moduleId}` }}>
        <WarningPanel>
          <WarningPanelTitle>Are you sure you want to delete {formModule.name.toLowerCase()} data?</WarningPanelTitle>
          <WarningPanelBody>
            Deleting {formModule.name.toLowerCase()} data will remove all saved information about {formModule.name.toLowerCase()} in the past 3 months, from{' '}
            {formatDate(formModule.startDate)} to {formatDate(formModule.endDate)}.
          </WarningPanelBody>
          <WarningPanelBody>This will not remove any saved {formModule.name.toLowerCase()} data from previous Management Information data collections.</WarningPanelBody>
          <WarningPanelBody>This cannot be undone.</WarningPanelBody>
          <WarningPanelBody>If you want to keep this service&apos;s user data for the past quarter, you can go back.</WarningPanelBody>
          <WarningPanelActions>
            <Button onClick={handleDelete} disabled={isLoading} buttonColour="#ffffff" buttonTextColour="#1d70b8">
              Yes, I want to delete
            </Button>
            <WarningPanelLink type="button" onClick={handleCancel}>
              Go back
            </WarningPanelLink>
          </WarningPanelActions>
        </WarningPanel>
      </GeneralLayout>
    </LoadingBox>
  );
};

export default DeleteModule;
