import { useEffect } from 'react';

export const SERVICE_NAME = 'Report data on Best Start Family Hubs and Healthy Babies';

const usePageTitle = (pageTitle?: string, hasErrors?: boolean, stepLabel?: string): void => {
  useEffect(() => {
    if (!pageTitle) return;
    const prefix = hasErrors ? 'Error: ' : '';
    const step = stepLabel ? ` - ${stepLabel}` : '';
    document.title = `${prefix}${pageTitle}${step} - ${SERVICE_NAME} - GOV.UK`;
  }, [pageTitle, hasErrors, stepLabel]);
};

export default usePageTitle;
