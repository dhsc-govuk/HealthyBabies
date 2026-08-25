// The bulk upload steps style their own summary (.bulk-upload-error-summary) rather than
// using the govuk-frontend class, so match both — otherwise this lookup returns null on
// those pages and they neither scroll to the summary nor move focus to it.
const ERROR_SUMMARY_SELECTOR = '.govuk-error-summary, .bulk-upload-error-summary';

const focusErrorSummary = (): void => {
  if (typeof window === 'undefined') return;

  const el = document.querySelector<HTMLElement>(ERROR_SUMMARY_SELECTOR);
  if (el) {
    el.scrollIntoView({ behavior: 'smooth', block: 'start' });
    if (el.tabIndex < 0) el.tabIndex = -1;
    el.focus({ preventScroll: true });
  }
};

export default focusErrorSummary;
