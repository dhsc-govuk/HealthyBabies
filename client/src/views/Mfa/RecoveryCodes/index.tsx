import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { Box, Typography, Paper, Button, CircularProgress, Alert } from '@mui/material';
import { useMfa } from '../../../components/MfaProvider';
import OtpInput from '../../../components/MfaProvider/OtpInput';
import RecoveryCodesList from '../../../components/MfaProvider/RecoveryCodesList';
import { useAuthProvider, roleToAreaMap } from '../../../components/AuthProvider';

const MfaRecoveryCodes = (): React.ReactElement => {
  const navigate = useNavigate();
  const { userRole } = useAuthProvider();
  const { recoveryCodes, regenerateCodes, clearRecoveryCodes, isEnabled } = useMfa();

  const [code, setCode] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(false);
  const [showCodes, setShowCodes] = useState(recoveryCodes.length > 0);

  const handleRegenerate = async (): Promise<void> => {
    if (code.length !== 6) {
      setError('Please enter a 6-digit code');
      return;
    }

    setIsLoading(true);
    setError(null);

    try {
      await regenerateCodes(code);
      setShowCodes(true);
      setCode('');
    } catch (err: any) {
      if (err?.response?.status === 401) {
        setError('Invalid code. Please check your authenticator app and try again.');
      } else if (err?.response?.status === 400) {
        setError('MFA is not enabled on your account.');
      } else {
        setError('Failed to regenerate codes. Please try again.');
      }
    } finally {
      setIsLoading(false);
    }
  };

  const handleClose = (): void => {
    clearRecoveryCodes();
    const homeRoute = userRole ? `/${roleToAreaMap(userRole)}/home` : '/';
    navigate(homeRoute);
  };

  if (!isEnabled) {
    return (
      <Box maxWidth={500} mx="auto" p={3}>
        <Alert severity="warning">
          Two-factor authentication is not enabled on your account.
        </Alert>
        <Button
          variant="contained"
          sx={{ mt: 2 }}
          onClick={() => navigate('/mfa/setup')}
        >
          Set up two-factor authentication
        </Button>
      </Box>
    );
  }

  return (
    <Box maxWidth={600} mx="auto" p={3}>
      <Typography variant="h4" gutterBottom textAlign="center">
        Recovery Codes
      </Typography>

      {showCodes && recoveryCodes.length > 0 ? (
        <Paper elevation={2} sx={{ p: 3 }}>
          <Typography variant="body2" color="text.secondary" sx={{ mb: 3 }} textAlign="center">
            Your new recovery codes have been generated. Save them in a safe place.
          </Typography>
          <RecoveryCodesList codes={recoveryCodes} onClose={handleClose} />
        </Paper>
      ) : (
        <Paper elevation={2} sx={{ p: 3 }}>
          <Typography variant="h6" gutterBottom textAlign="center">
            Generate new recovery codes
          </Typography>
          <Typography variant="body2" color="text.secondary" sx={{ mb: 3 }} textAlign="center">
            This will invalidate your existing recovery codes. Enter your authenticator code to
            continue.
          </Typography>

          {error && (
            <Alert severity="error" sx={{ mb: 3 }}>
              {error}
            </Alert>
          )}

          <Box mb={3}>
            <OtpInput
              value={code}
              onChange={setCode}
              disabled={isLoading}
              error={!!error}
            />
          </Box>

          <Box display="flex" gap={2}>
            <Button
              variant="contained"
              fullWidth
              onClick={handleRegenerate}
              disabled={code.length !== 6 || isLoading}
              sx={{ position: 'relative' }}
            >
              <span style={{ visibility: isLoading ? 'hidden' : undefined }}>Generate new codes</span>
              {isLoading && (
                <CircularProgress
                  size={20}
                  sx={{ position: 'absolute', top: '50%', left: '50%', marginTop: '-10px', marginLeft: '-10px', color: 'inherit' }}
                />
              )}
            </Button>
            <Button
              variant="outlined"
              onClick={() => navigate(-1)}
              disabled={isLoading}
            >
              Cancel
            </Button>
          </Box>
        </Paper>
      )}
    </Box>
  );
}

export default MfaRecoveryCodes;
