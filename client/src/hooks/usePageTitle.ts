import { useEffect } from 'react';

const SERVICE_NAME = 'Report data on Best Start Family Hubs and Healthy Babies';

const usePageTitle = (pageTitle?: string): void => {
  useEffect(() => {
    if (!pageTitle) return;
    document.title = `${pageTitle} - ${SERVICE_NAME} - GOV.UK`;
  }, [pageTitle]);
};

export default usePageTitle;
