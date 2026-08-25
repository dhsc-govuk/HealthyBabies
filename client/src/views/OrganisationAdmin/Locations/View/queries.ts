import axios from 'axios';
import { API_ROUTES } from '../../../../constants/apiRoutes';

interface LocationHomeDto {
  admins: number;
}

export const getLocationHome = (organisationId: string, locationId: string) => axios.get<LocationHomeDto>(API_ROUTES.organisations.locations.totals(organisationId, locationId));
