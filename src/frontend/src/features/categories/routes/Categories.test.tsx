import { screen, waitFor, waitForElementToBeRemoved, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { render } from '@tests/render';
import Categories from './Categories';

test('categories are displayed with their product counts', async () => {
  await render(<Categories />);

  expect(await screen.findByText(/dairy/i));
  expect(screen.getByText(/bakery/i));
  expect(screen.getByText(/frozen foods/i));
  expect(screen.getByLabelText(/dairy：2 款食物/i));
  expect(screen.getByLabelText(/bakery：1 款食物/i));
  expect(screen.getByLabelText(/frozen foods：沒有食物/i));
});

test('category can be created', async () => {
  await render(<Categories />);

  await userEvent.click(await screen.findByLabelText(/新增分類/));
  const dialog = within(screen.getByRole('dialog'));
  await userEvent.type(dialog.getByPlaceholderText(/分類名稱/), 'New fancy category');
  await userEvent.click(dialog.getByLabelText(/新增「new fancy category」/i));
  await waitForElementToBeRemoved(screen.queryByRole('dialog'));

  expect(screen.getByText(/new fancy category/i));
  expect(screen.getByLabelText(/new fancy category：沒有食物/i));
});

test('category can be edited', async () => {
  await render(<Categories />);

  await userEvent.click(await screen.findByLabelText(/編輯「cereals」/i));
  const dialog = within(screen.getByRole('dialog'));
  const name = dialog.getByPlaceholderText(/分類名稱/);
  await userEvent.clear(name);
  await userEvent.type(name, 'Modified Cereals');
  await userEvent.click(dialog.getByRole('button', { name: /儲存「modified cereals」/i }));

  expect(await screen.findByText(/modified cereals/i));
});

test('category can be deleted', async () => {
  await render(<Categories />);

  await userEvent.click(await screen.findByLabelText(/刪除「cereals」/i));
  const dialog = within(screen.getByRole('dialog'));
  await userEvent.click(dialog.getByLabelText(/刪除「cereals」/i));
  await waitForElementToBeRemoved(screen.queryByRole('dialog'));

  await waitFor(() => expect(screen.queryByText(/cereals/i)).not.toBeInTheDocument());
});
