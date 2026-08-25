import axios from 'axios';
import { API_ROUTES } from '../../../../constants/apiRoutes';

export const enableUserMfa = (userId: string) => axios.post(API_ROUTES.admin.users.enableMfa(userId));

export const disableUserMfa = (userId: string) => axios.post(API_ROUTES.admin.users.disableMfa(userId));

export const resetUserMfa = (userId: string) => axios.post(API_ROUTES.admin.users.resetMfa(userId));
