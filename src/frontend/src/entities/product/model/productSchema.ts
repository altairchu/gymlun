import { z } from 'zod';
import { quantitySchema } from '@/shared/lib';
import { NutritionValueSchema } from './NutritionValueSchema';

const CALORIES_ERROR = { error: '請輸入 1 至 1000 之間的整數' };

export const productNameSchema = z
  .string()
  .trim()
  .refine(name => name.length >= 1 && name.length <= 100, {
    error: '食物名稱須為 1 至 100 個字元',
  });

export const productSchema = z.object({
  id: z.number().optional(),
  name: productNameSchema,
  defaultQuantity: quantitySchema,
  category: z
    .object({
      id: z.number(),
      name: z.string(),
    })
    .nullable()
    .refine((category): boolean => category !== null, { error: '請選擇分類' }),
  calories: z.coerce
    .number<number>(CALORIES_ERROR)
    .int(CALORIES_ERROR)
    .min(1, CALORIES_ERROR)
    .max(1000, CALORIES_ERROR),
  protein: NutritionValueSchema,
  fats: NutritionValueSchema,
  carbs: NutritionValueSchema,
  sugar: NutritionValueSchema,
  salt: NutritionValueSchema,
  isFavorite: z.boolean(),
});

export type ProductFormValues = z.infer<typeof productSchema>;
