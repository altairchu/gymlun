import { ThemeProvider } from '@mui/material';
import { screen, waitFor, waitForElementToBeRemoved, within } from '@testing-library/react';
import { type UserEvent } from '@testing-library/user-event';
import { http, HttpResponse } from 'msw';
import { type ReactElement } from 'react';
import { Provider } from 'react-redux';
import { type Mock } from 'vitest';
import { configureStore } from '@/app/store';
import { theme } from '@/app/theme';
import { type CreateCategoryRequest } from '@/entities/category';
import {
  type OnSubmitProductFn,
  productModel,
  type SuggestProductNutritionRequest,
  type SuggestProductNutritionResponse,
} from '@/entities/product';
import { API_URL } from '@/shared/config';
import { type SelectOption } from '@/shared/types';
import { server } from '@tests/mockApi/server';
import { WithTriggerButton } from '@tests/sideEffects';
import { type PhotoChange, PHOTO_UNCHANGED } from '../../model';
import { ProductInputDialog } from './ProductInputDialog';

const NUTRITION_SUGGESTIONS_URL = `${API_URL}/api/v1/products/nutrition/suggestions`;
const CATEGORIES_URL = `${API_URL}/api/v1/categories`;

const DEFAULT_QUANTITY_LABEL = /^(預設份量|每份份量|每份重量) \(g\)$/;

const EMPTY_NUTRITION_SUGGESTIONS: SuggestProductNutritionResponse = {
  calories: null,
  protein: null,
  fats: null,
  carbs: null,
  sugar: null,
  salt: null,
};

class ProductInputDialogBuilder {
  private _onSubmitMock: Mock<OnSubmitProductFn> = vi.fn<OnSubmitProductFn>();
  private _product: productModel.ProductFormValues = productModel.EMPTY_FORM_VALUES;
  private readonly _categories: SelectOption[] = [];
  private _savedPhotoUrl: string | null = null;

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
                savedPhotoUrl={this._savedPhotoUrl}
                photo={PHOTO_UNCHANGED}
                photoSaveFailed={false}
                onSubmit={this._onSubmitMock}
                onPhotoChange={vi.fn<(photo: PhotoChange) => void>()}
                onClose={onTriggerClick}
              />
            )}
          </WithTriggerButton>
        </ThemeProvider>
      </Provider>
    );
  }

  withOnSubmitMock(onSubmitMock: Mock<OnSubmitProductFn>): this {
    this._onSubmitMock = onSubmitMock;
    return this;
  }

  withSavedPhoto(photoUrl: string): this {
    this._savedPhotoUrl = photoUrl;
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
    quantityUnit = productModel.QuantityUnit.Grams,
    categories = [],
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
      quantityUnit,
      categories,
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

interface CategoryCreationRequests {
  names: string[];
}

export const givenCategoryCreatedWithId = (id: number): CategoryCreationRequests => {
  const requests: CategoryCreationRequests = { names: [] };

  server.use(
    http.post(CATEGORIES_URL, async ({ request }) => {
      const body = (await request.json()) as CreateCategoryRequest;
      requests.names.push(body.name);
      return HttpResponse.json<number>(id);
    }),
  );

  return requests;
};

export const givenCategoryCreationFails = (): void => {
  server.use(
    http.post(CATEGORIES_URL, () =>
      HttpResponse.json(
        { title: 'Bad Request', detail: 'Category name is already taken' },
        { status: 400 },
      ),
    ),
  );
};

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

const getDefaultQuantityInput = (): HTMLElement =>
  screen.getByRole('textbox', { name: DEFAULT_QUANTITY_LABEL });

const getCategoriesInput = (): HTMLElement => screen.getByRole('combobox', { name: /分類/ });

export const whenDefaultQuantityChanged = async (
  user: UserEvent,
  quantity: string,
): Promise<void> => {
  await user.clear(getDefaultQuantityInput());
  await user.type(getDefaultQuantityInput(), quantity);
};

export const whenCategorySelected = async (user: UserEvent, name: RegExp): Promise<void> => {
  await user.click(getCategoriesInput());
  await user.click(screen.getByRole('option', { name }));
};

export const whenCategoriesOpened = async (user: UserEvent): Promise<void> => {
  await user.click(getCategoriesInput());
};

export const whenCategoryNameTyped = async (user: UserEvent, name: string): Promise<void> => {
  await user.type(getCategoriesInput(), name);
};

export const whenNewCategoryOptionClicked = async (
  user: UserEvent,
  name: string,
): Promise<void> => {
  await user.click(screen.getByRole('option', { name: `新增分類「${name}」` }));
};

export const whenCategoryRemoved = async (user: UserEvent, name: string): Promise<void> => {
  const chip = screen.getByRole('button', { name });
  await user.click(within(chip).getByTestId('CancelIcon'));
};

export const whenCountedInServings = async (user: UserEvent): Promise<void> => {
  await user.click(screen.getByRole('button', { name: '按件／份' }));
};

export const whenCountedInGrams = async (user: UserEvent): Promise<void> => {
  await user.click(screen.getByRole('button', { name: '按重量（克）' }));
};

export const whenPerServingNutritionEnabled = async (user: UserEvent): Promise<void> => {
  await user.click(screen.getByRole('switch', { name: '按每份輸入營養數值' }));
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
  onSubmitMock: Mock<OnSubmitProductFn>,
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
  expect(getDefaultQuantityInput()).toBeInvalid();
};

export const thenDefaultQuantityHasValue = (value: number): void => {
  expect(getDefaultQuantityInput()).toHaveValue(value.toString());
};

export const thenDefaultQuantityIsLabelled = (label: string): void => {
  expect(getDefaultQuantityInput()).toHaveAccessibleName(label);
};

export const thenCountingUnitIs = (label: string): void => {
  expect(screen.getByRole('button', { name: label })).toBePressed();
};

export const thenCategoryIsSelected = (name: string): void => {
  expect(screen.getByRole('button', { name })).toBeVisible();
};

export const thenCategoryIsNotSelected = (name: string): void => {
  expect(screen.queryByRole('button', { name })).not.toBeInTheDocument();
};

export const thenCategoryIsOffered = (name: string): void => {
  expect(screen.getByRole('option', { name })).toBeVisible();
};

export const thenCategoryOptionIsDisabled = (name: string): void => {
  expect(screen.getByRole('option', { name })).toHaveAttribute('aria-disabled', 'true');
};

export const thenNewCategoryIsNotOffered = (): void => {
  expect(screen.queryByRole('option', { name: /新增分類/ })).not.toBeInTheDocument();
};

export const thenCategoryLimitIsShown = (): void => {
  expect(getCategoriesInput()).toHaveAccessibleDescription('最多可選 10 個分類');
};

export const thenCategoryEventuallySelected = async (name: string): Promise<void> => {
  expect(await screen.findByRole('button', { name })).toBeVisible();
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
