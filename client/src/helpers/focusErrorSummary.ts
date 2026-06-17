const focusErrorSummary = (): void => {
  if (typeof window === 'undefined') return;

  const prefersReducedMotion = window.matchMedia?.('(prefers-reduced-motion: reduce)').matches;

  window.scrollTo({ top: 0, behavior: prefersReducedMotion ? 'auto' : 'smooth' });
};

export default focusErrorSummary;
