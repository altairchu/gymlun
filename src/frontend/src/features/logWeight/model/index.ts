import { z } from 'zod';
import { weightSchema } from './weightSchema';

export const schema = z.object({
  date: z.date({ error: '請選擇有效日期' }),
  weight: weightSchema,
});

export type FormValues = z.infer<typeof schema>;
