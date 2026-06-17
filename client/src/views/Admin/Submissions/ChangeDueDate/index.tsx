import React, { useState, useEffect } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { useQuery, useMutation, useQueryClient } from 'react-query';
import { LoadingBox } from 'govuk-react';
import { GeneralLayout } from '../../../../layouts';
import { GovUKButton, useGovUKNotification } from '../../../../components/GovUKComponents';
import { getSubmissionById } from '../View/queries';
import { processError } from '../../../../helpers/axiosErrorFallback';
import axios from 'axios';
import './styles.css';
import usePageTitle from '../../../../hooks/usePageTitle';

interface UpdateDataCollectionDto {
  id: string;
  name: string;
  description: string | null;
  startDate: string;
  endDate: string;
}

const updateSubmissionDates = (dto: UpdateDataCollectionDto) =>
  axios.put('/admin/data-collections/edit', dto);

const formatDateForInput = (dateString: string): string => {
  const date = new Date(dateString);
  return date.toISOString().split('T')[0];
};

const formatDateDisplay = (dateString: string): string => {
  const date = new Date(dateString);
  return date.toLocaleDateString('en-GB', {
    day: 'numeric',
    month: 'long',
    year: 'numeric',
  });
};

function ChangeDueDate(): React.ReactElement {
  usePageTitle('Change due date');
  const { submissionId } = useParams<{ submissionId: string }>();
  const navigate = useNavigate();
  const queryClient = useQueryClient();

  const { data, isLoading } = useQuery({
    queryKey: ['submission-view', submissionId],
    queryFn: () => getSubmissionById(submissionId!),
    enabled: !!submissionId,
  });

  const submission = data?.data;

  const { setNotification } = useGovUKNotification();
  const [dueDate, setDueDate] = useState('');
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (submission?.endDate && !dueDate) {
      setDueDate(formatDateForInput(submission.endDate));
    }
  }, [submission?.endDate, dueDate]);

  const { mutateAsync: updateDueDate, isLoading: isUpdating } = useMutation({
    mutationFn: () => {
      if (!submission || !dueDate) {
        return Promise.reject(new Error('Missing data'));
      }

      const newEndDate = new Date(dueDate);
      const startDate = new Date(submission.startDate);

      if (newEndDate <= startDate) {
        setError('Due date must be after the start date');
        return Promise.reject(new Error('Due date must be after the start date'));
      }

      return updateSubmissionDates({
        id: submissionId!,
        name: submission.name,
        description: submission.description,
        startDate: submission.startDate,
        endDate: newEndDate.toISOString(),
      });
    },
    onSuccess: () => {
      setNotification({ type: 'success', title: 'Success', message: 'Due date updated successfully' });
      queryClient.invalidateQueries(['submission-view', submissionId]);
      queryClient.invalidateQueries(['submissions-list']);
      navigate(`/admin/submissions/${submissionId}`);
    },
    onError: (err) => {
      if (err instanceof Error && err.message === 'Due date must be after the start date') {
        return;
      }
      processError(err, (e) => setNotification({ type: 'important', title: 'Error', message: e }));
    },
  });

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    await updateDueDate();
  };

  const handleCancel = () => {
    navigate(`/admin/submissions/${submissionId}`);
  };

  const hasChanged = submission ? dueDate !== formatDateForInput(submission.endDate) : false;

  return (
    <GeneralLayout
      breadcrumbs={[
        { label: 'Home', link: '/admin/home' },
        { label: 'Submissions', link: '/admin/submissions' },
        { label: submission?.name || 'Submission', link: `/admin/submissions/${submissionId}` },
        { label: 'Change due date', link: '' },
      ]}
    >
      <LoadingBox loading={isLoading}>
        {submission && (
          <>
            <h1 className="govuk-heading-l">Change due date</h1>
            <p className="govuk-body">
              Current due date: <strong>{formatDateDisplay(submission.endDate)}</strong>
            </p>
            <p className="govuk-body">
              Start date: <strong>{formatDateDisplay(submission.startDate)}</strong>
            </p>

            <form onSubmit={handleSubmit}>
              <div className={`govuk-form-group${error ? ' govuk-form-group--error' : ''}`}>
                <label className="govuk-label govuk-label--m" htmlFor="due-date">
                  New due date
                </label>
                <div className="govuk-hint">
                  The due date must be after the start date.
                </div>
                {error && (
                  <p className="govuk-error-message">
                    <span className="govuk-visually-hidden">Error:</span> {error}
                  </p>
                )}
                <input
                  className={`govuk-input govuk-input--width-10${error ? ' govuk-input--error' : ''}`}
                  id="due-date"
                  name="due-date"
                  type="date"
                  value={dueDate}
                  onChange={(e) => {
                    setDueDate(e.target.value);
                    setError(null);
                  }}
                />
              </div>

              <div className="govuk-button-group">
                <GovUKButton type="submit" disabled={isUpdating || !hasChanged}>
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

export default ChangeDueDate;
