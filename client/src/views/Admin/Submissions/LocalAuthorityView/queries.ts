import axios from 'axios';
import { API_ROUTES } from '../../../../constants/apiRoutes';

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
    downloadUrl: string | null;
    downloadSize: string | null;
    /** Short label shown next to the file name, e.g. "Submission pending". Null when downloadable. */
    status: string | null;
  }[];
  type: DataCollectionTypeValue;
  sections: ReviewSectionSummary[];
  /** Set once the submission has been accepted or reopened. */
  reviewedAt: string | null;
  reviewedBy: string | null;
  /** The reason given when reopening. Empty for an acceptance. */
  reviewNotes: string | null;
  /** This authority's current deadline — updated when a submission is reopened. */
  dueDate: string | null;
  /** Sections the reviewer asked the local authority to focus on when reopening. Advisory only. */
  sectionsToReview: ReviewSectionSummary[];
  /** Number of previous submissions this local authority has had rejected. */
  previousVersionCount: number;
}

export type DataCollectionTypeValue = 'delivery-plan' | 'management-information';

/** One reviewable section of a submitted Delivery Plan. Empty for management information. */
export interface ReviewSectionSummary {
  formModuleId: string;
  code: string;
  sectionNumber: number;
  name: string;
  allowsMultipleActivities: boolean;
}

export const getLocalAuthoritySubmission = (dataCollectionId: string, localAuthorityId: string) =>
  axios.get<LocalAuthoritySubmissionDetail>(API_ROUTES.admin.dataCollections.localAuthority(dataCollectionId, localAuthorityId));

export const getStatusLabel = (status: string): string => {
  switch (status) {
    case 'NotStarted':
      return 'Not started';
    case 'InProgress':
      return 'In progress';
    case 'Submitted':
      return 'Submitted';
    case 'Approved':
      return 'Accepted';
    // The domain records a rejection; users only ever see the submission as reopened.
    case 'Rejected':
      return 'Not started (Reopened)';
    case 'RequiresChanges':
      return 'Requires changes';
    default:
      return status;
  }
};

/** Accept and Reopen are only offered while the plan is awaiting a decision. */
export const isAwaitingReview = (status: string): boolean => status === 'Submitted';

export const approveSubmission = (dataCollectionId: string, localAuthorityId: string, comment?: string) =>
  axios.post(API_ROUTES.admin.dataCollections.localAuthorityApprove(dataCollectionId, localAuthorityId), { comment });

export interface ReopenSubmissionRequest {
  reason: string;
  resubmitBy: string;
  sectionsToReview: string[];
}

export const reopenSubmission = (dataCollectionId: string, localAuthorityId: string, body: ReopenSubmissionRequest) =>
  axios.post(API_ROUTES.admin.dataCollections.localAuthorityReopen(dataCollectionId, localAuthorityId), body);

export const formatDateOnly = (dateString: string | null): string => {
  if (!dateString) return '';
  return new Date(dateString).toLocaleDateString('en-GB', { day: 'numeric', month: 'long', year: 'numeric' });
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

export const downloadSubmission = (dataCollectionId: string, localAuthorityId: string) =>
  axios.get(API_ROUTES.admin.dataCollections.localAuthorityDownload(dataCollectionId, localAuthorityId), { responseType: 'blob' });

export const getDownloadFileName = (dataCollectionId: string, localAuthorityId: string): string => `submission_${dataCollectionId}_${localAuthorityId}.zip`;
