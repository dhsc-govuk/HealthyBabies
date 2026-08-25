import axios from 'axios';
import { API_ROUTES } from '../../../../constants/apiRoutes';

export interface GetAdminResponse {
  firstName: string;
  lastName: string;
  email: string;
  isActive: boolean;
  organisationId?: string;
  role?: string;
}

export const getAdmin = (userId: string) => axios.get<GetAdminResponse>(API_ROUTES.admin.users.organisationUsers.byId(userId));
