import React, { useState, useEffect } from 'react';
import { SERVICE_NAME } from '../../hooks/usePageTitle';

const SUFFIX = ` - ${SERVICE_NAME} - GOV.UK`;

/**
 * A step transition also moves focus (see useStepFocus), and its effect runs *before* the one
 * that sets document.title, so the two announcements compete.
 *
 * The region is assertive rather than polite because "polite" means "speak when idle": the
 * reader is still announcing the newly focused control, so a polite message is *discarded*
 * rather than deferred, leaving the new title in the DOM unannounced until some later
 * interaction. Assertive interrupts instead of queueing, so it is always spoken.
 *
 * The delay works with assertive, not instead of it — it lets the focus announcement finish
 * first so assertive speaks into silence. Announcing immediately would cut the focused
 * control's name off mid-sentence, trading one lost announcement for another.
 */
const ANNOUNCE_DELAY_MS = 500;

const AriaPageAnnouncer: React.FC = () => {
  const [announcement, setAnnouncement] = useState('');

  useEffect(() => {
    const titleEl = document.querySelector('title');
    if (!titleEl) return;

    let timer: ReturnType<typeof setTimeout> | undefined;

    const observer = new MutationObserver(() => {
      const full = document.title;
      const text = full.endsWith(SUFFIX) ? full.slice(0, -SUFFIX.length) : full;

      // Emptying the region first means a repeated title still reads as a change, so it is
      // announced again rather than silently ignored.
      setAnnouncement('');
      clearTimeout(timer);
      timer = setTimeout(() => setAnnouncement(text), ANNOUNCE_DELAY_MS);
    });

    // Assigning document.title may replace the child text node or mutate the existing
    // one in place depending on the engine, so watch both. Either can fire more than once
    // for a single assignment; the debounce above collapses that into one announcement.
    observer.observe(titleEl, { childList: true, characterData: true, subtree: true });
    return () => {
      observer.disconnect();
      clearTimeout(timer);
    };
  }, []);

  return (
    <div aria-live="assertive" aria-atomic="true" className="govuk-visually-hidden">
      {announcement}
    </div>
  );
};

export default AriaPageAnnouncer;
