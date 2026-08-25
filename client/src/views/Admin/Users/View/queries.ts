import axios from 'axios';
import { API_ROUTES } from '../../../../constants/apiRoutes';

export interface GetAdminResponse {
  id: string;
  firstName: string;
  lastName: string;
  email: string;
  isActive: boolean;
  role: string;
  organisation?: string;
  organisationId?: string;
  createdBy?: string;
  createdAt?: string;
  lastChangedBy?: string;
  lastChangedAt?: string;
}

// MFA State: 0=None, 1=PendingSetup, 2=Enabled, 3=Disabled
export type MfaState = number;

export interface MfaStatusResponse {
  isEnabled: boolean;
  enabledAt: string | null;
  recoveryCodesRemaining: number;
  state: MfaState;
}

export const getAdmin = (userId: string) => axios.get<GetAdminResponse>(API_ROUTES.admin.users.organisationUsers.byId(userId));

export const getMfaStatus = (userId: string) => axios.get<MfaStatusResponse>(API_ROUTES.admin.users.mfaStatus(userId));
