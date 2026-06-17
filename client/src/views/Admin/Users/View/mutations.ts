import axios from 'axios';

export const enableUserMfa = (userId: string) => axios.post(`admin/users/${userId}/enable-mfa`);

export const disableUserMfa = (userId: string) => axios.post(`admin/users/${userId}/disable-mfa`);

export const resetUserMfa = (userId: string) => axios.post(`admin/users/${userId}/reset-mfa`);
