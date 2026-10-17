import { z } from 'zod';

const RANGE_ERROR = { error: '體重須介乎 1 至 500 kg' };

export const weightSchema = z
  .string()
  .or(z.number())
  .transform(value => String(value).trim())
  .refine(value => /^\d+([.,]\d+)?$/.test(value), {
    error: '請輸入有效數字，小數點可用「.」或「,」',
  })
  .transform(value => Number(value.replace(',', '.')))
  .pipe(z.number().gte(1, RANGE_ERROR).lte(500, RANGE_ERROR));
