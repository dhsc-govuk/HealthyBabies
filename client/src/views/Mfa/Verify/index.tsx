import React, { useState, useCallback, useRef, useEffect } from 'react';
import usePageTitle from '../../../hooks/usePageTitle';

import { useNavigate } from 'react-router-dom';

import { ErrorSummary, InputField } from 'govuk-react';
import { GovUKButton, H1, H2, H4, Paragraph } from '../../../components/GovUKComponents';

import { useMfa } from '../../../components/MfaProvider';

import { useAuthProvider, roleToAreaMap } from '../../../components/AuthProvider';

import { getProfile } from '../../../components/AuthProvider/queries';

import { GovUKHeaderLogo } from '../../../components/Logos';

import LayoutFooter from '../../../layouts/General/LayoutFooter';

import GovUKPhaseBanner from '../../../components/GovUKComponents/GovUKPhaseBanner';

import GovUKBackLink from '../../../components/GovUKComponents/GovUKBackLink';

import {
  PageWrapper,
  Header,
  HeaderContainer,
  ServiceBanner,
  ServiceBannerContainer,
  ServiceName,
  MainContent,
  ContentContainer,
  ContentWrapper,
  InputWrapper,
  StyledLink,
  BodyText,
} from './styles';

const MfaVerify = (): React.ReactElement => {
  usePageTitle('Enter authentication code');
  const navigate = useNavigate();

  const { userRole, signOut } = useAuthProvider();

  const { verify, verifyWithRecovery, setRequiresVerification } = useMfa();

  const [code, setCode] = useState('');

  const [error, setError] = useState<string | null>(null);

  const [isLoading, setIsLoading] = useState(false);

  const [useRecoveryCode, setUseRecoveryCode] = useState(false);

  const hasSubmittedRef = useRef(false);

  const userRoleRef = useRef(userRole);

  useEffect(() => {
    if (userRole) {
      userRoleRef.current = userRole;
    }
  }, [userRole]);

  const handleCodeChange = useCallback(
    (e: React.ChangeEvent<HTMLInputElement>): void => {
      const value = useRecoveryCode ? e.target.value.toUpperCase() : e.target.value.replace(/\D/g, '').slice(0, 6);

      setCode(value);

      setError(null);

      if (value.length < 6) {
        hasSubmittedRef.current = false;
      }
    },
    [useRecoveryCode]
  );

  const handleVerify = async (): Promise<void> => {
    const codeToVerify = code.replace(/[-\s]/g, '');

    if (useRecoveryCode) {
      if (codeToVerify.length < 8) {
        setError('Enter a valid recovery code');

        return;
      }
    } else {
      if (codeToVerify.length !== 6) {
        setError('Enter a 6-digit code');

        return;
      }
    }

    setIsLoading(true);

    setError(null);

    try {
      if (useRecoveryCode) {
        await verifyWithRecovery(code);
      } else {
        await verify(codeToVerify);
      }

      setRequiresVerification(false);

      const redirectPath = sessionStorage.getItem('mfa_redirect_path');

      sessionStorage.removeItem('mfa_redirect_path');

      let role: string | null = userRoleRef.current || userRole;

      if (!role) {
        try {
          const profileResponse = await getProfile();

          role = profileResponse.data.role;
        } catch {
          // If profile fetch fails, navigate to sign-in
        }
      }

      const homeRoute = role ? `/${roleToAreaMap(role as any)}/home` : '/sign-in';

      navigate(redirectPath || homeRoute);
    } catch (err: any) {
      if (err?.response?.status === 401) {
        setError(useRecoveryCode ? "Check you've entered the correct recovery code, and try again." : "Check you've entered the correct authentication code, and try again.");
      } else if (err?.response?.status === 429) {
        setError('Too many attempts. Please wait a few minutes and try again.');
      } else {
        setError('Verification failed. Please try again.');
      }
    } finally {
      setIsLoading(false);
    }
  };

  const toggleRecoveryMode = (): void => {
    setUseRecoveryCode(!useRecoveryCode);

    setCode('');

    setError(null);
  };

  const handleKeyPress = (e: React.KeyboardEvent): void => {
    if (e.key === 'Enter') {
      e.preventDefault();

      handleVerify();
    }
  };

  return (
    <PageWrapper>
      <Header component="header" role="banner">
        <HeaderContainer>
          <a href="/" style={{ color: '#ffffff', textDecoration: 'none' }}>
            <GovUKHeaderLogo />
          </a>
        </HeaderContainer>
      </Header>

      <ServiceBanner component="section" aria-label="Service information">
        <ServiceBannerContainer>
          <ServiceName>Report data on Best Start Family Hubs and Healthy Babies</ServiceName>
        </ServiceBannerContainer>
      </ServiceBanner>

      <MainContent component="main" role="main">
        <ContentContainer>
          <GovUKPhaseBanner phase="beta" />

          <GovUKBackLink onClick={signOut}>
            Back
          </GovUKBackLink>

          {error && <ErrorSummary heading="There is a problem" errors={[{ targetName: 'code-input', text: error }]} />}

          <ContentWrapper>
            <H1>Enter authentication code</H1>

            {!useRecoveryCode ? (
              <>
                <Paragraph>Your account is protected with multi-factor authentication (MFA). Enter a one-time code to continue.</Paragraph>

                <Paragraph>Use a mobile authenticator app to get your one-time code. You set this up when you first signed in.</Paragraph>

                <InputWrapper>
                  <InputField
                    input={{
                      id: 'code-input',

                      name: 'code',

                      value: code,

                      onChange: handleCodeChange,

                      onKeyPress: handleKeyPress,

                      disabled: isLoading,

                      autoComplete: 'one-time-code',

                      inputMode: 'numeric',

                      pattern: '[0-9]*',

                      spellCheck: false,
                    }}
                    meta={{ error: error || undefined, touched: !!error }}
                    hint="For example, 654321">
                    <strong>Authentication code</strong>
                  </InputField>
                </InputWrapper>

                <GovUKButton onClick={handleVerify} disabled={code.length !== 6} isLoading={isLoading}>
                  Verify
                </GovUKButton>

                <H2>If you cannot sign in to your account</H2>

                <BodyText>
                  If you have problems signing in to your account, <StyledLink onClick={toggleRecoveryMode}>use a recovery code</StyledLink> or contact your administrator.
                </BodyText>
              </>
            ) : (
              <>
                <Paragraph>Enter one of your recovery codes. Each code can only be used once.</Paragraph>

                <InputField
                  input={{
                    id: 'code-input',

                    name: 'recovery-code',

                    value: code,

                    onChange: handleCodeChange,

                    onKeyPress: handleKeyPress,

                    disabled: isLoading,

                    placeholder: 'XXXX-XXXX-XXXX',

                    spellCheck: false,
                  }}
                  meta={{ error: error || undefined, touched: !!error }}
                  hint="For example, XXXX-XXXX-XXXX">
                  Recovery code
                </InputField>

                <GovUKButton onClick={handleVerify} disabled={code.length < 8} isLoading={isLoading}>
                  Verify
                </GovUKButton>

                <H4>If you cannot sign in to your account</H4>

                <BodyText>
                  If you have problems signing in to your account, <StyledLink onClick={toggleRecoveryMode}>use your authenticator app</StyledLink> or contact your administrator.
                </BodyText>
              </>
            )}
          </ContentWrapper>
        </ContentContainer>
      </MainContent>

      <LayoutFooter />
    </PageWrapper>
  );
};

export default MfaVerify;
