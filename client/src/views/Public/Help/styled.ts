import { styled } from '@mui/material/styles';
import { Box, Typography } from '@mui/material';

const govukBlack = '#0b0c0c';
const govukBlue = '#1d70b8';
const govukYellow = '#ffdd00';
const govukLightBlue = '#f4f8fb';
const govukBorderBlue = '#8eb8dc';

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

export const ServiceName = styled(Typography)(({ theme }) => ({
  fontFamily: '"GDS Transport", arial, sans-serif',
  fontWeight: 700,
  fontSize: '19px',
  color: govukBlack,
  [theme.breakpoints.down('sm')]: {
    fontSize: '16px',
  },
}));

export const BetaBanner = styled(Box)(() => ({
  borderBottom: '1px solid #b1b4b6',
}));

export const BetaBannerContainer = styled(Box)(() => ({
  maxWidth: '960px',
  margin: '0 auto',
  padding: '10px 15px',
  display: 'flex',
  alignItems: 'center',
  gap: '10px',
}));

export const BetaTag = styled('span')(() => ({
  fontFamily: '"GDS Transport", arial, sans-serif',
  fontSize: '14px',
  fontWeight: 700,
  backgroundColor: '#BBD4EA',
  padding: '2px 8px',
  letterSpacing: '1px',
}));

export const FeedbackText = styled(Typography)(() => ({
  fontFamily: '"GDS Transport", arial, sans-serif',
  fontSize: '14px',
  color: govukBlack,
}));

export const BackLink = styled('button')(() => ({
  fontFamily: '"GDS Transport", arial, sans-serif',
  fontSize: '14px',
  color: govukBlack,
  background: 'none',
  border: 'none',
  cursor: 'pointer',
  textDecoration: 'underline',
  padding: '15px 0',
  marginBottom: '15px',
  display: 'block',
  textAlign: 'left',
  '&:hover': {
    textDecorationThickness: '3px',
  },
  '&:focus': {
    outline: `3px solid ${govukYellow}`,
    outlineOffset: 0,
    backgroundColor: govukYellow,
  },
}));

export const HelpPageTitle = styled(Typography)(({ theme }) => ({
  fontFamily: '"GDS Transport", arial, sans-serif',
  fontSize: '48px',
  fontWeight: 700,
  color: govukBlack,
  marginBottom: '30px',
  lineHeight: 1.1,
  maxWidth: '640px',
  [theme.breakpoints.down('sm')]: {
    fontSize: '32px',
  },
}));

export const HelpParagraph = styled(Typography)(() => ({
  fontFamily: '"GDS Transport", arial, sans-serif',
  fontSize: '19px',
  color: govukBlack,
  marginBottom: '20px',
  lineHeight: 1.5,
  maxWidth: '640px',
}));

export const HelpSectionHeading = styled(Typography)(() => ({
  fontFamily: '"GDS Transport", arial, sans-serif',
  fontSize: '24px',
  fontWeight: 700,
  color: govukBlack,
  marginTop: '30px',
  marginBottom: '10px',
}));

export const HelpLink = styled('a')(() => ({
  fontFamily: '"GDS Transport", arial, sans-serif',
  color: govukBlue,
  textDecoration: 'underline',
  '&:hover': {
    textDecorationThickness: '3px',
  },
  '&:focus': {
    outline: `3px solid ${govukYellow}`,
    outlineOffset: 0,
    backgroundColor: govukYellow,
    color: govukBlack,
  },
}));
