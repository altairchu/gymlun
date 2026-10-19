import { type FC } from 'react';
import { PageContainer } from '@/shared/ui';
import { WorkoutDayList } from '@/widgets/WorkoutDay';

interface Props {
  date: string;
}

export const WorkoutsPage: FC<Props> = ({ date }) => (
  <PageContainer>
    <WorkoutDayList date={date} />
  </PageContainer>
);
