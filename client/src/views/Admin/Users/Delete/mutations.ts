import axios from 'axios';
import { API_ROUTES } from '../../../../constants/apiRoutes';

export const deleteAdmin = (userId: string) => axios.delete(API_ROUTES.admin.users.byId(userId));
