import { ThemeProvider } from '@mui/material';
import { screen, waitFor, waitForElementToBeRemoved } from '@testing-library/react';
import { type UserEvent } from '@testing-library/user-event';
import { http, HttpResponse } from 'msw';
import { type ReactElement } from 'react';
import { Provider } from 'react-redux';
import { type Mock } from 'vitest';
import { configureStore } from '@/app/store';
import { theme } from '@/app/theme';
import {
  productModel,
  type SuggestProductNutritionRequest,
  type SuggestProductNutritionResponse,
} from '@/entities/product';
import { API_URL } from '@/shared/config';
import { type SelectOption } from '@/shared/types';
import { server } from '@tests/mockApi/server';
import { WithTriggerButton } from '@tests/sideEffects';
import { ProductInputDialog } from './ProductInputDialog';

const NUTRITION_SUGGESTIONS_URL = `${API_URL}/api/v1/products/nutrition/suggestions`;

const EMPTY_NUTRITION_SUGGESTIONS: SuggestProductNutritionResponse = {
  calories: null,
  protein: null,
  fats: null,
  carbs: null,
  sugar: null,
  salt: null,
};

class ProductInputDialogBuilder {
  private _onSubmitMock: Mock = vi.fn();
  private _product: productModel.ProductFormValues = productModel.EMPTY_FORM_VALUES;
  private readonly _categories: SelectOption[] = [];

  please(): ReactElement {
    return (
      <Provider store={configureStore()}>
        <ThemeProvider theme={theme}>
          <WithTriggerButton label="Open">
            {({ active, onTriggerClick }) => (
              <ProductInputDialog
                opened={active}
                title="Product"
                submitText="Submit"
                isLoading={false}
                categories={this._categories}
                categoriesLoading={false}
                productFormValues={this._product}
                onSubmit={this._onSubmitMock}
                onClose={onTriggerClick}
              />
            )}
          </WithTriggerButton>
        </ThemeProvider>
      </Provider>
    );
  }

  withOnSubmitMock(onSubmitMock: Mock): this {
    this._onSubmitMock = onSubmitMock;
    return this;
  }

  withCategoriesForSelect(categories: SelectOption[]): this {
    this._categories.push(...categories);
    return this;
  }

  withProduct({
    name = '',
    calories = 100,
    defaultQuantity = 100,
    category = this._categories[0],
    protein = null,
    fats = null,
    carbs = null,
    sugar = null,
    salt = null,
    isFavorite = false,
  }: Partial<productModel.ProductFormValues>): this {
    this._product = {
      name,
      calories,
      defaultQuantity,
      category,
      protein,
      fats,
      carbs,
      sugar,
      salt,
      isFavorite,
    };
    return this;
  }
}

export const givenCategories = (...categoryNames: string[]): SelectOption[] =>
  categoryNames.map((name, index) => ({ id: index + 1, name }));

export const givenProductInputDialog = (): ProductInputDialogBuilder =>
  new ProductInputDialogBuilder();

interface NutritionSuggestionRequests {
  names: string[];
}

export const givenNutritionSuggestion = (
  response: Partial<SuggestProductNutritionResponse> = EMPTY_NUTRITION_SUGGESTIONS,
): NutritionSuggestionRequests => {
  const requests: NutritionSuggestionRequests = { names: [] };

  server.use(
    http.post(NUTRITION_SUGGESTIONS_URL, async ({ request }) => {
      const body = (await request.json()) as SuggestProductNutritionRequest;
      requests.names.push(body.name);
      return HttpResponse.json<SuggestProductNutritionResponse>({
        ...EMPTY_NUTRITION_SUGGESTIONS,
        ...response,
      });
    }),
  );

  return requests;
};

interface PendingNutritionSuggestion {
  resolve: () => void;
}

export const givenPendingNutritionSuggestion = (
  response: Partial<SuggestProductNutritionResponse>,
): PendingNutritionSuggestion => {
  let resolvePending!: () => void;
  const pending = new Promise<void>(resolve => {
    resolvePending = resolve;
  });

  server.use(
    http.post(NUTRITION_SUGGESTIONS_URL, async () => {
      await pending;
      return HttpResponse.json<SuggestProductNutritionResponse>({
        ...EMPTY_NUTRITION_SUGGESTIONS,
        ...response,
      });
    }),
  );

  return { resolve: resolvePending };
};

export const givenNutritionSuggestionFails = (): void => {
  server.use(
    http.post(NUTRITION_SUGGESTIONS_URL, () =>
      HttpResponse.json(
        { title: 'Internal Server Error', detail: 'Model response was invalid' },
        { status: 500 },
      ),
    ),
  );
};

export const whenDialogOpened = async (user: UserEvent): Promise<void> => {
  await user.click(screen.getByRole('button', { name: 'Open' }));
};

export const whenDialogClosed = async (user: UserEvent): Promise<void> => {
  await user.click(screen.getByRole('button', { name: /取消/ }));
};

export const whenCloseIconClicked = async (user: UserEvent): Promise<void> => {
  await user.click(screen.getByRole('button', { name: /關閉/ }));
};

export const whenProductNameChanged = async (user: UserEvent, name: string): Promise<void> => {
  await user.clear(screen.getByRole('textbox', { name: /名稱/ }));
  await user.type(screen.getByRole('textbox', { name: /名稱/ }), name);
};

export const whenCaloriesChanged = async (user: UserEvent, cost: string): Promise<void> => {
  await user.clear(screen.getByPlaceholderText(/卡路里/));
  await user.type(screen.getByPlaceholderText(/卡路里/), cost);
};

export const whenDefaultQuantityChanged = async (
  user: UserEvent,
  quantity: string,
): Promise<void> => {
  await user.clear(screen.getByPlaceholderText(/預設份量/));
  await user.type(screen.getByPlaceholderText(/預設份量/), quantity);
};

export const whenCategorySelected = async (user: UserEvent, name: RegExp): Promise<void> => {
  await user.click(screen.getByRole('combobox', { name: /分類/ }));
  await user.click(screen.getByRole('option', { name }));
};

export const whenNutritionPanelExpanded = async (user: UserEvent): Promise<void> => {
  await user.click(screen.getByRole('button', { name: /營養成分/ }));
};

export const whenProteinChanged = async (user: UserEvent, protein: string): Promise<void> => {
  await user.clear(screen.getByPlaceholderText(/蛋白質/));
  await user.type(screen.getByPlaceholderText(/蛋白質/), protein);
};

export const whenProteinCleared = async (user: UserEvent): Promise<void> => {
  await user.clear(screen.getByPlaceholderText(/蛋白質/));
};

export const whenFatsChanged = async (user: UserEvent, fats: string): Promise<void> => {
  await user.clear(screen.getByPlaceholderText(/脂肪/));
  await user.type(screen.getByPlaceholderText(/脂肪/), fats);
};

export const whenCarbsChanged = async (user: UserEvent, carbs: string): Promise<void> => {
  await user.clear(screen.getByPlaceholderText(/碳水化合物/));
  await user.type(screen.getByPlaceholderText(/碳水化合物/), carbs);
};

export const whenSugarChanged = async (user: UserEvent, sugar: string): Promise<void> => {
  await user.clear(screen.getByPlaceholderText(/糖/));
  await user.type(screen.getByPlaceholderText(/糖/), sugar);
};

export const whenSaltChanged = async (user: UserEvent, salt: string): Promise<void> => {
  await user.clear(screen.getByPlaceholderText(/鹽/));
  await user.type(screen.getByPlaceholderText(/鹽/), salt);
};

export const whenCategoryCleared = async (user: UserEvent): Promise<void> => {
  await user.clear(screen.getByRole('combobox', { name: /分類/ }));
};

export const whenProductSaved = async (user: UserEvent): Promise<void> => {
  await user.click(screen.getByRole('button', { name: /submit/i }));
};

export const whenSuggestClicked = async (user: UserEvent, name: RegExp): Promise<void> => {
  await user.click(screen.getByRole('button', { name }));
};

export const expectCategory = (name: string): SelectOption =>
  expect.objectContaining<Partial<SelectOption>>({ name }) as SelectOption;

export const thenProductFormIsVisible = async (): Promise<void> => {
  expect(await screen.findByRole('dialog', { name: /product/i })).toBeVisible();
};

export const thenFormValueContains = (
  onSubmitMock: Mock,
  product: productModel.ProductFormValues,
): void => {
  expect(onSubmitMock).toHaveBeenCalledWith<[productModel.ProductFormValues]>(product);
};

export const thenProductNameIsInvalid = (): void => {
  expect(screen.getByPlaceholderText(/食物名稱/)).toBeInvalid();
};

export const thenProductNameIsValid = (): void => {
  expect(screen.getByPlaceholderText(/食物名稱/)).toBeValid();
};

export const thenProductNameHasValue = (value: string): void => {
  expect(screen.getByPlaceholderText(/食物名稱/)).toHaveValue(value);
};

export const thenCaloriesIsInvalid = (): void => {
  expect(screen.getByPlaceholderText(/卡路里/)).toBeInvalid();
};

export const thenCaloriesHasValue = (value: number): void => {
  expect(screen.getByPlaceholderText(/卡路里/)).toHaveValue(value.toString());
};

export const thenDefaultQuantityIsInvalid = (): void => {
  expect(screen.getByPlaceholderText(/預設份量/)).toBeInvalid();
};

export const thenDefaultQuantityHasValue = (value: number): void => {
  expect(screen.getByPlaceholderText(/預設份量/)).toHaveValue(value.toString());
};

export const thenCategoryIsInvalid = (): void => {
  expect(screen.getByRole('combobox', { name: /分類/ })).toBeInvalid();
};

export const thenCategoryHasValue = (value: string): void => {
  expect(screen.getByRole('combobox', { name: /分類/ })).toHaveValue(value);
};

export const thenDialogShouldBeHidden = async (): Promise<void> => {
  await waitForElementToBeRemoved(screen.queryByRole('dialog'));
};

export const thenNutritionPanelIsExpanded = async (): Promise<void> => {
  await waitFor(() =>
    expect(screen.getByRole('button', { name: /營養成分/ })).toHaveAttribute(
      'aria-expanded',
      'true',
    ),
  );
};

const eventuallyHasValue = async (placeholder: RegExp, value: string): Promise<void> => {
  await waitFor(() => expect(screen.getByPlaceholderText(placeholder)).toHaveValue(value));
};

export const thenCaloriesEventuallyHasValue = async (value: string): Promise<void> =>
  await eventuallyHasValue(/卡路里/, value);

export const thenProteinEventuallyHasValue = async (value: string): Promise<void> =>
  await eventuallyHasValue(/蛋白質/, value);

export const thenFatsEventuallyHasValue = async (value: string): Promise<void> =>
  await eventuallyHasValue(/脂肪/, value);

export const thenCarbsEventuallyHasValue = async (value: string): Promise<void> =>
  await eventuallyHasValue(/碳水化合物/, value);

export const thenSuggestButtonIsDisabled = (name: RegExp): void => {
  expect(screen.getByRole('button', { name })).toBeDisabled();
};

export const thenSuggestButtonIsEnabled = (name: RegExp): void => {
  expect(screen.getByRole('button', { name })).toBeEnabled();
};

export const thenNameInputIsDisabled = (): void => {
  expect(screen.getByRole('textbox', { name: /名稱/ })).toBeDisabled();
};

export const thenSubmitIsDisabled = (): void => {
  expect(screen.getByRole('button', { name: /submit/i })).toBeDisabled();
};

export const thenSubmitIsEnabled = async (): Promise<void> => {
  await waitFor(() => expect(screen.getByRole('button', { name: /submit/i })).toBeEnabled());
};

export const thenAlertShown = async (message: RegExp): Promise<void> => {
  const alert = await screen.findByRole('alert');
  expect(alert).toHaveTextContent(message);
};
