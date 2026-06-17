import { styled } from '@mui/material/styles';
import { Box, Typography } from '@mui/material';

// GOV.UK Colors
const govukBlue = '#1d70b8';
const govukBlack = '#0b0c0c';
const govukWhite = '#ffffff';
const govukLightBlue = '#f4f8fb';
const govukBorderBlue = '#8eb8dc';

export const PageWrapper = styled(Box)(() => ({
  minHeight: '100vh',
  display: 'flex',
  flexDirection: 'column',
  backgroundColor: govukWhite,
}));

export const Header = styled(Box)(() => ({
  backgroundColor: govukBlue,
  padding: '10px 0',
}));

export const HeaderContainer = styled(Box)(() => ({
  maxWidth: '960px',
  margin: '0 auto',
  padding: '0 15px',
  display: 'flex',
  alignItems: 'center',
}));

export const LogoWrapper = styled(Box)(() => ({
  display: 'flex',
  alignItems: 'center',
  gap: '5px',
}));

export const LogoText = styled(Typography)(() => ({
  fontFamily: '"GDS Transport", arial, sans-serif',
  fontWeight: 700,
  fontSize: '30px',
  color: govukWhite,
  lineHeight: 1,
}));

export const ServiceBanner = styled(Box)(() => ({
  backgroundColor: govukLightBlue,
  padding: '15px 0',
  borderBottom: `2px solid ${govukBorderBlue}`,
}));

export const ServiceBannerContainer = styled(Box)(() => ({
  maxWidth: '960px',
  margin: '0 auto',
  padding: '0 15px',
}));

export const ServiceName = styled(Typography)(() => ({
  fontFamily: '"GDS Transport", arial, sans-serif',
  fontWeight: 700,
  fontSize: '19px',
  color: govukBlack,
}));

export const MainContent = styled(Box)(() => ({
  flexGrow: 1,
  backgroundColor: govukWhite,
}));

export const ContentContainer = styled(Box)(() => ({
  maxWidth: '960px',
  margin: '0 auto',
  padding: '0px 15px',
}));

export const StyledLink = styled('span')(() => ({
  fontFamily: '"GDS Transport", arial, sans-serif',
  fontSize: '19px',
  color: govukBlue,
  textDecoration: 'underline',
  cursor: 'pointer',
  '&:hover': {
    textDecorationThickness: '3px',
  },
}));

export const BodyText = styled(Typography)(() => ({
  fontFamily: '"GDS Transport", arial, sans-serif',
  fontSize: '19px',
  color: govukBlack,
  marginBottom: '20px',
  lineHeight: 1.5,
}));

export const ContentWrapper = styled(Box)(() => ({
  maxWidth: '610px',
}));

export const InputWrapper = styled(Box)(() => ({
  maxWidth: '200px',
  marginBottom: '20px',
}));