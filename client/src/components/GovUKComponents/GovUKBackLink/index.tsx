import React from 'react';
import { Link } from 'react-router-dom';
import './styles.css';

interface GovUKBackLinkProps {
  href?: string;
  children?: React.ReactNode;
  onClick?: (e: React.MouseEvent) => void;
}

/**
 * GOV.UK back link component.
 * - When a real href is provided: renders as a React Router Link (SPA navigation)
 * - When no href or href="#" is provided: renders as a link-styled button for JavaScript-only navigation
 */
function GovUKBackLink({ href, children = 'Back', onClick }: GovUKBackLinkProps): React.ReactElement {
  if (href && href !== '#') {
    return (
      <Link to={href} className="govuk-back-link" onClick={onClick}>
        {children}
      </Link>
    );
  }

  // Rendered as a real button (not an anchor with a placeholder href) so keyboard and
  // assistive-technology semantics are correct. GovUKLinkButton is deliberately not used here:
  // it applies `govuk-link` and `font: inherit`, which would paint the back link link-blue and
  // at body font size instead of matching the anchor version above.
  return (
    <button type="button" className="govuk-back-link govuk-back-link--button" onClick={onClick}>
      {children}
    </button>
  );
}

export default GovUKBackLink;
