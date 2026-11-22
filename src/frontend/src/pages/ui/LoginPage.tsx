import { Alert, Box, Container, Paper, Stack } from '@mui/material';
import { type FC } from 'react';
import { DemoModeWarning, type NavigateToHref, SignInForm } from '@/features/auth';
import { UpdateAppBanner } from '@/features/updateApp';
import { DEMO_MODE_ENABLED } from '@/shared/config';
import { AppName, Center } from '@/shared/ui';

interface Props {
  returnUrl: string;
  navigate: NavigateToHref;
  atlasLoginFailed?: boolean;
}

export const LoginPage: FC<Props> = ({ returnUrl, navigate, atlasLoginFailed = false }) => {
  return (
    <>
      <Box
        component={Paper}
        elevation={0}
        sx={{
          position: 'fixed',
          top: 0,
          left: 0,
          right: 0,
        }}
      >
        <UpdateAppBanner />
      </Box>
      <Center>
        <Container maxWidth="md" disableGutters>
          {DEMO_MODE_ENABLED && (
            <Box sx={{ mb: 3 }}>
              <DemoModeWarning />
            </Box>
          )}
          <Paper
            component={Stack}
            spacing={3}
            sx={{
              p: { xs: 3, sm: 4 },
              margin: 'auto',
              width: '100%',
              alignItems: 'center',
            }}
          >
            <AppName />
            {atlasLoginFailed && (
              <Alert severity="error">未能完成 AtlasApple 登入，請再試一次。</Alert>
            )}
            <SignInForm returnUrl={returnUrl} navigate={navigate} />
          </Paper>
        </Container>
      </Center>
    </>
  );
};
