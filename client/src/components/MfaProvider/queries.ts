import axios from 'axios';
import { MfaStatusResponse, MfaSetupResponse, MfaSetupCompleteResponse, MfaVerifyRequest, MfaVerifyResponse, MfaRecoveryCodesResponse } from './types';

export const getMfaStatus = () => axios.get<MfaStatusResponse>('mfa/status');

export const setupMfa = () => axios.post<MfaSetupResponse>('mfa/setup');

export const verifyMfaSetup = (code: string) =>
  axios.post<MfaSetupCompleteResponse>('mfa/setup/verify', { code } as MfaVerifyRequest);

export const verifyMfa = (code: string) =>
  axios.post<MfaVerifyResponse>('mfa/verify', { code } as MfaVerifyRequest);

export const verifyMfaRecovery = (code: string) =>
  axios.post<MfaVerifyResponse>('mfa/verify/recovery', { code } as MfaVerifyRequest);

export const regenerateRecoveryCodes = (code: string) =>
  axios.post<MfaRecoveryCodesResponse>('mfa/recovery-codes/regenerate', { code } as MfaVerifyRequest);

export const mfaLogout = () => axios.post('mfa/logout');
