import axios from 'axios';

export const deleteAdmin = (userId: string) => axios.delete(`/admin/users/${userId}`);
