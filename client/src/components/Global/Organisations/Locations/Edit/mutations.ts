import axios from 'axios';
import { API_ROUTES } from '../../../../../constants/apiRoutes';

export interface UpdateLocationDto {
  id: string;
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

export const updateLocation = (organisationId: string, location: UpdateLocationDto, apiBasePath?: string) => {
  const url = apiBasePath ? `${apiBasePath}/edit` : API_ROUTES.organisations.locations.edit(organisationId);
  return axios.put<UpdateLocationDto>(url, location);
};
