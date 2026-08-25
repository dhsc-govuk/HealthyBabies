const FOCUSABLE_SELECTOR = ['input:not([type="hidden"])', 'select', 'textarea', 'button', '[tabindex]:not([tabindex="-1"])']
  .map((selector) => `${selector}:not([disabled])`)
  .join(', ');

const isFocusable = (el: HTMLElement): boolean => el.matches(FOCUSABLE_SELECTOR);

/**
 * Resolves an error-summary fragment (e.g. "SPCP01", "fhs01", "dateopened") to the form
 * control the user should be taken to.
 *
 * govuk-frontend's own ErrorSummary JavaScript normally does this, but it is never
 * initialised in this app (there is no `initAll` call), so a summary link falls back to
 * plain native fragment navigation. That breaks whenever the id does not sit on a focusable
 * element, which is the common case here:
 *
 *  - radio/checkbox groups put the id only on each option input (`CODE-Yes`), so no element
 *    with id `CODE` exists at all and the link does nothing;
 *  - date fields put the id on a non-focusable `<div class="govuk-date-input">`, so the page
 *    scrolls but focus never moves;
 *  - some summaries emit the raw question code while the field renders a lowercased id.
 *
 * Resolution order: exact id → case-insensitive id → first `CODE-*` descendant id (groups).
 * Whichever element is found, focus lands on it if focusable, otherwise on its first
 * focusable descendant.
 */
const resolveTarget = (rawId: string): HTMLElement | null => {
  const id = rawId.replace(/^#/, '');
  if (!id) return null;

  const candidates = (): HTMLElement[] => Array.from(document.querySelectorAll<HTMLElement>('[id]'));

  const focusableWithin = (el: HTMLElement): HTMLElement => {
    if (isFocusable(el)) return el;
    return el.querySelector<HTMLElement>(FOCUSABLE_SELECTOR) ?? el;
  };

  const exact = document.getElementById(id);
  if (exact) return focusableWithin(exact);

  const lower = id.toLowerCase();

  const caseInsensitive = candidates().find((el) => el.id.toLowerCase() === lower);
  if (caseInsensitive) return focusableWithin(caseInsensitive);

  // Grouped controls: the summary names the question, the DOM ids are `question-option`.
  const grouped = candidates().filter((el) => el.id.toLowerCase().startsWith(`${lower}-`));
  const groupedFocusable = grouped.find(isFocusable);
  if (groupedFocusable) return groupedFocusable;
  if (grouped.length > 0) return focusableWithin(grouped[0]);

  return null;
};

/**
 * Moves focus to the form control an error-summary link points at, matching the GOV.UK
 * Design System behaviour. Returns true if a target was found and focused.
 */
export const focusErrorTargetById = (rawId: string): boolean => {
  const target = resolveTarget(rawId);
  if (!target) return false;

  // Scroll the whole form group so the field's label/legend and error message are visible,
  // then focus without a second competing scroll.
  const scrollAnchor = target.closest('.govuk-form-group') ?? target;
  scrollAnchor.scrollIntoView({ block: 'center' });

  if (!isFocusable(target) && !target.hasAttribute('tabindex')) {
    target.setAttribute('tabindex', '-1');
  }
  target.focus({ preventScroll: true });
  return true;
};

/**
 * Click handler for an error-summary link. Keeps the href intact (so it still behaves like a
 * link for keyboard, middle-click and copy) but takes over focus management when the target
 * is resolvable.
 */
const focusErrorTarget = (event: React.MouseEvent<HTMLAnchorElement>, rawId: string): void => {
  if (focusErrorTargetById(rawId)) {
    event.preventDefault();
  }
};

export default focusErrorTarget;
