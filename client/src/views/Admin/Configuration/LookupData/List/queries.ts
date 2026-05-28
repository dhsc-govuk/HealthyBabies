import axios from 'axios';
import { GlobalDataDto } from '../../../../../components/Global/Queries/globalData';

export const getAllGlobalData = () => axios.get<GlobalDataDto[]>('/global-data');

export const getGlobalDataEntities = () => axios.get<string[]>('/global-data/entities');
