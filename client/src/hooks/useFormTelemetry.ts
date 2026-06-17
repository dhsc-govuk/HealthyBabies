import { useCallback, useRef } from 'react';
import { appInsights } from '../telemetry';

type FormType =
  | 'service_user_submission'
  | 'outcome_score_submission'
  | 'wider_service_submission'
  | 'bulk_upload_submission'
  | 'module_form'
  | 'section_form'
  | 'service_create'
  | 'service_edit'
  | 'location_create'
  | 'location_edit';

const useFormTelemetry = (formType: FormType, moduleName?: string) => {
  const started = useRef(false);

  const track = useCallback(
    (name: string, properties?: Record<string, string>) => {
      appInsights.trackEvent({ name }, { formType, ...(moduleName ? { moduleName } : {}), ...properties });
    },
    [formType, moduleName]
  );

  const trackStarted = useCallback(() => {
    if (started.current) return;
    started.current = true;
    track('form_started');
  }, [track]);

  const trackSectionCompleted = useCallback((section: string) => track('form_section_completed', { section }), [track]);

  const trackReviewReached = useCallback(() => track('form_review_reached'), [track]);

  const trackSubmitAttempted = useCallback(() => track('form_submit_attempted'), [track]);

  const trackSubmitted = useCallback(() => track('form_submitted'), [track]);

  const trackAbandoned = useCallback(() => track('form_abandoned'), [track]);

  const trackDraftSaved = useCallback(() => track('form_draft_saved'), [track]);

  const trackValidationFailed = useCallback(
    (field: string, rule: string) => track('validation_failed', { field, rule, commandType: formType }),
    [track, formType]
  );

  return {
    trackStarted,
    trackSectionCompleted,
    trackReviewReached,
    trackSubmitAttempted,
    trackSubmitted,
    trackAbandoned,
    trackDraftSaved,
    trackValidationFailed,
  };
};

export default useFormTelemetry;
