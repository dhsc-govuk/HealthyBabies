import React, { useState, useEffect } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { useQuery, useMutation, useQueryClient } from 'react-query';
import { LoadingBox } from 'govuk-react';
import { GeneralLayout } from '../../../../layouts';
import { GovUKButton, useGovUKNotification } from '../../../../components/GovUKComponents';
import { getSubmissionById, updateSubmissionDates } from '../View/queries';
import { processError } from '../../../../helpers/axiosErrorFallback';
import './styles.css';
import usePageTitle from '../../../../hooks/usePageTitle';
import { ROUTES } from '../../../../constants/routes';

// End dates are stored as UK-local-midnight-as-UTC, so read and write them with local-time accessors.
// Going through toISOString() here would disagree with formatDateDisplay below and shift the date by a
// day during BST. For the same reason parseDateInput must not use new Date('YYYY-MM-DD'), which the
// spec parses as UTC midnight rather than local midnight.
const formatDateForInput = (dateString: string): string => {
  const date = new Date(dateString);
  const month = String(date.getMonth() + 1).padStart(2, '0');
  const day = String(date.getDate()).padStart(2, '0');
  return `${date.getFullYear()}-${month}-${day}`;
};

const parseDateInput = (value: string): Date => {
  const [year, month, day] = value.split('-').map(Number);
  return new Date(year, month - 1, day);
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

      const newEndDate = parseDateInput(dueDate);
      const startDate = new Date(submission.startDate);

      if (newEndDate <= startDate) {
        setError('Due date must be after the start date');
        return Promise.reject(new Error('Due date must be after the start date'));
      }

      return updateSubmissionDates(submissionId!, { endDate: newEndDate.toISOString() });
    },
    onSuccess: () => {
      setNotification({ type: 'success', title: 'Success', message: 'Due date updated successfully' });
      queryClient.invalidateQueries(['submission-view', submissionId]);
      queryClient.invalidateQueries(['submissions-list']);
      navigate(ROUTES.admin.submissions.view(submissionId));
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
    navigate(ROUTES.admin.submissions.view(submissionId));
  };

  const hasChanged = submission ? dueDate !== formatDateForInput(submission.endDate) : false;

  return (
    <GeneralLayout
      breadcrumbs={[
        { label: 'Home', link: ROUTES.admin.home },
        { label: 'Submissions', link: ROUTES.admin.submissions.list },
        { label: submission?.name || 'Submission', link: ROUTES.admin.submissions.view(submissionId) },
        { label: 'Change due date', link: '' },
      ]}>
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
                <div className="govuk-hint">The due date must be after the start date.</div>
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
                  href={ROUTES.admin.submissions.view(submissionId)}
                  className="govuk-link"
                  onClick={(e) => {
                    e.preventDefault();
                    handleCancel();
                  }}>
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
