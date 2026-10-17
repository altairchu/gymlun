import { screen, waitFor, waitForElementToBeRemoved, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { render } from '@tests/render';
import Products from './Products';

test('products are loaded into table', async () => {
  await render(<Products />);

  const bread = await screen.findByText(/bread/i);

  expect(bread).toBeVisible();
  expect(screen.getByLabelText(/「bread」的卡路里為 250/i)).toBeVisible();
  expect(screen.getByLabelText(/「bread」屬於「cereals」分類/i)).toBeVisible();
});

test('product can be selected', async () => {
  await render(<Products />);

  const breadCheckbox = await screen.findByLabelText(/選取「bread」/i);
  await userEvent.click(breadCheckbox);

  expect(screen.getByText(/已選取 1 項/));
});

test('all products can be selected', async () => {
  await render(<Products />);

  const selectAll = await screen.findByLabelText(/全選/);
  await waitFor(() => expect(selectAll).toBeEnabled());
  await userEvent.click(selectAll);

  expect(screen.getByText(/已選取 (\d)+ 項/));
});

test('products can be deleted', async () => {
  await render(<Products />);

  const breadCheckbox = await screen.findByLabelText(/選取「bread」/i);
  await userEvent.click(breadCheckbox);
  await userEvent.click(screen.getByLabelText(/刪除已選取的食物/));
  const dialog = within(screen.getByRole('dialog'));
  await userEvent.click(dialog.getByRole('button', { name: '是' }));
  await waitForElementToBeRemoved(screen.queryByRole('dialog'));

  await waitFor(() => expect(screen.queryByText(/bread/i)).not.toBeInTheDocument());
  expect(screen.queryByText(/已選取 (\d)+ 項/)).not.toBeInTheDocument();
});

test('products can be filtered by category', async () => {
  const user = userEvent.setup();
  await render(<Products />);

  const categoryField = await screen.findByLabelText(/^分類$/);
  await user.click(categoryField);
  const categorySelect = await screen.findByRole('listbox', { name: /^分類$/ });
  const cereals = within(categorySelect).getByRole('option', { name: /cereals/i });
  await user.click(cereals);

  expect(await screen.findByText(/oats/i));
  await waitFor(() => expect(screen.queryByText(/milk/i)).not.toBeInTheDocument());
});

test('products can be filtered by name', async () => {
  const user = userEvent.setup();

  await render(<Products />);
  const searchField = await screen.findByPlaceholderText(/按名稱搜尋/);
  await user.type(searchField, 'bre');

  expect(screen.getByText(/bread/i));
  expect(screen.queryByText(/rice/i)).not.toBeInTheDocument();
});

test('products in table are split by pages', async () => {
  await render(<Products />, { pageSizeOverride: 2 });

  const paginationForTheFirstPage = await screen.findByText(/第 1 項至第 .* 項/);
  expect(paginationForTheFirstPage).toBeVisible();

  await userEvent.click(screen.getByLabelText(/下一頁/));
  const paginationForTheSecondPage = await screen.findByText(/第 .* 項至第 .* 項/);
  expect(paginationForTheSecondPage).not.toHaveTextContent(/第 1 項至第 .* 項/);
});
