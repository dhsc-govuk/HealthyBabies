import { useEffect } from 'react';
import focusErrorSummary from '../helpers/focusErrorSummary';

const useErrorSummaryFocus = (submitAttempts: number, hasErrors: boolean): void => {
  // Only re-focus when a new submit attempt happens, not on every subsequent change to
  // hasErrors — otherwise fixing one field while others are still in error (or a live-validated
  // field like a date input toggling its error state as the user types) repeatedly rips focus
  // away from whatever the user is actively editing.
  useEffect(() => {
    if (submitAttempts === 0 || !hasErrors) return;
    focusErrorSummary();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [submitAttempts]);
};

export default useErrorSummaryFocus;
