import { createFileRoute } from '@tanstack/react-router';
import { BodyAnalysisPage } from '@/pages/ui/gym/BodyAnalysisPage';

export const Route = createFileRoute('/_app/body-analysis')({
  staticData: { appBar: { variant: 'menu', title: '身體' } },
  component: BodyAnalysisPage,
});
