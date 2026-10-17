export enum QuantityUnit {
  Grams = 1,
  Servings = 2,
}

export const QUANTITY_UNIT_SHORT_LABELS: Record<QuantityUnit, string> = {
  [QuantityUnit.Grams]: '克',
  [QuantityUnit.Servings]: '份',
};

const roundToTenths = (value: number): number => Math.round(value * 10) / 10;

export const toGrams = (amount: number, unit: QuantityUnit, servingGrams: number): number =>
  unit === QuantityUnit.Servings ? Math.round(amount * servingGrams) : Math.round(amount);

export const fromGrams = (grams: number, unit: QuantityUnit, servingGrams: number): number =>
  unit === QuantityUnit.Servings ? roundToTenths(grams / servingGrams) : grams;

export const formatAmount = (amount: number, unit: QuantityUnit): string =>
  unit === QuantityUnit.Servings ? `${amount} 份` : `${amount} g`;

export const formatQuantity = (grams: number, unit: QuantityUnit, servingGrams: number): string =>
  unit === QuantityUnit.Servings
    ? `${fromGrams(grams, unit, servingGrams)} 份 · ${grams} g`
    : `${grams} g`;
