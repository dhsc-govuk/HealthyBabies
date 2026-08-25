import axios from 'axios';
import { API_ROUTES } from '../../../../../constants/apiRoutes';

export const deleteGlobalData = (id: string) => axios.delete(API_ROUTES.globalData.byId(id));
