import React, { useState, useId } from 'react';
import { Link as RouterLink, useLocation } from 'react-router-dom';
import './styles.css';

export interface SideNavigationItem {
  text: string;
  href: string;
  id?: string;
  nested?: SideNavigationItem[];
}

export interface SideNavigationSection {
  heading: string;
  items: SideNavigationItem[];
}

interface GovUKSideNavigationProps {
  label?: string;
  sections?: SideNavigationSection[];
  items?: SideNavigationItem[];
  activeSection?: string;
}

function GovUKSideNavigation({ label = 'Pages in this section', sections, items, activeSection }: GovUKSideNavigationProps): React.ReactElement {
  const location = useLocation();
  // Below the tablet breakpoint the nav collapses behind a "Menu" button so it never
  // becomes a scrollable region requiring two-dimensional scrolling (WCAG 1.4.10).
  const [menuOpen, setMenuOpen] = useState(false);
  const reactId = useId();
  const navId = `app-subnav-${reactId}`;
  const headingId = `app-subnav-heading-${reactId}`;

  const isActive = (href: string) => {
    if (href.startsWith('#')) {
      const sectionId = href.substring(1);
      if (activeSection) {
        return sectionId === activeSection;
      }
      return location.hash === href;
    }
    return location.pathname === href || location.pathname.startsWith(href + '/');
  };

  const linkClasses = 'app-subnav__link govuk-link govuk-link--no-visited-state govuk-link--no-underline';

  const renderNestedItems = (nestedItems: SideNavigationItem[]) => (
    <ul className="app-subnav__section app-subnav__section--nested">
      {nestedItems.map((item, index) => {
        const active = isActive(item.href);
        const isAnchor = item.href.startsWith('#');

        return (
          <li key={item.id || index} className={`app-subnav__section-item${active ? ' app-subnav__section-item--current' : ''}`}>
            {isAnchor ? (
              <a href={item.href} className={linkClasses} aria-current={active ? 'page' : undefined}>
                {item.text}
              </a>
            ) : (
              <RouterLink to={item.href} className={linkClasses} aria-current={active ? 'page' : undefined}>
                {item.text}
              </RouterLink>
            )}
          </li>
        );
      })}
    </ul>
  );

  const renderItems = (navItems: SideNavigationItem[]) => (
    <ul className="app-subnav__section">
      {navItems.map((item, index) => {
        const active = isActive(item.href);
        const isAnchor = item.href.startsWith('#');

        return (
          <li key={item.id || index} className={`app-subnav__section-item${active ? ' app-subnav__section-item--current' : ''}`}>
            {isAnchor ? (
              <a href={item.href} className={linkClasses} aria-current={active ? 'page' : undefined}>
                {item.text}
              </a>
            ) : (
              <RouterLink to={item.href} className={linkClasses} aria-current={active ? 'page' : undefined}>
                {item.text}
              </RouterLink>
            )}
            {item.nested && item.nested.length > 0 && renderNestedItems(item.nested)}
          </li>
        );
      })}
    </ul>
  );

  return (
    <>
      <button type="button" className="app-subnav__toggle" aria-controls={navId} aria-expanded={menuOpen} onClick={() => setMenuOpen((open) => !open)}>
        Menu
      </button>
      <nav
        id={navId}
        className={`app-subnav app-subnav--collapsible${menuOpen ? ' app-subnav--open' : ''}`}
        aria-labelledby={headingId}>
        <h2 className="govuk-visually-hidden" id={headingId}>
          {label}
        </h2>
        {sections
          ? sections.map((section, sectionIndex) => (
              <React.Fragment key={sectionIndex}>
                <h3 className={`app-subnav__theme${sectionIndex === 0 ? ' app-subnav__theme--first' : ''}`}>{section.heading}</h3>
                {renderItems(section.items)}
              </React.Fragment>
            ))
          : items
          ? renderItems(items)
          : null}
      </nav>
    </>
  );
}

export default GovUKSideNavigation;
