import React, { useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { useMutation, useQuery, useQueryClient } from 'react-query';
import { GeneralLayout } from '../../../../layouts';
import { LoadingSpinner, GovUKButton, GovUKInputWithSuffix, SummaryList } from '../../../../components/GovUKComponents';
import useErrorSummaryFocus from '../../../../hooks/useErrorSummaryFocus';
import useFormTelemetry from '../../../../hooks/useFormTelemetry';
import { getWiderServiceCategoryForm, saveWiderServiceCategoryForm } from '../queries';
import './styles.css';
import usePageTitle from '../../../../hooks/usePageTitle';

type ViewMode = 'form' | 'summary';

const WiderServiceForm = (): React.ReactElement => {
  usePageTitle('Provide wider service data');
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const { trackStarted, trackReviewReached, trackSubmitAttempted, trackSubmitted, trackAbandoned, trackDraftSaved } = useFormTelemetry('wider_service_submission');
  const { submissionId, moduleId, categoryId } = useParams<{
    submissionId: string;
    moduleId: string;
    categoryId: string;
  }>();

  const [viewMode, setViewMode] = useState<ViewMode>('form');
  const [userCount, setUserCount] = useState<string>('');
  const [submitAttempts, setSubmitAttempts] = useState(0);

  const { data, isLoading } = useQuery({
    queryKey: ['wider-service-category-form', submissionId, moduleId, categoryId],
    queryFn: () => getWiderServiceCategoryForm(submissionId!, moduleId!, categoryId!),
    enabled: !!submissionId && !!moduleId && !!categoryId,
    onSuccess: (response) => {
      trackStarted();
      if (response.data.userCount !== null && response.data.userCount !== undefined) {
        setUserCount(response.data.userCount.toString());
      }
    },
  });

  const saveMutation = useMutation({
    mutationFn: (markComplete: boolean) =>
      saveWiderServiceCategoryForm(submissionId!, moduleId!, categoryId!, {
        userCount: userCount ? parseInt(userCount, 10) : null,
        markComplete,
      }),
    onError: () => {
      setSubmitAttempts((n) => n + 1);
    },
    onSuccess: (_data, markComplete) => {
      if (markComplete) {
        trackSubmitted();
      } else {
        trackDraftSaved();
      }
      queryClient.invalidateQueries(['organisation-admin-submission', submissionId]);
      queryClient.invalidateQueries(['wider-service-users-module', submissionId, moduleId]);
      queryClient.invalidateQueries(['wider-service-category-form', submissionId, moduleId, categoryId]);
      navigate(`/organisation-admin/submissions/${submissionId}/modules/${moduleId}/wider-services`, {
        state: {
          notification: {
            type: 'success',
            title: 'Wider services user data saved',
            message: `Wider services user data for '${formData?.categoryName}' has been successfully saved.`,
          },
        },
      });
    },
  });

  const formData = data?.data;

  useErrorSummaryFocus(submitAttempts, saveMutation.isError);

  const handleContinue = () => {
    trackReviewReached();
    setViewMode('summary');
    window.scrollTo(0, 0);
  };

  const handleBack = () => {
    if (viewMode === 'summary') {
      setViewMode('form');
      window.scrollTo(0, 0);
    } else {
      trackAbandoned();
      navigate(`/organisation-admin/submissions/${submissionId}/modules/${moduleId}/wider-services`);
    }
  };

  const handleSaveAsDraft = () => {
    saveMutation.mutate(false);
  };

  const handleConfirmAndSave = () => {
    trackSubmitAttempted();
    saveMutation.mutate(true);
  };

  if (isLoading) {
    return (
      <GeneralLayout>
        <LoadingSpinner loading label="Loading form" />
      </GeneralLayout>
    );
  }

  if (!formData) {
    return (
      <GeneralLayout>
        <h1 className="govuk-heading-l">Category not found</h1>
        <p className="govuk-body">The category you are looking for does not exist.</p>
        <GovUKButton onClick={() => navigate(`/organisation-admin/submissions/${submissionId}/modules/${moduleId}/wider-services`)}>Back to wider services</GovUKButton>
      </GeneralLayout>
    );
  }

  const totalSteps = 2;
  const currentStep = viewMode === 'form' ? 1 : 2;

  if (viewMode === 'summary') {
    return (
      <GeneralLayout backLink={{ href: '#', onClick: handleBack }}>
        <span className="govuk-caption-l">
          Step {currentStep} of {totalSteps}
        </span>
        <h1 className="govuk-heading-l">Provide data for {formData.categoryName}</h1>

        <h2 className="govuk-heading-m">Check your answers before saving your data</h2>
        <p className="govuk-body">
          Check the information you've entered before saving the wider services user data. Make sure the details are accurate and complete. You can go back to make changes if
          anything needs updating.
        </p>

        <SummaryList
          noOuterBorder
          halfWidthColumns
          items={[
            {
               label: formData.label ?? '',
              value: userCount ? `${userCount} users` : 'Not provided',
              onAction: () => {
                setViewMode('form');
                window.scrollTo(0, 0);
              },
              actionLabel: 'Change',
            },
          ]}
        />

        <h2 className="govuk-heading-m">Now save your wider service users data</h2>
        <p className="govuk-body">
          By saving the service user data for wider services in the '{formData.categoryName}' category, you are confirming that, to the best of your knowledge, the details you are
          providing are correct. You may return and update these details before the quarterly Management Information submission.
        </p>

        <GovUKButton onClick={handleConfirmAndSave} isLoading={saveMutation.isLoading}>
          Confirm and save
        </GovUKButton>

        {saveMutation.isError && (
          <div className="govuk-error-summary" data-module="govuk-error-summary">
            <h2 className="govuk-error-summary__title">There was a problem saving</h2>
            <div className="govuk-error-summary__body">
              <p>Please try again or contact support if the problem persists.</p>
            </div>
          </div>
        )}
      </GeneralLayout>
    );
  }

  return (
    <GeneralLayout backLink={{ href: '#', onClick: handleBack }}>
      <span className="govuk-caption-l">
        Step {currentStep} of {totalSteps}
      </span>
      <h1 className="govuk-heading-l">Provide data for {formData.categoryName}</h1>

      <p className="govuk-body">
        Tell us about the users who accessed the wider services in this category over the past 3 months. This information will be included in your quarterly Management Information
        data collection.
      </p>

      <hr className="govuk-section-break govuk-section-break--l govuk-section-break--visible" />

      <GovUKInputWithSuffix
        id="user-count"
        name="user-count"
        label={formData.label ?? ''}
        hint={formData.helpText ?? undefined}
        suffix="users"
        value={userCount}
        type="number"
        width="5"
        questionCode="QWSU01"
        onChange={(e: React.ChangeEvent<HTMLInputElement>) => setUserCount(e.target.value)}
      />

      <hr className="govuk-section-break govuk-section-break--l govuk-section-break--visible" />

      <div className="govuk-button-group">
        <GovUKButton onClick={handleContinue}>Continue</GovUKButton>
        <GovUKButton className="govuk-button govuk-button--secondary" onClick={handleSaveAsDraft} isLoading={saveMutation.isLoading}>
          Save as draft and exit
        </GovUKButton>
      </div>

      {saveMutation.isError && (
        <div className="govuk-error-summary" data-module="govuk-error-summary">
          <h2 className="govuk-error-summary__title">There was a problem saving</h2>
          <div className="govuk-error-summary__body">
            <p>Please try again or contact support if the problem persists.</p>
          </div>
        </div>
      )}
    </GeneralLayout>
  );
};

export default WiderServiceForm;
