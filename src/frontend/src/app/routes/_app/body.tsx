import { createFileRoute } from '@tanstack/react-router';
import { BodyCompositionPage } from '@/pages/ui/gym/BodyCompositionPage';

export const Route = createFileRoute('/_app/body')({
  staticData: { appBar: { variant: 'menu', title: '身體' } },
  component: BodyCompositionPage,
});
