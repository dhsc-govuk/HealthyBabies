import React, { useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { useQuery, useMutation, useQueryClient } from 'react-query';
import { LoadingBox } from 'govuk-react';
import { GeneralLayout } from '../../../../layouts';
import { GovUKButton, useGovUKNotification } from '../../../../components/GovUKComponents';
import { getSubmissionById, getSubmissionStatus } from '../View/queries';
import { processError } from '../../../../helpers/axiosErrorFallback';
import axios from 'axios';
import './styles.css';
import usePageTitle from '../../../../hooks/usePageTitle';

type StatusOption = 'Planned' | 'Open' | 'Closed';

interface UpdateDataCollectionDto {
  id: string;
  name: string;
  description: string | null;
  startDate: string;
  endDate: string;
}

const updateSubmissionDates = (dto: UpdateDataCollectionDto) =>
  axios.put('/admin/data-collections/edit', dto);

function ChangeStatus(): React.ReactElement {
  usePageTitle('Change status');
  const { submissionId } = useParams<{ submissionId: string }>();
  const navigate = useNavigate();
  const queryClient = useQueryClient();

  const { data, isLoading } = useQuery({
    queryKey: ['submission-view', submissionId],
    queryFn: () => getSubmissionById(submissionId!),
    enabled: !!submissionId,
  });

  const submission = data?.data;
  const currentStatus = submission ? getSubmissionStatus(submission) : null;

  const { setNotification } = useGovUKNotification();
  const [selectedStatus, setSelectedStatus] = useState<StatusOption | null>(null);

  React.useEffect(() => {
    if (currentStatus && !selectedStatus) {
      setSelectedStatus(currentStatus);
    }
  }, [currentStatus, selectedStatus]);

  const { mutateAsync: updateStatus, isLoading: isUpdating } = useMutation({
    mutationFn: () => {
      if (!submission || !selectedStatus) {
        return Promise.reject(new Error('Missing data'));
      }

      const now = new Date();
      let startDate = new Date(submission.startDate);
      let endDate = new Date(submission.endDate);

      if (selectedStatus === 'Open') {
        if (startDate > now) {
          startDate = now;
        }
        if (endDate < now) {
          endDate = new Date(now);
          endDate.setMonth(endDate.getMonth() + 1);
        }
      } else if (selectedStatus === 'Planned') {
        const tomorrow = new Date(now);
        tomorrow.setDate(tomorrow.getDate() + 1);
        if (startDate <= now) {
          startDate = tomorrow;
        }
        if (endDate <= startDate) {
          endDate = new Date(startDate);
          endDate.setMonth(endDate.getMonth() + 1);
        }
      } else if (selectedStatus === 'Closed') {
        const yesterday = new Date(now);
        yesterday.setDate(yesterday.getDate() - 1);
        if (endDate >= now) {
          endDate = yesterday;
        }
        if (startDate > endDate) {
          startDate = new Date(endDate);
          startDate.setMonth(startDate.getMonth() - 1);
        }
      }

      return updateSubmissionDates({
        id: submissionId!,
        name: submission.name,
        description: submission.description,
        startDate: startDate.toISOString(),
        endDate: endDate.toISOString(),
      });
    },
    onSuccess: () => {
      setNotification({ type: 'success', title: 'Success', message: 'Status updated successfully' });
      queryClient.invalidateQueries(['submission-view', submissionId]);
      queryClient.invalidateQueries(['submissions-list']);
      navigate(`/admin/submissions/${submissionId}`);
    },
    onError: (error) => {
      processError(error, (e) => setNotification({ type: 'important', title: 'Error', message: e }));
    },
  });

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    await updateStatus();
  };

  const handleCancel = () => {
    navigate(`/admin/submissions/${submissionId}`);
  };

  const statusOptions: { value: StatusOption; label: string; hint: string }[] = [
    {
      value: 'Planned',
      label: 'Planned',
      hint: 'The submission has not started yet. Local authorities cannot submit data.',
    },
    {
      value: 'Open',
      label: 'Open',
      hint: 'The submission is currently open. Local authorities can submit their data.',
    },
    {
      value: 'Closed',
      label: 'Closed',
      hint: 'The submission has ended. Local authorities can no longer submit data.',
    },
  ];

  return (
    <GeneralLayout
      breadcrumbs={[
        { label: 'Home', link: '/admin/home' },
        { label: 'Submissions', link: '/admin/submissions' },
        { label: submission?.name || 'Submission', link: `/admin/submissions/${submissionId}` },
        { label: 'Change status', link: '' },
      ]}
    >
      <LoadingBox loading={isLoading}>
        {submission && (
          <>
            <h1 className="govuk-heading-l">Change status</h1>
            <p className="govuk-body">
              Current status: <strong>{currentStatus}</strong>
            </p>

            <form onSubmit={handleSubmit}>
              <div className="govuk-form-group">
                <fieldset className="govuk-fieldset">
                  <legend className="govuk-fieldset__legend govuk-fieldset__legend--m">
                    <h2 className="govuk-fieldset__heading">Select a new status</h2>
                  </legend>
                  <div className="govuk-radios">
                    {statusOptions.map((option) => (
                      <div className="govuk-radios__item" key={option.value}>
                        <input
                          className="govuk-radios__input"
                          id={`status-${option.value}`}
                          name="status"
                          type="radio"
                          value={option.value}
                          checked={selectedStatus === option.value}
                          onChange={() => setSelectedStatus(option.value)}
                        />
                        <label
                          className="govuk-label govuk-radios__label"
                          htmlFor={`status-${option.value}`}
                        >
                          {option.label}
                        </label>
                        <div className="govuk-hint govuk-radios__hint">
                          {option.hint}
                        </div>
                      </div>
                    ))}
                  </div>
                </fieldset>
              </div>

              <div className="govuk-button-group">
                <GovUKButton type="submit" disabled={isUpdating || selectedStatus === currentStatus}>
                  {isUpdating ? 'Saving...' : 'Save changes'}
                </GovUKButton>
                <a
                  href={`/admin/submissions/${submissionId}`}
                  className="govuk-link"
                  onClick={(e) => {
                    e.preventDefault();
                    handleCancel();
                  }}
                >
                  Cancel
                </a>
              </div>
            </form>
          </>
        )}
      </LoadingBox>
    </GeneralLayout>
  );
}

export default ChangeStatus;
