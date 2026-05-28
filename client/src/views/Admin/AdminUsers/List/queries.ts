import axios from 'axios';

export interface GetAdminsResponse {
  id: string;
  firstName: string;
  lastName: string;
  email: string;
  isActive: boolean;
  mfaStatus: string | null;
}

export const getAdmins = () => axios.get<GetAdminsResponse[]>('/admin/users');
