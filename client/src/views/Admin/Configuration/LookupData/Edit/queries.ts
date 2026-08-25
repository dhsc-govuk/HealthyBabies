import axios from 'axios';
import { GlobalDataDto } from '../../../../../components/Global/Queries/globalData';
import { API_ROUTES } from '../../../../../constants/apiRoutes';

export const getGlobalDataById = (id: string) => axios.get<GlobalDataDto>(API_ROUTES.globalData.byId(id));
