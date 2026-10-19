import { screen, waitForElementToBeRemoved, within } from '@testing-library/react';
import userEvent, { type UserEvent } from '@testing-library/user-event';
import { http, HttpResponse } from 'msw';
import { type Product, productModel } from '@/entities/product';
import { API_URL } from '@/shared/config';
import { server } from '@tests/mockApi/server';
import { render } from '@tests/render';
import { EditProduct } from './EditProduct';

const PHOTO_URL = '/api/v1/products/1/photo?v=7';

const bread: Product = {
  id: 1,
  name: 'Bread',
  caloriesCost: 250,
  defaultQuantity: 50,
  quantityUnit: productModel.QuantityUnit.Servings,
  categories: [],
  protein: null,
  fats: null,
  carbs: null,
  sugar: null,
  salt: null,
  isFavorite: false,
  photoUrl: PHOTO_URL,
};

const renderEditProduct = (product: Product): Promise<unknown> =>
  render(
    <EditProduct
      product={product}
      renderTrigger={openDialog => (
        <button type="button" onClick={openDialog}>
          編輯
        </button>
      )}
    />,
  );

const whenDialogOpened = async (user: UserEvent): Promise<HTMLElement> => {
  await user.click(await screen.findByRole('button', { name: '編輯' }));
  return screen.findByRole('dialog', { name: '編輯食物' });
};

test('the saved photo is shown in the dialog', async () => {
  const user = userEvent.setup();
  await renderEditProduct(bread);

  const dialog = within(await whenDialogOpened(user));

  expect(dialog.getByRole('img', { name: '食物相片' })).toHaveAttribute(
    'src',
    `${API_URL}${PHOTO_URL}`,
  );
  expect(dialog.getByRole('button', { name: '換相' })).toBeVisible();
});

test('a removed photo is deleted when the product is saved', async () => {
  const user = userEvent.setup();
  const deletedPhotoOf = vi.fn<(productId: string) => void>();
  server.use(
    http.delete<{ id: string }>(`${API_URL}/api/v1/products/:id/photo`, ({ params }) => {
      deletedPhotoOf(params.id);
      return new HttpResponse(null, { status: 204 });
    }),
  );
  await renderEditProduct(bread);

  const dialog = within(await whenDialogOpened(user));
  await user.click(dialog.getByRole('button', { name: '移除相' }));
  expect(dialog.getByRole('button', { name: '加相' })).toBeVisible();
  await user.click(dialog.getByRole('button', { name: '儲存' }));
  await waitForElementToBeRemoved(() => screen.queryByRole('dialog'));

  expect(deletedPhotoOf).toHaveBeenCalledExactlyOnceWith('1');
});

test('a photo that cannot be saved keeps the dialog open, and the product unchanged', async () => {
  const user = userEvent.setup();
  const editProduct = vi.fn<() => void>();
  server.use(
    http.put(`${API_URL}/api/v1/products/:id/photo`, () => new HttpResponse(null, { status: 400 })),
    http.put(`${API_URL}/api/v1/products/:id`, () => {
      editProduct();
      return new HttpResponse(null, { status: 200 });
    }),
  );
  await renderEditProduct(bread);

  const dialog = within(await whenDialogOpened(user));
  await user.upload(
    dialog.getByRole('button', { name: '換相' }),
    new File(['photo'], 'bread.jpg', { type: 'image/jpeg' }),
  );
  await user.click(dialog.getByRole('button', { name: '儲存' }));

  expect(await dialog.findByText('相片儲存唔到，請再試一次')).toBeVisible();
  expect(screen.getByRole('dialog', { name: '編輯食物' })).toBeVisible();
  expect(editProduct).not.toHaveBeenCalled();
});
