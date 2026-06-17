import axios from 'axios';

export interface UpdateAdminDto {
  id: string | undefined;
  firstName: string;
  lastName: string;
  email: string;
  organisationId: string;
  role: string;
  isActive: boolean;
}

export const updateAdmin = (user: UpdateAdminDto) => axios.put<UpdateAdminDto>('admin/users/edit', user);
