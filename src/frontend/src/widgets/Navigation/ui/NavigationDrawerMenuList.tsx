import CalendarMonthIcon from '@mui/icons-material/CalendarMonth';
import CalendarTodayIcon from '@mui/icons-material/CalendarToday';
import CategoryIcon from '@mui/icons-material/Category';
import MonitorWeightIcon from '@mui/icons-material/MonitorWeight';
import RestaurantIcon from '@mui/icons-material/Restaurant';
import { Box, List, ListItem, ListItemIcon, ListItemText } from '@mui/material';
import { linkOptions } from '@tanstack/react-router';
import { type FC } from 'react';
import { RouterListItemButton } from '@/shared/ui';

const NAV_LINKS = linkOptions([
  {
    icon: <CalendarTodayIcon />,
    title: '今日',
    to: '/',
    activeOptions: { exact: true, includeSearch: false },
  },
  {
    icon: <CalendarMonthIcon />,
    title: '歷史記錄',
    to: '/history',
  },
  {
    icon: <MonitorWeightIcon />,
    title: '體重',
    to: '/weight',
  },
  {
    icon: <RestaurantIcon />,
    title: '食物',
    to: '/products',
  },
  {
    icon: <CategoryIcon />,
    title: '分類',
    to: '/categories',
  },
]);

export const NavigationDrawerMenuList: FC = () => (
  <List>
    {NAV_LINKS.map(({ icon, title, ...link }) => (
      <ListItem key={title} disablePadding>
        <RouterListItemButton
          {...link}
          activeProps={{ selected: true, disableTouchRipple: true }}
          sx={theme => ({
            '&.Mui-selected': {
              backgroundColor: theme.palette.action.selected,
              pointerEvents: 'none',
            },
          })}
        >
          <Box component={ListItemIcon}>{icon}</Box>
          <ListItemText primary={title} />
        </RouterListItemButton>
      </ListItem>
    ))}
  </List>
);
