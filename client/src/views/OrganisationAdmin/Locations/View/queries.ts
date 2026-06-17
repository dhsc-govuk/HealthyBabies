import axios from 'axios';

interface LocationHomeDto {
  admins: number;
}

export const getLocationHome = (organisationId: string, locationId: string) => axios.get<LocationHomeDto>(`/organisations/${organisationId}/locations/${locationId}/totals`);
