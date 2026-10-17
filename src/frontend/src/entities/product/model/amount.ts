import { QuantityUnit, fromGrams, toGrams } from './quantityUnit';

export const MIN_GRAMS = 1;
export const MAX_GRAMS = 5000;

export const parseAmount = (amount: string): number | null => {
  const value = Number(amount.trim().replace(',', '.'));
  return amount.trim() !== '' && Number.isFinite(value) && value > 0 ? value : null;
};

export const amountToGrams = (
  amount: string,
  unit: QuantityUnit,
  servingGrams: number,
): number | null => {
  const value = parseAmount(amount);

  if (value === null) {
    return null;
  }

  const grams = toGrams(value, unit, servingGrams);
  return grams >= MIN_GRAMS && grams <= MAX_GRAMS ? grams : null;
};

export const gramsToAmount = (grams: number, unit: QuantityUnit, servingGrams: number): string =>
  fromGrams(grams, unit, servingGrams).toString();

export const defaultAmount = (unit: QuantityUnit, servingGrams: number): string =>
  unit === QuantityUnit.Servings ? '1' : servingGrams.toString();

export const convertAmount = (
  amount: string,
  from: QuantityUnit,
  to: QuantityUnit,
  servingGrams: number,
): string => {
  const value = parseAmount(amount);

  if (value === null) {
    return defaultAmount(to, servingGrams);
  }

  return gramsToAmount(toGrams(value, from, servingGrams), to, servingGrams);
};

const GRAMS_STEP = 10;

export const stepAmount = (amount: string, unit: QuantityUnit, direction: 1 | -1): string => {
  const value = parseAmount(amount);

  if (unit === QuantityUnit.Servings) {
    if (value === null) {
      return '1';
    }

    if (direction < 0) {
      return (value > 1 ? value - 1 : 0.5).toString();
    }

    return (value < 1 ? 1 : value + 1).toString();
  }

  if (value === null) {
    return GRAMS_STEP.toString();
  }

  return Math.max(MIN_GRAMS, Math.round(value) + direction * GRAMS_STEP).toString();
};

export const caloriesOf = (caloriesPer100g: number, grams: number): number =>
  Math.floor((caloriesPer100g * grams) / 100);
