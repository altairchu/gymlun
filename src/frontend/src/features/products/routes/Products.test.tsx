import { screen, waitFor, waitForElementToBeRemoved, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { render } from '@tests/render';
import Products from './Products';

test('products are loaded into table', async () => {
  await render(<Products />);

  const bread = await screen.findByText(/bread/i);

  expect(bread).toBeVisible();
  expect(screen.getByLabelText(/「bread」的卡路里為 250/i)).toBeVisible();
  expect(within(screen.getByRole('list', { name: '「Bread」的分類' })).getByText('Cereals'));
});

test('every category of a product is shown', async () => {
  await render(<Products />);

  const beefCategories = await screen.findByRole('list', { name: '「Beef」的分類' });

  expect(within(beefCategories).getAllByRole('listitem')).toHaveLength(2);
  expect(within(beefCategories).getByText('Meat'));
  expect(within(beefCategories).getByText('高蛋白'));
});

test('the default quantity shows the counting unit', async () => {
  await render(<Products />);

  expect(await screen.findByLabelText('「Bread」的份量為 每份 50 g')).toHaveTextContent(
    '每份 50 g',
  );
  expect(screen.getByLabelText('「Beef」的份量為 100 g')).toHaveTextContent('100 g');
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

  expect(screen.getByText(/已選取 10 項/));
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

test('products can be filtered by one of their several categories', async () => {
  const user = userEvent.setup();
  await render(<Products />);

  await user.click(await screen.findByLabelText(/^分類$/));
  const categorySelect = await screen.findByRole('listbox', { name: /^分類$/ });
  await user.click(within(categorySelect).getByRole('option', { name: '高蛋白' }));

  expect(await screen.findByText('雞胸肉'));
  expect(screen.getByText('Beef'));
  expect(screen.queryByText('Bread')).not.toBeInTheDocument();
});

test('products can be filtered by name', async () => {
  const user = userEvent.setup();

  await render(<Products />);
  const searchField = await screen.findByPlaceholderText(/按名稱搜尋/);
  await user.type(searchField, 'bre');

  await waitFor(() => expect(screen.queryByText(/beef/i)).not.toBeInTheDocument());
  expect(screen.getByText(/bread/i));
});

test('products can be filtered by a single Chinese character', async () => {
  const user = userEvent.setup();

  await render(<Products />);
  const searchField = await screen.findByPlaceholderText(/按名稱搜尋/);
  await user.type(searchField, '雞');

  await waitFor(() => expect(screen.queryByText(/beef/i)).not.toBeInTheDocument());
  expect(screen.getByText('雞蛋'));
  expect(screen.getByText('雞胸肉'));
});

test('products in table are split by pages', async () => {
  await render(<Products />, { pageSizeOverride: 2 });

  expect(await screen.findByText('第 1 項至第 2 項，共 15 項')).toBeVisible();

  await userEvent.click(screen.getByLabelText(/下一頁/));

  expect(await screen.findByText('第 3 項至第 4 項，共 15 項')).toBeVisible();
});

test('a product can be added with several categories and counted in servings', async () => {
  const user = userEvent.setup();
  await render(<Products />);

  await user.click(await screen.findByRole('button', { name: '加入新食物' }));
  const dialog = within(await screen.findByRole('dialog', { name: '新食物' }));
  await user.type(dialog.getByRole('textbox', { name: '名稱' }), 'Apricot');
  await user.click(dialog.getByRole('button', { name: '按件／份' }));
  await user.clear(dialog.getByRole('textbox', { name: '每份重量 (g)' }));
  await user.type(dialog.getByRole('textbox', { name: '每份重量 (g)' }), '35');
  await user.click(dialog.getByRole('combobox', { name: /分類/ }));
  await user.click(await screen.findByRole('option', { name: 'Fruits' }));
  await user.click(dialog.getByRole('combobox', { name: /分類/ }));
  await user.click(await screen.findByRole('option', { name: '高蛋白' }));
  await user.click(dialog.getByRole('button', { name: '加入' }));
  await waitForElementToBeRemoved(() => screen.queryByRole('dialog'));

  const apricotCategories = await screen.findByRole('list', { name: '「Apricot」的分類' });
  expect(within(apricotCategories).getByText('Fruits'));
  expect(within(apricotCategories).getByText('高蛋白'));
  expect(screen.getByLabelText('「Apricot」的份量為 每份 35 g')).toBeVisible();
});

test('a product can be edited to remove all its categories', async () => {
  const user = userEvent.setup();
  await render(<Products />);

  await user.click(await screen.findByRole('button', { name: '編輯食物「Beef」' }));
  const dialog = within(await screen.findByRole('dialog', { name: '編輯食物' }));
  await user.click(within(dialog.getByRole('button', { name: 'Meat' })).getByTestId('CancelIcon'));
  await user.click(
    within(dialog.getByRole('button', { name: '高蛋白' })).getByTestId('CancelIcon'),
  );
  await user.click(dialog.getByRole('button', { name: '儲存' }));
  await waitForElementToBeRemoved(() => screen.queryByRole('dialog'));

  expect(screen.getByText('Beef'));
  expect(screen.queryByRole('list', { name: '「Beef」的分類' })).not.toBeInTheDocument();
});
