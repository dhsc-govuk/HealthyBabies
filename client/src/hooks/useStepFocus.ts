import { useRef, useEffect } from 'react';

// Moves focus to the first heading inside the ref'd container on every step change.
// Falls back to focusing the container itself if no heading is found.
// Use: const ref = useStepFocus(currentStep); then <div ref={ref}>…</div>
export default function useStepFocus<T extends HTMLElement = HTMLElement>(step: unknown): React.RefObject<T> {
  const ref = useRef<T>(null);

  useEffect(() => {
    const el = ref.current;
    if (!el) return;
    const target = el.querySelector<HTMLElement>('h1, h2, h3, h4') ?? el;
    if (!target.hasAttribute('tabindex')) target.setAttribute('tabindex', '-1');
    target.focus({ preventScroll: true });
  }, [step]); // eslint-disable-line react-hooks/exhaustive-deps

  return ref as React.RefObject<T>;
}
