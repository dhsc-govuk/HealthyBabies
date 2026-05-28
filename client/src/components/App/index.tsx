import { GlobalStyle } from 'govuk-react';
import 'govuk-frontend/dist/govuk/govuk-frontend.min.css';
import '../../styles/govuk-refreshed-branding.css';
import { BrowserRouter } from 'react-router-dom';
import { PublicClientApplication } from '@azure/msal-browser';
import { MsalProvider } from '@azure/msal-react';
import { CssBaseline } from '@mui/material';
import { styled } from '@mui/material/styles';
import { SnackbarProvider } from 'notistack';
import AuthProvider, { AuthContext, msalConfig } from '../AuthProvider';
import { MfaProvider } from '../MfaProvider';
import AppRoutes from '../../Routes';
import { CustomThemeProvider, DashboardProvider, GovUKNotificationProvider, type ThemeModes } from '../GovUKComponents';
import useLocalStorage from '../../hooks/useLocalStorage';
import { withRouter } from '../DashboardProviderArgs';
import { QueryClient, QueryClientProvider } from 'react-query';
import axios from 'axios';
import appConfig from '../../config.json';
import { loginRequest } from '../AuthProvider';
import { stagingRequestInterceptor } from '../../helpers/requestStaging';

const modes: ThemeModes = {
  primary: {
    main: '#1d70b8',
    light: '#5694ca',
    contrastText: '#ffffff',
  },
  secondary: {
    main: '#505a5f',
    light: '#626a6e',
    contrastText: '#ffffff',
  },
  light: {
    background: {
      paper: '#ffffff',
      default: '#f3f2f1',
    },
    text: {
      primary: '#0b0c0c',
      secondary: '#505a5f',
    },
  },
  dark: {
    background: {
      paper: '#0b0c0c',
      default: '#0b0c0c',
    },
    text: {
      primary: '#ffffff',
      secondary: '#b1b4b6',
    },
  },
};

const Root = styled('div')(() => ({ position: 'absolute', width: '100%', height: '100%', fontFamily: '"GDS Transport", arial, sans-serif' }));

const msalInstance = new PublicClientApplication(msalConfig);

// Track MSAL initialization state
let msalInitialized = false;
const msalInitPromise = msalInstance.initialize().then(() => {
  return msalInstance.handleRedirectPromise();
}).then(() => {
  msalInitialized = true;
}).catch((error) => {
  console.error('MSAL initialization error:', error);
  msalInitialized = true; // Still mark as initialized to prevent hanging
});

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      refetchOnWindowFocus: false,
    },
  },
});

(() => {
  if (!appConfig.api_url) {
    throw new Error('API URL is undefined');
  }
})();

const API_URL = appConfig.api_url;

axios.defaults.baseURL = API_URL;
axios.defaults.withCredentials = true;

// Stage large JSON bodies via /system/request-staging to avoid WAF 403s on large form submissions.
// Registered first so the rewritten request is then signed by the auth interceptor below.
axios.interceptors.request.use(stagingRequestInterceptor);

axios.interceptors.request.use(
  async (config) => {
    // Wait for MSAL to initialize before trying to acquire tokens
    if (!msalInitialized) {
      await msalInitPromise;
    }
    
    const account = msalInstance.getAllAccounts()[0];
    if (!account) {
      console.warn('No MSAL account found - request will be unauthenticated');
      return config;
    }
    try {
      const msalResponse = await msalInstance.acquireTokenSilent({
        ...loginRequest,
        account: account,
      });
      config.headers['Authorization'] = 'Bearer ' + msalResponse.accessToken;
    } catch (error) {
      console.error('Failed to acquire token:', error);
    }
    return config;
  },
  (err) => {
    return Promise.reject(err);
  }
);

axios.interceptors.response.use(
  (response) => response,
  (error) => {
    const basePath = import.meta.env.BASE_URL || '/';
    const rawPath = window.location.pathname;
    const currentPath = rawPath.startsWith(basePath)
      ? rawPath.slice(basePath.replace(/\/$/, '').length) || '/'
      : rawPath;
    const mfaVerifiedAt = sessionStorage.getItem('mfa_verified_at');
    const isRecentlyVerified = mfaVerifiedAt && (Date.now() - parseInt(mfaVerifiedAt, 10)) < 5000;
    const mfaPath = '/mfa/';
    if (error.response?.status === 403 && !currentPath.startsWith(mfaPath) && !isRecentlyVerified) {
      const errorCode = error.response?.data?.error;
      const rootPath = '/';
      const signInPath = '/sign-in';
      if (errorCode === 'MFA_SETUP_REQUIRED') {
        // Don't store root or sign-in as redirect paths
        if (currentPath !== rootPath && currentPath !== signInPath) {
          sessionStorage.setItem('mfa_redirect_path', currentPath);
        }
        window.location.href = `${basePath}mfa/setup`.replace('//', '/');
        return new Promise(() => {}); // Prevent further processing
      } else if (errorCode === 'MFA_REQUIRED') {
        // Don't store root or sign-in as redirect paths
        if (currentPath !== rootPath && currentPath !== signInPath) {
          sessionStorage.setItem('mfa_redirect_path', currentPath);
        }
        window.location.href = `${basePath}mfa/verify`.replace('//', '/');
        return new Promise(() => {}); // Prevent further processing
      }
    }
    return Promise.reject(error);
  }
);

const App = () => {
  return (
    <MsalProvider instance={msalInstance}>
      <QueryClientProvider client={queryClient}>
        <AuthProvider>
           <MfaProvider>
            <CustomThemeProvider storageProvider={useLocalStorage} modes={modes}>
            <DashboardProvider AuthContext={AuthContext} withRouter={withRouter} storageProvider={useLocalStorage} dashboardItems={[]}>
              <SnackbarProvider maxSnack={3} autoHideDuration={3000} anchorOrigin={{ vertical: 'top', horizontal: 'right' }}>
              <CssBaseline />
                <GlobalStyle />
                <Root>
                  <BrowserRouter basename={(import.meta.env.BASE_URL || '/').replace(/\/$/, '') || '/'}>
                    <GovUKNotificationProvider>
                      <AppRoutes />
                    </GovUKNotificationProvider>
                  </BrowserRouter>
                </Root>
              </SnackbarProvider>
            </DashboardProvider>
            </CustomThemeProvider>
          </MfaProvider>
        </AuthProvider>
      </QueryClientProvider>
    </MsalProvider>
  );
};

export default App;
