import { Box, Typography, styled } from '@mui/material';

// GOV.UK Colors
export const govukBlack = '#0b0c0c';
export const govukBlue = '#1d70b8';
export const govukWhite = '#ffffff';
export const govukLightGrey = '#f3f2f1';
export const govukLightBlue = '#f4f8fb';
export const govukMidGrey = '#b1b4b6';
export const govukBorderBlue = '#8eb8dc';

// Header (Blue bar with GOV.UK logo)
export const Header = styled(Box)(() => ({
  backgroundColor: govukBlue,
  padding: '10px 0',
}));

export const HeaderContainer = styled(Box)(() => ({
  maxWidth: '960px',
  margin: '0 auto',
  padding: '0 15px',
}));

export const LogoWrapper = styled(Box)(() => ({
  display: 'flex',
  alignItems: 'center',
  gap: '6px',
}));

export const LogoText = styled(Typography)(({ theme }) => ({
  fontFamily: '"GDS Transport", arial, sans-serif',
  fontWeight: 700,
  fontSize: '30px',
  color: govukWhite,
  lineHeight: 1,
  [theme.breakpoints.down('sm')]: {
    fontSize: '24px',
  },
}));

// Service Banner (Light blue bar with service name)
export const ServiceBanner = styled(Box)(() => ({
  backgroundColor: govukLightBlue,
  padding: '15px 0',
  borderBottom: `2px solid ${govukBorderBlue}`,
}));

export const ServiceBannerContainer = styled(Box)(({ theme }) => ({
  maxWidth: '960px',
  margin: '0 auto',
  padding: '0 15px',
  display: 'flex',
  alignItems: 'center',
  justifyContent: 'space-between',
  gap: '20px',
  [theme.breakpoints.down('md')]: {
    flexDirection: 'column',
    alignItems: 'flex-start',
    gap: '10px',
  },
}));

export const ServiceName = styled(Typography)(({ theme }) => ({
  fontFamily: '"GDS Transport", arial, sans-serif',
  fontWeight: 700,
  fontSize: '19px',
  color: govukBlack,
  [theme.breakpoints.down('sm')]: {
    fontSize: '16px',
  },
}));


// User Profile
export const UserProfileWrapper = styled(Box)(() => ({
  position: 'relative',
  marginLeft: 'auto',
}));

export const UserProfileButton = styled('button')(() => ({
  display: 'flex',
  alignItems: 'center',
  justifyContent: 'center',
  width: '40px',
  height: '40px',
  borderRadius: '50%',
  backgroundColor: govukWhite,
  border: 'none',
  cursor: 'pointer',
  '&:hover': {
    backgroundColor: '#e8e8e8',
  },
  '&:focus': {
    outline: `3px solid #ffdd00`,
    outlineOffset: 0,
  },
}));

export const UserInitials = styled('span')(() => ({
  fontFamily: '"GDS Transport", arial, sans-serif',
  fontSize: '16px',
  fontWeight: 700,
  color: govukBlue,
}));

export const UserDropdown = styled(Box)(({ theme }) => ({
  position: 'absolute',
  top: '50px',
  right: 0,
  backgroundColor: govukWhite,
  border: `1px solid ${govukMidGrey}`,
  boxShadow: '0 2px 6px rgba(0,0,0,0.15)',
  minWidth: '200px',
  zIndex: 1000,
  [theme.breakpoints.down('sm')]: {
    minWidth: '180px',
  },
}));

export const UserDropdownHeader = styled(Box)(() => ({
  padding: '15px',
  borderBottom: `1px solid ${govukMidGrey}`,
}));

export const UserName = styled(Typography)(() => ({
  fontFamily: '"GDS Transport", arial, sans-serif',
  fontSize: '16px',
  fontWeight: 700,
  color: govukBlack,
}));

export const UserEmail = styled(Typography)(() => ({
  fontFamily: '"GDS Transport", arial, sans-serif',
  fontSize: '14px',
  color: govukBlack,
  marginTop: '4px',
}));

export const LogoutButton = styled('button')(() => ({
  fontFamily: '"GDS Transport", arial, sans-serif',
  fontSize: '16px',
  color: govukBlue,
  backgroundColor: 'transparent',
  border: 'none',
  padding: '15px',
  width: '100%',
  textAlign: 'left',
  cursor: 'pointer',
  '&:hover': {
    backgroundColor: govukLightGrey,
  },
}));

