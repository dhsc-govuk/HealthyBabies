import axios from 'axios';

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
  const url = apiBasePath ? `${apiBasePath}/edit` : `/organisations/${organisationId}/locations/edit`;
  return axios.put<UpdateLocationDto>(url, location);
};
