import { Box, styled } from '@mui/material';

// GOV.UK Colors
const govukBlack = '#0b0c0c';
const govukLightBlue = '#f4f8fb';
const govukBlue = '#1d70b8';

export const FooterRoot = styled(Box)(() => ({
  backgroundColor: govukLightBlue,
  borderTop: `10px solid ${govukBlue}`,
  marginTop: 'auto',
}));

export const FooterContainer = styled(Box)(() => ({
  maxWidth: '960px',
  margin: '0 auto',
  padding: '25px 15px 15px',
}));

export const FooterSection = styled(Box)(() => ({
  marginBottom: '30px',
}));

export const FooterHeading = styled('h2')(() => ({
  fontFamily: '"GDS Transport", arial, sans-serif',
  fontSize: '19px',
  fontWeight: 700,
  color: govukBlack,
  marginBottom: '20px',
  marginTop: 0,
  paddingBottom: '10px',
  borderBottom: '1px solid #b1b4b6',
}));

export const FooterList = styled('ul')(() => ({
  listStyle: 'none',
  padding: 0,
  margin: 0,
  columnCount: 2,
  columnGap: '30px',
}));

export const FooterListItem = styled('li')(() => ({
  marginBottom: '10px',
  breakInside: 'avoid',
}));

export const FooterLink = styled('a')(() => ({
  fontFamily: '"GDS Transport", arial, sans-serif',
  fontSize: '16px',
  color: govukBlack,
  textDecoration: 'underline',
  textDecorationColor: govukBlack,
  '&:hover': {
    textDecorationThickness: '3px',
    textUnderlineOffset: '0.1em',
  },
}));

export const FooterDivider = styled(Box)(() => ({
  borderTop: '1px solid #b1b4b6',
  marginTop: '30px',
  marginBottom: '25px',
}));

export const FooterMeta = styled(Box)(() => ({
  paddingTop: '15px',
  display: 'flex',
  justifyContent: 'space-between',
  alignItems: 'flex-end',
  flexWrap: 'wrap',
  gap: '15px',
}));

export const FooterMetaLinks = styled(Box)(() => ({
  display: 'flex',
  flexWrap: 'wrap',
  gap: '15px',
}));

export const FooterMetaLink = styled('a')(() => ({
  fontFamily: '"GDS Transport", arial, sans-serif',
  fontSize: '14px',
  color: govukBlack,
  textDecoration: 'underline',
  '&:hover': {
    textDecorationThickness: '3px',
    textUnderlineOffset: '0.1em',
  },
}));

export const FooterLicence = styled(Box)(() => ({
  display: 'flex',
  alignItems: 'flex-start',
  gap: '10px',
  marginTop: '15px',
}));

export const FooterLicenceText = styled('span')(() => ({
  fontFamily: '"GDS Transport", arial, sans-serif',
  fontSize: '14px',
  color: govukBlack,
  '& a': {
    color: govukBlack,
    textDecoration: 'underline',
  },
}));

export const OglLogoWrapper = styled(Box)(() => ({
  display: 'flex',
  alignItems: 'center',
  gap: '10px',
}));

export const CopyrightLogo = styled(Box)(() => ({
  display: 'flex',
  flexDirection: 'column',
  alignItems: 'center',
  gap: '5px',
}));

export const CopyrightText = styled('a')(() => ({
  fontFamily: '"GDS Transport", arial, sans-serif',
  fontSize: '14px',
  color: govukBlack,
  textDecoration: 'none',
  '&:hover': {
    textDecoration: 'underline',
  },
}));
