import { screen, waitFor, waitForElementToBeRemoved, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { render } from '@tests/render';
import Categories from './Categories';

test('categories are displayed with their product counts', async () => {
  await render(<Categories />);

  expect(await screen.findByText(/dairy/i));
  expect(screen.getByText(/bakery/i));
  expect(screen.getByText('高蛋白'));
  expect(screen.getByLabelText(/dairy：2 款食物/i));
  expect(screen.getByLabelText(/bakery：1 款食物/i));
  expect(screen.getByLabelText('肉類：2 款食物'));
  expect(screen.getByLabelText('高蛋白：5 款食物'));
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

test.each(['飯', '肉丸'])('category with a short Chinese name "%s" can be created', async name => {
  await render(<Categories />);

  await userEvent.click(await screen.findByLabelText(/新增分類/));
  const dialog = within(screen.getByRole('dialog'));
  await userEvent.type(dialog.getByPlaceholderText(/分類名稱/), name);
  await userEvent.click(dialog.getByLabelText(`新增「${name}」並關閉對話框`));
  await waitForElementToBeRemoved(screen.queryByRole('dialog'));

  expect(screen.getByLabelText(`${name}：沒有食物`));
});

test('category name is trimmed when created', async () => {
  await render(<Categories />);

  await userEvent.click(await screen.findByLabelText(/新增分類/));
  const dialog = within(screen.getByRole('dialog'));
  await userEvent.type(dialog.getByPlaceholderText(/分類名稱/), '  湯  ');
  await userEvent.click(dialog.getByLabelText('新增「湯」並關閉對話框'));
  await waitForElementToBeRemoved(screen.queryByRole('dialog'));

  expect(screen.getByLabelText('湯：沒有食物'));
});

test.each([
  ['only whitespace', '   '],
  ['longer than 64 characters', 'a'.repeat(65)],
])('category with a name of %s cannot be created', async (_, name) => {
  await render(<Categories />);

  await userEvent.click(await screen.findByLabelText(/新增分類/));
  const dialog = within(screen.getByRole('dialog'));
  await userEvent.type(dialog.getByPlaceholderText(/分類名稱/), name);

  expect(dialog.getByPlaceholderText(/分類名稱/)).toBeInvalid();
  expect(dialog.getByText('分類名稱須為 1 至 64 個字元'));
  expect(dialog.getByRole('button', { name: /^新增「.*」並關閉對話框$/ })).toBeDisabled();
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

test('category can be renamed to a 1-character Chinese name', async () => {
  await render(<Categories />);

  await userEvent.click(await screen.findByLabelText('編輯「飯麵」'));
  const dialog = within(screen.getByRole('dialog'));
  const name = dialog.getByPlaceholderText(/分類名稱/);
  await userEvent.clear(name);
  await userEvent.type(name, '麵');
  await userEvent.click(dialog.getByRole('button', { name: '儲存「麵」並關閉對話框' }));

  expect(await screen.findByLabelText('麵：1 款食物'));
});

test('category can be deleted', async () => {
  await render(<Categories />);

  await userEvent.click(await screen.findByLabelText(/刪除「cereals」/i));
  const dialog = within(screen.getByRole('dialog'));
  await userEvent.click(dialog.getByLabelText(/刪除「cereals」/i));
  await waitForElementToBeRemoved(screen.queryByRole('dialog'));

  await waitFor(() => expect(screen.queryByText(/cereals/i)).not.toBeInTheDocument());
});
