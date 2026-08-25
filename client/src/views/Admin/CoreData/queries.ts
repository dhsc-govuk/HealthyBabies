import axios from 'axios';
import { API_ROUTES } from '../../../constants/apiRoutes';

export const downloadSitesCsv = () =>
  axios.get(API_ROUTES.admin.coreData.sitesDownload, { responseType: 'blob' });

export const downloadServicesCsv = () =>
  axios.get(API_ROUTES.admin.coreData.servicesDownload, { responseType: 'blob' });

export const triggerCsvDownload = (data: BlobPart, fileName: string): void => {
  const blob = new Blob([data], { type: 'text/csv' });
  const url = window.URL.createObjectURL(blob);
  const link = document.createElement('a');
  link.href = url;
  link.download = fileName;
  document.body.appendChild(link);
  link.click();
  document.body.removeChild(link);
  window.URL.revokeObjectURL(url);
};
