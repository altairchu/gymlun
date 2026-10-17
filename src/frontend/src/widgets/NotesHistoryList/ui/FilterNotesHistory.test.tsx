import { screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { type Mock } from 'vitest';
import { render } from '@tests/render';
import { FilterNotesHistory, type OnApplyFilterFn } from './FilterNotesHistory';

const openFilter = async (): Promise<{ onApply: Mock<OnApplyFilterFn> }> => {
  const onApply = vi.fn<OnApplyFilterFn>();
  await render(<FilterNotesHistory date={new Date(2023, 9, 19)} onApply={onApply} />);
  await userEvent.click(screen.getByRole('button', { name: /顯示篩選/ }));
  return { onApply };
};

test('the dialog is the only place the filter can be confirmed or dismissed', async () => {
  await openFilter();

  expect(screen.getByRole('button', { name: /^取消$/ })).toBeVisible();
  expect(screen.getByRole('button', { name: /^套用$/ })).toBeVisible();
  expect(screen.queryByRole('button', { name: /^確認$/ })).not.toBeInTheDocument();
});

test('the picked month survives until it is applied', async () => {
  const { onApply } = await openFilter();

  await userEvent.click(screen.getByRole('radio', { name: '十二月' }));
  await userEvent.click(screen.getByRole('button', { name: /^套用$/ }));

  expect(onApply).toHaveBeenCalledExactlyOnceWith<Parameters<OnApplyFilterFn>>(12, 2023);
});
