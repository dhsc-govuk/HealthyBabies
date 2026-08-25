import React from 'react';

interface GovUKSkipLinkProps {
  href?: string;
  children?: React.ReactNode;
}

const GovUKSkipLink: React.FC<GovUKSkipLinkProps> = ({ 
  href = '#main-content', 
  children = 'Skip to main content' 
}) => {
  const handleClick = (e: React.MouseEvent<HTMLAnchorElement>) => {
    e.preventDefault();
    const targetId = href.replace('#', '');
    const region = document.getElementById(targetId);
    if (!region) return;

    // Land on the region's first heading rather than the region itself, so a screen reader
    // announces what the page is about ("Manage services, heading level 1") instead of an
    // unnamed region. Falls back to the region when a page has no heading.
    const target = region.querySelector<HTMLElement>('h1, h2, h3, h4, h5, h6') ?? region;
    target.setAttribute('tabindex', '-1');
    target.focus();
    target.scrollIntoView({ behavior: 'smooth' });
  };

  return (
    <a 
      href={href} 
      className="govuk-skip-link" 
      data-module="govuk-skip-link"
      onClick={handleClick}
    >
      {children}
    </a>
  );
};

export default GovUKSkipLink;
