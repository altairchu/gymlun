import { type PathParams, http, type HttpHandler } from 'msw';
import {
  type WeightLogBody,
  type GetWeightLogsResponse,
  type UpdateWeightLogBody,
} from '@/entities/weightLog';
import { API_URL } from '@/shared/config';
import { db } from '../db';
import { DelayedHttpResponse } from '../DelayedHttpResponse';

export const handlers: HttpHandler[] = [
  http.get(`${API_URL}/api/weight-logs`, ({ request }) => {
    const url = new URL(request.url);
    const from = url.searchParams.get('from');
    const to = url.searchParams.get('to');

    if (!from || !to) {
      return DelayedHttpResponse.badRequest();
    }

    return DelayedHttpResponse.json<GetWeightLogsResponse>({
      weightLogs: db.weightLog
        .findMany({
          where: {
            date: {
              gte: from,
              lte: to,
            },
          },
          orderBy: { date: 'desc' },
        })
        .map(({ date, value }) => ({ date, value })),
    });
  }),

  http.post<PathParams, WeightLogBody>(`${API_URL}/api/weight-logs`, async ({ request }) => {
    const { date, value } = await request.json();

    db.weightLog.create({
      date,
      value,
    });

    return await DelayedHttpResponse.ok();
  }),

  http.put<{ date: string }, UpdateWeightLogBody>(
    `${API_URL}/api/weight-logs/:date`,
    async ({ params, request }) => {
      const { value } = await request.json();

      const updated = db.weightLog.update({
        where: { date: { equals: params.date } },
        data: { value },
      });

      return updated ? await DelayedHttpResponse.ok() : await DelayedHttpResponse.notFound();
    },
  ),

  http.delete<{ date: string }>(`${API_URL}/api/weight-logs/:date`, async ({ params }) => {
    const deleted = db.weightLog.delete({
      where: { date: { equals: params.date } },
    });

    return deleted ? await DelayedHttpResponse.ok() : await DelayedHttpResponse.notFound();
  }),
];
