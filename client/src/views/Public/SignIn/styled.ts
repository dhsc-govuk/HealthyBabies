import { styled } from '@mui/material/styles';
import { Box, Typography } from '@mui/material';

// GOV.UK Colors
export const govukBlack = '#0b0c0c';
export const govukBlue = '#1d70b8';
export const govukGreen = '#00703c';
export const govukWhite = '#ffffff';
export const govukLightGrey = '#f3f2f1';
export const govukYellow = '#ffdd00';
export const govukLightBlue = '#f4f8fb';

export const PageWrapper = styled(Box)(() => ({
  minHeight: '100vh',
  display: 'flex',
  flexDirection: 'column',
}));

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

export const ServiceBanner = styled(Box)(() => ({
  backgroundColor: govukLightBlue,
  padding: '15px 0',
  borderBottom: '2px solid #8eb8dc',
}));

export const ServiceBannerContainer = styled(Box)(() => ({
  maxWidth: '960px',
  margin: '0 auto',
  padding: '0 15px',
}));

export const ServiceName = styled(Typography)(({ theme }) => ({
  fontFamily: '"GDS Transport", arial, sans-serif',
  fontWeight: 700,
  fontSize: '24px',
  color: govukBlack,
  [theme.breakpoints.down('sm')]: {
    fontSize: '19px',
  },
}));

export const MainContent = styled(Box)(() => ({
  flexGrow: 1,
  backgroundColor: govukWhite,
}));

export const ContentContainer = styled(Box)(({ theme }) => ({
  maxWidth: '960px',
  margin: '0 auto',
  padding: '40px 15px',
  [theme.breakpoints.down('sm')]: {
    padding: '20px 15px',
  },
}));

export const SignInPanel = styled(Box)(({ theme }) => ({
  maxWidth: '500px',
  width: '100%',
  border: `1px solid ${govukBlack}`,
  borderTop: `5px solid ${govukBlue}`,
  padding: '30px',
  backgroundColor: govukWhite,
  boxSizing: 'border-box',
  [theme.breakpoints.down('sm')]: {
    padding: '20px',
  },
}));

export const SignInTitle = styled(Typography)(({ theme }) => ({
  fontFamily: '"GDS Transport", arial, sans-serif',
  fontSize: '36px',
  fontWeight: 700,
  color: govukBlack,
  marginBottom: '20px',
  [theme.breakpoints.down('sm')]: {
    fontSize: '27px',
  },
}));

export const SignInDescription = styled(Typography)(() => ({
  fontFamily: '"GDS Transport", arial, sans-serif',
  fontSize: '19px',
  color: govukBlack,
  marginBottom: '30px',
  lineHeight: 1.5,
}));

export const SignInButton = styled('button')(() => ({
  fontFamily: '"GDS Transport", arial, sans-serif',
  fontSize: '19px',
  fontWeight: 700,
  padding: '10px 20px',
  backgroundColor: govukGreen,
  color: govukWhite,
  border: 'none',
  borderRadius: 0,
  cursor: 'pointer',
  boxShadow: '0 2px 0 #002d18',
  '&:hover': {
    backgroundColor: '#005a30',
  },
  '&:focus': {
    outline: `3px solid ${govukYellow}`,
    outlineOffset: 0,
    backgroundColor: govukYellow,
    color: govukBlack,
    boxShadow: `0 2px 0 ${govukBlack}`,
  },
}));

export const ProgressWrapper = styled(Box)(() => ({
  display: 'flex',
  alignItems: 'center',
  gap: '15px',
}));

export const ProgressText = styled(Typography)(() => ({
  fontFamily: '"GDS Transport", arial, sans-serif',
  fontSize: '19px',
  color: govukBlack,
}));

export const HelpSection = styled(Box)(() => ({
  marginTop: '30px',
  maxWidth: '500px',
}));

export const HelpTitle = styled(Typography)(() => ({
  fontFamily: '"GDS Transport", arial, sans-serif',
  fontSize: '19px',
  fontWeight: 700,
  color: govukBlack,
  marginBottom: '10px',
}));

export const HelpText = styled(Typography)(() => ({
  fontFamily: '"GDS Transport", arial, sans-serif',
  fontSize: '16px',
  color: govukBlack,
  lineHeight: 1.5,
  '& a': {
    color: govukBlue,
    textDecoration: 'underline',
    '&:hover': {
      textDecorationThickness: '3px',
    },
  },
}));

export const Footer = styled(Box)(() => ({
  backgroundColor: govukLightGrey,
  borderTop: '1px solid #b1b4b6',
  padding: '25px 0',
}));

export const FooterContainer = styled(Box)(() => ({
  maxWidth: '960px',
  margin: '0 auto',
  padding: '0 15px',
}));

export const FooterLinks = styled(Box)(({ theme }) => ({
  display: 'flex',
  gap: '20px',
  flexWrap: 'wrap',
  marginBottom: '15px',
  [theme.breakpoints.down('sm')]: {
    gap: '10px 20px',
  },
}));

export const FooterLink = styled('a')(() => ({
  fontFamily: '"GDS Transport", arial, sans-serif',
  fontSize: '14px',
  color: govukBlack,
  textDecoration: 'none',
  '&:hover': {
    textDecoration: 'underline',
  },
}));

export const LicenceWrapper = styled(Box)(({ theme }) => ({
  display: 'flex',
  alignItems: 'flex-start',
  gap: '10px',
  [theme.breakpoints.down('sm')]: {
    flexDirection: 'column',
  },
}));

export const LicenceText = styled(Typography)(() => ({
  fontFamily: '"GDS Transport", arial, sans-serif',
  fontSize: '14px',
  color: govukBlack,
  '& a': {
    color: govukBlack,
  },
}));

export const Breadcrumbs = styled(Box)(() => ({
  maxWidth: '960px',
  margin: '0 auto',
  marginLeft: '25%',
  padding: '15px 15px 0',
  fontFamily: '"GDS Transport", arial, sans-serif',
  fontSize: '14px',
  color: govukBlack,
  display: 'flex',
  alignItems: 'center',
  gap: '8px',
  '& a': {
    color: govukBlack,
    textDecoration: 'underline',
    '&:hover': {
      textDecorationThickness: '3px',
    },
  },
}));

export const BreadcrumbChevron = styled('span')(() => ({
  display: 'inline-block',
  width: '7px',
  height: '7px',
  borderTop: `1px solid ${govukBlack}`,
  borderRight: `1px solid ${govukBlack}`,
  transform: 'rotate(45deg)',
}));

export const PageTitle = styled(Typography)(({ theme }) => ({
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

export const Paragraph = styled(Typography)(() => ({
  fontFamily: '"GDS Transport", arial, sans-serif',
  fontSize: '19px',
  color: govukBlack,
  marginBottom: '20px',
  lineHeight: 1.5,
  maxWidth: '640px',
}));

export const BulletList = styled('ul')(() => ({
  fontFamily: '"GDS Transport", arial, sans-serif',
  fontSize: '19px',
  color: govukBlack,
  lineHeight: 1.5,
  paddingLeft: '20px',
  marginBottom: '20px',
  maxWidth: '640px',
  '& li': {
    marginBottom: '5px',
  },
}));

export const SectionHeading = styled(Typography)(() => ({
  fontFamily: '"GDS Transport", arial, sans-serif',
  fontSize: '24px',
  fontWeight: 700,
  color: govukBlack,
  marginTop: '30px',
  marginBottom: '10px',
}));

export const Link = styled('a')(() => ({
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

export const StartButton = styled('button')(() => ({
  fontFamily: '"GDS Transport", arial, sans-serif',
  fontSize: '19px',
  fontWeight: 700,
  padding: '8px 20px 8px 15px',
  backgroundColor: govukGreen,
  color: govukWhite,
  border: 'none',
  borderRadius: 0,
  cursor: 'pointer',
  boxShadow: '0 2px 0 #002d18',
  display: 'inline-flex',
  alignItems: 'center',
  gap: '10px',
  marginTop: '10px',
  marginBottom: '30px',
  '&:hover': {
    backgroundColor: '#005a30',
  },
  '&:focus': {
    outline: `3px solid ${govukYellow}`,
    outlineOffset: 0,
    backgroundColor: govukYellow,
    color: govukBlack,
    boxShadow: `0 2px 0 ${govukBlack}`,
  },
  '& svg': {
    width: '17px',
    height: '17px',
  },
}));
