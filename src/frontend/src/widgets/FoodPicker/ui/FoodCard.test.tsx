import { render, screen } from '@testing-library/react';
import { productModel, type ProductSelectOption } from '@/entities/product';
import { API_URL } from '@/shared/config';
import { FoodCard } from './FoodCard';

const dumplings: ProductSelectOption = {
  id: 4,
  name: '豬肉韭菜餃',
  defaultQuantity: 25,
  quantityUnit: productModel.QuantityUnit.Servings,
  categoryIds: [],
  calories: 220,
  protein: null,
  fats: null,
  carbs: null,
  sugar: null,
  salt: null,
  isFavorite: false,
  photoUrl: null,
};

test('a food with a photo shows it on its card', () => {
  render(
    <FoodCard
      product={{ ...dumplings, photoUrl: '/api/v1/products/4/photo?v=2' }}
      selectedLabel={null}
      onToggle={vi.fn<(product: ProductSelectOption) => void>()}
    />,
  );

  expect(screen.getByRole('img', { name: '「豬肉韭菜餃」的相片' })).toHaveAttribute(
    'src',
    `${API_URL}/api/v1/products/4/photo?v=2`,
  );
});

test('a food without a photo shows only its name and calories', () => {
  render(
    <FoodCard
      product={dumplings}
      selectedLabel={null}
      onToggle={vi.fn<(product: ProductSelectOption) => void>()}
    />,
  );

  expect(screen.getByRole('button', { name: /豬肉韭菜餃/ })).toHaveTextContent('55 kcal');
  expect(screen.queryByRole('img')).not.toBeInTheDocument();
});
