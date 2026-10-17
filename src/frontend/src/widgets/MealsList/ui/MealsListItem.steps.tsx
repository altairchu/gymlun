import { render, screen, waitFor, waitForElementToBeRemoved } from '@testing-library/react';
import { type UserEvent } from '@testing-library/user-event';
import { RootProvider } from '@/app/RootProvider';
import { configureStore } from '@/app/store';
import { type noteModel } from '@/entities/note';
import { MealsListItem } from './MealsListItem';

interface GivenMealsListItemArgs {
  mealType: noteModel.MealType;
}

export const givenMealsListItem = ({ mealType }: GivenMealsListItemArgs): void => {
  const store = configureStore();
  const date = '2023-10-19';

  render(
    <RootProvider store={store}>
      <MealsListItem date={date} mealType={mealType} />
    </RootProvider>,
  );
};

export const whenAddNoteButtonClicked = async (
  user: UserEvent,
  mealName: string,
): Promise<void> => {
  const addNoteButton = screen.getByRole('button', { name: new RegExp(`加入${mealName}`) });
  await waitFor(() => expect(addNoteButton).toBeEnabled());
  await user.click(addNoteButton);
};

export const whenNoteExpanded = async (user: UserEvent, noteName: RegExp): Promise<void> => {
  const noteButton = await screen.findByRole('button', { name: noteName });
  await user.click(noteButton);
};

export const whenEditNoteClicked = async (user: UserEvent): Promise<void> => {
  const noteButton = await screen.findByRole('button', { name: '編輯' });
  await user.click(noteButton);
};

export const whenProductSearched = async (user: UserEvent, searchText: string): Promise<void> => {
  await user.type(screen.getByPlaceholderText(/搜尋食物/), searchText);
};

export const whenExistingProductSelected = async (
  user: UserEvent,
  productName: RegExp,
): Promise<void> => {
  await user.click(await screen.findByRole('button', { name: productName }));
};

export const whenQuantityChanged = async (user: UserEvent, quantity: number): Promise<void> => {
  await user.clear(screen.getByPlaceholderText(/食物份量/));
  await user.type(screen.getByPlaceholderText(/食物份量/), quantity.toString());
};

export const whenProductAddedFromInput = async (
  user: UserEvent,
  searchText: string,
): Promise<void> => {
  await user.click(
    await screen.findByRole('button', { name: new RegExp(`加入「${searchText}」`, 'i') }),
  );
};

export const whenProductNameCompleted = async (user: UserEvent, nameEnd: string): Promise<void> => {
  await user.type(screen.getByPlaceholderText(/食物名稱/), nameEnd);
};

export const whenProductNameEdited = async (user: UserEvent, name: string): Promise<void> => {
  await user.clear(screen.getByPlaceholderText(/食物名稱/));
  await user.type(screen.getByPlaceholderText(/食物名稱/), name);
};

export const whenProductCaloriesSet = async (user: UserEvent, calories: number): Promise<void> => {
  await user.clear(screen.getByPlaceholderText(/卡路里/));
  await user.type(screen.getByPlaceholderText(/卡路里/), calories.toString());
};

export const whenProductDefaultQuantitySet = async (
  user: UserEvent,
  quantity: number,
): Promise<void> => {
  await user.clear(screen.getByPlaceholderText(/預設份量/));
  await user.type(screen.getByPlaceholderText(/預設份量/), quantity.toString());
};

export const whenProductCategorySelected = async (
  user: UserEvent,
  category: RegExp,
): Promise<void> => {
  await user.click(screen.getByRole('combobox', { name: /分類/ }));
  await user.click(screen.getByRole('option', { name: category }));
};

export const whenProductAdded = async (user: UserEvent): Promise<void> => {
  await user.click(screen.getByRole('button', { name: '加入' }));
};

export const whenProductSaved = async (user: UserEvent): Promise<void> => {
  await user.click(screen.getByRole('button', { name: '儲存' }));
};

export const whenNoteAdded = async (user: UserEvent): Promise<void> => {
  await user.click(screen.getByRole('button', { name: '加入' }));
};

export const whenNoteSaved = async (user: UserEvent): Promise<void> => {
  await user.click(screen.getByRole('button', { name: '儲存' }));
};

export const whenProductEditClicked = async (user: UserEvent): Promise<void> => {
  await user.click(screen.getByRole('button', { name: '編輯食物' }));
};

export const thenMealHeaderIsVisible = async (): Promise<void> => {
  const header = await screen.findByRole('listitem', { name: /午餐，[1-9][0-9]* kcal/ });
  expect(header).toBeVisible();
};

export const thenMealsAreVisible = (): void => {
  expect(screen.getAllByRole('button').length).toBeGreaterThan(1);
};

export const thenDialogVisible = async (dialogTitle: RegExp): Promise<void> => {
  expect(await screen.findByRole('dialog', { name: dialogTitle })).toBeVisible();
};

export const thenNoteCannotBeAdded = (): void => {
  expect(screen.getByRole('button', { name: '加入' })).toBeDisabled();
};

export const thenNoteCanBeAdded = (): void => {
  expect(screen.getByRole('button', { name: '加入' })).toBeEnabled();
};

export const thenProductCanBeAdded = (): void => {
  expect(screen.getByRole('button', { name: '加入' })).toBeEnabled();
};

export const thenProductCanBeSaved = (): void => {
  expect(screen.getByRole('button', { name: '儲存' })).toBeEnabled();
};

export const thenDialogNotVisible = async (): Promise<void> => {
  await waitForElementToBeRemoved(screen.queryByRole('dialog'));
};

export const thenProductHasValue = (expectedValue: string): void => {
  expect(screen.getByRole('textbox', { name: '食物' })).toHaveValue(expectedValue);
};

export const thenProductCategoryHasValue = (expectedValue: string): void => {
  expect(screen.getByRole('combobox', { name: /分類/ })).toHaveValue(expectedValue);
};

export const thenQuantityHasValue = (expectedValue: string): void => {
  expect(screen.getByPlaceholderText(/食物份量/)).toHaveValue(expectedValue);
};

export const thenSingleNoteVisible = (noteName: RegExp): void => {
  expect(screen.getByRole('button', { name: noteName })).toBeVisible();
};

export const whenFavoriteQuickAdded = async (user: UserEvent, chipName: RegExp): Promise<void> => {
  const chip = await screen.findByRole('button', { name: chipName });
  await waitFor(() => expect(chip).not.toHaveAttribute('aria-disabled', 'true'));
  await user.click(chip);
};

export const thenNoteAppears = async (noteName: RegExp): Promise<void> => {
  expect(await screen.findByRole('button', { name: noteName })).toBeVisible();
};
