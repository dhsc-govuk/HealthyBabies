import { styled } from '@mui/material/styles';
import { Box, Typography } from '@mui/material';

// GOV.UK Colors
const govukBlue = '#1d70b8';
const govukBlack = '#0b0c0c';
const govukWhite = '#ffffff';
const govukLightGrey = '#f3f2f1';
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

export const ContentWrapper = styled(Box)(() => ({
  maxWidth: '610px',
}));

export const InputWrapper = styled(Box)(() => ({
  maxWidth: '200px',
  marginBottom: '20px',
}));

export const QrCodeWrapper = styled(Box)(() => ({
  display: 'flex',
  justifyContent: 'center',
  marginBottom: '30px',
  padding: '20px',
  backgroundColor: govukWhite,
  border: '1px solid #b1b4b6',
}));

export const ManualKeyBox = styled(Box)(() => ({
  padding: '15px',
  backgroundColor: govukLightGrey,
  border: '1px solid #b1b4b6',
  fontFamily: '"GDS Transport", arial, sans-serif',
  fontSize: '16px',
  wordBreak: 'break-all',
  textAlign: 'center',
  marginBottom: '20px',
}));

export const RecoveryCodesWrapper = styled(Box)(() => ({
  padding: '20px',
  backgroundColor: govukLightGrey,
  border: '1px solid #b1b4b6',
  marginBottom: '30px',
}));

export const RecoveryCodesList = styled(Box)(() => ({
  display: 'grid',
  gridTemplateColumns: 'repeat(2, 1fr)',
  gap: '10px',
}));

export const RecoveryCodeItem = styled(Box)(() => ({
  fontFamily: '"GDS Transport", arial, sans-serif',
  fontSize: '16px',
  padding: '8px 12px',
  backgroundColor: govukWhite,
  border: '1px solid #b1b4b6',
  textAlign: 'center',
}));

export const ButtonGroup = styled(Box)(() => ({
  display: 'flex',
  gap: '15px',
  marginBottom: '30px',
}));
