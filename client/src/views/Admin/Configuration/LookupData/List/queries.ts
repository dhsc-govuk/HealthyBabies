import axios from 'axios';
import { GlobalDataDto } from '../../../../../components/Global/Queries/globalData';
import { API_ROUTES } from '../../../../../constants/apiRoutes';

export const getAllGlobalData = () => axios.get<GlobalDataDto[]>(API_ROUTES.globalData.list);

export const getGlobalDataEntities = () => axios.get<string[]>(API_ROUTES.globalData.entities);
