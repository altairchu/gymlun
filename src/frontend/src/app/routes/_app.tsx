import { AppBar, Container, LinearProgress, Toolbar } from '@mui/material';
import {
  createFileRoute,
  Outlet,
  redirect,
  useRouter,
  useRouterState,
} from '@tanstack/react-router';
import { authApi, useAuthStatusCheckEffect } from '@/features/auth';
import { UpdateAppBanner } from '@/features/updateApp';
import { verifyGymAccount } from '@/shared/api/expectedAccount';
import { APP_BAR_HEIGHT_SM, APP_BAR_HEIGHT_XS } from '@/shared/constants';
import { AppLoader } from '@/shared/ui';
import { GymNavigation } from '@/widgets/GymNavigation/ui/GymNavigation';
import { Navigation } from '@/widgets/Navigation';
import './-gymWorkspace.css';

export const Route = createFileRoute('/_app')({
  staticData: { appBar: null },
  beforeLoad: async ({ context, location, preload }) => {
    const authStatusQueryPromise = context.store.dispatch(
      authApi.endpoints.getStatus.initiate({}, { forceRefetch: !preload }),
    );

    try {
      const authStatusQuery = await authStatusQueryPromise;

      if (!authStatusQuery.data?.isAuthenticated) {
        throw redirect({ to: '/login', search: { returnUrl: location.href } });
      }

      if (!verifyGymAccount(authStatusQuery.data.accountId)) {
        window.location.reload();
      }
    } finally {
      authStatusQueryPromise.unsubscribe();
    }
  },
  pendingComponent: AppLoader,
  pendingMs: 0,
  component: AuthenticatedLayout,
});

function AuthenticatedLayout() {
  const router = useRouter();
  const loading = useRouterState({ select: state => state.isLoading });

  useAuthStatusCheckEffect(router.history.push);

  return (
    <>
      <AppBar position="sticky">
        <Toolbar disableGutters>
          <Container>
            <Navigation />
          </Container>
        </Toolbar>
        <LinearProgress
          sx={theme => ({
            display: loading ? 'block' : 'none',
            position: 'absolute',
            top: APP_BAR_HEIGHT_XS,
            left: 0,
            width: '100%',

            [theme.breakpoints.up('sm')]: {
              top: APP_BAR_HEIGHT_SM,
            },
          })}
        />
      </AppBar>
      <UpdateAppBanner withAppBar />
      <div className="gym-workspace">
        <GymNavigation />
        <main className="gym-workspace-content">
          <Outlet />
        </main>
      </div>
    </>
  );
}
