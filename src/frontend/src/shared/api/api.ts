import { createApi, fetchBaseQuery } from '@reduxjs/toolkit/query/react';
import { API_URL } from '@/shared/config';
import { getExpectedGymAccount } from './expectedAccount';

const query = fetchBaseQuery({
  baseUrl: API_URL,
  prepareHeaders: (headers, { arg }) => {
    const accountId = getExpectedGymAccount();
    const path = typeof arg === 'string' ? arg : arg.url;
    if (accountId !== null && path.startsWith('/api/') && !path.startsWith('/api/v1/auth/')) {
      headers.set('X-Gym-Expected-Account', accountId);
    }
    return headers;
  },
});

export const api = createApi({
  tagTypes: ['note', 'product', 'category', 'weightLog', 'workoutSet'],
  baseQuery: async (args, baseApi, extraOptions) => {
    const result = await query(args, baseApi, extraOptions);
    if (result.meta?.response?.headers.get('X-Gym-Account-Changed') === 'true') {
      window.location.reload();
    }
    return result;
  },
  refetchOnFocus: true,
  endpoints: () => ({}),
});
