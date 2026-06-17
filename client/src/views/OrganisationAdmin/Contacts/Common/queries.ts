import { getGlobalDataByEntity } from '../../../../components/Global/Queries/globalData';

export const getContactRoles = () => getGlobalDataByEntity('CONTACT_ROLES');
