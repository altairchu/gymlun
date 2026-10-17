import { type ProductFormValues } from './productSchema';

const SCALED_FIELDS = ['protein', 'fats', 'carbs', 'sugar', 'salt'] as const;

type ScaledValues = Pick<ProductFormValues, 'calories' | (typeof SCALED_FIELDS)[number]>;

const roundToHundredths = (value: number): number => Math.round(value * 100) / 100;

const scale = (values: ScaledValues, factor: number): ScaledValues => {
  const scaled: ScaledValues = { ...values, calories: Math.round(values.calories * factor) };

  SCALED_FIELDS.forEach(field => {
    const value = values[field];
    scaled[field] = value === null ? null : roundToHundredths(value * factor);
  });

  return scaled;
};

export const toPerServing = (valuesPer100g: ScaledValues, servingGrams: number): ScaledValues =>
  scale(valuesPer100g, servingGrams / 100);

export const toPer100g = (valuesPerServing: ScaledValues, servingGrams: number): ScaledValues =>
  scale(valuesPerServing, 100 / servingGrams);
