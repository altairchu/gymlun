import { Typography } from '@mui/material';
import { type FC } from 'react';
import { RouterLink } from '@/shared/ui';

export const NotFoundPage: FC = () => {
  return (
    <>
      <Typography variant="h1" gutterBottom>
        找不到頁面
      </Typography>
      <RouterLink to="/">返回日記</RouterLink>
    </>
  );
};
