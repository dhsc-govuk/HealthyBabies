import React from 'react';
import { Box } from '@mui/material';
import { useLocation, Link } from 'react-router-dom';
import { EnumUserRole, useAuthProvider } from '../../components/AuthProvider';
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
  hideNavigation?: boolean;
}

const adminNavigation: NavItem[] = [
  { label: 'Local authorities', href: '/admin/organisations' },
  { label: 'LA users', href: '/admin/la-users' },
  { label: 'Departmental users', href: '/admin/departmental-users' },
  { label: 'Data collections', href: '/admin/data-collections' },
  { label: 'Service Form', href: '/admin/configuration/service-form-questions' },
  { label: 'Delivery Location Form', href: '/admin/configuration/site-form-questions' },
  { label: 'Data Collection Form', href: '/admin/configuration/data-collection-form-questions' },
  { label: 'Form Modules', href: '/admin/configuration/data-collection-form-modules' },
  { label: 'Lookup Data', href: '/admin/configuration/lookup-data' },
  { label: 'Guidance', href: '/admin/guidance' },
];

const organisationAdminNavigation: NavItem[] = [{ label: 'LA users', href: '/organisation-admin/la-users' }];

const SettingsLayout = ({ children, breadcrumbs, currentPage, navigation, backLink, hideNavigation = false }: Props): React.ReactElement => {
  usePageTitle(currentPage);
  const { userRole } = useAuthProvider();
  const defaultNavigation = userRole === EnumUserRole.ORGANISATION_ADMIN ? organisationAdminNavigation : adminNavigation;
  const effectiveNavigation = navigation ?? defaultNavigation;
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
          <Box className="settings-layout">
            {/* Left Navigation */}
            {!hideNavigation && (
              <nav className="app-subnav settings-nav" aria-labelledby="settings-subnav-heading">
                <h2 className="govuk-visually-hidden" id="settings-subnav-heading">
                  Settings navigation
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
            )}

            {/* Main Content */}
            <Box className="settings-content">
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

export default SettingsLayout;
