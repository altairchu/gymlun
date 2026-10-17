import { render, screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { RootProvider } from '@/app/RootProvider';
import { configureStore } from '@/app/store';
import { dateLib } from '@/shared/lib';
import { WeightLogsList } from './WeightLogsList';

afterEach(() => {
  vi.restoreAllMocks();
});

test('I can log my current weight', async () => {
  const store = configureStore();
  const user = userEvent.setup();
  vi.spyOn(dateLib, 'getCurrentDate').mockReturnValue(new Date('2022-01-30'));

  render(
    <RootProvider store={store}>
      <WeightLogsList weightLogsRequest={{ from: '2022-01-01', to: '2022-01-31' }} />
    </RootProvider>,
  );

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
