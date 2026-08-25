import axios from 'axios';
import { API_ROUTES } from '../../../../constants/apiRoutes';

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
  type: 'delivery-plan' | 'management-information';
  createdAt: string;
  updatedAt: string | null;
  createdByName: string | null;
  lastModifiedByName: string | null;
  localAuthorities?: LocalAuthorityAssignment[];
  formModules?: FormModuleAssignment[];
}

export const getDataCollections = () =>
  axios.get<DataCollectionResponse[]>(API_ROUTES.admin.dataCollections.list);

export const getDataCollection = (id: string) =>
  axios.get<DataCollectionResponse>(API_ROUTES.admin.dataCollections.byId(id));

export const getDataCollectionWithLocalAuthorities = (id: string) =>
  axios.get<DataCollectionResponse>(API_ROUTES.admin.dataCollections.localAuthorities(id));

export const updateDataCollectionLocalAuthorities = (id: string, localAuthorityIds: string[]) =>
  axios.put<DataCollectionResponse>(API_ROUTES.admin.dataCollections.localAuthorities(id), {
    localAuthorityIds,
  });

export const removeLocalAuthorityFromDataCollection = (dataCollectionId: string, localAuthorityId: string) =>
  axios.delete<DataCollectionResponse>(API_ROUTES.admin.dataCollections.localAuthority(dataCollectionId, localAuthorityId));

export const updateLocalAuthorityEndDate = (dataCollectionId: string, localAuthorityId: string, endDate: string | null) =>
  axios.patch<DataCollectionResponse>(API_ROUTES.admin.dataCollections.localAuthorityEndDate(dataCollectionId, localAuthorityId), {
    endDate,
  });

export const deleteDataCollection = (id: string) =>
  axios.delete<DataCollectionResponse>(API_ROUTES.admin.dataCollections.byId(id));

export const getDataCollectionFull = (id: string) =>
  axios.get<DataCollectionResponse>(API_ROUTES.admin.dataCollections.full(id));

export const closeDataCollection = (id: string) =>
  axios.post<DataCollectionResponse>(API_ROUTES.admin.dataCollections.close(id));

export const revertDataCollectionToDraft = (id: string) =>
  axios.post<DataCollectionResponse>(API_ROUTES.admin.dataCollections.revertToDraft(id));

export const duplicateDataCollection = (id: string, newName?: string) =>
  axios.post<DataCollectionResponse>(API_ROUTES.admin.dataCollections.duplicate(id), {
    newName,
  });

export interface FormDefinitionResponse {
  id: string;
  name: string;
  description: string | null;
  lastChangedAt: string | null;
}

export const getAvailableForms = () =>
  axios.get<FormDefinitionResponse[]>(API_ROUTES.admin.forms);

export interface DataCollectionFormModuleResponse {
  id: string;
  code: string;
  sectionNumber: number;
  name: string;
  description: string | null;
  lastChangedOn: string;
  isActive: boolean;
  type: 'delivery-plan' | 'management-information';
}

export const getDataCollectionFormModules = () =>
  axios.get<DataCollectionFormModuleResponse[]>(API_ROUTES.admin.dataCollections.formModules);
