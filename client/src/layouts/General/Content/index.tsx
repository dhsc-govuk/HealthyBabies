import React from 'react';
import { Box } from '@mui/material';
import { GovUKBreadcrumbs, GovUKBackLink, GovUKNotificationDisplay } from '../../../components/GovUKComponents';
import usePageTitle from '../../../hooks/usePageTitle';
import './styles.css';

interface Props extends React.PropsWithChildren {
  breadcrumbs?: { label: string; link: string }[];
  currentPage?: string;
  endContent?: React.ReactElement;
  phaseBanner?: React.ReactElement;
  backLink?: { href: string; onClick?: () => void };
}

const Content = ({ children, breadcrumbs, currentPage, endContent, phaseBanner, backLink }: Props): React.ReactElement => {
  usePageTitle(currentPage);
  return (
    <Box component="main" className="govuk-main-wrapper" id="main-content" role="main">
      <Box className="govuk-width-container">
        {phaseBanner}
        {breadcrumbs && breadcrumbs.length > 0 && <GovUKBreadcrumbs items={breadcrumbs.map((item) => ({ label: item.label, href: item.link }))} />}
        {backLink && (
          <GovUKBackLink
            href={backLink.href}
            onClick={
              backLink.onClick
                ? (e) => {
                    e.preventDefault();
                    backLink.onClick?.();
                  }
                : undefined
            }
          />
        )}
        {(currentPage || endContent) && (
          <div className="content-header">
            <div className="content-header__title">{currentPage && <h1 className="govuk-heading-l">{currentPage}</h1>}</div>
            {endContent && <div className="content-header__actions">{endContent}</div>}
          </div>
        )}

        <GovUKNotificationDisplay />
        <div className="content-body">{children}</div>
      </Box>
    </Box>
  );
};

export default Content;
