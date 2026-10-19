import { z } from 'zod';

const EXERCISE_MAX_LENGTH = 100;
const REPS_RANGE_ERROR = { error: '次數須係 1 至 1000 之間嘅整數' };
const WEIGHT_RANGE_ERROR = { error: '重量須介乎 0 至 1000 kg' };

export const MAX_SETS = 30;

export const exerciseSchema = z
  .string()
  .trim()
  .min(1, { error: '請輸入動作' })
  .max(EXERCISE_MAX_LENGTH, { error: `動作名稱唔可以多過 ${EXERCISE_MAX_LENGTH} 個字` });

export const repsSchema = z
  .string()
  .or(z.number())
  .transform(value => String(value).trim())
  .refine(value => /^\d+$/.test(value), REPS_RANGE_ERROR)
  .transform(Number)
  .pipe(z.number().int().gte(1, REPS_RANGE_ERROR).lte(1000, REPS_RANGE_ERROR));

export const weightKgSchema = z
  .string()
  .or(z.number())
  .transform(value => String(value).trim())
  .refine(value => /^\d+([.,]\d+)?$/.test(value), {
    error: '請輸入有效數字，小數點可用「.」或「,」',
  })
  .transform(value => Number(value.replace(',', '.')))
  .pipe(z.number().gte(0, WEIGHT_RANGE_ERROR).lte(1000, WEIGHT_RANGE_ERROR));
