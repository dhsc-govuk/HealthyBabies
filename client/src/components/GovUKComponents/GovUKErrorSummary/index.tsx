import React, { useEffect, useRef } from 'react';
import focusErrorTarget, { focusErrorTargetById } from '../../../helpers/focusErrorTarget';

export interface ErrorItem {
  /**
   * Id of the field this error belongs to. Omit for errors with no associated field (e.g. a
   * generic server failure) — the entry then renders as text rather than a link to nowhere.
   */
  targetName?: string;
  text: string;
  /**
   * Run before focusing — for errors whose field lives on a different step of the flow
   * (e.g. a server-side duplicate-name error raised on the check-answers step). The target
   * is focused on the next frame, once the step that owns it has rendered.
   */
  onNavigate?: () => void;
}

interface GovUKErrorSummaryProps {
  errors: ErrorItem[];
  heading?: string;
}

const GovUKErrorSummary = ({ errors, heading = 'There is a problem' }: GovUKErrorSummaryProps): React.ReactElement | null => {
  const ref = useRef<HTMLDivElement>(null);

  useEffect(() => {
    ref.current?.focus();
  }, []);

  if (errors.length === 0) return null;

  return (
    <div className="govuk-error-summary" tabIndex={-1} ref={ref} data-module="govuk-error-summary">
      <div role="alert">
        <h2 className="govuk-error-summary__title">{heading}</h2>
        <div className="govuk-error-summary__body">
          <ul className="govuk-list govuk-error-summary__list">
            {errors.map((error) => {
              const targetName = error.targetName;
              return (
                <li key={targetName ?? error.text}>
                  {targetName ? (
                    <a
                      href={`#${targetName}`}
                      onClick={
                        error.onNavigate
                          ? (e) => {
                              e.preventDefault();
                              error.onNavigate?.();
                              requestAnimationFrame(() => focusErrorTargetById(targetName));
                            }
                          : (e) => focusErrorTarget(e, targetName)
                      }>
                      {error.text}
                    </a>
                  ) : (
                    error.text
                  )}
                </li>
              );
            })}
          </ul>
        </div>
      </div>
    </div>
  );
};

export default GovUKErrorSummary;
