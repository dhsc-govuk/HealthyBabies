import { useEffect } from 'react';
import focusErrorSummary from '../helpers/focusErrorSummary';

const useErrorSummaryFocus = (submitAttempts: number, hasErrors: boolean): void => {
  useEffect(() => {
    if (submitAttempts === 0 || !hasErrors) return;
    focusErrorSummary();
  }, [submitAttempts, hasErrors]);
};

export default useErrorSummaryFocus;
