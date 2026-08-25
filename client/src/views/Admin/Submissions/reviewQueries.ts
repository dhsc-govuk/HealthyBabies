import axios from 'axios';

import { API_ROUTES } from '../../../constants/apiRoutes';
import { ReviewAnswerGroup } from '../../../components/Global/ReviewAnswerGroups';
import { ReviewActivityRow } from '../../../components/Global/ReviewActivitiesTable';
import { ReviewSectionSummary } from './LocalAuthorityView/queries';

export type { ReviewActivityRow };

export interface ReviewNavigationLink {
  id: string;
  name: string;
  position: number;
}

export interface ReviewNavigation {
  position: number;
  total: number;
  previous: ReviewNavigationLink | null;
  next: ReviewNavigationLink | null;
}

export interface ReviewSectionDetail {
  formModuleId: string;
  code: string;
  sectionNumber: number;
  name: string;
  description: string | null;
  dataCollectionName: string;
  localAuthorityName: string;
  allowsMultipleActivities: boolean;
  answerGroups: ReviewAnswerGroup[];
  activities: ReviewActivityRow[];
  navigation: ReviewNavigation;
}

export interface ReviewActivityDetail {
  activityId: string;
  activityName: string;
  formModuleId: string;
  moduleName: string;
  sectionNumber: number;
  dataCollectionName: string;
  localAuthorityName: string;
  answerGroups: ReviewAnswerGroup[];
  navigation: ReviewNavigation;
}

export const getReviewSection = (dataCollectionId: string, localAuthorityId: string, formModuleId: string) =>
  axios.get<ReviewSectionDetail>(API_ROUTES.admin.dataCollections.reviewSection(dataCollectionId, localAuthorityId, formModuleId));

export const getReviewActivity = (dataCollectionId: string, localAuthorityId: string, formModuleId: string, activityId: string) =>
  axios.get<ReviewActivityDetail>(API_ROUTES.admin.dataCollections.reviewActivity(dataCollectionId, localAuthorityId, formModuleId, activityId));

/** One row on the "Previous submissions" list - a past round DHSC rejected, frozen at rejection. */
export interface SubmissionVersionSummary {
  versionNumber: number;
  submittedBy: string | null;
  submittedAt: string | null;
}

export interface SubmissionVersionDetail {
  versionNumber: number;
  submittedBy: string | null;
  submittedByEmail: string | null;
  submittedAt: string | null;
  reviewNotes: string | null;
  sectionsToReview: ReviewSectionSummary[];
  sections: ReviewSectionSummary[];
}

export interface SubmissionVersionSectionDetail {
  formModuleId: string;
  code: string;
  sectionNumber: number;
  name: string;
  allowsMultipleActivities: boolean;
  answerGroups: ReviewAnswerGroup[];
  activities: ReviewActivityRow[];
  navigation: ReviewNavigation;
}

export interface SubmissionVersionActivityDetail {
  activityId: string;
  activityName: string;
  formModuleId: string;
  moduleName: string;
  sectionNumber: number;
  answerGroups: ReviewAnswerGroup[];
  navigation: ReviewNavigation;
}

export const getSubmissionVersions = (dataCollectionId: string, localAuthorityId: string) =>
  axios.get<SubmissionVersionSummary[]>(API_ROUTES.admin.dataCollections.versions.list(dataCollectionId, localAuthorityId));

export const getSubmissionVersionDetail = (dataCollectionId: string, localAuthorityId: string, versionNumber: number) =>
  axios.get<SubmissionVersionDetail>(API_ROUTES.admin.dataCollections.versions.byNumber(dataCollectionId, localAuthorityId, versionNumber));

export const getSubmissionVersionSection = (dataCollectionId: string, localAuthorityId: string, versionNumber: number, formModuleId: string) =>
  axios.get<SubmissionVersionSectionDetail>(API_ROUTES.admin.dataCollections.versions.section(dataCollectionId, localAuthorityId, versionNumber, formModuleId));

export const getSubmissionVersionActivity = (dataCollectionId: string, localAuthorityId: string, versionNumber: number, formModuleId: string, activityId: string) =>
  axios.get<SubmissionVersionActivityDetail>(API_ROUTES.admin.dataCollections.versions.activity(dataCollectionId, localAuthorityId, versionNumber, formModuleId, activityId));

export const downloadSubmissionVersion = async (dataCollectionId: string, localAuthorityId: string, versionNumber: number): Promise<Blob> => {
  const response = await axios.get(API_ROUTES.admin.dataCollections.versions.download(dataCollectionId, localAuthorityId, versionNumber), {
    responseType: 'blob',
  });
  return response.data;
};

export const getSubmissionVersionDownloadFileName = (versionNumber: number): string => `submission-version-${versionNumber}.zip`;
