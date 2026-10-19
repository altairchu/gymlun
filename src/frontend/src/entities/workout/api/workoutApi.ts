import { api } from '@/shared/api';
import { createUrl } from '@/shared/lib';
import {
  type GetExercisesResponse,
  type GetWorkoutSetsRequest,
  type GetWorkoutSetsResponse,
  type LogWorkoutSetsRequest,
  type LogWorkoutSetsResponse,
  type UpdateWorkoutSetArgs,
} from './contracts';

export const workoutApi = api.injectEndpoints({
  endpoints: create => ({
    workoutSets: create.query<GetWorkoutSetsResponse, GetWorkoutSetsRequest>({
      query: request => createUrl('/api/v1/workout-sets', { ...request }),
      providesTags: ['workoutSet'],
    }),

    exercises: create.query<GetExercisesResponse, void>({
      query: () => '/api/v1/workout-sets/exercises',
      providesTags: ['workoutSet'],
    }),

    logWorkoutSets: create.mutation<LogWorkoutSetsResponse, LogWorkoutSetsRequest>({
      query: request => ({
        method: 'POST',
        url: '/api/v1/workout-sets',
        body: request,
      }),
      invalidatesTags: ['workoutSet'],
    }),

    updateWorkoutSet: create.mutation<void, UpdateWorkoutSetArgs>({
      query: ({ id, request }) => ({
        method: 'PUT',
        url: `/api/v1/workout-sets/${id}`,
        body: request,
      }),
      invalidatesTags: ['workoutSet'],
    }),

    deleteWorkoutSet: create.mutation<void, number>({
      query: id => ({
        method: 'DELETE',
        url: `/api/v1/workout-sets/${id}`,
      }),
      invalidatesTags: ['workoutSet'],
    }),
  }),
});
