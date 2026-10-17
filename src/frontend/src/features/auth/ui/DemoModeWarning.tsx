import { Alert, AlertTitle, Typography } from '@mui/material';
import { type FC } from 'react';

export const DemoModeWarning: FC = () => (
  <Alert severity="warning">
    <AlertTitle>示範模式已啟用</AlertTitle>
    <Typography component="p" variant="body2" gutterBottom>
      此應用程式正以示範模式運行。所有資料均為模擬資料，部分功能可能無法正常運作
    </Typography>
    <Typography component="p" variant="body2">
      請按登入按鈕繼續，你將以虛擬用戶身分登入
    </Typography>
  </Alert>
);
