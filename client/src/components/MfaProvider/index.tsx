import React, { createContext, useContext, useState, useCallback } from 'react';
import { useQuery, useMutation, useQueryClient } from 'react-query';
import {
  getMfaStatus,
  setupMfa,
  verifyMfaSetup,
  verifyMfa,
  verifyMfaRecovery,
  regenerateRecoveryCodes
} from './queries';
import { defaultStaleTime } from '../../helpers/queriesParams';
import { MfaContextValue, MfaRecoveryCodesResponse, MfaSetupCompleteResponse, MfaSetupResponse, MfaVerifyResponse } from './types';

const MfaContext = createContext<MfaContextValue | null>(null);

interface MfaProviderProps {
  children: React.ReactNode;
}

export const MfaProvider = ({ children }: MfaProviderProps): React.ReactElement => {
  const queryClient = useQueryClient();
  const [setupData, setSetupData] = useState<MfaSetupResponse | null>(null);
  const [recoveryCodes, setRecoveryCodes] = useState<string[]>([]);
  const [requiresVerification, setRequiresVerification] = useState(false);

  const {
    data: statusData,
    isLoading,
    refetch: refetchStatus,
  } = useQuery({
    queryKey: 'mfa-status',
    queryFn: getMfaStatus,
    staleTime: defaultStaleTime,
    retry: false,
    onError: () => {
      // Silently handle errors - user might not be authenticated yet
    },
  });

  const status = statusData?.data ?? null;
  const isEnabled = status?.isEnabled ?? false;

  const setupMutation = useMutation(setupMfa, {
    onSuccess: (response) => {
      setSetupData(response.data);
    },
  });

  const verifySetupMutation = useMutation(verifyMfaSetup, {
    onSuccess: (response) => {
      sessionStorage.setItem('mfa_verified_at', Date.now().toString());
      setRecoveryCodes(response.data.recoveryCodes);
      setSetupData(null);
      // Invalidate to refetch profile data now that MFA session is set
      queryClient.invalidateQueries('profile-data');
      queryClient.invalidateQueries('mfa-status');
    },
  });

  const verifyMutation = useMutation(verifyMfa, {
    onSuccess: () => {
      sessionStorage.setItem('mfa_verified_at', Date.now().toString());
      setRequiresVerification(false);
      // Invalidate to refetch profile data now that MFA session is set
      queryClient.invalidateQueries('profile-data');
      queryClient.invalidateQueries('mfa-status');
    },
  });

  const verifyRecoveryMutation = useMutation(verifyMfaRecovery, {
    onSuccess: () => {
      sessionStorage.setItem('mfa_verified_at', Date.now().toString());
      setRequiresVerification(false);
      // Invalidate to refetch profile data now that MFA session is set
      queryClient.invalidateQueries('profile-data');
      queryClient.invalidateQueries('mfa-status');
    },
  });

  const regenerateMutation = useMutation(regenerateRecoveryCodes, {
    onSuccess: (response) => {
      setRecoveryCodes(response.data.codes);
      queryClient.invalidateQueries('mfa-status');
    },
  });

  const initiateSetup = useCallback(async (): Promise<MfaSetupResponse> => {
    const response = await setupMutation.mutateAsync();
    return response.data;
  }, [setupMutation]);

  const completeSetup = useCallback(
    async (code: string): Promise<MfaSetupCompleteResponse> => {
      const response = await verifySetupMutation.mutateAsync(code);
      return response.data;
    },
    [verifySetupMutation]
  );

  const verify = useCallback(
    async (code: string): Promise<MfaVerifyResponse> => {
      const response = await verifyMutation.mutateAsync(code);
      return response.data;
    },
    [verifyMutation]
  );

  const verifyWithRecovery = useCallback(
    async (code: string): Promise<MfaVerifyResponse> => {
      const response = await verifyRecoveryMutation.mutateAsync(code);
      return response.data;
    },
    [verifyRecoveryMutation]
  );

  const regenerateCodes = useCallback(
    async (code: string): Promise<MfaRecoveryCodesResponse> => {
      const response = await regenerateMutation.mutateAsync(code);
      return response.data;
    },
    [regenerateMutation]
  );

  const clearSetupData = useCallback(() => {
    setSetupData(null);
  }, []);

  const clearRecoveryCodes = useCallback(() => {
    setRecoveryCodes([]);
  }, []);

  const contextValue: MfaContextValue = {
    status,
    isLoading,
    isEnabled,
    requiresVerification,
    setRequiresVerification,
    setupData,
    recoveryCodes,
    initiateSetup,
    completeSetup,
    verify,
    verifyWithRecovery,
    regenerateCodes,
    clearSetupData,
    clearRecoveryCodes,
    refetchStatus: () => refetchStatus(),
  };

  return <MfaContext.Provider value={contextValue}>{children}</MfaContext.Provider>;
}

export const useMfa = (): MfaContextValue => {
  const context = useContext(MfaContext);
  if (context === null) {
    throw new Error('useMfa must be used within a MfaProvider');
  }
  return context;
}

export default MfaProvider;
