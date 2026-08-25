import React, { useState, useEffect } from 'react';
import { Box, ClickAwayListener } from '@mui/material';
import { Link as RouterLink, useLocation } from 'react-router-dom';
import { GovUKHeaderLogo } from '../../../components/Logos';
import { useAuthProvider, EnumUserRole } from '../../../components/AuthProvider';
import {
  Header,
  HeaderContainer,
  UserProfileWrapper,
  UserProfileButton,
  UserInitials,
  UserDropdown,
  UserDropdownHeader,
  UserName,
  UserEmail,
  LogoutButton,
  govukWhite,
} from './styles';
import { ROUTES } from '../../../constants/routes';
import './service-navigation.css';

const adminNavigation = [
  { label: 'Home', href: ROUTES.admin.home },
  { label: 'Trends', href: ROUTES.admin.trends },
  { label: 'Submissions', href: ROUTES.admin.submissions.list },
  { label: 'Settings', href: ROUTES.admin.settings },
];

const organisationAdminNavigation = [
  { label: 'Summary', href: ROUTES.organisationAdmin.summary },
  { label: 'Core data', href: ROUTES.organisationAdmin.locations.list },
  { label: 'Submissions', href: ROUTES.organisationAdmin.submissions.list },
  { label: 'Guidance', href: ROUTES.organisationAdmin.help },
];

const deliveryPlanNavigation = [
  { label: 'Summary', href: ROUTES.deliveryPlan.summary },
  { label: 'Core data', href: ROUTES.deliveryPlan.coreData },
  { label: 'Submissions', href: ROUTES.deliveryPlan.submissions.list },
  { label: 'Guidance', href: ROUTES.deliveryPlan.help },
];

interface Props {
  serviceName?: string;
  navigation?: { label: string; href: string }[];
}

const LayoutHeader = ({
  serviceName = 'Report data on Best Start Family Hubs and Healthy Babies',
  navigation,
}: Props): React.ReactElement => {
  const location = useLocation();
  const { user, userRole, signOut } = useAuthProvider();

  const effectiveNavigation = React.useMemo(() => {
    if (navigation) return navigation;
    if (userRole === EnumUserRole.ORGANISATION_ADMIN) return organisationAdminNavigation;
    if (userRole === EnumUserRole.DELIVERY_PLAN_LEAD) return deliveryPlanNavigation;
    return adminNavigation;
  }, [navigation, userRole]);

  const [dropdownOpen, setDropdownOpen] = useState(false);

  // Mobile nav toggle — mirrors the GDS JS behaviour:
  // [hidden] is removed from the toggle button after mount (JS available),
  // then aria-expanded drives the CSS sibling selector that shows/hides the list.
  const [mounted, setMounted] = useState(false);
  const [mobileNavOpen, setMobileNavOpen] = useState(false);

  useEffect(() => {
    setMounted(true);
  }, []);

  const isActive = (href: string) => {
    if (href === ROUTES.admin.settings) {
      const settingsPaths = [ROUTES.admin.settings, ROUTES.admin.organisations.list, ROUTES.admin.departmentalUsers.list, ROUTES.admin.laUsers.list, ROUTES.admin.dataCollections.list, ROUTES.admin.forms, ROUTES.admin.guidance, ROUTES.admin.configuration.root, ROUTES.admin.coreData];
      return settingsPaths.some(path => location.pathname.startsWith(path));
    }
    if (href === ROUTES.organisationAdmin.locations.list) {
      return location.pathname.startsWith(ROUTES.organisationAdmin.coreData);
    }
    return location.pathname.startsWith(href);
  };

  const getInitials = (name?: string) => {
    if (!name) return 'U';
    const parts = name.split(' ');
    if (parts.length >= 2) {
      return `${parts[0][0]}${parts[parts.length - 1][0]}`.toUpperCase();
    }
    return name[0].toUpperCase();
  };

  const handleLogout = () => {
    setDropdownOpen(false);
    signOut();
  };

  const homeUrl = userRole === EnumUserRole.ORGANISATION_ADMIN
    ? ROUTES.organisationAdmin.home
    : userRole === EnumUserRole.DELIVERY_PLAN_LEAD
      ? ROUTES.deliveryPlan.home
      : ROUTES.admin.home;

  return (
    <Box component="header" role="banner">
      {/* GOV.UK Header — blue bar with logo and user avatar */}
      <Header>
        <HeaderContainer sx={{ display: 'flex', flexWrap: 'wrap', gap: 1, justifyContent: 'space-between', alignItems: 'center' }}>
          <a href="/" className="govuk-header__link govuk-header__link--homepage" style={{ color: govukWhite, textDecoration: 'none' }}>
            <GovUKHeaderLogo />
          </a>

          {/* User Profile */}
          <ClickAwayListener onClickAway={() => setDropdownOpen(false)}>
            <UserProfileWrapper>
              <UserProfileButton
                onClick={() => setDropdownOpen(!dropdownOpen)}
                aria-label="User menu"
                aria-expanded={dropdownOpen}
              >
                <UserInitials>{getInitials(user?.full_name)}</UserInitials>
              </UserProfileButton>
              {dropdownOpen && (
                <UserDropdown>
                  <UserDropdownHeader>
                    <UserName>{user?.full_name || 'User'}</UserName>
                    <UserEmail>{user?.email || ''}</UserEmail>
                  </UserDropdownHeader>
                  <LogoutButton onClick={handleLogout}>Sign out</LogoutButton>
                </UserDropdown>
              )}
            </UserProfileWrapper>
          </ClickAwayListener>
        </HeaderContainer>
      </Header>

      {/* Service Navigation — GDS-compliant light-blue bar */}
      <section
        aria-label="Service information"
        className="govuk-service-navigation"
        data-module="govuk-service-navigation"
      >
        <div className="govuk-width-container">
          <div className="govuk-service-navigation__container">

            {/* Service name */}
            <span className="govuk-service-navigation__service-name">
              <RouterLink to={homeUrl} className="govuk-service-navigation__link">
                {serviceName}
              </RouterLink>
            </span>

            {/* Navigation */}
            {effectiveNavigation.length > 0 && (
              <nav aria-label="Menu" className="govuk-service-navigation__wrapper">
                {/*
                  Mobile toggle button.
                  – Before JS loads (mounted=false): hidden attribute present → no-JS
                    CSS fallback keeps the list visible.
                  – After mount: hidden removed, aria-expanded drives CSS show/hide.
                */}
                <button
                  type="button"
                  className="govuk-service-navigation__toggle govuk-js-service-navigation-toggle"
                  aria-controls="navigation"
                  aria-expanded={mobileNavOpen ? 'true' : 'false'}
                  onClick={() => setMobileNavOpen(open => !open)}
                  hidden={!mounted}
                >
                  Menu
                </button>

                <ul className="govuk-service-navigation__list" id="navigation">
                  {effectiveNavigation.map((item) => {
                    const active = isActive(item.href);
                    return (
                      <li
                        key={item.label}
                        className={`govuk-service-navigation__item${active ? ' govuk-service-navigation__item--active' : ''}`}
                      >
                        <RouterLink
                          to={item.href}
                          className="govuk-service-navigation__link"
                          aria-current={active ? 'page' : undefined}
                        >
                          {active
                            ? <strong className="govuk-service-navigation__active-fallback">{item.label}</strong>
                            : item.label
                          }
                        </RouterLink>
                      </li>
                    );
                  })}
                </ul>
              </nav>
            )}

          </div>
        </div>
      </section>
    </Box>
  );
};

export default LayoutHeader;
