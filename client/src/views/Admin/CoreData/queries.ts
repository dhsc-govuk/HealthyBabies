import axios from 'axios';

export const downloadSitesCsv = () =>
  axios.get('/admin/core-data/sites/download', { responseType: 'blob' });

export const downloadServicesCsv = () =>
  axios.get('/admin/core-data/services/download', { responseType: 'blob' });

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
