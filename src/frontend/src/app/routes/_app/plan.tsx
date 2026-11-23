import { createFileRoute } from '@tanstack/react-router';
import { GymPlanPage } from '@/pages/ui/gym/GymPlanPage';

export const Route = createFileRoute('/_app/plan')({
  staticData: { appBar: { variant: 'menu', title: '目標' } },
  component: GymPlanPage,
});
