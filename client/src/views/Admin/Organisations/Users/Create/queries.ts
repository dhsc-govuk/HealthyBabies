import axios from 'axios';
import { API_ROUTES } from '../../../../../constants/apiRoutes';

interface OrganisationDto {
  name: string;
}

export const getOrganisation = (organisationId: string) => axios.get<OrganisationDto>(API_ROUTES.organisations.byId(organisationId));
