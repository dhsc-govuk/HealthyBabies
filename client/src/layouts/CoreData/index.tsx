import React from 'react';
import { Box } from '@mui/material';
import { useLocation, Link } from 'react-router-dom';
import LayoutHeader from '../General/LayoutHeader';
import LayoutFooter from '../General/LayoutFooter';
import GovUKPhaseBanner from '../../components/GovUKComponents/GovUKPhaseBanner';
import { GovUKBreadcrumbs, GovUKBackLink, GovUKNotificationDisplay, GovUKSkipLink } from '../../components/GovUKComponents';
import usePageTitle from '../../hooks/usePageTitle';
import './styles.css';

const govukWhite = '#ffffff';

interface NavItem {
  label: string;
  href: string;
}

interface Props extends React.PropsWithChildren {
  breadcrumbs?: { label: string; link: string }[];
  currentPage?: string;
  navigation?: NavItem[];
  backLink?: { href: string; onClick?: () => void };
}

const coreDataNavigation: NavItem[] = [
  { label: 'Delivery locations', href: '/organisation-admin/core-data/delivery-locations' },
  { label: 'Services', href: '/organisation-admin/core-data/services' },
  { label: 'Wider service categories', href: '/organisation-admin/core-data/wider-service-categories' },
];

const CoreDataLayout = ({ children, breadcrumbs, currentPage, navigation, backLink }: Props): React.ReactElement => {
  usePageTitle(currentPage);
  const effectiveNavigation = navigation ?? coreDataNavigation;
  const location = useLocation();

  const isActive = (href: string): boolean => {
    return location.pathname === href || location.pathname.startsWith(`${href}/`);
  };

  return (
    <Box
      sx={{
        minHeight: '100vh',
        display: 'flex',
        flexDirection: 'column',
        backgroundColor: govukWhite,
      }}>
      <GovUKSkipLink />
      <LayoutHeader />

      <Box component="main" className="govuk-main-wrapper" id="main-content" role="main">
        <Box className="govuk-width-container">
          <GovUKPhaseBanner phase="beta" />
          <Box className="core-data-layout">
            {/* Left Navigation */}
            <nav className="app-subnav core-data-nav" aria-labelledby="core-data-subnav-heading">
              <h2 className="govuk-visually-hidden" id="core-data-subnav-heading">
                Core data navigation
              </h2>
              <ul className="app-subnav__section">
                {effectiveNavigation.map((item) => (
                  <li key={item.href} className={`app-subnav__section-item${isActive(item.href) ? ' app-subnav__section-item--current' : ''}`}>
                    <Link
                      to={item.href}
                      className="app-subnav__link govuk-link govuk-link--no-visited-state govuk-link--no-underline"
                      aria-current={isActive(item.href) ? 'page' : undefined}>
                      {item.label}
                    </Link>
                  </li>
                ))}
              </ul>
            </nav>

            {/* Main Content */}
            <Box className="core-data-content">
              {breadcrumbs && breadcrumbs.length > 0 && <GovUKBreadcrumbs items={breadcrumbs.map((item) => ({ label: item.label, href: item.link }))} />}
              {backLink && (
                <GovUKBackLink
                  href={backLink.href}
                  onClick={
                    backLink.onClick
                      ? (e) => {
                          e.preventDefault();
                          backLink.onClick?.();
                        }
                      : undefined
                  }
                />
              )}
              {currentPage && <h1 className="govuk-heading-l">{currentPage}</h1>}
              <GovUKNotificationDisplay />
              {children}
            </Box>
          </Box>
        </Box>
      </Box>

      <LayoutFooter />
    </Box>
  );
};

export default CoreDataLayout;
