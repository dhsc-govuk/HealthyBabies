import axios from 'axios';
import { API_ROUTES } from '../../../../constants/apiRoutes';

export interface UpdateAdminDto {
  id: string | undefined;
  firstName: string;
  lastName: string;
  email: string;
  organisationId: string;
  role: string;
  isActive: boolean;
}

export const updateAdmin = (user: UpdateAdminDto) => axios.put<UpdateAdminDto>(API_ROUTES.admin.users.organisationUsers.edit, user);
