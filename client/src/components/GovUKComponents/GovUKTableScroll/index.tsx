import React, { useRef, useState, useEffect } from 'react';
import './styles.css';

interface GovUKTableScrollProps extends React.PropsWithChildren {
  /** Accessible name for the scrollable region, e.g. "Allocations table". */
  label?: string;
}

/**
 * Horizontal scroll container for tables with too many columns to practically stack at
 * 320px / 400% zoom. Only this region scrolls, so the page itself never scrolls
 * horizontally (WCAG 1.4.10).
 *
 * The region becomes focusable *only while it actually overflows*, so narrow tables don't
 * add a redundant tab stop for every user. A scrollable region must be reachable by
 * keyboard (WCAG 2.1.1) — a non-scrolling one must not be.
 */
const GovUKTableScroll = ({ children, label = 'Table' }: GovUKTableScrollProps): React.ReactElement => {
  const ref = useRef<HTMLDivElement>(null);
  const [scrollable, setScrollable] = useState(false);

  useEffect(() => {
    const el = ref.current;
    if (!el) return;

    const measure = () => setScrollable(el.scrollWidth > el.clientWidth + 1);
    measure();

    // Observe the wrapper (viewport/zoom changes) and the table itself (data loading in,
    // columns changing) — the wrapper's own box does not change when its content grows.
    const observer = new ResizeObserver(measure);
    observer.observe(el);
    if (el.firstElementChild) observer.observe(el.firstElementChild);

    return () => observer.disconnect();
  }, [children]);

  return (
    <div
      ref={ref}
      className="govuk-table-scroll"
      tabIndex={scrollable ? 0 : undefined}
      role={scrollable ? 'region' : undefined}
      aria-label={scrollable ? label : undefined}>
      {children}
    </div>
  );
};

export default GovUKTableScroll;
