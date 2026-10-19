import { createFileRoute } from '@tanstack/react-router';
import { lazy } from 'react';
import { z } from 'zod';
import { workoutApi } from '@/entities/workout';
import { WorkoutsPage } from '@/pages/ui/WorkoutsPage';
import { getDefaultDate } from './-getDefaultDate';

const WorkoutsDateSwitcherSlot = lazy(() =>
  import('./-WorkoutsDateSwitcherSlot').then(module => ({
    default: module.WorkoutsDateSwitcherSlot,
  })),
);

export const Route = createFileRoute('/_app/workouts')({
  validateSearch: z.object({
    date: z.iso.date().optional().catch(undefined),
  }),
  staticData: {
    appBar: {
      variant: 'menu',
      title: { Component: WorkoutsDateSwitcherSlot },
    },
  },
  loaderDeps: ({ search }) => ({ date: search.date ?? getDefaultDate() }),
  loader: async ({ context, deps, preload }) => {
    const workoutSetsQueryPromise = context.store.dispatch(
      workoutApi.endpoints.workoutSets.initiate(
        { from: deps.date, to: deps.date },
        { forceRefetch: !preload },
      ),
    );

    try {
      await workoutSetsQueryPromise;
    } finally {
      workoutSetsQueryPromise.unsubscribe();
    }
  },
  component: WorkoutsRoute,
});

function WorkoutsRoute() {
  const { date } = Route.useLoaderDeps();

  return <WorkoutsPage date={date} />;
}
