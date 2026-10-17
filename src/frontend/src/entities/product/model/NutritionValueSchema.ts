import { z } from 'zod';

const RANGE_ERROR = { error: '數值須介乎 0 至 1000' };

export const NutritionValueSchema = z
  .union([z.string(), z.number(), z.null()])
  .transform(value => (value === null ? '' : String(value).trim()))
  .refine(value => value === '' || /^\d+([.,]\d{1,2})?$/.test(value), {
    error: '請輸入有效數字，最多兩位小數',
  })
  .transform(value => {
    if (value === '') {
      return null;
    }
    const normalized = value.replace(',', '.');
    const num = Number(normalized);
    return isNaN(num) ? null : num;
  })
  .pipe(z.number().min(0, RANGE_ERROR).max(1000, RANGE_ERROR).nullable());
