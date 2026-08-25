import { GlobalStyle } from 'govuk-react';
import 'govuk-frontend/dist/govuk/govuk-frontend.min.css';
import '../../styles/govuk-refreshed-branding.css';
import { BrowserRouter } from 'react-router-dom';
import { InteractionRequiredAuthError, PublicClientApplication } from '@azure/msal-browser';
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
import AriaPageAnnouncer from '../AriaPageAnnouncer';
import { QueryClient, QueryClientProvider } from 'react-query';
import axios from 'axios';
import appConfig from '../../config.json';
import { loginRequest } from '../AuthProvider';
import { stagingRequestInterceptor } from '../../helpers/requestStaging';
import { ROUTES } from '../../constants/routes';

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

// Required for govuk-frontend CSS to apply enhanced (desktop) styles — tabs, etc.
document.documentElement.classList.add('govuk-frontend-supported');

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
      if (error instanceof InteractionRequiredAuthError) {
        // Silent renewal failed because the identity provider has no session to renew from (its
        // SSO cookie was blocked, expired, or never set) - sending the request without a token
        // would just come back as a 401 and leave the caller to figure out why. Go straight to an
        // interactive sign-in instead, and never resolve so the doomed request is never sent.
        await msalInstance.loginRedirect(loginRequest);
        return new Promise(() => {});
      }
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
    const mfaPath = `${ROUTES.mfa.root}/`;
    if (error.response?.status === 403 && !currentPath.startsWith(mfaPath) && !isRecentlyVerified) {
      const errorCode = error.response?.data?.error;
      const rootPath = ROUTES.home;
      const signInPath = ROUTES.signIn;
      if (errorCode === 'MFA_SETUP_REQUIRED') {
        // Don't store root or sign-in as redirect paths
        if (currentPath !== rootPath && currentPath !== signInPath) {
          sessionStorage.setItem('mfa_redirect_path', currentPath);
        }
        window.location.href = `${basePath}${ROUTES.mfa.setup}`.replace('//', '/');
        return new Promise(() => {}); // Prevent further processing
      } else if (errorCode === 'MFA_REQUIRED') {
        // Don't store root or sign-in as redirect paths
        if (currentPath !== rootPath && currentPath !== signInPath) {
          sessionStorage.setItem('mfa_redirect_path', currentPath);
        }
        window.location.href = `${basePath}${ROUTES.mfa.verify}`.replace('//', '/');
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
                {/* id is the landing spot for focus on every route change. It sits ahead of the
                    skip link in the DOM, so the user's first Tab reaches "Skip to main content"
                    (see useRouteFocus). */}
                <Root id="app-top" tabIndex={-1}>
                  <AriaPageAnnouncer />
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
