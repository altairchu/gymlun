import { Link, useLocation } from '@tanstack/react-router';
import { type FC } from 'react';
import { PulsoIcon } from '@/shared/ui/PulsoIcon/PulsoIcon';

const tabs = [
  { to: '/', label: '今日', icon: 'calendar-days' },
  { to: '/plan', label: '目標', icon: 'target' },
  { to: '/body', label: '身體', icon: 'scan-line' },
  { to: '/workouts', label: '訓練', icon: 'dumbbell' },
  { to: '/products', label: '食物庫', icon: 'cooking-pot' },
] as const;

export const GymNavigation: FC = () => {
  const pathname = useLocation({ select: location => location.pathname });
  const section =
    pathname === '/body-analysis' || pathname === '/weight'
      ? '/body'
      : pathname === '/history'
        ? '/'
        : pathname === '/categories'
          ? '/products'
          : pathname;

  return (
    <nav className="gym-navigation" aria-label="GymLun">
      {tabs.map(tab => (
        <Link
          key={tab.to}
          to={tab.to}
          activeOptions={{ exact: true, includeSearch: false }}
          data-selected={section === tab.to ? 'true' : undefined}
        >
          <PulsoIcon name={tab.icon} />
          <span>{tab.label}</span>
        </Link>
      ))}
    </nav>
  );
};
