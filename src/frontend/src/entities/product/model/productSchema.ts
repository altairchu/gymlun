import { z } from 'zod';
import { NutritionValueSchema } from './NutritionValueSchema';
import { QuantityUnit } from './quantityUnit';

export const MAX_PRODUCT_CATEGORIES = 10;

const WHOLE_NUMBER_ERROR = { error: '請輸入 1 至 1000 之間的整數' };

const wholeNumberFrom1To1000 = z.coerce
  .number<number>(WHOLE_NUMBER_ERROR)
  .int(WHOLE_NUMBER_ERROR)
  .min(1, WHOLE_NUMBER_ERROR)
  .max(1000, WHOLE_NUMBER_ERROR);

export const productNameSchema = z
  .string()
  .trim()
  .refine(name => name.length >= 1 && name.length <= 100, {
    error: '食物名稱須為 1 至 100 個字元',
  });

export const productSchema = z.object({
  id: z.number().optional(),
  name: productNameSchema,
  defaultQuantity: wholeNumberFrom1To1000,
  quantityUnit: z.enum(QuantityUnit),
  categories: z
    .array(
      z.object({
        id: z.number(),
        name: z.string(),
      }),
    )
    .max(MAX_PRODUCT_CATEGORIES, { error: `最多可選 ${MAX_PRODUCT_CATEGORIES} 個分類` }),
  calories: wholeNumberFrom1To1000,
  protein: NutritionValueSchema,
  fats: NutritionValueSchema,
  carbs: NutritionValueSchema,
  sugar: NutritionValueSchema,
  salt: NutritionValueSchema,
  isFavorite: z.boolean(),
});

export type ProductFormValues = z.infer<typeof productSchema>;
