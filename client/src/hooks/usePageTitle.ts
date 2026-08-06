import { useEffect } from 'react';

const SERVICE_NAME = 'Report data on Best Start Family Hubs and Healthy Babies';

const usePageTitle = (pageTitle?: string, hasErrors?: boolean): void => {
  useEffect(() => {
    if (!pageTitle) return;
    const prefix = hasErrors ? 'Error: ' : '';
    document.title = `${prefix}${pageTitle} - ${SERVICE_NAME} - GOV.UK`;
  }, [pageTitle, hasErrors]);
};

export default usePageTitle;
