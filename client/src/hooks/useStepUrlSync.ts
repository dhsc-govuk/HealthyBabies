import { useEffect, useRef } from 'react';
import { useSearchParams } from 'react-router-dom';

interface UseStepUrlSyncOptions<T extends string> {
  /** The flow's current step, owned by the caller's reducer. */
  step: T;
  /** All steps in order. The first is treated as the flow's entry point. */
  steps: readonly T[];
  /** Called when the URL asks for a different step, e.g. after browser Back/Forward. */
  onStepChange: (step: T) => void;
  /**
   * Whether the in-memory data a later step depends on is still present. A bulk upload holds
   * its parsed file in memory only, so a reload or a deep link into a later step has nothing
   * to show — the flow is sent back to its first step instead of rendering an empty table.
   */
  isResumable: boolean;
  /**
   * Steps the URL should ignore entirely — transient states such as "processing". While the
   * flow sits on one of these the URL keeps showing the previous step, so that step stays in
   * history and Back never lands on a transient state. (Replacing the entry instead would
   * overwrite the previous step and lose it from history.)
   */
  transientSteps?: readonly T[];
}

/**
 * Mirrors a multi-step flow's current step into a `?step=` search param.
 *
 * The bulk upload steps live in reducer state, so the browser creates no history entry per
 * step and Back leaves the flow entirely instead of returning to the previous step. Putting
 * the step in the URL gives each one its own entry, so Back/Forward move within the flow —
 * and because the page title is derived from the step, the title reverts and is re-announced
 * automatically.
 */
export default function useStepUrlSync<T extends string>({ step, steps, onStepChange, isResumable, transientSteps = [] }: UseStepUrlSyncOptions<T>): void {
  const [searchParams, setSearchParams] = useSearchParams();
  const urlStep = searchParams.get('step') as T | null;
  const hasSyncedRef = useRef(false);

  // URL -> state: browser Back/Forward, and the initial load of a deep link.
  useEffect(() => {
    const isFirstRun = !hasSyncedRef.current;
    hasSyncedRef.current = true;

    // While on a transient step the URL deliberately lags behind, so a mismatch is expected
    // and must not drag the flow backwards.
    if (transientSteps.includes(step)) return;
    if (!urlStep || urlStep === step || !steps.includes(urlStep)) return;

    // Only a deep link or a reload can land on a later step with no data behind it. Applying
    // this to Back/Forward as well would rewrite the URL and teleport the user to step 1,
    // which looks exactly like being thrown out of the flow.
    if (isFirstRun && urlStep !== steps[0] && !isResumable) {
      const next = new URLSearchParams(searchParams);
      next.set('step', steps[0]);
      setSearchParams(next, { replace: true });
      return;
    }

    onStepChange(urlStep);
    // Only react to the URL changing; re-running on every render would fight the effect below.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [urlStep]);

  // state -> URL: the flow advancing, or going back via its own in-page controls.
  useEffect(() => {
    if (transientSteps.includes(step) || urlStep === step) return;

    const next = new URLSearchParams(searchParams);
    next.set('step', step);
    // Only moving forward creates a history entry. The entry step replaces so Back from it
    // leaves the flow, and an in-page "Back" replaces too — pushing there would add a
    // forward entry, so the browser Back button would then move *deeper* into the flow.
    const movingBackwards = urlStep !== null && steps.indexOf(step) < steps.indexOf(urlStep);
    setSearchParams(next, { replace: step === steps[0] || movingBackwards });
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [step]);
}
