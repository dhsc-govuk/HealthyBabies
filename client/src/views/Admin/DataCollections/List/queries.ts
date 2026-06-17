import axios from 'axios';

export interface LocalAuthorityAssignment {
  id: string;
  name: string;
  assignedAt: string;
  endDate: string | null;
}

export interface FormModuleAssignment {
  id: string;
  code: string;
  sectionNumber: number;
  name: string;
  description: string | null;
}

export interface DataCollectionResponse {
  id: string;
  name: string;
  description: string | null;
  startDate: string;
  endDate: string;
  isSubmittedByAllLocalAuthorities: boolean;
  status: 'Draft' | 'Planned' | 'Open' | 'Closed';
  createdAt: string;
  updatedAt: string | null;
  createdByName: string | null;
  lastModifiedByName: string | null;
  localAuthorities?: LocalAuthorityAssignment[];
  formModules?: FormModuleAssignment[];
}

export const getDataCollections = () =>
  axios.get<DataCollectionResponse[]>('/admin/data-collections');

export const getDataCollection = (id: string) =>
  axios.get<DataCollectionResponse>(`/admin/data-collections/${id}`);

export const getDataCollectionWithLocalAuthorities = (id: string) =>
  axios.get<DataCollectionResponse>(`/admin/data-collections/${id}/local-authorities`);

export const updateDataCollectionLocalAuthorities = (id: string, localAuthorityIds: string[]) =>
  axios.put<DataCollectionResponse>(`/admin/data-collections/${id}/local-authorities`, {
    localAuthorityIds,
  });

export const removeLocalAuthorityFromDataCollection = (dataCollectionId: string, localAuthorityId: string) =>
  axios.delete<DataCollectionResponse>(`/admin/data-collections/${dataCollectionId}/local-authorities/${localAuthorityId}`);

export const updateLocalAuthorityEndDate = (dataCollectionId: string, localAuthorityId: string, endDate: string | null) =>
  axios.patch<DataCollectionResponse>(`/admin/data-collections/${dataCollectionId}/local-authorities/${localAuthorityId}/end-date`, {
    endDate,
  });

export const deleteDataCollection = (id: string) =>
  axios.delete<DataCollectionResponse>(`/admin/data-collections/${id}`);

export const getDataCollectionFull = (id: string) =>
  axios.get<DataCollectionResponse>(`/admin/data-collections/${id}/full`);

export const closeDataCollection = (id: string) =>
  axios.post<DataCollectionResponse>(`/admin/data-collections/${id}/close`);

export const revertDataCollectionToDraft = (id: string) =>
  axios.post<DataCollectionResponse>(`/admin/data-collections/${id}/revert-to-draft`);

export const duplicateDataCollection = (id: string, newName?: string) =>
  axios.post<DataCollectionResponse>(`/admin/data-collections/${id}/duplicate`, {
    newName,
  });

export interface FormDefinitionResponse {
  id: string;
  name: string;
  description: string | null;
  lastChangedAt: string | null;
}

export const getAvailableForms = () =>
  axios.get<FormDefinitionResponse[]>('/admin/forms');

export interface DataCollectionFormModuleResponse {
  id: string;
  code: string;
  sectionNumber: number;
  name: string;
  description: string | null;
  lastChangedOn: string;
  isActive: boolean;
}

export const getDataCollectionFormModules = () =>
  axios.get<DataCollectionFormModuleResponse[]>('/admin/data-collections/form-modules');
