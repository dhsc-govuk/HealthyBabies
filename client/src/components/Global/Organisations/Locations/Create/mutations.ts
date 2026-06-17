import axios from 'axios';

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
  const url = apiBasePath ? `${apiBasePath}/create` : `/organisations/${organisationId}/locations/create`;
  return axios.post<CreateLocationDto>(url, location);
};
