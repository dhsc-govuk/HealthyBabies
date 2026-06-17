import axios from 'axios';

export interface GetLocationDto {
  id: string;
  name: string;
  isActive: boolean;
}

export const getLocations = (organisationId: string, apiBasePath?: string) => {
  const url = apiBasePath ? apiBasePath : `/organisations/${organisationId}/locations`;
  return axios.get<GetLocationDto[]>(url);
};
