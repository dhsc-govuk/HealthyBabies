import axios from 'axios';
import { API_ROUTES } from '../../../../../constants/apiRoutes';

export interface CreateLocationDto {
  id?: string;
  name: string;
  deliverySiteName: string;
  postCode: string;
  referenceNumber: string;
  statusOfSite: string;
  typeOfSite: string;
  nameChange: boolean;
  dateOpened: Date;
  bsfhBranding: string;
  locationType: string[];
  clarificationComments: string;
  isActive: boolean;
}

export const createLocation = (organisationId: string, location: CreateLocationDto, apiBasePath?: string) => {
  const url = apiBasePath ? `${apiBasePath}/create` : API_ROUTES.organisations.locations.create(organisationId);
  return axios.post<CreateLocationDto>(url, location);
};
