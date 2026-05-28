import axios from 'axios';

export interface GetMeResponse {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  isActive: boolean;
  role: string;
  organisationId: string;
  locationId: string;
  organisationName: string;
}

export interface MfaStatusResponse {
  isEnabled: boolean;
  enabledAt: string | null;
  recoveryCodesRemaining: number;
}

export const getProfile = () => axios.get<GetMeResponse>('profile/me');

export const getMfaStatus = () => axios.get<MfaStatusResponse>('mfa/status');
