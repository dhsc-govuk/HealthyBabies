import React, { useState } from 'react';
import { useMutation, useQuery, useQueryClient } from 'react-query';
import { useNavigate, useParams } from 'react-router-dom';

import { GeneralLayout } from '../../../../layouts';
import { GovUKButton, GovUKInsetText, LoadingSpinner, useGovUKNotification } from '../../../../components/GovUKComponents';
import { getLocalAuthoritySubmission, approveSubmission } from '../LocalAuthorityView/queries';
import { processError } from '../../../../helpers/axiosErrorFallback';
import { ROUTES } from '../../../../constants/routes';
import usePageTitle from '../../../../hooks/usePageTitle';

/** Mirrors the reopen reason's limit, for consistency. */
const MAXIMUM_COMMENT_LENGTH = 500;

type Step = 'details' | 'check-answers';

function AcceptSubmission(): React.ReactElement {
  usePageTitle('Accept submission');
  const { submissionId, localAuthorityId } = useParams<{ submissionId: string; localAuthorityId: string }>();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const { setNotification } = useGovUKNotification();

  const [step, setStep] = useState<Step>('details');
  const [comment, setComment] = useState('');

  const { data, isLoading } = useQuery({
    queryKey: ['local-authority-submission', submissionId, localAuthorityId],
    queryFn: () => getLocalAuthoritySubmission(submissionId!, localAuthorityId!),
    enabled: !!submissionId && !!localAuthorityId,
  });

  const submission = data?.data;
  const backToSubmission = ROUTES.admin.submissions.localAuthority(submissionId, localAuthorityId);

  const { mutateAsync: accept, isLoading: accepting } = useMutation({
    mutationFn: () => approveSubmission(submissionId!, localAuthorityId!, comment.trim() || undefined),
    onSuccess: () => {
      queryClient.invalidateQueries(['local-authority-submission', submissionId, localAuthorityId]);
      navigate(backToSubmission, {
        state: {
          successTitle: 'Submission accepted',
          successMessage: 'Your review is now complete.',
        },
      });
    },
    onError: (err) => {
      processError(err, (e) => setNotification({ type: 'important', title: 'Error', message: e }));
    },
  });

  const handleContinue = (e: React.FormEvent) => {
    e.preventDefault();
    setStep('check-answers');
  };

  if (isLoading) {
    return (
      <GeneralLayout>
        <LoadingSpinner />
      </GeneralLayout>
    );
  }

  if (!submission) {
    return (
      <GeneralLayout>
        <p className="govuk-body">Failed to load the local authority submission.</p>
      </GeneralLayout>
    );
  }

  if (step === 'check-answers') {
    return (
      <GeneralLayout>
        <a
          href={backToSubmission}
          className="govuk-back-link"
          onClick={(e) => {
            e.preventDefault();
            setStep('details');
          }}>
          Back
        </a>

        <span className="govuk-caption-l">{submission.dataCollectionName}</span>
        <h1 className="govuk-heading-l">Check your answers</h1>
        <GovUKInsetText>
          Local authority: <strong>{submission.localAuthorityName}</strong>
        </GovUKInsetText>

        <h2 className="govuk-heading-m">Check your answers before accepting the submission</h2>
        <p className="govuk-body">
          Check the information you&apos;ve entered before accepting the submission for the local authority. Make sure the details are accurate and complete. You can go back to
          make changes if anything needs updating.
        </p>

        <dl className="govuk-summary-list">
          <div className="govuk-summary-list__row">
            <dt className="govuk-summary-list__key">Add a comment (optional)</dt>
            <dd className="govuk-summary-list__value">{comment.trim() || 'No comment added'}</dd>
            <dd className="govuk-summary-list__actions">
              <a
                className="govuk-link"
                href="#change-comment"
                onClick={(e) => {
                  e.preventDefault();
                  setStep('details');
                }}>
                Change<span className="govuk-visually-hidden"> the comment</span>
              </a>
            </dd>
          </div>
        </dl>

        <h2 className="govuk-heading-m">Now accept the submission</h2>
        <p className="govuk-body">You will not be able to reopen the submission after you accept it.</p>

        <GovUKButton onClick={() => accept()} disabled={accepting}>
          {accepting ? 'Accepting...' : 'Accept'}
        </GovUKButton>
      </GeneralLayout>
    );
  }

  return (
    <GeneralLayout>
      <a href={backToSubmission} className="govuk-back-link">
        Back
      </a>

      <span className="govuk-caption-l">{submission.dataCollectionName}</span>
      <h1 className="govuk-heading-l">Accept submission</h1>
      <GovUKInsetText>
        Local authority: <strong>{submission.localAuthorityName}</strong>
      </GovUKInsetText>

      <form onSubmit={handleContinue}>
        <div className="govuk-form-group">
          <label className="govuk-label govuk-label--s" htmlFor="accept-comment">
            Add a comment (optional)
          </label>
          <div id="accept-comment-hint" className="govuk-hint">
            You can add an optional comment about this submission.
          </div>
          <textarea
            className="govuk-textarea"
            id="accept-comment"
            name="accept-comment"
            rows={5}
            aria-describedby="accept-comment-hint"
            value={comment}
            maxLength={MAXIMUM_COMMENT_LENGTH}
            onChange={(e) => setComment(e.target.value)}
          />
          <div className="govuk-hint">You can enter up to {MAXIMUM_COMMENT_LENGTH} characters</div>
        </div>

        <GovUKButton type="submit">Continue</GovUKButton>
      </form>
    </GeneralLayout>
  );
}

export default AcceptSubmission;
