import React, { useContext, useEffect, useState } from 'react';
import { useMsal, useIsAuthenticated } from '@azure/msal-react';
import { OrganisationSettingDto, User } from '../../types';
import { LogLevel } from '@azure/msal-browser';
import { getProfile } from './queries';
import { mfaLogout } from '../MfaProvider/queries';
import appConfig from '../../config';
import { useQuery } from 'react-query';
import { defaultStaleTime } from '../../helpers/queriesParams';
import { appInsights } from '../../telemetry';

(() => {
  if (!appConfig.ad_tenant_id) {
    throw new Error('Tenant ID is undefined');
  }
  if (!appConfig.ad_client_id) {
    throw new Error('Client Id is undefined');
  }
  if (!appConfig.ad_authority) {
    throw new Error('Authority is undefined');
  }
  if (!appConfig.ad_redirect_uri) {
    throw new Error('Redirect URI is undefined');
  }
  if (!appConfig.ad_scope) {
    throw new Error('Scope is undefined');
  }
})();

const config = {
  tenantId: appConfig.ad_tenant_id,
  clientId: appConfig.ad_client_id,
  authority: appConfig.ad_authority,
  redirectUri: appConfig.ad_redirect_uri,
  scopes: [appConfig.ad_scope],
};

export const msalConfig = {
  auth: {
    clientId: config.clientId,
    authority: config.authority,
    knownAuthorities: [new URL(config.authority).hostname],
    redirectUri: config.redirectUri,
    postLogoutRedirectUri: import.meta.env.BASE_URL || '/',
    navigateToLoginRequestUrl: false,
  },
  cache: {
    cacheLocation: 'sessionStorage',
    storeAuthStateInCookie: false,
  },
  system: {
    loggerOptions: {
      loggerCallback: (level: any, message: any, containsPii: any) => {
        if (containsPii) {
          return;
        }
        switch (level) {
          case LogLevel.Error:
            console.error(message);
            return;
          default:
            return;
        }
      },
    },
  },
};

export const loginRequest = {
  scopes: config.scopes,
};

export enum EnumUserRole {
  ADMIN = 'admin',
  ORGANISATION_ADMIN = 'organisation admin',
}

export const roleToAreaMap = (role: EnumUserRole) => {
  switch (role) {
    case EnumUserRole.ADMIN:
      return 'admin';
    case EnumUserRole.ORGANISATION_ADMIN:
      return 'organisation-admin';
    default:
      return 'unknown';
  }
};

export const adminRoles = [EnumUserRole.ADMIN];
export const organisationAdminRoles = [EnumUserRole.ORGANISATION_ADMIN];
interface AuthContextItems {
  userId: string | null;
  user: User | null;
  userRole: EnumUserRole | null;
  setting: OrganisationSettingDto | null;
  organisationId: string | null;
  locationId: string | null;
  signIn: () => void;
  signOut: () => Promise<void>;
}

export const AuthContext = React.createContext<AuthContextItems | null>(null);

type Props = { children: React.ReactNode };

const AuthProvider = ({ children }: Props): React.ReactElement => {
  const { instance, accounts } = useMsal();
  const isAuthenticated = useIsAuthenticated();
  const [subId, setSubId] = useState<string | null>(null);
  const [user, setUser] = useState<User | null>(null);
  const [userRole, setUserRole] = useState<EnumUserRole | null>(null);
  const [setting, setSetting] = useState<OrganisationSettingDto | null>(null);

  const { data: profileData, isSuccess: profileLoaded } = useQuery({
    queryKey: 'profile-data',
    queryFn: getProfile,
    enabled: !!subId,
    staleTime: defaultStaleTime,
  });

  useEffect(() => {
    const signIn = async () => {
      if (accounts.length > 0) {
        const tokenRequest = { ...loginRequest, account: accounts[0] };
        const response = await instance.acquireTokenSilent(tokenRequest);
        setSubId(response.account?.localAccountId ?? '');
      }
    };
    if (isAuthenticated && !subId) {
      signIn().catch(console.error);
    }
  }, [isAuthenticated, subId, accounts, instance]);

  useEffect(() => {
    if (profileLoaded && profileData?.data) {
      const { data } = profileData;
      const role = data.role as EnumUserRole;
      const user = {
        id: data.id,
        email: data.email,
        first_name: data.firstName,
        last_name: data.lastName,
        full_name: data.firstName + ' ' + data.lastName,
        user_type: roleToAreaMap(role),
        organisation_id: data.organisationId,
        location_id: data.locationId,
        organisation_name: data.organisationName,
      } as User;

      appInsights.setAuthenticatedUserContext(data.id);
      setUserRole(role);
      setUser(user);
      setSetting((s) => s);
    }
  }, [profileData, profileLoaded]);

  const authContext: AuthContextItems = {
    user,
    userId: subId,
    userRole,
    setting,
    organisationId: user?.organisation_id ?? null,
    locationId: user?.location_id ?? null,

    signIn: () => {
      instance
        .loginRedirect({
          ...loginRequest,
          redirectUri: import.meta.env.BASE_URL || '/',
          prompt: 'login',
          extraQueryParameters: { domain_hint: appConfig.ad_domain_hint },
        })
        .catch((error) => console.log(error));
    },
    signOut: async () => {
      try {
        await mfaLogout();
      } catch {
        // Ignore errors - user might not have MFA session
      }
      appInsights.clearAuthenticatedUserContext();
      await instance.logoutRedirect({
        postLogoutRedirectUri: import.meta.env.BASE_URL || '/',
      });
      setSubId(null);
      setUser(null);
    },
  };

  return <AuthContext.Provider value={authContext}>{children}</AuthContext.Provider>;
};

export default AuthProvider;

export const useAuthProvider = () => {
  const authContext = useContext(AuthContext);
  if (authContext === null) {
    throw new Error('No AuthContext');
  }

  return authContext;
};
