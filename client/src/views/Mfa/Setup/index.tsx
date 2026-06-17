import React, { useState, useEffect, useCallback, useRef } from 'react';
import { useNavigate } from 'react-router-dom';
import { CircularProgress } from '@mui/material';
import { QRCodeSVG } from 'qrcode.react';
import { useIsAuthenticated, useMsal } from '@azure/msal-react';
import { InteractionStatus } from '@azure/msal-browser';
import {
  Button,
  ErrorSummary,
  InputField,
  Panel,
  WarningText,
  Details,
  LoadingBox,
} from 'govuk-react';
import { GovUKButton, GovUKSkipLink } from '../../../components/GovUKComponents';
import usePageTitle from '../../../hooks/usePageTitle';
import { useMfa } from '../../../components/MfaProvider';
import { useAuthProvider, roleToAreaMap } from '../../../components/AuthProvider';
import { GovUKHeaderLogo } from '../../../components/Logos';
import LayoutFooter from '../../../layouts/General/LayoutFooter';
import GovUKPhaseBanner from '../../../components/GovUKComponents/GovUKPhaseBanner';
import GovUKBackLink from '../../../components/GovUKComponents/GovUKBackLink';
import '../styles.css';

const MfaSetup = (): React.ReactElement => {
  usePageTitle('Set up two-factor authentication');
  const navigate = useNavigate();
  const { inProgress, accounts } = useMsal();
  const isAuthenticated = useIsAuthenticated();
  const { userRole } = useAuthProvider();
  const { setupData, recoveryCodes, initiateSetup, completeSetup, clearRecoveryCodes, isEnabled } = useMfa();
  
  const isMsalLoading = inProgress !== InteractionStatus.None || (isAuthenticated && accounts.length === 0);

  const [activeStep, setActiveStep] = useState(0);
  const [code, setCode] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [isVerifying, setIsVerifying] = useState(false);
  const [isInitializing, setIsInitializing] = useState(true);
  const hasSubmittedRef = useRef(false);
  const userRoleRef = useRef(userRole);

  useEffect(() => {
    if (userRole) {
      userRoleRef.current = userRole;
    }
  }, [userRole]);

  useEffect(() => {
    if (activeStep === 2 || recoveryCodes.length > 0) {
      return;
    }

    if (isMsalLoading) {
      return;
    }
    
    if (!isAuthenticated) {
      navigate('/sign-in');
      return;
    }

    if (isEnabled) {
      const homeRoute = userRole ? `/${roleToAreaMap(userRole)}/home` : '/';
      navigate(homeRoute);
      return;
    }

    if (setupData) {
      setIsInitializing(false);
      return;
    }

    let isMounted = true;

    const initSetup = async (retryCount = 0): Promise<void> => {
      try {
        await initiateSetup();
      } catch (err: any) {
        if (!isMounted) return;
        
        if (err?.response?.status === 401 && retryCount < 3) {
          setTimeout(() => initSetup(retryCount + 1), 1000);
          return;
        }
        
        if (err?.response?.status === 409) {
          const homeRoute = userRole ? `/${roleToAreaMap(userRole)}/home` : '/';
          navigate(homeRoute);
        } else {
          const errorMessage = err?.response?.data?.message || err?.message || 'Failed to initialize MFA setup. Please try again.';
          setError(errorMessage);
        }
      } finally {
        if (isMounted) {
          setIsInitializing(false);
        }
      }
    };

    initSetup();

    return () => {
      isMounted = false;
    };
  }, [isMsalLoading, isAuthenticated, isEnabled, navigate, userRole, setupData, accounts.length, activeStep, recoveryCodes.length]);

  const handleCodeChange = useCallback((e: React.ChangeEvent<HTMLInputElement>): void => {
    const value = e.target.value.replace(/\D/g, '').slice(0, 6);
    setCode(value);
    setError(null);
    if (value.length < 6) {
      hasSubmittedRef.current = false;
    }
  }, []);

  const handleVerify = async (e?: React.FormEvent): Promise<void> => {
    if (e) e.preventDefault();
    
    if (code.length !== 6) {
      setError('Enter a 6-digit code');
      return;
    }

    setIsVerifying(true);
    setError(null);

    try {
      await completeSetup(code);
      setActiveStep(2);
    } catch (err: any) {
      if (err?.response?.status === 401) {
        setError("Check you've entered the correct code from your authenticator app, and try again.");
      } else if (err?.response?.status === 400) {
        setError('Setup session expired. Please refresh the page to start again.');
      } else {
        setError('Verification failed. Please try again.');
      }
    } finally {
      setIsVerifying(false);
    }
  };

  const handleFinish = (): void => {
    clearRecoveryCodes();
    const role = userRoleRef.current || userRole;
    const homeRoute = role ? `/${roleToAreaMap(role)}/home` : '/sign-in';
    navigate(homeRoute);
  };

  const handleCopyRecoveryCodes = (): void => {
    const codesText = recoveryCodes.join('\n');
    navigator.clipboard.writeText(codesText);
  };

  const handleDownloadRecoveryCodes = (): void => {
    const codesText = recoveryCodes.join('\n');
    const blob = new Blob([codesText], { type: 'text/plain' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = 'recovery-codes.txt';
    a.click();
    URL.revokeObjectURL(url);
  };


  const handleKeyPress = (e: React.KeyboardEvent): void => {
    if (e.key === 'Enter') {
      e.preventDefault();
      handleVerify();
    }
  };

  if (isInitializing) {
    return (
      <div className="mfa-page">
        <GovUKSkipLink />
        <header className="govuk-header" data-module="govuk-header">
          <div className="govuk-header__container govuk-width-container">
            <div className="govuk-header__logo">
              <a href="https://www.gov.uk" className="govuk-header__link govuk-header__link--homepage">
                <GovUKHeaderLogo />
              </a>
            </div>
          </div>
        </header>
        <main className="mfa-page__main" id="main-content" role="main">
          <div className="govuk-width-container">
            <div className="mfa-page__loading">
              <LoadingBox loading={true}>
                <div style={{ minHeight: '200px' }} />
              </LoadingBox>
            </div>
          </div>
        </main>
        <LayoutFooter />
      </div>
    );
  }

  return (
    <div className="mfa-page">
      <GovUKSkipLink />
      <header className="govuk-header" data-module="govuk-header">
        <div className="govuk-header__container govuk-width-container">
          <div className="govuk-header__logo">
            <a href="https://www.gov.uk" className="govuk-header__link govuk-header__link--homepage">
              <GovUKHeaderLogo />
            </a>
          </div>
        </div>
      </header>

      <section className="mfa-page__service-banner" aria-label="Service information">
        <div className="govuk-width-container">
          <span className="mfa-page__service-name">Report data on Best Start Family Hubs and Healthy Babies</span>
        </div>
      </section>

      <main className="mfa-page__main" id="main-content" role="main">
        <div className="govuk-width-container">
          <GovUKPhaseBanner phase="beta" />
          <div className="govuk-main-wrapper">
            {activeStep > 0 && activeStep < 2 && (
              <GovUKBackLink onClick={() => setActiveStep(activeStep - 1)}>Back</GovUKBackLink>
            )}

            {error && (
              <ErrorSummary
                heading="There is a problem"
                errors={[{ targetName: 'code-input', text: error }]}
              />
            )}

            {/* Step 1: Scan QR Code */}
            {activeStep === 0 && (
              <>
                <h1 className="govuk-heading-xl">Set up two-factor authentication</h1>
                
                <p className="govuk-body">
                  Two-factor authentication adds an extra layer of security to your account. You will need to enter a code from your authenticator app each time you sign in.
                </p>

                <h2 className="govuk-heading-l">Scan the QR code</h2>
                
                <p className="govuk-body">
                  Use an authenticator app on your phone to scan this QR code. We recommend using Microsoft Authenticator or Google Authenticator.
                </p>

                {setupData && (
                  <>
                    <div className="mfa-page__qr-wrapper">
                      <QRCodeSVG value={setupData.qrCodeUri} size={200} level="M" />
                    </div>

                    <Details summary="I cannot scan the QR code">
                      <p className="govuk-body">Enter this key manually in your authenticator app:</p>
                      <div className="mfa-page__manual-key">{setupData.manualEntryKey}</div>
                    </Details>

                    <Button onClick={() => setActiveStep(1)}>
                      Continue
                    </Button>
                  </>
                )}

                {!setupData && !error && (
                  <div className="mfa-page__loading">
                    <CircularProgress />
                  </div>
                )}
              </>
            )}

            {/* Step 2: Verify Code */}
            {activeStep === 1 && (
              <div className="mfa-page__content-wrapper">
                <h1 className="govuk-heading-xl">Enter authentication code</h1>
                
                <p className="govuk-body">
                  Open your authenticator app and enter the 6-digit code shown for Family Hubs.
                </p>

                <div className="mfa-page__input-wrapper">
                  <InputField
                    input={{
                      id: 'code-input',
                      name: 'code',
                      value: code,
                      onChange: handleCodeChange,
                      onKeyPress: handleKeyPress,
                      disabled: isVerifying,
                      autoComplete: 'one-time-code',
                      inputMode: 'numeric',
                      pattern: '[0-9]*',
                      spellCheck: false,
                    }}
                    meta={{ error: error || undefined, touched: !!error }}
                    hint="For example, 654321"
                  >
                    <strong>Authentication code</strong>
                  </InputField>
                </div>

                <GovUKButton
                  onClick={() => handleVerify()}
                  disabled={code.length !== 6}
                  isLoading={isVerifying}
                >
                  Verify
                </GovUKButton>
              </div>
            )}

            {/* Step 3: Recovery Codes */}
            {activeStep === 2 && recoveryCodes.length > 0 && (
              <>
                <Panel title="Two-factor authentication is now set up" />

                <h2 className="govuk-heading-l">Save your recovery codes</h2>
                
                <p className="govuk-body">
                  Recovery codes can be used to access your account if you lose access to your authenticator app. Each code can only be used once.
                </p>

                <WarningText>
                  Save these codes in a secure place. You will not be able to see them again.
                </WarningText>

                <div className="mfa-page__recovery-codes">
                  <ul className="mfa-page__recovery-codes-list">
                    {recoveryCodes.map((recoveryCode, index) => (
                      <li key={index} className="mfa-page__recovery-code">{recoveryCode}</li>
                    ))}
                  </ul>
                </div>

                <div className="mfa-page__button-group">
                  <Button buttonColour="#f3f2f1" buttonTextColour="#0b0c0c" onClick={handleCopyRecoveryCodes}>
                    Copy codes
                  </Button>
                  <Button buttonColour="#f3f2f1" buttonTextColour="#0b0c0c" onClick={handleDownloadRecoveryCodes}>
                    Download codes
                  </Button>
                </div>

                <Button onClick={handleFinish}>
                  Continue to dashboard
                </Button>
              </>
            )}
          </div>
        </div>
      </main>

      <LayoutFooter />
    </div>
  );
}

export default MfaSetup;
