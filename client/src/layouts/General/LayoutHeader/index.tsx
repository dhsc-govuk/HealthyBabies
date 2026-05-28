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
import './service-navigation.css';

const adminNavigation = [
  { label: 'Home', href: '/admin/home' },
  { label: 'Trends', href: '/admin/trends' },
  { label: 'Submissions', href: '/admin/submissions' },
  { label: 'Settings', href: '/admin/settings' },
];

const organisationAdminNavigation = [
  { label: 'Core data', href: '/organisation-admin/core-data/delivery-locations' },
  { label: 'Submissions', href: '/organisation-admin/submissions' },
  { label: 'Guidance', href: '/organisation-admin/help' },
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
    if (href === '/admin/settings') {
      const settingsPaths = ['/admin/settings', '/admin/organisations', '/admin/departmental-users', '/admin/la-users', '/admin/data-collections', '/admin/forms', '/admin/guidance', '/admin/configuration'];
      return settingsPaths.some(path => location.pathname.startsWith(path));
    }
    if (href === '/organisation-admin/core-data/delivery-locations') {
      return location.pathname.startsWith('/organisation-admin/core-data');
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
    ? '/organisation-admin/home'
    : '/admin/home';

  return (
    <Box component="header" role="banner">
      {/* GOV.UK Header — blue bar with logo and user avatar */}
      <Header>
        <HeaderContainer sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
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
