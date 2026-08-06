import React from 'react';
import { useNavigate, useParams, useLocation } from 'react-router-dom';
import { useMutation, useQueryClient } from 'react-query';
import { LoadingBox, Button } from 'govuk-react';
import { GeneralLayout } from '../../../../../../layouts';
import { deleteActivity } from '../../../queries';
import { getModuleContent } from '../../moduleContent';
import { WarningPanel, WarningPanelTitle, WarningPanelBody, WarningPanelActions, WarningPanelLink } from '../../../../../../styles/govuk-global';

interface LocationState {
  activityName?: string;
  moduleCode?: string;
}

const DeleteActivity = (): React.ReactElement => {
  const navigate = useNavigate();
  const routerLocation = useLocation();
  const queryClient = useQueryClient();
  const { submissionId, moduleId, activityId } = useParams<{
    submissionId: string;
    moduleId: string;
    activityId: string;
  }>();

  const state = routerLocation.state as LocationState | null;
  const activityName = state?.activityName;
  const content = getModuleContent(state?.moduleCode);

  const apiBase = routerLocation.pathname.startsWith('/organisation-admin') ? 'organisation-admin' : 'delivery-plan';
  const basePath = `/${apiBase}`;
  const activitiesUrl = `${basePath}/submissions/${submissionId}/modules/${moduleId}/activities`;
  const viewUrl = `${basePath}/submissions/${submissionId}/modules/${moduleId}/activities/${activityId}/view`;

  const deleteMutation = useMutation({
    mutationFn: () => deleteActivity(submissionId!, moduleId!, activityId!, apiBase),
    onSuccess: () => {
      queryClient.invalidateQueries(['activities-module', submissionId, moduleId]);
      navigate(activitiesUrl, {
        state: {
          notification: {
            type: 'success',
            title: `${content.activityNoun.charAt(0).toUpperCase() + content.activityNoun.slice(1)} deleted`,
            message: `${activityName ?? content.activityNoun} has been successfully deleted.`,
          },
        },
      });
    },
  });

  const handleDelete = () => {
    deleteMutation.mutate();
  };

  const handleCancel = () => {
    navigate(viewUrl);
  };

  const displayName = activityName ?? `this ${content.activityNoun}`;

  return (
    <LoadingBox loading={deleteMutation.isLoading}>
      <GeneralLayout backLink={{ href: '#', onClick: handleCancel }}>
        <WarningPanel>
          <WarningPanelTitle>Are you sure you want to delete this {content.activityNoun}?</WarningPanelTitle>
          <WarningPanelBody>
            Deleting <strong>{displayName}</strong> will permanently remove all saved information for this {content.activityNoun}.
          </WarningPanelBody>
          <WarningPanelBody>
            <strong>This cannot be undone.</strong>
          </WarningPanelBody>
          <WarningPanelBody>
            If you want to keep this {content.activityNoun}, select &apos;Go back&apos;.
          </WarningPanelBody>
          <WarningPanelActions>
            <Button onClick={handleDelete} disabled={deleteMutation.isLoading} buttonColour="#ffffff" buttonTextColour="#1d70b8">
              Yes, delete this {content.activityNoun}
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

export default DeleteActivity;
