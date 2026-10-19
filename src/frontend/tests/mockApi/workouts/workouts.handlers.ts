import { http, type HttpHandler, type PathParams } from 'msw';
import {
  type GetExercisesResponse,
  type GetWorkoutSetsResponse,
  type LogWorkoutSetsRequest,
  type LogWorkoutSetsResponse,
  type UpdateWorkoutSetRequest,
} from '@/entities/workout';
import { API_URL } from '@/shared/config';
import { DelayedHttpResponse } from '../DelayedHttpResponse';
import * as workoutsService from './workouts.service';

export const handlers: HttpHandler[] = [
  http.get(`${API_URL}/api/v1/workout-sets`, async ({ request }) => {
    const url = new URL(request.url);
    const from = url.searchParams.get('from');
    const to = url.searchParams.get('to');

    if (from === null || to === null || !workoutsService.isValidRange(from, to)) {
      return await DelayedHttpResponse.badRequest();
    }

    return await DelayedHttpResponse.json<GetWorkoutSetsResponse>({
      workoutSets: workoutsService
        .getInRange(from, to)
        .map(({ id, date, exercise, reps, weightKg }) => ({ id, date, exercise, reps, weightKg })),
    });
  }),

  http.get(`${API_URL}/api/v1/workout-sets/exercises`, async () =>
    DelayedHttpResponse.json<GetExercisesResponse>({
      exercises: workoutsService.getExercises(),
    }),
  ),

  http.post<PathParams, LogWorkoutSetsRequest>(
    `${API_URL}/api/v1/workout-sets`,
    async ({ request }) => {
      const ids = workoutsService.log(await request.json());

      if (!ids) {
        return await DelayedHttpResponse.badRequest();
      }

      return await DelayedHttpResponse.json<LogWorkoutSetsResponse>({ ids });
    },
  ),

  http.put<{ id: string }, UpdateWorkoutSetRequest>(
    `${API_URL}/api/v1/workout-sets/:id`,
    async ({ params, request }) => {
      const result = workoutsService.update(parseInt(params.id), await request.json());

      switch (result) {
        case 'Success':
          return await DelayedHttpResponse.ok();
        case 'NotFound':
          return await DelayedHttpResponse.notFound();
        case 'Invalid':
          return await DelayedHttpResponse.badRequest();
      }
    },
  ),

  http.delete<{ id: string }>(`${API_URL}/api/v1/workout-sets/:id`, async ({ params }) =>
    workoutsService.deleteOne(parseInt(params.id))
      ? await DelayedHttpResponse.noContent()
      : await DelayedHttpResponse.notFound(),
  ),
];
