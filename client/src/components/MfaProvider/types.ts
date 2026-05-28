export interface RecoveryCodesListProps {
  codes: string[];
  onClose?: () => void;
}

export interface MfaContextValue {
  status: MfaStatusResponse | null;
  isLoading: boolean;
  isEnabled: boolean;
  requiresVerification: boolean;
  setRequiresVerification: (value: boolean) => void;
  setupData: MfaSetupResponse | null;
  recoveryCodes: string[];
  initiateSetup: () => Promise<MfaSetupResponse>;
  completeSetup: (code: string) => Promise<MfaSetupCompleteResponse>;
  verify: (code: string) => Promise<MfaVerifyResponse>;
  verifyWithRecovery: (code: string) => Promise<MfaVerifyResponse>;
  regenerateCodes: (code: string) => Promise<MfaRecoveryCodesResponse>;
  clearSetupData: () => void;
  clearRecoveryCodes: () => void;
  refetchStatus: () => void;
}

export interface MfaStatusResponse {
  isEnabled: boolean;
  enabledAt: string | null;
  recoveryCodesRemaining: number;
}

export interface MfaSetupResponse {
  qrCodeUri: string;
  manualEntryKey: string;
}

export interface MfaSetupCompleteResponse {
  success: boolean;
  recoveryCodes: string[];
}

export interface MfaVerifyResponse {
  success: boolean;
  sessionId: string;
  expiresAt: string;
}

export interface MfaRecoveryCodesResponse {
  codes: string[];
}

export interface MfaVerifyRequest {
  code: string;
}
