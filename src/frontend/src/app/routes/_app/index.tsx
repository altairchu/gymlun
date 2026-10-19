import { createFileRoute } from '@tanstack/react-router';
import { lazy } from 'react';
import { z } from 'zod';
import { noteApi } from '@/entities/note';
import { IndexPage } from '@/pages/ui/IndexPage';
import { getDefaultDate } from './-getDefaultDate';

const DateSwitcherSlot = lazy(() =>
  import('./-DateSwitcherSlot').then(module => ({ default: module.DateSwitcherSlot })),
);

export const Route = createFileRoute('/_app/')({
  validateSearch: z.object({
    date: z.iso.date().optional().catch(undefined),
  }),
  staticData: {
    appBar: {
      variant: 'menu',
      title: { Component: DateSwitcherSlot },
    },
  },
  loaderDeps: ({ search }) => ({ date: search.date ?? getDefaultDate() }),
  loader: async ({ context, deps, preload }) => {
    const notesQueryPromise = context.store.dispatch(
      noteApi.endpoints.notes.initiate({ date: deps.date }, { forceRefetch: !preload }),
    );

    try {
      await notesQueryPromise;
    } finally {
      notesQueryPromise.unsubscribe();
    }
  },
  component: IndexRoute,
});

function IndexRoute() {
  const { date } = Route.useLoaderDeps();

  return <IndexPage date={date} />;
}
