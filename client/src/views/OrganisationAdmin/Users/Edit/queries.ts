import axios from 'axios';
import { OrganisationUserRole } from '../../../../components/Global/Organisations/Users/Common';
import { API_ROUTES } from '../../../../constants/apiRoutes';

interface OrganisationUserDto {
  firstName: string;
  lastName: string;
  email: string;
  isActive: boolean;
  role: OrganisationUserRole;
}

export const getOrganisationUser = (organisationId: string, userId: string) => axios.get<OrganisationUserDto>(API_ROUTES.organisations.users.byId(organisationId, userId));
