import axios from 'axios';
import { API_ROUTES } from '../../../../constants/apiRoutes';

export interface GetAdminsResponse {
  id: string;
  firstName: string;
  lastName: string;
  email: string;
  isActive: boolean;
  mfaStatus: string | null;
}

export const getAdmins = () => axios.get<GetAdminsResponse[]>(API_ROUTES.admin.users.list);
