import { screen } from '@testing-library/react';
import { productModel } from '@/entities/product';
import { render } from '@tests/render';
import { NutritionSummaryWidget } from './NutritionSummaryWidget';

const nutritionValues: Record<productModel.NutritionValueType, number> = {
  calories: 2994,
  protein: 97.6,
  fats: 47.9,
  carbs: 146.3,
  sugar: 19.1,
  salt: 2.4,
};

const types: productModel.NutritionValueType[] = [
  'calories',
  'protein',
  'fats',
  'carbs',
  'sugar',
  'salt',
];

test.each(types)('the %s value is shown in the colour that identifies it', async type => {
  await render(<NutritionSummaryWidget nutritionValues={nutritionValues} />);

  const { color } = productModel.nutritionValuesConfig[type];

  expect(screen.getByText(nutritionValues[type].toString())).toHaveStyle({ color });
});

test('nutrient values are rounded to one decimal place', async () => {
  await render(<NutritionSummaryWidget nutritionValues={{ ...nutritionValues, protein: 97.58 }} />);

  expect(screen.getByText('97.6')).toBeVisible();
});

test('an unknown nutrient value is shown as a dash', async () => {
  await render(<NutritionSummaryWidget nutritionValues={{ ...nutritionValues, sugar: null }} />);

  expect(screen.getByText('—')).toBeVisible();
});
