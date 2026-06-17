import { styled } from '@mui/material/styles';
import { Box, Typography, Button as MuiButton } from '@mui/material';

// GOV.UK Colors - exported for use with govuk-react components
// Based on GOV.UK Design System colour palette
// https://design-system.service.gov.uk/styles/colour/
export const govukColors = {
  // Main colours
  blue: '#1d70b8', // $govuk-brand-colour
  black: '#0b0c0c', // $govuk-text-colour
  white: '#ffffff', // govuk-colour("white")
  red: '#d4351c', // $govuk-error-colour
  yellow: '#ffdd00', // govuk-colour("yellow")
  green: '#00703c', // $govuk-success-colour
  greenDark: '#005a30', // Custom darker green

  // Grey scale
  grey1: '#505a5f', // $govuk-secondary-text-colour / govuk-colour("dark-grey")
  grey2: '#b1b4b6', // $govuk-border-colour / govuk-colour("mid-grey")
  grey3: '#f3f2f1', // govuk-colour("light-grey")
  grey4: '#dee0e2', // Custom mid-light grey

  // Extended palette
  orange: '#f47738', // govuk-colour("orange") - for warnings
  turquoise: '#28a197', // govuk-colour("turquoise")
};

// Note: Use govuk-react components where available:
// - Table, Tag, Link, SearchBox, WarningText, Select, etc.
// Below are ONLY custom styles not available in govuk-react

// Actions Dropdown - custom component for user actions menu
export const ActionsDropdownContainer = styled(Box)(() => ({
  position: 'relative',
  display: 'inline-block',
}));

export const ActionsDropdownButton = styled(MuiButton)(() => ({
  fontFamily: '"GDS Transport", arial, sans-serif',
  fontSize: '19px',
  fontWeight: 400,
  padding: '7px 16px 5px',
  backgroundColor: govukColors.grey3,
  color: govukColors.black,
  border: 'none',
  borderRadius: 0,
  textTransform: 'none',
  lineHeight: '24px',
  boxShadow: '0 2px 0 #929191',
  '&:hover': {
    backgroundColor: '#dbdad9',
  },
  '&:focus': {
    outline: `3px solid ${govukColors.yellow}`,
    outlineOffset: 0,
  },
}));

export const ActionsDropdownMenu = styled(Box)(() => ({
  position: 'absolute',
  top: '100%',
  right: 0,
  backgroundColor: govukColors.white,
  border: `1px solid ${govukColors.grey4}`,
  boxShadow: '0 2px 6px rgba(0,0,0,0.15)',
  zIndex: 100,
  minWidth: '200px',
}));

export const ActionsDropdownItem = styled('button')<{ variant?: 'danger' }>(({ variant }) => ({
  display: 'block',
  width: '100%',
  padding: '10px 15px',
  fontFamily: '"GDS Transport", arial, sans-serif',
  fontSize: '16px',
  textAlign: 'left',
  backgroundColor: 'transparent',
  border: 'none',
  cursor: 'pointer',
  color: variant === 'danger' ? govukColors.red : govukColors.black,
  '&:hover': {
    backgroundColor: govukColors.grey3,
  },
  '&:focus': {
    outline: `3px solid ${govukColors.yellow}`,
    outlineOffset: '-3px',
  },
}));

export const ActionsDropdownDivider = styled('hr')(() => ({
  border: 'none',
  borderTop: `1px solid ${govukColors.grey2}`,
  margin: '5px 0',
}));

// Page Header with Actions - layout helper
export const PageHeaderContainer = styled(Box)(() => ({
  display: 'flex',
  justifyContent: 'space-between',
  alignItems: 'center',
  marginBottom: '30px',
  flexWrap: 'wrap',
  gap: '20px',
  '& h1, & h2, & h3, & .govuk-heading-xl, & .govuk-heading-l, & .govuk-heading-m': {
    margin: 0,
    marginBottom: 0,
  },
}));

export const PageTitle = styled('h1')(({ theme }) => ({
  fontFamily: '"GDS Transport", arial, sans-serif',
  fontSize: '36px',
  lineHeight: 1.1111111111,
  fontWeight: 700,
  color: govukColors.black,
  margin: 0,
  [theme.breakpoints.down('md')]: {
    fontSize: '27px',
    lineHeight: 1.1111111111,
  },
}));

export const PageHeaderActions = styled(Box)(() => ({
  display: 'flex',
  gap: '10px',
  alignItems: 'center',
  '& button': {
    marginBottom: 0,
  },
}));

// Filter/Search Bar - layout helper
export const FilterBar = styled(Box)(() => ({
  display: 'flex',
  justifyContent: 'space-between',
  alignItems: 'center',
  marginBottom: '20px',
  flexWrap: 'wrap',
  gap: '15px',
}));

// Warning Panel - for confirmation pages (delete, close, revert, etc.)
// Blue panel with white text, used for destructive action confirmations
export const WarningPanel = styled(Box)(() => ({
  backgroundColor: govukColors.blue,
  padding: '30px',
  marginBottom: '30px',
}));

export const WarningPanelTitle = styled(Typography)(() => ({
  fontFamily: '"GDS Transport", arial, sans-serif',
  fontSize: '36px',
  fontWeight: 700,
  color: govukColors.white,
  margin: '0 0 20px 0',
  lineHeight: 1.2,
}));

export const WarningPanelBody = styled(Typography)(() => ({
  fontFamily: '"GDS Transport", arial, sans-serif',
  fontSize: '19px',
  color: govukColors.white,
  margin: '0 0 15px 0',
  lineHeight: 1.5,
  '&:last-of-type': {
    marginBottom: '25px',
  },
}));

export const WarningPanelActions = styled(Box)(() => ({
  display: 'flex',
  alignItems: 'center',
  gap: '20px',
  marginTop: '20px',
  '& button': {
    marginBottom: 0,
  },
  '& a': {
    color: govukColors.white,
  },
  '& a:hover': {
    color: govukColors.white,
  },
  '& a:focus': {
    color: govukColors.black,
  },
}));

// Shared styles for WarningPanelLink - used by both button and anchor variants
// !important is required on color to beat the global `a:not(...):visited` selector in
// govuk-refreshed-branding.css which has very high specificity.
const warningPanelLinkStyles = {
  background: 'none',
  border: 'none',
  color: govukColors.white + ' !important',
  textDecoration: 'underline',
  cursor: 'pointer',
  fontFamily: '"GDS Transport", arial, sans-serif',
  fontSize: '19px',
  padding: 0,
  '&:hover': {
    color: govukColors.white + ' !important',
  },
  '&:visited': {
    color: govukColors.white + ' !important',
  },
  '&:focus': {
    color: govukColors.black + ' !important',
    backgroundColor: govukColors.yellow,
    outline: '3px solid transparent',
    boxShadow: '0 -2px ' + govukColors.yellow + ', 0 4px ' + govukColors.black,
  },
} as const;

export const WarningPanelLink = styled('button')(() => warningPanelLinkStyles);

export const WarningPanelAnchor = styled('a')(() => warningPanelLinkStyles);
