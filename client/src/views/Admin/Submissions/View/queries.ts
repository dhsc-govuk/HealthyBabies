import axios from 'axios';
import { SubmissionDataCollection, SubmissionStatus, getSubmissionStatus } from '../List/queries';
import { API_ROUTES } from '../../../../constants/apiRoutes';

export interface LocalAuthoritySubmission {
  id: string;
  localAuthorityId: string;
  localAuthorityName: string;
  status: 'NotStarted' | 'InProgress' | 'Submitted' | 'Approved' | 'Rejected' | 'RequiresChanges';
  submittedAt: string | null;
  submittedBy: string | null;
}

export interface SubmissionDataCollectionWithLAs extends SubmissionDataCollection {
  localAuthoritySubmissions: LocalAuthoritySubmission[];
}

export interface ConsolidatedFile {
  id: string;
  name: string;
  description: string;
  csvSize: number;
  jsonSize: number;
  csvUrl: string;
  jsonUrl: string;
}

export type LocalAuthorityFilterStatus = 'all' | 'submitted' | 'approved' | 'in-progress' | 'not-started' | 'rejected' | 'requires-changes';

export const getSubmissionById = (id: string) => axios.get<SubmissionDataCollectionWithLAs>(API_ROUTES.admin.dataCollections.byId(id));

// Returns the collection itself, not the submission view - this endpoint has no localAuthoritySubmissions.
export const updateSubmissionDates = (id: string, dates: { startDate?: string; endDate?: string }) =>
  axios.patch<SubmissionDataCollection>(API_ROUTES.admin.dataCollections.dates(id), dates);

export const getConsolidatedFiles = (dataCollectionId: string) => axios.get<ConsolidatedFile[]>(API_ROUTES.admin.dataCollections.files(dataCollectionId));

export const formatFileSize = (bytes: number): string => {
  if (bytes < 1024) return `${bytes} B`;
  if (bytes < 1024 * 1024) return `${Math.round(bytes / 1024)} kB`;
  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
};

export const getLocalAuthorityStatusLabel = (status: LocalAuthoritySubmission['status']): string => {
  switch (status) {
    case 'NotStarted':
      return 'Not started';
    case 'InProgress':
      return 'In progress';
    case 'Submitted':
      return 'Submitted';
    case 'Approved':
      return 'Approved';
    case 'Rejected':
      return 'Rejected';
    case 'RequiresChanges':
      return 'Requires changes';
    default:
      return status;
  }
};

export const getLocalAuthorityStatusTagColour = (status: LocalAuthoritySubmission['status']): 'grey' | 'green' | 'yellow' | 'red' | undefined => {
  switch (status) {
    case 'NotStarted':
      return 'grey';
    case 'InProgress':
      return 'yellow';
    case 'Submitted':
      return 'green';
    case 'Approved':
      return 'green';
    case 'Rejected':
      return 'red';
    case 'RequiresChanges':
      return 'yellow';
    default:
      return undefined;
  }
};

export { getSubmissionStatus };
export type { SubmissionStatus };

export type DownloadFormat = 'csv' | 'json';

export const downloadConsolidatedSubmission = (dataCollectionId: string, format: DownloadFormat) =>
  axios.get(API_ROUTES.admin.dataCollections.download(dataCollectionId, format), { responseType: 'blob' });

export const getConsolidatedDownloadFileName = (dataCollectionId: string, format: DownloadFormat): string => {
  return format === 'csv' ? `data_collection_${dataCollectionId}.zip` : `data_collection_${dataCollectionId}.json`;
};

export const getConsolidatedDownloadMimeType = (format: DownloadFormat): string => {
  return format === 'csv' ? 'application/zip' : 'application/json';
};
