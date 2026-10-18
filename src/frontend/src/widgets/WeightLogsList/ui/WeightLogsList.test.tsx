import { render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { RootProvider } from '@/app/RootProvider';
import { configureStore } from '@/app/store';
import { dateLib } from '@/shared/lib';
import { WeightLogsList } from './WeightLogsList';

const renderJanuaryWeightLogs = (): void => {
  render(
    <RootProvider store={configureStore()}>
      <WeightLogsList weightLogsRequest={{ from: '2022-01-01', to: '2022-01-31' }} />
    </RootProvider>,
  );
};

const waitForDialogToClose = (): Promise<void> =>
  waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument());

afterEach(() => {
  vi.restoreAllMocks();
});

test('I can log my current weight', async () => {
  const user = userEvent.setup();
  vi.spyOn(dateLib, 'getCurrentDate').mockReturnValue(new Date('2022-01-30'));
  renderJanuaryWeightLogs();

  await user.click(screen.getByRole('button', { name: /記錄體重/ }));
  expect(await screen.findByRole('dialog')).toBeVisible();

  const dateField = await screen.findByRole('group', { name: /日期/ });
  const weightField = await screen.findByPlaceholderText(/體重/);
  expect(within(dateField).getByRole('spinbutton', { name: '日' })).toHaveTextContent('30');
  expect(within(dateField).getByRole('spinbutton', { name: '月' })).toHaveTextContent('01');
  expect(within(dateField).getByRole('spinbutton', { name: '年' })).toHaveTextContent('2022');
  expect(weightField).toHaveValue('73');

  await user.click(screen.getByRole('button', { name: /選擇日期/ }));
  await user.click(
    within(screen.getByRole('dialog', { name: /日期/ })).getByRole('gridcell', { name: '29' }),
  );

  await user.clear(weightField);
  await user.type(weightField, '75');

  await user.click(screen.getByRole('button', { name: '儲存' }));
  expect(await screen.findByRole('listitem', { name: /2022年1月29日：75 kg/ })).toBeVisible();
});

test('a logged weight can be changed', async () => {
  const user = userEvent.setup();
  renderJanuaryWeightLogs();

  await user.click(await screen.findByRole('button', { name: '修改2022年1月28日：73 kg' }));
  const dialog = await screen.findByRole('dialog', { name: '2022年1月28日' });
  const weightField = within(dialog).getByRole('textbox', { name: '體重' });
  expect(weightField).toHaveValue('73');

  await user.clear(weightField);
  await user.type(weightField, '72,5');
  await user.click(within(dialog).getByRole('button', { name: '儲存' }));

  await waitForDialogToClose();
  expect(await screen.findByRole('listitem', { name: '2022年1月28日：72.5 kg' })).toBeVisible();
});

test('a weight out of range cannot be saved', async () => {
  const user = userEvent.setup();
  renderJanuaryWeightLogs();

  await user.click(await screen.findByRole('button', { name: '修改2022年1月28日：73 kg' }));
  const dialog = await screen.findByRole('dialog', { name: '2022年1月28日' });
  const weightField = within(dialog).getByRole('textbox', { name: '體重' });

  await user.clear(weightField);
  await user.type(weightField, '501');

  expect(await within(dialog).findByText('體重須介乎 1 至 500 kg')).toBeVisible();
  expect(within(dialog).getByRole('button', { name: '儲存' })).toBeDisabled();
});

test('deleting a logged weight takes a second tap to confirm', async () => {
  const user = userEvent.setup();
  renderJanuaryWeightLogs();

  await user.click(await screen.findByRole('button', { name: '修改2022年1月28日：73 kg' }));
  const dialog = await screen.findByRole('dialog', { name: '2022年1月28日' });

  await user.click(within(dialog).getByRole('button', { name: '刪除呢項記錄' }));
  expect(dialog).toBeVisible();
  await user.click(within(dialog).getByRole('button', { name: '確定刪除？' }));

  await waitForDialogToClose();
  await waitFor(() =>
    expect(screen.queryByRole('listitem', { name: /2022年1月28日/ })).not.toBeInTheDocument(),
  );
  expect(screen.getByRole('listitem', { name: '2022年1月21日：71 kg' })).toBeVisible();
});
