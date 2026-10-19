import { getRouteApi } from '@tanstack/react-router';
import { type FC } from 'react';
import { SelectDateView } from '@/features/note/selectDate';
import { dateLib } from '@/shared/lib';

const route = getRouteApi('/_app/workouts');

export const WorkoutsDateSwitcherSlot: FC = () => {
  const { date } = route.useLoaderDeps();
  const navigate = route.useNavigate();

  return (
    <SelectDateView
      currentDate={new Date(date)}
      onSubmitDate={newDate => {
        navigate({ search: { date: dateLib.formatToISOStringWithoutTime(newDate) } });
      }}
    />
  );
};
