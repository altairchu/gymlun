import {
  act,
  fireEvent,
  screen,
  waitFor,
  waitForElementToBeRemoved,
  within,
} from '@testing-library/react';
import userEvent, { type UserEvent } from '@testing-library/user-event';
import { render } from '@tests/render';
import { IndexPage } from './IndexPage';

const DAY_WITH_FOOD_LOGS = '2023-10-20';
const EMPTY_DAY = '2023-10-22';

const findFoodGrid = (): Promise<HTMLElement> => screen.findByRole('region', { name: '揀食物' });

const findFoodCard = async (name: RegExp): Promise<HTMLElement> =>
  within(await findFoodGrid()).findByRole('button', { name });

const openTimeline = async (user: UserEvent): Promise<HTMLElement> => {
  await user.click(await screen.findByRole('tab', { name: /時間線/ }));
  return screen.findByRole('region', { name: '今日時間線' });
};

const findMeal = (time: string): Promise<HTMLElement> =>
  screen.findByRole('listitem', { name: new RegExp(`^${time}，`) });

test('the food grid shows every food to pick from', async () => {
  await render(<IndexPage date={DAY_WITH_FOOD_LOGS} />);

  expect(await findFoodCard(/白飯/)).toBeVisible();
  expect(await findFoodCard(/Chocolate cake/)).toBeVisible();
});

test('picking two categories shows only the foods in both of them', async () => {
  const user = userEvent.setup();
  await render(<IndexPage date={DAY_WITH_FOOD_LOGS} />);
  const grid = await findFoodGrid();

  await user.click(await within(grid).findByRole('button', { name: '肉類' }));
  await user.click(within(grid).getByRole('button', { name: 'Frozen foods' }));

  expect(within(grid).getByRole('button', { name: /豬肉韭菜餃/ })).toBeVisible();
  expect(within(grid).queryByRole('button', { name: /雞胸肉/ })).not.toBeInTheDocument();
  expect(within(grid).queryByRole('button', { name: /Chocolate cake/ })).not.toBeInTheDocument();
});

test('picking a category again shows every food again', async () => {
  const user = userEvent.setup();
  await render(<IndexPage date={DAY_WITH_FOOD_LOGS} />);
  const grid = await findFoodGrid();

  await user.click(await within(grid).findByRole('button', { name: '飯麵' }));
  expect(within(grid).queryByRole('button', { name: /Beef/ })).not.toBeInTheDocument();

  await user.click(within(grid).getByRole('button', { name: '飯麵' }));
  expect(within(grid).getByRole('button', { name: /Beef/ })).toBeVisible();
});

test('the favourites filter shows only the foods eaten often', async () => {
  const user = userEvent.setup();
  await render(<IndexPage date={DAY_WITH_FOOD_LOGS} />);
  const grid = await findFoodGrid();
  await within(grid).findByRole('button', { name: /Beef/ });

  await user.click(within(grid).getByRole('button', { name: '常食' }));

  expect(within(grid).getByRole('button', { name: /雞胸肉/ })).toBeVisible();
  expect(within(grid).queryByRole('button', { name: /Beef/ })).not.toBeInTheDocument();
});

test('searching shows the foods whose names contain the search', async () => {
  const user = userEvent.setup();
  await render(<IndexPage date={DAY_WITH_FOOD_LOGS} />);
  const grid = await findFoodGrid();
  await within(grid).findByRole('button', { name: /白飯/ });

  await user.type(within(grid).getByRole('searchbox', { name: '搜尋食物' }), '雞');

  expect(within(grid).getByRole('button', { name: /雞蛋/ })).toBeVisible();
  expect(within(grid).getByRole('button', { name: /雞胸肉/ })).toBeVisible();
  expect(within(grid).queryByRole('button', { name: /白飯/ })).not.toBeInTheDocument();
});

test('a food added from the grid is picked straight away', async () => {
  const user = userEvent.setup();
  await render(<IndexPage date={EMPTY_DAY} />);
  const grid = await findFoodGrid();
  await within(grid).findByRole('button', { name: /白飯/ });
  await user.type(within(grid).getByRole('searchbox', { name: '搜尋食物' }), '豆腐');

  await user.click(within(grid).getByRole('button', { name: '新增「豆腐」' }));
  const dialog = await screen.findByRole('dialog', { name: '新食物' });
  expect(within(dialog).getByRole('textbox', { name: '名稱' })).toHaveValue('豆腐');
  await user.click(within(dialog).getByRole('button', { name: '加入' }));

  await waitForElementToBeRemoved(dialog);
  expect(await findFoodCard(/豆腐/)).toHaveAttribute('aria-pressed', 'true');
  expect(screen.getByText('已揀 1 樣')).toBeVisible();
});

test('a food counted in servings is picked as one serving', async () => {
  const user = userEvent.setup();
  await render(<IndexPage date={EMPTY_DAY} />);

  await user.click(await findFoodCard(/豬肉韭菜餃/));

  expect(screen.getByText('已揀 1 樣')).toBeVisible();
  await user.click(screen.getByRole('button', { name: '揀數量同時間' }));
  expect(await screen.findByRole('heading', { name: '已揀 1 樣 · 50 kcal' })).toBeVisible();
  expect(screen.getByRole('textbox', { name: '豬肉韭菜餃數量' })).toHaveValue('1');
});

test('a weighed food is picked as its usual portion in grams', async () => {
  const user = userEvent.setup();
  await render(<IndexPage date={EMPTY_DAY} />);

  await user.click(await findFoodCard(/白飯/));
  await user.click(screen.getByRole('button', { name: '揀數量同時間' }));

  expect(await screen.findByRole('textbox', { name: '白飯數量' })).toHaveValue('200');
});

test('switching a picked food from servings to grams keeps the same amount of food', async () => {
  const user = userEvent.setup();
  await render(<IndexPage date={EMPTY_DAY} />);
  await user.click(await findFoodCard(/豬肉韭菜餃/));
  await user.click(screen.getByRole('button', { name: '揀數量同時間' }));
  const amount = await screen.findByRole('textbox', { name: '豬肉韭菜餃數量' });
  await user.clear(amount);
  await user.type(amount, '10');

  await user.click(screen.getByRole('button', { name: '克' }));

  expect(amount).toHaveValue('250');
});

test('picked foods are logged together at the time picked', async () => {
  const user = userEvent.setup();
  await render(<IndexPage date={EMPTY_DAY} />);
  await user.click(await findFoodCard(/豬肉韭菜餃/));
  await user.click(screen.getByRole('button', { name: /白飯/ }));
  await user.click(screen.getByRole('button', { name: '揀數量同時間' }));
  await user.click(await screen.findByRole('button', { name: '增加豬肉韭菜餃' }));
  await user.click(screen.getByRole('button', { name: '午餐 12:30' }));

  await user.click(screen.getByRole('button', { name: '加入 2 樣（12:30）' }));

  expect(await screen.findByText('已加入 2 樣食物（12:30）')).toBeVisible();
  await openTimeline(user);
  const lunch = await findMeal('12:30');
  expect(within(lunch).getByText('豬肉韭菜餃')).toBeVisible();
  expect(within(lunch).getByText('2 份 · 50 g')).toBeVisible();
  expect(within(lunch).getByText('白飯')).toBeVisible();
  expect(within(lunch).getByText('200 g')).toBeVisible();
});

test('an amount that is not a number cannot be logged', async () => {
  const user = userEvent.setup();
  await render(<IndexPage date={EMPTY_DAY} />);
  await user.click(await findFoodCard(/白飯/));
  await user.click(screen.getByRole('button', { name: '揀數量同時間' }));

  await user.clear(await screen.findByRole('textbox', { name: '白飯數量' }));

  expect(screen.getByText('請輸入 1 至 5000 g 之間嘅數量')).toBeVisible();
  expect(screen.getByRole('button', { name: /^加入 1 樣/ })).toBeDisabled();
});

test('the timeline shows the foods of the day at the times they were eaten', async () => {
  const user = userEvent.setup();
  await render(<IndexPage date={DAY_WITH_FOOD_LOGS} />);

  await openTimeline(user);

  const breakfast = await findMeal('08:00');
  expect(within(breakfast).getByText('Apple')).toBeVisible();
  expect(within(breakfast).getByText('Milk')).toBeVisible();
  expect(within(await findMeal('19:00')).getByText('Bread')).toBeVisible();
});

test('a food moves 15 minutes later with the down arrow on its handle', async () => {
  const user = userEvent.setup();
  await render(<IndexPage date={DAY_WITH_FOOD_LOGS} />);
  await openTimeline(user);
  await findMeal('08:00');

  const handle = screen.getByRole('button', { name: '移動Apple' });
  act(() => {
    handle.focus();
  });
  await user.keyboard('{ArrowDown}');

  expect(within(await findMeal('08:15')).getByText('Apple')).toBeVisible();
  expect(within(await findMeal('08:00')).queryByText('Apple')).not.toBeInTheDocument();
});

test('dragging a food on the timeline moves it to the time it is dropped at', async () => {
  const user = userEvent.setup();
  await render(<IndexPage date={DAY_WITH_FOOD_LOGS} />);
  await openTimeline(user);
  await findMeal('08:00');

  const handle = screen.getByRole('button', { name: '移動Apple' });
  fireEvent.pointerDown(handle, { button: 0, clientY: 100 });
  fireEvent.pointerMove(window, { clientY: 172 });
  expect(screen.getByRole('status')).toHaveTextContent('09:00 · Apple');
  fireEvent.pointerUp(window);

  expect(within(await findMeal('09:00')).getByText('Apple')).toBeVisible();
});

test('dragging a meal by its time moves every food in it', async () => {
  const user = userEvent.setup();
  await render(<IndexPage date={DAY_WITH_FOOD_LOGS} />);
  await openTimeline(user);
  await findMeal('19:00');

  const handle = screen.getByRole('button', { name: '移動 19:00 全部食物' });
  fireEvent.pointerDown(handle, { button: 0, clientY: 500 });
  fireEvent.pointerMove(window, { clientY: 536 });
  fireEvent.pointerUp(window);

  const dinner = await findMeal('19:30');
  expect(within(dinner).getByText('Bread')).toBeVisible();
  expect(within(dinner).getByText('Beef')).toBeVisible();
});

test('a logged food can be changed to another amount and time', async () => {
  const user = userEvent.setup();
  await render(<IndexPage date={DAY_WITH_FOOD_LOGS} />);
  await openTimeline(user);
  await user.click(await screen.findByRole('button', { name: '修改Apple' }));
  const dialog = await screen.findByRole('dialog', { name: 'Apple' });

  await user.click(within(dialog).getByRole('button', { name: '增加Apple' }));
  await user.click(within(dialog).getByRole('button', { name: '下午茶 15:30' }));
  await user.click(within(dialog).getByRole('button', { name: '儲存' }));

  const snack = await screen.findByRole('listitem', { name: /^15:30，3 樣/ });
  expect(within(snack).getByText('Apple')).toBeVisible();
  expect(within(snack).getByText('2 份 · 300 g')).toBeVisible();
});

test('deleting a logged food takes a second tap to confirm', async () => {
  const user = userEvent.setup();
  await render(<IndexPage date={DAY_WITH_FOOD_LOGS} />);
  await openTimeline(user);
  await user.click(await screen.findByRole('button', { name: '修改Milk' }));
  const dialog = await screen.findByRole('dialog', { name: 'Milk' });

  await user.click(within(dialog).getByRole('button', { name: '刪除呢項記錄' }));
  expect(dialog).toBeVisible();
  await user.click(within(dialog).getByRole('button', { name: '確定刪除？' }));

  await waitForElementToBeRemoved(dialog);
  await waitFor(() =>
    expect(screen.queryByRole('button', { name: '修改Milk' })).not.toBeInTheDocument(),
  );
  expect(within(await findMeal('08:00')).getByText('Apple')).toBeVisible();
});
