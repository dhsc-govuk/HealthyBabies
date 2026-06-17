import axios from 'axios';

export interface GetAdminResponse {
  firstName: string;
  lastName: string;
  email: string;
  isActive: boolean;
  organisationId?: string;
  role?: string;
}

export const getAdmin = (userId: string) => axios.get<GetAdminResponse>(`admin/users/organisation-users/${userId}`);
