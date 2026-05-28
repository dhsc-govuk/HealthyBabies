import React from 'react';
import { Link } from 'react-router-dom';

interface GovUKBackLinkProps {
  href?: string;
  children?: React.ReactNode;
  onClick?: (e: React.MouseEvent) => void;
}

/**
 * GOV.UK back link component.
 * Always renders as an anchor (<a>) for consistent GOV.UK styling and focus behaviour.
 * - When a real href is provided: renders as a React Router Link (SPA navigation)
 * - When no href or href="#" is provided: renders as an <a href="#"> with preventDefault (JS-only navigation, e.g. multi-step forms)
 */
function GovUKBackLink({ href, children = 'Back', onClick }: GovUKBackLinkProps): React.ReactElement {
  if (href && href !== '#') {
    return (
      <Link to={href} className="govuk-back-link" onClick={onClick}>
        {children}
      </Link>
    );
  }

  return (
    <a
      href="#"
      className="govuk-back-link"
      onClick={(e) => {
        e.preventDefault();
        onClick?.(e);
      }}>
      {children}
    </a>
  );
}

export default GovUKBackLink;
