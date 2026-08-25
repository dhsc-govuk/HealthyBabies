import React from 'react';
import { Link } from 'react-router-dom';

export interface GovUKPaginationLink {
  /** Bold link text, e.g. "Previous section" or "Next activity". */
  title: string;
  /** Secondary line under the title, e.g. "4 of 9". Optional. */
  label?: string;
  /** Destination. Rendered as a React Router Link so the target is a real, shareable URL. */
  href: string;
}

export interface GovUKPaginationProps {
  previous?: GovUKPaginationLink;
  next?: GovUKPaginationLink;
  /** Distinguishes multiple pagination blocks on one page for assistive technology. */
  ariaLabel?: string;
}

/**
 * GOV.UK pagination, block variant (`govuk-pagination--block`).
 *
 * Used for "previous / next" navigation through an ordered set of pages where each link needs a
 * descriptive title rather than a page number — sections of a submission, activities within a
 * section. Omit `previous` or `next` at the start or end of the set; omitting both renders nothing.
 *
 * The numbered variant re-exported from `govuk-react` covers a different pattern (paged result
 * lists) and does not support the title/label pair this markup needs.
 */
function GovUKPagination({ previous, next, ariaLabel = 'Pagination' }: GovUKPaginationProps): React.ReactElement | null {
  if (!previous && !next) {
    return null;
  }

  return (
    <nav className="govuk-pagination govuk-pagination--block" aria-label={ariaLabel}>
      {previous && (
        <div className="govuk-pagination__prev">
          <Link className="govuk-link govuk-link--no-visited-state govuk-pagination__link" to={previous.href} rel="prev">
            <svg
              className="govuk-pagination__icon govuk-pagination__icon--prev"
              xmlns="http://www.w3.org/2000/svg"
              height="13"
              width="15"
              aria-hidden="true"
              focusable="false"
              viewBox="0 0 15 13">
              <path d="m6.5938-0.0078125-6.7266 6.7266 6.7441 6.4062 1.377-1.449-4.1856-3.9768h12.896v-2h-12.984l4.2931-4.2931-1.414-1.414z" />
            </svg>
            <span className="govuk-pagination__link-title">{previous.title}</span>
            {previous.label && (
              <>
                <span className="govuk-visually-hidden">:</span>
                <span className="govuk-pagination__link-label">{previous.label}</span>
              </>
            )}
          </Link>
        </div>
      )}

      {next && (
        <div className="govuk-pagination__next">
          <Link className="govuk-link govuk-link--no-visited-state govuk-pagination__link" to={next.href} rel="next">
            <svg
              className="govuk-pagination__icon govuk-pagination__icon--next"
              xmlns="http://www.w3.org/2000/svg"
              height="13"
              width="15"
              aria-hidden="true"
              focusable="false"
              viewBox="0 0 15 13">
              <path d="m8.107-0.0078125-1.4136 1.414 4.2926 4.293h-12.986v2h12.896l-4.1855 3.9766 1.377 1.4492 6.7441-6.4062-6.7246-6.7266z" />
            </svg>
            <span className="govuk-pagination__link-title">{next.title}</span>
            {next.label && (
              <>
                <span className="govuk-visually-hidden">:</span>
                <span className="govuk-pagination__link-label">{next.label}</span>
              </>
            )}
          </Link>
        </div>
      )}
    </nav>
  );
}

export default GovUKPagination;
