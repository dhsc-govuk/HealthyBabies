import React from 'react';
import './styles.css';

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
    const targetElement = document.getElementById(targetId);
    if (targetElement) {
      targetElement.setAttribute('tabindex', '-1');
      targetElement.focus();
      targetElement.scrollIntoView({ behavior: 'smooth' });
    }
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
