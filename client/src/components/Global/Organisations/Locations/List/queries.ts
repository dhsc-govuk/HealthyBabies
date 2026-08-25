import axios from 'axios';
import { API_ROUTES } from '../../../../../constants/apiRoutes';

export interface GetLocationDto {
  id: string;
  name: string;
  isActive: boolean;
}

export const getLocations = (organisationId: string, apiBasePath?: string) => {
  const url = apiBasePath ? apiBasePath : API_ROUTES.organisations.locations.list(organisationId);
  return axios.get<GetLocationDto[]>(url);
};

// BLUM-7251: the LA's own submitted sites data as CSV, scoped server-side to the caller's own
// organisation.
export const downloadSitesExport = async (): Promise<Blob> => {
  const response = await axios.get(API_ROUTES.organisationAdmin.coreData.sitesDownload, {
    responseType: 'blob',
  });
  return response.data;
};
