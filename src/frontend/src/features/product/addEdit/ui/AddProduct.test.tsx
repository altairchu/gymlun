import { screen, waitForElementToBeRemoved, within } from '@testing-library/react';
import userEvent, { type UserEvent } from '@testing-library/user-event';
import { http, HttpResponse } from 'msw';
import { type ComponentProps } from 'react';
import {
  type CreateProductRequest,
  type CreateProductResponse,
  productModel,
} from '@/entities/product';
import { API_URL } from '@/shared/config';
import { server } from '@tests/mockApi/server';
import { render } from '@tests/render';
import { AddProduct } from './AddProduct';

type OnCreatedFn = NonNullable<ComponentProps<typeof AddProduct>['onCreated']>;

const givenProductCreatedWithId = (id: number): CreateProductRequest[] => {
  const requests: CreateProductRequest[] = [];

  server.use(
    http.post(`${API_URL}/api/v1/products`, async ({ request }) => {
      requests.push((await request.json()) as CreateProductRequest);
      return HttpResponse.json<CreateProductResponse>({ id });
    }),
  );

  return requests;
};

const whenProductSaved = async (user: UserEvent): Promise<void> => {
  await user.click(screen.getByRole('button', { name: '加入' }));
  await waitForElementToBeRemoved(() => screen.queryByRole('dialog'));
};

test('the default trigger opens an empty new product dialog', async () => {
  const user = userEvent.setup();
  await render(<AddProduct />);

  await user.click(await screen.findByRole('button', { name: '加入新食物' }));

  const dialog = within(await screen.findByRole('dialog', { name: '新食物' }));
  expect(dialog.getByRole('textbox', { name: '名稱' })).toHaveValue('');
});

test('a custom trigger opens the dialog with the name pre-filled', async () => {
  const user = userEvent.setup();
  await render(
    <AddProduct
      initialName="雞翼"
      renderTrigger={openDialog => (
        <button type="button" onClick={openDialog}>
          新增「雞翼」
        </button>
      )}
    />,
  );

  expect(screen.queryByRole('button', { name: '加入新食物' })).not.toBeInTheDocument();
  await user.click(await screen.findByRole('button', { name: '新增「雞翼」' }));

  const dialog = within(await screen.findByRole('dialog', { name: '新食物' }));
  expect(dialog.getByRole('textbox', { name: '名稱' })).toHaveValue('雞翼');
});

test('the id of the created product is reported once the dialog closes', async () => {
  const user = userEvent.setup();
  const onCreated = vi.fn<OnCreatedFn>();
  givenProductCreatedWithId(99);
  await render(<AddProduct initialName="雞翼" onCreated={onCreated} />);

  await user.click(await screen.findByRole('button', { name: '加入新食物' }));
  await screen.findByRole('dialog', { name: '新食物' });
  await whenProductSaved(user);

  expect(onCreated).toHaveBeenCalledExactlyOnceWith(99);
});

test('the created product is sent with its categories and counting unit', async () => {
  const user = userEvent.setup();
  const requests = givenProductCreatedWithId(99);
  await render(<AddProduct initialName="雞翼" onCreated={vi.fn<OnCreatedFn>()} />);

  await user.click(await screen.findByRole('button', { name: '加入新食物' }));
  const dialog = within(await screen.findByRole('dialog', { name: '新食物' }));
  await user.click(dialog.getByRole('button', { name: '按件／份' }));
  await user.clear(dialog.getByRole('textbox', { name: '每份重量 (g)' }));
  await user.type(dialog.getByRole('textbox', { name: '每份重量 (g)' }), '40');
  await user.type(dialog.getByRole('combobox', { name: /分類/ }), '肉類');
  await user.click(await screen.findByRole('option', { name: '肉類' }));
  await user.type(dialog.getByRole('combobox', { name: /分類/ }), '炸物');
  await user.click(await screen.findByRole('option', { name: '新增分類「炸物」' }));
  expect(await dialog.findByRole('button', { name: '炸物' }));
  await whenProductSaved(user);

  expect(requests).toStrictEqual<CreateProductRequest[]>([
    {
      name: '雞翼',
      caloriesCost: 100,
      defaultQuantity: 40,
      quantityUnit: productModel.QuantityUnit.Servings,
      categoryIds: [8, 11],
      protein: null,
      fats: null,
      carbs: null,
      sugar: null,
      salt: null,
      isFavorite: false,
    },
  ]);
});
