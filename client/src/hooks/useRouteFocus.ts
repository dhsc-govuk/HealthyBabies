import { useEffect } from 'react';
import { useLocation } from 'react-router-dom';

const APP_TOP_ID = 'app-top';
const MAIN_CONTENT_ID = 'main-content';

/**
 * Resets keyboard focus to the top of the page on every route change (WCAG 2.4.3,
 * DAC_Focus_Order_01).
 *
 * This is a single-page app, so a route change swaps the content without the browser resetting
 * focus. Focus would otherwise stay wherever the user left it — often a footer link, or
 * document.body once the element it was on unmounted — so a screen reader starts reading from
 * the top of the document with no sense that the page changed.
 *
 * The target is the app's outermost container, which sits *ahead of the skip link* in the DOM.
 * That reproduces what a full page load gives you: focus at the very start of the page, so the
 * user's first Tab lands on "Skip to main content" (which is visually hidden until focused, and
 * hides again once they Tab past it). Focusing the skip link itself would reveal it on every
 * navigation, including for mouse users; focusing the header instead would put the skip link
 * behind the user, reachable only by Shift+Tab.
 *
 * Main content is reached by activating the skip link, which focuses the region's first heading
 * (see GovUKSkipLink).
 *
 * Called once inside the router so it covers every route rather than being repeated per view.
 */
export default function useRouteFocus(): void {
  const { pathname } = useLocation();

  useEffect(() => {
    window.scrollTo(0, 0);

    const target = document.getElementById(APP_TOP_ID) ?? document.getElementById(MAIN_CONTENT_ID);
    if (!target) return;

    if (!target.hasAttribute('tabindex')) target.setAttribute('tabindex', '-1');
    target.focus({ preventScroll: true });
    // Keyed on pathname only: the bulk upload flows sync a ?step= search param, and reacting to
    // that would pull focus back to the top on every step change, fighting useStepFocus.
  }, [pathname]);
}
