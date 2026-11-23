import { useEffect } from 'react';
import { verifyGymAccount } from '@/shared/api/expectedAccount';
import { AUTH_CHECK_INTERVAL } from '@/shared/config';
import { authApi } from '../api';
import { type NavigateToHref, signOut } from '../lib';

export const useAuthStatusCheckEffect = (navigate: NavigateToHref): void => {
  const [getAuthStatus] = authApi.useLazyGetStatusQuery();

  useEffect(() => {
    const checkStatus = (): void => {
      void (async () => {
        const authStatus = await getAuthStatus({});

        if (!authStatus.data?.isAuthenticated) {
          await signOut(navigate);
        } else if (!verifyGymAccount(authStatus.data.accountId)) {
          window.location.reload();
        }
      })();
    };
    const interval = setInterval(checkStatus, AUTH_CHECK_INTERVAL);
    window.addEventListener('focus', checkStatus);

    return () => {
      clearInterval(interval);
      window.removeEventListener('focus', checkStatus);
    };
  }, [getAuthStatus, navigate]);
};
