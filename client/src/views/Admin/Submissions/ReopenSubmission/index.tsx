import React, { useState } from 'react';
import { useMutation, useQuery, useQueryClient } from 'react-query';
import { useNavigate, useParams } from 'react-router-dom';

import { GeneralLayout } from '../../../../layouts';
import { GovUKButton, GovUKDateField, GovUKErrorSummary, GovUKFieldset, GovUKInsetText, LoadingSpinner, useGovUKNotification } from '../../../../components/GovUKComponents';
import type { CheckboxOption } from '../../../../components/GovUKComponents/GovUKFieldset';
import type { ErrorItem } from '../../../../components/GovUKComponents/GovUKErrorSummary';
import { getLocalAuthoritySubmission, reopenSubmission, formatDateOnly } from '../LocalAuthorityView/queries';
import { processError } from '../../../../helpers/axiosErrorFallback';
import { ROUTES } from '../../../../constants/routes';
import usePageTitle from '../../../../hooks/usePageTitle';

/** Days ahead the resubmission deadline may be set, mirrored by the server-side validator. */
const MAXIMUM_DAYS_AHEAD = 90;

/** Mirrors the server-side validator's limit on the reopen reason. */
const MAXIMUM_REASON_LENGTH = 500;

type Step = 'details' | 'check-answers';

/** GovUKDateField emits DD/MM/YYYY; the API takes an ISO date. */
const toIsoDate = (ddmmyyyy: string): string | null => {
  const parts = ddmmyyyy.split('/');
  if (parts.length !== 3) return null;
  const [day, month, year] = parts;
  return `${year}-${month.padStart(2, '0')}-${day.padStart(2, '0')}`;
};

/**
 * Local calendar date as a YYYY-MM-DD string, built from local date parts (not toISOString, which
 * converts to UTC and would shift the date near midnight under BST). Comparing two YYYY-MM-DD
 * strings this way is lexicographically equivalent to comparing them chronologically, so date
 * bounds can be checked without ever constructing a Date from the ISO string (new Date('YYYY-MM-DD')
 * parses as UTC midnight, which doesn't match a local-midnight Date and causes an off-by-one
 * comparison during BST).
 */
const toLocalIsoDateString = (date: Date): string => {
  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, '0');
  const day = String(date.getDate()).padStart(2, '0');
  return `${year}-${month}-${day}`;
};

function ReopenSubmission(): React.ReactElement {
  usePageTitle('Reopen submission');
  const { submissionId, localAuthorityId } = useParams<{ submissionId: string; localAuthorityId: string }>();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const { setNotification } = useGovUKNotification();

  const [step, setStep] = useState<Step>('details');
  const [reason, setReason] = useState('');
  const [sectionsToReview, setSectionsToReview] = useState<string[]>([]);
  const [resubmitBy, setResubmitBy] = useState('');
  const [errors, setErrors] = useState<ErrorItem[]>([]);

  const { data, isLoading } = useQuery({
    queryKey: ['local-authority-submission', submissionId, localAuthorityId],
    queryFn: () => getLocalAuthoritySubmission(submissionId!, localAuthorityId!),
    enabled: !!submissionId && !!localAuthorityId,
  });

  const submission = data?.data;
  const backToSubmission = ROUTES.admin.submissions.localAuthority(submissionId, localAuthorityId);

  const sectionOptions: CheckboxOption[] =
    submission?.sections.map((section) => ({
      value: section.formModuleId,
      text: `Section ${section.sectionNumber}: ${section.name}`,
    })) ?? [];

  const { mutateAsync: reopen, isLoading: reopening } = useMutation({
    mutationFn: () =>
      reopenSubmission(submissionId!, localAuthorityId!, {
        reason: reason.trim(),
        resubmitBy: toIsoDate(resubmitBy)!,
        sectionsToReview,
      }),
    onSuccess: () => {
      queryClient.invalidateQueries(['local-authority-submission', submissionId, localAuthorityId]);
      navigate(backToSubmission, {
        state: {
          successTitle: 'Submission reopened',
          // TODO(BLUM-6284): once IEmailNotificationService can send the reopen email, restore
          // the wireframe copy - "We sent an email to the local authority with the reason for
          // reopening." Until then this must not claim an email was sent.
          successMessage: 'The local authority can now make changes and resubmit information by the new due date. Your reason for reopening is shown on their submission.',
        },
      });
    },
    onError: (err) => {
      processError(err, (e) => setNotification({ type: 'important', title: 'Error', message: e }));
    },
  });

  const validate = (): boolean => {
    const found: ErrorItem[] = [];

    if (!reason.trim()) {
      found.push({ targetName: 'reopen-reason', text: 'A reason is required to reopen a submission' });
    }

    const iso = toIsoDate(resubmitBy);
    if (!iso) {
      found.push({ targetName: 'resubmit-by', text: 'Enter the date the local authority should resubmit by' });
    } else {
      const today = new Date();
      const todayIso = toLocalIsoDateString(today);
      const latest = new Date(today);
      latest.setDate(latest.getDate() + MAXIMUM_DAYS_AHEAD);
      const latestIso = toLocalIsoDateString(latest);

      if (iso <= todayIso) {
        found.push({ targetName: 'resubmit-by', text: 'The resubmission date must be in the future' });
      } else if (iso > latestIso) {
        found.push({ targetName: 'resubmit-by', text: `The resubmission date must be within ${MAXIMUM_DAYS_AHEAD} days of today` });
      }
    }

    setErrors(found);
    return found.length === 0;
  };

  const handleContinue = (e: React.FormEvent) => {
    e.preventDefault();
    if (validate()) {
      setStep('check-answers');
    }
  };

  const fieldError = (targetName: string) => errors.find((err) => err.targetName === targetName)?.text;

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

        <h2 className="govuk-heading-m">Check your answers before reopening the submission</h2>
        <p className="govuk-body">
          Check the information you&apos;ve entered before reopening the submission for the local authority. Make sure the details are accurate and complete. You can go back to
          make changes if anything needs updating.
        </p>

        <dl className="govuk-summary-list">
          <div className="govuk-summary-list__row">
            <dt className="govuk-summary-list__key">Why are you reopening this {submission.dataCollectionName} submission?</dt>
            <dd className="govuk-summary-list__value">{reason}</dd>
            <dd className="govuk-summary-list__actions">
              <a
                className="govuk-link"
                href="#change-reason"
                onClick={(e) => {
                  e.preventDefault();
                  setStep('details');
                }}>
                Change<span className="govuk-visually-hidden"> the reason for reopening</span>
              </a>
            </dd>
          </div>
          <div className="govuk-summary-list__row">
            <dt className="govuk-summary-list__key">Which sections require the local authority&apos;s attention?</dt>
            <dd className="govuk-summary-list__value">
              {submission.sections
                .filter((section) => sectionsToReview.includes(section.formModuleId))
                .map((section) => section.name)
                .join(', ') || 'None selected'}
            </dd>
            <dd className="govuk-summary-list__actions">
              <a
                className="govuk-link"
                href="#change-sections"
                onClick={(e) => {
                  e.preventDefault();
                  setStep('details');
                }}>
                Change<span className="govuk-visually-hidden"> the sections needing attention</span>
              </a>
            </dd>
          </div>
          <div className="govuk-summary-list__row">
            <dt className="govuk-summary-list__key">By what date should the local authority resubmit its {submission.dataCollectionName}?</dt>
            <dd className="govuk-summary-list__value">{formatDateOnly(toIsoDate(resubmitBy))}</dd>
            <dd className="govuk-summary-list__actions">
              <a
                className="govuk-link"
                href="#change-date"
                onClick={(e) => {
                  e.preventDefault();
                  setStep('details');
                }}>
                Change<span className="govuk-visually-hidden"> the resubmission date</span>
              </a>
            </dd>
          </div>
        </dl>

        <h2 className="govuk-heading-m">Now reopen the submission</h2>
        <p className="govuk-body">The local authority will be able to make changes and resubmit information by the new due date.</p>
        {/* TODO(BLUM-6284): when the reopen email is implemented, this becomes
            "We will email the local authority contact with the reason for reopening". */}
        <p className="govuk-body">Your reason for reopening will be shown to the local authority on their submission. Their main contact is:</p>
        <ul className="govuk-list govuk-list--bullet">
          <li>
            <strong>{submission.contact?.organisationName ?? submission.localAuthorityName}</strong>
            {submission.contact?.email ? ` (${submission.contact.email})` : ''}
          </li>
        </ul>
        <p className="govuk-body">
          If these contact details are wrong,{' '}
          <a className="govuk-link" href={ROUTES.admin.organisations.view(localAuthorityId)}>
            update the local authority profile
          </a>{' '}
          before reopening the submission.
        </p>

        <GovUKButton onClick={() => reopen()} disabled={reopening}>
          {reopening ? 'Reopening...' : 'Reopen submission'}
        </GovUKButton>
      </GeneralLayout>
    );
  }

  return (
    <GeneralLayout>
      <a href={backToSubmission} className="govuk-back-link">
        Back
      </a>

      {errors.length > 0 && <GovUKErrorSummary errors={errors} />}

      <span className="govuk-caption-l">{submission.dataCollectionName}</span>
      <h1 className="govuk-heading-l">Reopen submission</h1>
      <GovUKInsetText>
        Local authority: <strong>{submission.localAuthorityName}</strong>
      </GovUKInsetText>

      <form onSubmit={handleContinue}>
        <div className={`govuk-form-group ${fieldError('reopen-reason') ? 'govuk-form-group--error' : ''}`}>
          <label className="govuk-label govuk-label--s" htmlFor="reopen-reason">
            Why are you reopening this {submission.dataCollectionName} submission?
          </label>
          <div id="reopen-reason-hint" className="govuk-hint">
            Give a brief reason for reopening. It will be visible to the local authority.
          </div>
          {fieldError('reopen-reason') && (
            <p id="reopen-reason-error" className="govuk-error-message">
              <span className="govuk-visually-hidden">Error:</span> {fieldError('reopen-reason')}
            </p>
          )}
          <textarea
            className={`govuk-textarea ${fieldError('reopen-reason') ? 'govuk-textarea--error' : ''}`}
            id="reopen-reason"
            name="reopen-reason"
            rows={5}
            aria-describedby={`reopen-reason-hint${fieldError('reopen-reason') ? ' reopen-reason-error' : ''}`}
            value={reason}
            maxLength={MAXIMUM_REASON_LENGTH}
            onChange={(e) => setReason(e.target.value)}
          />
          <div className="govuk-hint">You can enter up to {MAXIMUM_REASON_LENGTH} characters</div>
        </div>

        {sectionOptions.length > 0 && (
          <GovUKFieldset.CheckboxGroup
            name="sections-to-review"
            legend="Which sections require the local authority's attention?"
            legendSize="s"
            hint={`Select the forms of the ${submission.dataCollectionName} data collection you want the local authority to review. They will still be able to make changes to all forms.`}
            options={sectionOptions}
            value={sectionsToReview}
            onChange={(value, checked, allValues) => setSectionsToReview(allValues)}
          />
        )}

        <GovUKDateField
          id="resubmit-by"
          legend={`By what date should the local authority resubmit its ${submission.dataCollectionName}?`}
          hint={`This will be the new due date for this submission for the selected local authority only. It must be within ${MAXIMUM_DAYS_AHEAD} days of today.`}
          value={resubmitBy}
          error={fieldError('resubmit-by')}
          allowFuture
          disallowPast
          required
          onChange={setResubmitBy}
        />

        <GovUKButton type="submit">Continue</GovUKButton>
      </form>
    </GeneralLayout>
  );
}

export default ReopenSubmission;
