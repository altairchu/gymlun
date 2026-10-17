import { Typography } from '@mui/material';
import { type FC } from 'react';
import { RouterLink } from '@/shared/ui';

export const ErrorPage: FC = () => {
  return (
    <>
      <Typography variant="h1" gutterBottom>
        哎呀！
      </Typography>
      <Typography component="p">抱歉，你要找的頁面不存在，或發生了意外錯誤</Typography>
      <RouterLink to="/">返回主頁</RouterLink>
    </>
  );
};
