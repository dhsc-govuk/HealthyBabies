import axios from 'axios';

export interface SubmissionDataCollection {
  id: string;
  name: string;
  description: string | null;
  startDate: string;
  endDate: string;
  createdAt: string;
  updatedAt: string | null;
}

export type SubmissionStatus = 'Planned' | 'Open' | 'Closed';

export interface SubmissionItem {
  id: string;
  name: string;
  dueDate: string;
  status: SubmissionStatus;
}

export const getSubmissions = () =>
  axios.get<SubmissionDataCollection[]>('/admin/data-collections');

export const getSubmissionStatus = (collection: SubmissionDataCollection): SubmissionStatus => {
  const now = new Date();
  const startDate = new Date(collection.startDate);
  const endDate = new Date(collection.endDate);

  if (now < startDate) {
    return 'Planned';
  } else if (now >= startDate && now <= endDate) {
    return 'Open';
  } else {
    return 'Closed';
  }
};

export const isOngoingOrUpcoming = (collection: SubmissionDataCollection): boolean => {
  const status = getSubmissionStatus(collection);
  return status === 'Planned' || status === 'Open';
};
