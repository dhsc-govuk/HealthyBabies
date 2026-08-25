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
  /** Set false when a child owns document.title (multi-step flows appending "Step X of Y"). */
  manageTitle?: boolean;
}

const GeneralLayout = ({ children, breadcrumbs, currentPage, endContent, navigation, backLink, manageTitle = true }: Props): React.ReactElement => {
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
      <Content
        currentPage={currentPage}
        breadcrumbs={breadcrumbs ?? []}
        endContent={endContent}
        backLink={backLink}
        manageTitle={manageTitle}
        phaseBanner={<GovUKPhaseBanner phase="beta" />}>
        {children}
      </Content>
      <LayoutFooter />
    </Box>
  );
};

export default GeneralLayout;
