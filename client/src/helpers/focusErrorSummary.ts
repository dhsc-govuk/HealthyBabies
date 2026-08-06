const focusErrorSummary = (): void => {
  if (typeof window === 'undefined') return;

  const el = document.querySelector<HTMLElement>('.govuk-error-summary');
  if (el) {
    el.scrollIntoView({ behavior: 'smooth', block: 'start' });
    if (el.tabIndex < 0) el.tabIndex = -1;
    el.focus({ preventScroll: true });
  }
};

export default focusErrorSummary;
