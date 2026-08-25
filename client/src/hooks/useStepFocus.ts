import { useRef, useEffect } from 'react';

const FOCUSABLE_SELECTOR = ['input:not([type="hidden"])', 'select', 'textarea', 'button', 'a[href]', '[tabindex]']
  .map((selector) => `${selector}:not([disabled]):not([aria-hidden="true"]):not([tabindex="-1"])`)
  .join(', ');

const isVisible = (el: HTMLElement): boolean => !!(el.offsetWidth || el.offsetHeight || el.getClientRects().length);

export const findFirstInteractive = (container: HTMLElement): HTMLElement | null =>
  Array.from(container.querySelectorAll<HTMLElement>(FOCUSABLE_SELECTOR)).find(isVisible) ?? null;

// Focuses a fallback target (heading, else the container) so the user is never stranded on
// the previous step's button or on document.body — the latter makes NVDA restart reading
// from the top of the navigation bar.
const focusFallback = (container: HTMLElement): HTMLElement => {
  const target = container.querySelector<HTMLElement>('h1, h2, h3, h4') ?? container;
  if (!target.hasAttribute('tabindex')) target.setAttribute('tabindex', '-1');
  target.focus({ preventScroll: true });
  return target;
};

/**
 * Moves focus to the first interactive element inside the ref'd step container on every step
 * change (WCAG 2.4.3). Step content can render asynchronously — after a parse or validation
 * round trip — so if nothing focusable exists yet we focus the container immediately for
 * orientation, then upgrade to the first interactive element once it appears.
 *
 * Use: const ref = useStepFocus(currentStep); then <div ref={ref}>…</div>
 */
export default function useStepFocus<T extends HTMLElement = HTMLElement>(step: unknown): React.RefObject<T> {
  const ref = useRef<T>(null);
  // Seeded with the step we mounted on, so arriving on the page is never mistaken for a step
  // transition. Arrival focus belongs to the route: useRouteFocus puts focus at the top of the
  // page so the user's first Tab reaches "Skip to main content" and they choose when to enter
  // the content. Focusing a form field here on mount would steal that.
  //
  // This compares the step value rather than counting runs, because a run counter is not safe
  // against effects that fire more than once for the same step: StrictMode replays every mount
  // effect in development (mount -> cleanup -> mount) on the same instance, so refs survive and
  // the replay would sail past a "skip the first run" guard and focus the field anyway. A
  // data-loading page mounting this a second time once its spinner is replaced is the same
  // problem. Keying on the value makes both no-ops, since the step has not changed.
  const prevStepRef = useRef(step);

  useEffect(() => {
    if (step === prevStepRef.current) return;
    prevStepRef.current = step;

    const container = ref.current;
    if (!container) return;

    window.scrollTo(0, 0);

    const immediate = findFirstInteractive(container);
    if (immediate) {
      immediate.focus({ preventScroll: true });
      return;
    }

    // Nothing interactive yet: orient the user at the top now, then watch for async content.
    const fallback = focusFallback(container);

    let frame = 0;
    let attempts = 0;
    const upgrade = () => {
      const current = ref.current;
      if (!current) return;
      // Bail out if the user has already moved focus themselves.
      const stillOurs = document.activeElement === fallback || document.activeElement === document.body;
      if (!stillOurs) return;
      const target = findFirstInteractive(current);
      if (target) {
        target.focus({ preventScroll: true });
        return;
      }
      if (attempts++ > 30) return;
      frame = requestAnimationFrame(upgrade);
    };
    frame = requestAnimationFrame(upgrade);
    return () => cancelAnimationFrame(frame);
  }, [step]); // eslint-disable-line react-hooks/exhaustive-deps

  return ref as React.RefObject<T>;
}
