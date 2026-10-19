import { render, screen, waitFor, within } from '@testing-library/react';
import userEvent, { type UserEvent } from '@testing-library/user-event';
import { http, HttpResponse } from 'msw';
import { RootProvider } from '@/app/RootProvider';
import { configureStore } from '@/app/store';
import { API_URL } from '@/shared/config';
import { server } from '@tests/mockApi/server';
import { WorkoutDayList } from './WorkoutDayList';

const DAY_WITH_WORKOUT = '2023-10-19';
const EMPTY_DAY = '2023-10-18';

const renderDay = (date: string): void => {
  render(
    <RootProvider store={configureStore()}>
      <WorkoutDayList date={date} />
    </RootProvider>,
  );
};

const findExercise = (name: string): Promise<HTMLElement> => screen.findByRole('region', { name });

const setsOf = (exercise: HTMLElement): (string | null)[] =>
  within(exercise)
    .getAllByRole('listitem')
    .map(set => set.getAttribute('aria-label'));

const openLogDialog = async (user: UserEvent): Promise<HTMLElement> => {
  await user.click(await screen.findByRole('button', { name: /記錄訓練/ }));
  return screen.findByRole('dialog', { name: '記錄訓練' });
};

const fillSet = async (
  user: UserEvent,
  dialog: HTMLElement,
  setName: string,
  reps: string,
  weightKg: string,
): Promise<void> => {
  const set = within(dialog).getByRole('group', { name: setName });
  const repsField = within(set).getByRole('textbox', { name: '次數' });
  const weightField = within(set).getByRole('textbox', { name: '重量' });

  await user.clear(repsField);
  await user.type(repsField, reps);
  await user.clear(weightField);
  await user.type(weightField, weightKg);
};

const waitForDialogToClose = (): Promise<void> =>
  waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument());

test('the day shows each exercise with its sets in the order they were done', async () => {
  renderDay(DAY_WITH_WORKOUT);

  const benchPress = await findExercise('Bench Press');
  expect(within(benchPress).getByRole('heading', { name: 'Bench Press' })).toBeVisible();
  expect(setsOf(benchPress)).toStrictEqual<string[]>([
    '第 1 組：10 次，60 kg',
    '第 2 組：8 次，62.5 kg',
    '第 3 組：6 次，65 kg',
  ]);
  expect(within(benchPress).getByText('第 1 組')).toBeVisible();
  expect(within(benchPress).getByText('10 次')).toBeVisible();
  expect(within(benchPress).getByText('60 kg')).toBeVisible();

  expect(setsOf(await findExercise('Pull-up'))).toStrictEqual<string[]>([
    '第 1 組：8 次，自身重量',
    '第 2 組：7 次，自身重量',
  ]);
  expect(
    screen.getAllByRole('region').map(exercise => exercise.getAttribute('aria-label')),
  ).toStrictEqual<string[]>(['Bench Press', 'Pull-up', 'Squat']);
});

test('a day without any training says so', async () => {
  renderDay(EMPTY_DAY);

  expect(await screen.findByText('呢日未有訓練記錄')).toBeVisible();
  expect(screen.queryByRole('region')).not.toBeInTheDocument();
});

test('I can log a new exercise with two sets', async () => {
  const user = userEvent.setup();
  renderDay(EMPTY_DAY);
  const dialog = await openLogDialog(user);

  await user.type(within(dialog).getByRole('combobox', { name: '動作' }), 'Overhead Press');
  await fillSet(user, dialog, '第 1 組', '10', '40');
  await user.click(within(dialog).getByRole('button', { name: '加一組' }));

  const secondSet = within(dialog).getByRole('group', { name: '第 2 組' });
  expect(within(secondSet).getByRole('textbox', { name: '次數' })).toHaveValue('10');
  expect(within(secondSet).getByRole('textbox', { name: '重量' })).toHaveValue('40');

  await fillSet(user, dialog, '第 2 組', '8', '42,5');
  await user.click(within(dialog).getByRole('button', { name: '儲存' }));

  await waitForDialogToClose();
  expect(setsOf(await findExercise('Overhead Press'))).toStrictEqual<string[]>([
    '第 1 組：10 次，40 kg',
    '第 2 組：8 次，42.5 kg',
  ]);
  expect(screen.queryByText('呢日未有訓練記錄')).not.toBeInTheDocument();
});

test('an exercise done before can be picked from the list', async () => {
  const user = userEvent.setup();
  renderDay(DAY_WITH_WORKOUT);
  const dialog = await openLogDialog(user);

  await user.click(within(dialog).getByRole('combobox', { name: '動作' }));
  const options = await screen.findAllByRole('option');
  expect(options.map(option => option.textContent)).toStrictEqual<string[]>([
    'Squat',
    'Pull-up',
    'Bench Press',
    'Deadlift',
  ]);

  await user.click(screen.getByRole('option', { name: 'Squat' }));
  await fillSet(user, dialog, '第 1 組', '5', '0');
  await user.click(within(dialog).getByRole('button', { name: '儲存' }));

  await waitForDialogToClose();
  await waitFor(async () =>
    expect(setsOf(await findExercise('Squat'))).toStrictEqual<string[]>([
      '第 1 組：5 次，80 kg',
      '第 2 組：5 次，自身重量',
    ]),
  );
});

test('a set can be removed before saving, but not the only one', async () => {
  const user = userEvent.setup();
  renderDay(EMPTY_DAY);
  const dialog = await openLogDialog(user);

  expect(within(dialog).getByRole('button', { name: '移除第 1 組' })).toBeDisabled();
  await user.click(within(dialog).getByRole('button', { name: '加一組' }));
  await user.click(within(dialog).getByRole('button', { name: '移除第 2 組' }));

  expect(within(dialog).getAllByRole('group')).toHaveLength(1);
  expect(within(dialog).getByRole('button', { name: '移除第 1 組' })).toBeDisabled();
});

test('reps out of range cannot be saved', async () => {
  const user = userEvent.setup();
  renderDay(EMPTY_DAY);
  const dialog = await openLogDialog(user);

  await user.type(within(dialog).getByRole('combobox', { name: '動作' }), 'Squat');
  await fillSet(user, dialog, '第 1 組', '0', '60');

  expect(await within(dialog).findByText('次數須係 1 至 1000 之間嘅整數')).toBeVisible();
  expect(within(dialog).getByRole('button', { name: '儲存' })).toBeDisabled();
});

test('a workout that fails to save keeps the dialog open with an error', async () => {
  server.use(http.post(`${API_URL}/api/v1/workout-sets`, () => HttpResponse.error()));
  const user = userEvent.setup();
  renderDay(EMPTY_DAY);
  const dialog = await openLogDialog(user);

  await user.type(within(dialog).getByRole('combobox', { name: '動作' }), 'Squat');
  await fillSet(user, dialog, '第 1 組', '5', '80');
  await user.click(within(dialog).getByRole('button', { name: '儲存' }));

  expect(await within(dialog).findByRole('alert')).toHaveTextContent('儲存唔到，請再試一次');
  expect(dialog).toBeVisible();
});

test('a logged set can be changed', async () => {
  const user = userEvent.setup();
  renderDay(DAY_WITH_WORKOUT);

  await user.click(await screen.findByRole('button', { name: '修改第 2 組：8 次，62.5 kg' }));
  const dialog = await screen.findByRole('dialog', { name: '改呢一組' });
  expect(within(dialog).getByRole('combobox', { name: '動作' })).toHaveValue('Bench Press');
  expect(within(dialog).getByRole('textbox', { name: '次數' })).toHaveValue('8');
  const weightField = within(dialog).getByRole('textbox', { name: '重量' });
  expect(weightField).toHaveValue('62.5');

  await user.clear(weightField);
  await user.type(weightField, '65');
  await user.click(within(dialog).getByRole('button', { name: '儲存' }));

  await waitForDialogToClose();
  expect(
    await within(await findExercise('Bench Press')).findByRole('listitem', {
      name: '第 2 組：8 次，65 kg',
    }),
  ).toBeVisible();
});

test('deleting a set takes a second tap to confirm', async () => {
  const user = userEvent.setup();
  renderDay(DAY_WITH_WORKOUT);

  await user.click(await screen.findByRole('button', { name: '修改第 1 組：8 次，自身重量' }));
  const dialog = await screen.findByRole('dialog', { name: '改呢一組' });

  await user.click(within(dialog).getByRole('button', { name: '刪除呢一組' }));
  expect(dialog).toBeVisible();
  await user.click(within(dialog).getByRole('button', { name: '確定刪除？' }));

  await waitForDialogToClose();
  await waitFor(async () =>
    expect(setsOf(await findExercise('Pull-up'))).toStrictEqual<string[]>([
      '第 1 組：7 次，自身重量',
    ]),
  );
});
