import { screen, render as rtlRender } from '@testing-library/react';
import { type ComponentProps } from 'react';
import { type Product, productModel } from '@/entities/product';
import { render } from '@tests/render';
import ProductsTable from './ProductsTable';

type OnCheckedChangeFn = ComponentProps<typeof ProductsTable>['onCheckedChange'];

const givenProduct = (product: Partial<Product>): Product => ({
  id: 1,
  name: '雞蛋',
  caloriesCost: 143,
  defaultQuantity: 55,
  quantityUnit: productModel.QuantityUnit.Servings,
  categories: [],
  protein: null,
  fats: null,
  carbs: null,
  sugar: null,
  salt: null,
  isFavorite: false,
  ...product,
});

describe('when products are empty', () => {
  test('should show empty data message', () => {
    rtlRender(
      <ProductsTable products={[]} checkedIds={[]} onCheckedChange={vi.fn<OnCheckedChangeFn>()} />,
    );

    expect(screen.getByText(/找不到食物/));
  });
});

test('should show every category of a product with short Chinese names', async () => {
  const product = givenProduct({
    categories: [
      { id: 1, name: '飯' },
      { id: 2, name: '肉類' },
    ],
  });

  await render(
    <ProductsTable
      products={[product]}
      checkedIds={[]}
      onCheckedChange={vi.fn<OnCheckedChangeFn>()}
    />,
  );

  const categories = await screen.findByRole('list', { name: '「雞蛋」的分類' });
  expect(categories).toHaveTextContent('飯肉類');
});

test('should not show a category list for a product without categories', async () => {
  await render(
    <ProductsTable
      products={[givenProduct({ categories: [] })]}
      checkedIds={[]}
      onCheckedChange={vi.fn<OnCheckedChangeFn>()}
    />,
  );

  expect(await screen.findByText('雞蛋'));
  expect(screen.queryByRole('list', { name: '「雞蛋」的分類' })).not.toBeInTheDocument();
});

test.each([
  [productModel.QuantityUnit.Servings, '每份 55 g'],
  [productModel.QuantityUnit.Grams, '55 g'],
])('should show the default quantity of a product counted in unit %i', async (unit, text) => {
  await render(
    <ProductsTable
      products={[givenProduct({ quantityUnit: unit })]}
      checkedIds={[]}
      onCheckedChange={vi.fn<OnCheckedChangeFn>()}
    />,
  );

  expect(await screen.findByLabelText(`「雞蛋」的份量為 ${text}`)).toHaveTextContent(text);
});
