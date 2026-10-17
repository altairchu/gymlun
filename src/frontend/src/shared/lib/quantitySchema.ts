import { z } from 'zod';

const QUANTITY_ERROR = { error: '請輸入 10 至 1000 之間的整數' };

export const quantitySchema = z.coerce
  .number<number>(QUANTITY_ERROR)
  .int(QUANTITY_ERROR)
  .min(10, QUANTITY_ERROR)
  .max(1000, QUANTITY_ERROR);
