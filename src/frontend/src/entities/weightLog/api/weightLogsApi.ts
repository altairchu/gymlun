import { api } from '@/shared/api';
import { createUrl } from '@/shared/lib';

export interface GetWeightLogsResponse {
  weightLogs: WeightLogItem[];
}

export interface GetWeightLogsRequest {
  from: string;
  to: string;
}

export interface WeightLogItem {
  date: string;
  value: number;
}

export interface WeightLogBody {
  date: string;
  value: number;
}

export interface UpdateWeightLogBody {
  value: number;
}

export interface UpdateWeightLogRequest {
  date: string;
  body: UpdateWeightLogBody;
}

export const weightLogsApi = api.injectEndpoints({
  endpoints: create => ({
    weightLogs: create.query<GetWeightLogsResponse, GetWeightLogsRequest>({
      query: request => ({
        method: 'GET',
        url: createUrl('/api/weight-logs', { ...request }),
      }),
      providesTags: ['weightLog'],
    }),

    add: create.mutation<void, WeightLogBody>({
      query: body => ({
        method: 'POST',
        url: '/api/weight-logs',
        body,
      }),
      invalidatesTags: ['weightLog'],
    }),

    update: create.mutation<void, UpdateWeightLogRequest>({
      query: ({ date, body }) => ({
        method: 'PUT',
        url: `/api/weight-logs/${date}`,
        body,
      }),
      invalidatesTags: ['weightLog'],
    }),

    delete: create.mutation<void, string>({
      query: date => ({
        method: 'DELETE',
        url: `/api/weight-logs/${date}`,
      }),
      invalidatesTags: ['weightLog'],
    }),
  }),
});
