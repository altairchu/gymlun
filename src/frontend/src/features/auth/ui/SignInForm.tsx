import { Box, Button } from '@mui/material';
import { type FC } from 'react';
import { authApi } from '../api/authApi';
import { type NavigateToHref, signIn } from '../lib';
import GoogleIcon from './GoogleIcon';

interface Props {
  returnUrl: string;
  navigate: NavigateToHref;
}

export const SignInForm: FC<Props> = ({ returnUrl, navigate }) => {
  const { data, isLoading, isError } = authApi.useGetStatusQuery({});
  const atlas = data?.provider === 'atlasApple';

  return (
    <Box
      sx={{
        display: 'flex',
        justifyContent: 'center',
      }}
    >
      <Button
        startIcon={atlas ? undefined : <GoogleIcon />}
        disabled={isLoading || isError}
        variant="outlined"
        onClick={() => {
          void signIn(returnUrl, navigate);
        }}
        sx={theme => ({
          width: '250px',
          textTransform: 'none',
          color: theme.palette.text.secondary,
          borderColor: theme.palette.divider,

          '&:hover': {
            borderColor: theme.palette.action.hover,
            backgroundColor: theme.palette.action.hover,
          },
        })}
      >
        {atlas ? '使用 AtlasApple 帳戶登入' : '使用 Google 登入'}
      </Button>
    </Box>
  );
};
