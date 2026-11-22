import { createFileRoute } from '@tanstack/react-router';
import { GymConnectionsPage } from '@/pages/ui/gym/GymConnectionsPage';

export const Route = createFileRoute('/_app/ai-connect')({
  staticData: { appBar: { variant: 'menu', title: 'AI 連接' } },
  component: GymConnectionsPage,
});
