import React from 'react';
import { Box } from '@mui/material';
import LayoutHeader from './LayoutHeader';
import LayoutFooter from './LayoutFooter';
import Content from './Content';
import GovUKPhaseBanner from '../../components/GovUKComponents/GovUKPhaseBanner';
import { GovUKSkipLink } from '../../components/GovUKComponents';

// GOV.UK Colors
const govukWhite = '#ffffff';

interface Props extends React.PropsWithChildren {
  breadcrumbs?: { label: string; link: string }[];
  currentPage?: string;
  endContent?: React.ReactElement;
  navigation?: { label: string; href: string }[];
  backLink?: { href: string; onClick?: () => void };
}

const GeneralLayout = ({ children, breadcrumbs, currentPage, endContent, navigation, backLink }: Props): React.ReactElement => {
  return (
    <Box
      sx={{
        minHeight: '100vh',
        display: 'flex',
        flexDirection: 'column',
        backgroundColor: govukWhite,
      }}
    >
      <GovUKSkipLink />
      <LayoutHeader navigation={navigation} />
      <Content currentPage={currentPage} breadcrumbs={breadcrumbs ?? []} endContent={endContent} backLink={backLink} phaseBanner={<GovUKPhaseBanner phase="beta" />}>
        {children}
      </Content>
      <LayoutFooter />
    </Box>
  );
};

export default GeneralLayout;
