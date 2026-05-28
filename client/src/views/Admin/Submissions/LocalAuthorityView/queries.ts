import axios from 'axios';

export interface LocalAuthoritySubmissionDetail {
  id: string;
  dataCollectionId: string;
  dataCollectionName: string;
  localAuthorityId: string;
  localAuthorityName: string;
  status: string;
  submittedAt: string | null;
  submittedBy: string | null;
  submittedByEmail: string | null;
  contact: {
    organisationName: string;
    addressLine1: string;
    addressLine2: string | null;
    city: string;
    postcode: string;
    country: string;
    phone: string | null;
    email: string | null;
  } | null;
  files: {
    name: string;
    description: string;
    csvUrl: string | null;
    csvSize: string | null;
    jsonUrl: string | null;
    jsonSize: string | null;
  }[];
}

export const getLocalAuthoritySubmission = (
  dataCollectionId: string,
  localAuthorityId: string
) =>
  axios.get<LocalAuthoritySubmissionDetail>(
    `/admin/data-collections/${dataCollectionId}/local-authorities/${localAuthorityId}`
  );

export const getStatusLabel = (status: string): string => {
  switch (status) {
    case 'NotStarted':
      return 'Not started';
    case 'InProgress':
      return 'In progress';
    case 'Submitted':
      return 'Submitted';
    case 'Approved':
      return 'Complete';
    case 'Rejected':
      return 'Rejected';
    case 'RequiresChanges':
      return 'Requires changes';
    default:
      return status;
  }
};

export const formatDateTime = (dateString: string | null): string => {
  if (!dateString) return '';
  const date = new Date(dateString);
  const day = date.getDate();
  const month = date.toLocaleDateString('en-GB', { month: 'long' });
  const year = date.getFullYear();
  const hours = date.getHours().toString().padStart(2, '0');
  const minutes = date.getMinutes().toString().padStart(2, '0');
  return `${day} ${month} ${year}, ${hours}:${minutes}`;
};

export type DownloadFormat = 'csv' | 'json';

export const downloadSubmission = (
  dataCollectionId: string,
  localAuthorityId: string,
  format: DownloadFormat
) =>
  axios.get(
    `/admin/data-collections/${dataCollectionId}/local-authorities/${localAuthorityId}/download?format=${format}`,
    { responseType: 'blob' }
  );

export const getDownloadFileName = (
  dataCollectionId: string,
  localAuthorityId: string,
  format: DownloadFormat
): string => {
  return format === 'csv'
    ? `submission_${dataCollectionId}_${localAuthorityId}.zip`
    : `submission_${dataCollectionId}_${localAuthorityId}.json`;
};

export const getDownloadMimeType = (format: DownloadFormat): string => {
  return format === 'csv' ? 'application/zip' : 'application/json';
};
