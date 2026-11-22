import CloseIcon from '@mui/icons-material/Close';
import MenuIcon from '@mui/icons-material/Menu';
import { Box, IconButton, Skeleton, Typography } from '@mui/material';
import { Link, useLocation, useMatches } from '@tanstack/react-router';
import { type FC, Suspense, useState } from 'react';
import { PulsoIcon } from '@/shared/ui/PulsoIcon/PulsoIcon';
import { type AppBarConfig } from '../model';
import { NavigationDrawer } from './NavigationDrawer';

interface AppBarTitleProps {
  title: AppBarConfig['title'];
}

const SlotSkeleton: FC = () => (
  <Typography variant="h6" component="span">
    <Skeleton variant="text" width={120} />
  </Typography>
);

const AppBarTitle: FC<AppBarTitleProps> = ({ title }) => (
  <Typography variant="h6" component="h1">
    {typeof title === 'string' ? (
      title
    ) : (
      <Suspense fallback={<SlotSkeleton />}>
        <title.Component />
      </Suspense>
    )}
  </Typography>
);

export const Navigation: FC = () => {
  const appBar = useMatches({ select: matches => matches.at(-1)?.staticData.appBar });
  const pathname = useLocation({ select: location => location.pathname });
  const [drawerVisible, setDrawerVisible] = useState(false);
  const [drawerPathname, setDrawerPathname] = useState(pathname);

  if (pathname !== drawerPathname) {
    setDrawerPathname(pathname);
    setDrawerVisible(false);
  }

  if (!appBar) {
    return null;
  }

  switch (appBar.variant) {
    case 'menu':
      return (
        <Box
          sx={{
            display: 'flex',
            justifyContent: 'space-between',
            alignItems: 'center',
            gap: 1,
          }}
        >
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 3 }}>
            <IconButton
              color="inherit"
              edge="start"
              aria-label="開啟選單"
              onClick={() => {
                setDrawerVisible(visible => !visible);
              }}
            >
              {drawerVisible ? <CloseIcon /> : <MenuIcon />}
            </IconButton>
            <NavigationDrawer
              visible={drawerVisible}
              toggle={() => {
                setDrawerVisible(false);
              }}
            />
            <AppBarTitle title={appBar.title} />
          </Box>
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
            {appBar.Actions ? (
              <Suspense fallback={<SlotSkeleton />}>
                <appBar.Actions />
              </Suspense>
            ) : null}
            <IconButton
              color="inherit"
              href="https://atlasapple.space/"
              aria-label="返回 AtlasApple"
            >
              <PulsoIcon name="arrow-left" />
            </IconButton>
            <IconButton color="inherit" component={Link} to="/ai-connect" aria-label="AI 連接">
              <PulsoIcon name="plug" />
            </IconButton>
          </Box>
        </Box>
      );
  }
};
