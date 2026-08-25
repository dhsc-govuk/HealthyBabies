import axios from 'axios';
import { MfaStatusResponse, MfaSetupResponse, MfaSetupCompleteResponse, MfaVerifyRequest, MfaVerifyResponse, MfaRecoveryCodesResponse } from './types';
import { API_ROUTES } from '../../constants/apiRoutes';

export const getMfaStatus = () => axios.get<MfaStatusResponse>(API_ROUTES.mfa.status);

export const setupMfa = () => axios.post<MfaSetupResponse>(API_ROUTES.mfa.setup);

export const verifyMfaSetup = (code: string) =>
  axios.post<MfaSetupCompleteResponse>(API_ROUTES.mfa.setupVerify, { code } as MfaVerifyRequest);

export const verifyMfa = (code: string) =>
  axios.post<MfaVerifyResponse>(API_ROUTES.mfa.verify, { code } as MfaVerifyRequest);

export const verifyMfaRecovery = (code: string) =>
  axios.post<MfaVerifyResponse>(API_ROUTES.mfa.verifyRecovery, { code } as MfaVerifyRequest);

export const regenerateRecoveryCodes = (code: string) =>
  axios.post<MfaRecoveryCodesResponse>(API_ROUTES.mfa.regenerateRecoveryCodes, { code } as MfaVerifyRequest);

export const mfaLogout = () => axios.post(API_ROUTES.mfa.logout);
