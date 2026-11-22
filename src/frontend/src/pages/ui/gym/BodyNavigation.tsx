import { Link } from '@tanstack/react-router';
import { type FC } from 'react';

const tabs = [
  { to: '/body', label: '量度' },
  { to: '/body-analysis', label: '數據分析' },
  { to: '/weight', label: '體重趨勢' },
] as const;

export const BodyNavigation: FC = () => (
  <nav className="gym-body-tabs" aria-label="身體記錄">
    {tabs.map(tab => (
      <Link key={tab.to} to={tab.to} activeOptions={{ exact: true }}>
        {tab.label}
      </Link>
    ))}
  </nav>
);
