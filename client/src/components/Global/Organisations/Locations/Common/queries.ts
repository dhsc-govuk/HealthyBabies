import { getGlobalDataByEntity } from '../../../../../components/Global/Queries/globalData';

export const getLocationTypes = () => getGlobalDataByEntity('LOCATION_TYPES');

export const getSiteTypes = () => getGlobalDataByEntity('SITES_TYPES');
