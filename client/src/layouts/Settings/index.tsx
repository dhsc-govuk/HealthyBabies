import React, { useState } from 'react';
import { Box } from '@mui/material';
import { useLocation, Link } from 'react-router-dom';
import { EnumUserRole, useAuthProvider } from '../../components/AuthProvider';
import LayoutHeader from '../General/LayoutHeader';
import LayoutFooter from '../General/LayoutFooter';
import GovUKPhaseBanner from '../../components/GovUKComponents/GovUKPhaseBanner';
import { GovUKBreadcrumbs, GovUKBackLink, GovUKNotificationDisplay, GovUKSkipLink } from '../../components/GovUKComponents';
import usePageTitle from '../../hooks/usePageTitle';
import { ROUTES } from '../../constants/routes';
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
  /** Set false when a child owns document.title (multi-step flows appending "Step X of Y"). */
  manageTitle?: boolean;
}

const adminNavigation: NavItem[] = [
  { label: 'Local authorities', href: ROUTES.admin.organisations.list },
  { label: 'LA users', href: ROUTES.admin.laUsers.list },
  { label: 'Departmental users', href: ROUTES.admin.departmentalUsers.list },
  { label: 'Data collections', href: ROUTES.admin.dataCollections.list },
  { label: 'Service Form', href: ROUTES.admin.configuration.serviceFormQuestions.list },
  { label: 'Delivery Location Form', href: ROUTES.admin.configuration.siteFormQuestions.list },
  { label: 'Data Collection Form', href: ROUTES.admin.configuration.dataCollectionFormQuestions.list },
  { label: 'Delivery Plan Form', href: ROUTES.admin.configuration.deliveryPlanFormQuestions.list },
  { label: 'Form Modules', href: ROUTES.admin.configuration.dataCollectionFormModules },
  { label: 'Lookup Data', href: ROUTES.admin.configuration.lookupData.list },
  { label: 'Guidance', href: ROUTES.admin.guidance },
  { label: 'Core Data downloads', href: ROUTES.admin.coreData },
];

const organisationAdminNavigation: NavItem[] = [{ label: 'LA users', href: ROUTES.organisationAdmin.laUsers.list }];

const SettingsLayout = ({ children, breadcrumbs, currentPage, navigation, backLink, hideNavigation = false, manageTitle = true }: Props): React.ReactElement => {
  usePageTitle(manageTitle ? currentPage : undefined);
  const { userRole } = useAuthProvider();
  const defaultNavigation = userRole === EnumUserRole.ORGANISATION_ADMIN ? organisationAdminNavigation : adminNavigation;
  const effectiveNavigation = navigation ?? defaultNavigation;
  const location = useLocation();
  // Below the tablet breakpoint the secondary nav collapses behind a "Menu" button so the
  // page reflows to a single column without a nested scrollable region (WCAG 1.4.10).
  const [navMenuOpen, setNavMenuOpen] = useState(false);

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
              <>
                <button type="button" className="app-subnav__toggle" aria-controls="settings-nav-list" aria-expanded={navMenuOpen} onClick={() => setNavMenuOpen((open) => !open)}>
                  Menu
                </button>
                <nav
                  id="settings-nav-list"
                  className={`app-subnav app-subnav--collapsible settings-nav${navMenuOpen ? ' app-subnav--open' : ''}`}
                  aria-labelledby="settings-subnav-heading">
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
              </>
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
