import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { type OnSubmitProductFn, productModel } from '@/entities/product';
import {
  expectCategory,
  givenCategories,
  givenCategoryCreatedWithId,
  givenCategoryCreationFails,
  givenNutritionSuggestion,
  givenNutritionSuggestionFails,
  givenPendingNutritionSuggestion,
  givenProductInputDialog,
  thenAlertShown,
  thenCaloriesEventuallyHasValue,
  thenCaloriesHasValue,
  thenCaloriesIsInvalid,
  thenCarbsEventuallyHasValue,
  thenCategoryEventuallySelected,
  thenCategoryIsNotSelected,
  thenCategoryIsOffered,
  thenCategoryIsSelected,
  thenCategoryLimitIsShown,
  thenCategoryOptionIsDisabled,
  thenCountingUnitIs,
  thenDefaultQuantityHasValue,
  thenDefaultQuantityIsInvalid,
  thenDefaultQuantityIsLabelled,
  thenDialogShouldBeHidden,
  thenFatsEventuallyHasValue,
  thenFormValueContains,
  thenNameInputIsDisabled,
  thenNewCategoryIsNotOffered,
  thenNutritionPanelIsExpanded,
  thenProductFormIsVisible,
  thenProductNameHasValue,
  thenProductNameIsInvalid,
  thenProductNameIsValid,
  thenProteinEventuallyHasValue,
  thenSubmitIsDisabled,
  thenSubmitIsEnabled,
  thenSuggestButtonIsDisabled,
  thenSuggestButtonIsEnabled,
  whenCaloriesChanged,
  whenCarbsChanged,
  whenCategoriesOpened,
  whenCategoryNameTyped,
  whenCategoryRemoved,
  whenCategorySelected,
  whenCloseIconClicked,
  whenCountedInGrams,
  whenCountedInServings,
  whenDefaultQuantityChanged,
  whenDialogClosed,
  whenDialogOpened,
  whenFatsChanged,
  whenNewCategoryOptionClicked,
  whenNutritionPanelExpanded,
  whenPerServingNutritionEnabled,
  whenProductNameChanged,
  whenProductSaved,
  whenProteinChanged,
  whenProteinCleared,
  whenSaltChanged,
  whenSugarChanged,
  whenSuggestClicked,
} from './ProductInputDialog.fixture';

const { QuantityUnit } = productModel;

test('the product name is submitted trimmed', async () => {
  const user = userEvent.setup();
  const onSubmitMock = vi.fn<OnSubmitProductFn>();
  const categories = givenCategories('Vegetables');

  render(
    givenProductInputDialog()
      .withOnSubmitMock(onSubmitMock)
      .withCategoriesForSelect(categories)
      .withProduct({ name: '  Potato  ', categories: [categories[0]], calories: 150 })
      .please(),
  );

  await whenDialogOpened(user);
  await thenProductFormIsVisible();
  await whenProductSaved(user);

  expect(onSubmitMock).toHaveBeenCalledWith(
    expect.objectContaining<Partial<productModel.ProductFormValues>>({ name: 'Potato' }),
  );
});

test('I can add new product', async () => {
  const user = userEvent.setup();
  const onSubmitMock = vi.fn<OnSubmitProductFn>();
  const categories = givenCategories('Fruits', 'Vegetables');

  render(
    givenProductInputDialog()
      .withOnSubmitMock(onSubmitMock)
      .withCategoriesForSelect(categories)
      .please(),
  );

  await whenDialogOpened(user);
  await thenProductFormIsVisible();

  await whenProductNameChanged(user, 'Potato');
  await whenCaloriesChanged(user, '150');
  await whenDefaultQuantityChanged(user, '120');
  await whenCategorySelected(user, /vegetables/i);
  await whenProductSaved(user);
  thenFormValueContains(onSubmitMock, {
    name: 'Potato',
    calories: 150,
    defaultQuantity: 120,
    quantityUnit: QuantityUnit.Grams,
    categories: [expectCategory('Vegetables')],
    protein: null,
    fats: null,
    carbs: null,
    sugar: null,
    salt: null,
    isFavorite: false,
  });
});

test('I can edit product', async () => {
  const user = userEvent.setup();
  const onSubmitMock = vi.fn<OnSubmitProductFn>();
  const categories = givenCategories('Vegetables', 'Vegetables new');

  render(
    givenProductInputDialog()
      .withCategoriesForSelect(categories)
      .withProduct({
        name: 'Potato',
        calories: 150,
        defaultQuantity: 120,
        categories: [categories[0]],
      })
      .withOnSubmitMock(onSubmitMock)
      .please(),
  );

  await whenDialogOpened(user);
  await thenProductFormIsVisible();

  await whenProductNameChanged(user, 'Potato edited');
  await whenCaloriesChanged(user, '140');
  await whenDefaultQuantityChanged(user, '110');
  await whenCategorySelected(user, /vegetables new/i);
  await whenNutritionPanelExpanded(user);
  await whenProteinChanged(user, '1.2');
  await whenFatsChanged(user, '0.4');
  await whenCarbsChanged(user, '25.8');
  await whenSugarChanged(user, '0.7');
  await whenSaltChanged(user, '0.05');
  await whenProductSaved(user);
  thenFormValueContains(onSubmitMock, {
    name: 'Potato edited',
    calories: 140,
    defaultQuantity: 110,
    quantityUnit: QuantityUnit.Grams,
    categories: [expectCategory('Vegetables'), expectCategory('Vegetables new')],
    protein: 1.2,
    fats: 0.4,
    carbs: 25.8,
    sugar: 0.7,
    salt: 0.05,
    isFavorite: false,
  });
});

test('I cannot add product with invalid name, calories cost or default quantity', async () => {
  const user = userEvent.setup();
  const categories = givenCategories('Fruits', 'Vegetables');

  render(givenProductInputDialog().withCategoriesForSelect(categories).please());

  await whenDialogOpened(user);
  await whenProductNameChanged(user, '   ');
  await whenCaloriesChanged(user, '0');
  await whenDefaultQuantityChanged(user, '0');
  await whenProductSaved(user);
  thenProductNameIsInvalid();
  thenCaloriesIsInvalid();
  thenDefaultQuantityIsInvalid();
});

test('I cannot save a default quantity over 1000 g', async () => {
  const user = userEvent.setup();
  const onSubmitMock = vi.fn<OnSubmitProductFn>();

  render(
    givenProductInputDialog().withOnSubmitMock(onSubmitMock).withProduct({ name: 'Rice' }).please(),
  );

  await whenDialogOpened(user);
  await whenDefaultQuantityChanged(user, '1001');
  await whenProductSaved(user);

  thenDefaultQuantityIsInvalid();
  expect(onSubmitMock).not.toHaveBeenCalled();
});

test('I can save a default quantity of 1 g', async () => {
  const user = userEvent.setup();
  const onSubmitMock = vi.fn<OnSubmitProductFn>();

  render(
    givenProductInputDialog().withOnSubmitMock(onSubmitMock).withProduct({ name: 'Salt' }).please(),
  );

  await whenDialogOpened(user);
  await whenDefaultQuantityChanged(user, '1');
  await whenProductSaved(user);

  expect(onSubmitMock).toHaveBeenCalledWith(
    expect.objectContaining<Partial<productModel.ProductFormValues>>({ defaultQuantity: 1 }),
  );
});

test('Dialog input is cleared on close', async () => {
  const user = userEvent.setup();
  const categories = givenCategories('Fruits', 'Fruits new');

  render(
    givenProductInputDialog()
      .withCategoriesForSelect(categories)
      .withProduct({
        name: 'Red apple',
        calories: 60,
        defaultQuantity: 120,
        categories: [categories[0]],
      })
      .please(),
  );

  await whenDialogOpened(user);
  await whenProductNameChanged(user, 'Green apple');
  await whenCaloriesChanged(user, '70');
  await whenDefaultQuantityChanged(user, '110');
  await whenCategorySelected(user, /fruits new/i);
  await whenCountedInServings(user);
  await whenDialogClosed(user);
  await thenDialogShouldBeHidden();

  await whenDialogOpened(user);
  thenProductNameHasValue('Red apple');
  thenCaloriesHasValue(60);
  thenDefaultQuantityHasValue(120);
  thenCategoryIsSelected('Fruits');
  thenCategoryIsNotSelected('Fruits new');
  thenCountingUnitIs('按重量（克）');
});

test('New product name input is valid by default', async () => {
  const user = userEvent.setup();
  const categories = givenCategories('Fruits');

  render(givenProductInputDialog().withCategoriesForSelect(categories).please());

  await whenDialogOpened(user);
  thenProductNameIsValid();
});

describe('categories', () => {
  test('I can save a product without categories', async () => {
    const user = userEvent.setup();
    const onSubmitMock = vi.fn<OnSubmitProductFn>();
    const categories = givenCategories('Fruits', 'Vegetables');

    render(
      givenProductInputDialog()
        .withOnSubmitMock(onSubmitMock)
        .withCategoriesForSelect(categories)
        .withProduct({ name: 'Water' })
        .please(),
    );

    await whenDialogOpened(user);
    await whenProductSaved(user);

    expect(onSubmitMock).toHaveBeenCalledWith(
      expect.objectContaining<Partial<productModel.ProductFormValues>>({ categories: [] }),
    );
  });

  test('I can select several categories', async () => {
    const user = userEvent.setup();
    const onSubmitMock = vi.fn<OnSubmitProductFn>();
    const categories = givenCategories('肉類', '高蛋白', '飯麵');

    render(
      givenProductInputDialog()
        .withOnSubmitMock(onSubmitMock)
        .withCategoriesForSelect(categories)
        .withProduct({ name: '雞胸肉' })
        .please(),
    );

    await whenDialogOpened(user);
    await whenCategorySelected(user, /肉類/);
    await whenCategorySelected(user, /高蛋白/);
    await whenProductSaved(user);

    thenCategoryIsSelected('肉類');
    thenCategoryIsSelected('高蛋白');
    expect(onSubmitMock).toHaveBeenCalledWith(
      expect.objectContaining<Partial<productModel.ProductFormValues>>({
        categories: [expectCategory('肉類'), expectCategory('高蛋白')],
      }),
    );
  });

  test('a selected category is no longer offered', async () => {
    const user = userEvent.setup();
    const categories = givenCategories('肉類', '高蛋白');

    render(
      givenProductInputDialog()
        .withCategoriesForSelect(categories)
        .withProduct({ name: '雞胸肉', categories: [categories[0]] })
        .please(),
    );

    await whenDialogOpened(user);
    await whenCategoriesOpened(user);

    thenCategoryIsOffered('高蛋白');
    expect(screen.queryByRole('option', { name: '肉類' })).not.toBeInTheDocument();
  });

  test('I can remove a selected category', async () => {
    const user = userEvent.setup();
    const onSubmitMock = vi.fn<OnSubmitProductFn>();
    const categories = givenCategories('肉類', '高蛋白');

    render(
      givenProductInputDialog()
        .withOnSubmitMock(onSubmitMock)
        .withCategoriesForSelect(categories)
        .withProduct({ name: '雞胸肉', categories })
        .please(),
    );

    await whenDialogOpened(user);
    await whenCategoryRemoved(user, '肉類');
    await whenProductSaved(user);

    thenCategoryIsNotSelected('肉類');
    expect(onSubmitMock).toHaveBeenCalledWith(
      expect.objectContaining<Partial<productModel.ProductFormValues>>({
        categories: [expectCategory('高蛋白')],
      }),
    );
  });

  test('I can create a category inline by typing its name', async () => {
    const user = userEvent.setup();
    const onSubmitMock = vi.fn<OnSubmitProductFn>();
    const categories = givenCategories('肉類', '飯麵');
    const requests = givenCategoryCreatedWithId(42);

    render(
      givenProductInputDialog()
        .withOnSubmitMock(onSubmitMock)
        .withCategoriesForSelect(categories)
        .withProduct({ name: '白飯', categories: [categories[1]] })
        .please(),
    );

    await whenDialogOpened(user);
    await whenCategoryNameTyped(user, '  飯 ');
    await whenNewCategoryOptionClicked(user, '飯');
    await thenCategoryEventuallySelected('飯');
    await whenProductSaved(user);

    expect(requests.names).toStrictEqual<string[]>(['飯']);
    expect(onSubmitMock).toHaveBeenCalledWith(
      expect.objectContaining<Partial<productModel.ProductFormValues>>({
        categories: [expectCategory('飯麵'), { id: 42, name: '飯' }],
      }),
    );
  });

  test('creating a category is not offered for a name that exists in another letter case', async () => {
    const user = userEvent.setup();
    const categories = givenCategories('Fruits');

    render(
      givenProductInputDialog()
        .withCategoriesForSelect(categories)
        .withProduct({ name: 'Apple' })
        .please(),
    );

    await whenDialogOpened(user);
    await whenCategoryNameTyped(user, 'fruits');

    thenCategoryIsOffered('Fruits');
    thenNewCategoryIsNotOffered();
  });

  test('creating a category is not offered for an already selected name', async () => {
    const user = userEvent.setup();
    const categories = givenCategories('肉類');

    render(
      givenProductInputDialog()
        .withCategoriesForSelect(categories)
        .withProduct({ name: '雞胸肉', categories })
        .please(),
    );

    await whenDialogOpened(user);
    await whenCategoryNameTyped(user, '肉類');

    thenNewCategoryIsNotOffered();
  });

  test('creating a category is not offered for a name longer than 64 characters', async () => {
    const user = userEvent.setup();

    render(givenProductInputDialog().withProduct({ name: 'Apple' }).please());

    await whenDialogOpened(user);
    await whenCategoryNameTyped(user, 'a'.repeat(65));

    thenNewCategoryIsNotOffered();
  });

  test('an error is shown when creating a category fails', async () => {
    const user = userEvent.setup();
    givenCategoryCreationFails();

    render(givenProductInputDialog().withProduct({ name: '白飯' }).please());

    await whenDialogOpened(user);
    await whenCategoryNameTyped(user, '飯');
    await whenNewCategoryOptionClicked(user, '飯');

    await thenAlertShown(/category name is already taken/i);
    thenCategoryIsNotSelected('飯');
  });

  test('no more categories can be added after 10 are selected', async () => {
    const user = userEvent.setup();
    const categories = givenCategories(...Array.from({ length: 11 }, (_, i) => `分類${i + 1}`));

    render(
      givenProductInputDialog()
        .withCategoriesForSelect(categories)
        .withProduct({ name: '雜錦飯', categories: categories.slice(0, 10) })
        .please(),
    );

    await whenDialogOpened(user);
    await whenCategoriesOpened(user);

    thenCategoryOptionIsDisabled('分類11');
    thenCategoryLimitIsShown();
  });
});

describe('counting unit', () => {
  test('a new product is counted in grams by default', async () => {
    const user = userEvent.setup();

    render(givenProductInputDialog().please());

    await whenDialogOpened(user);

    thenCountingUnitIs('按重量（克）');
    thenDefaultQuantityIsLabelled('預設份量 (g)');
  });

  test('the default quantity is the weight of one piece when counted in servings', async () => {
    const user = userEvent.setup();
    const onSubmitMock = vi.fn<OnSubmitProductFn>();

    render(
      givenProductInputDialog()
        .withOnSubmitMock(onSubmitMock)
        .withProduct({ name: '豬肉韭菜餃' })
        .please(),
    );

    await whenDialogOpened(user);
    await whenCountedInServings(user);
    thenCountingUnitIs('按件／份');
    thenDefaultQuantityIsLabelled('每份重量 (g)');

    await whenDefaultQuantityChanged(user, '25');
    await whenProductSaved(user);

    expect(onSubmitMock).toHaveBeenCalledWith(
      expect.objectContaining<Partial<productModel.ProductFormValues>>({
        quantityUnit: QuantityUnit.Servings,
        defaultQuantity: 25,
      }),
    );
  });

  test('I can switch a product counted in servings back to grams', async () => {
    const user = userEvent.setup();
    const onSubmitMock = vi.fn<OnSubmitProductFn>();

    render(
      givenProductInputDialog()
        .withOnSubmitMock(onSubmitMock)
        .withProduct({ name: '白飯', quantityUnit: QuantityUnit.Servings, defaultQuantity: 200 })
        .please(),
    );

    await whenDialogOpened(user);
    thenCountingUnitIs('按件／份');
    thenDefaultQuantityIsLabelled('每份重量 (g)');

    await whenCountedInGrams(user);
    thenDefaultQuantityIsLabelled('預設份量 (g)');
    await whenProductSaved(user);

    expect(onSubmitMock).toHaveBeenCalledWith(
      expect.objectContaining<Partial<productModel.ProductFormValues>>({
        quantityUnit: QuantityUnit.Grams,
        defaultQuantity: 200,
      }),
    );
  });

  test('nutrition entered per serving is converted with the weight of one piece', async () => {
    const user = userEvent.setup();
    const onSubmitMock = vi.fn<OnSubmitProductFn>();

    render(
      givenProductInputDialog()
        .withOnSubmitMock(onSubmitMock)
        .withProduct({
          name: '雞蛋',
          quantityUnit: QuantityUnit.Servings,
          defaultQuantity: 50,
          calories: 140,
        })
        .please(),
    );

    await whenDialogOpened(user);
    await whenPerServingNutritionEnabled(user);
    thenCaloriesHasValue(70);

    await whenCaloriesChanged(user, '75');
    await whenProductSaved(user);

    expect(onSubmitMock).toHaveBeenCalledWith(
      expect.objectContaining<Partial<productModel.ProductFormValues>>({
        quantityUnit: QuantityUnit.Servings,
        defaultQuantity: 50,
        calories: 150,
      }),
    );
  });

  test('nutrition entered per serving is converted with the usual portion in grams', async () => {
    const user = userEvent.setup();
    const onSubmitMock = vi.fn<OnSubmitProductFn>();

    render(
      givenProductInputDialog()
        .withOnSubmitMock(onSubmitMock)
        .withProduct({ name: '白飯', defaultQuantity: 200, calories: 130 })
        .please(),
    );

    await whenDialogOpened(user);
    await whenPerServingNutritionEnabled(user);
    thenDefaultQuantityIsLabelled('每份份量 (g)');
    thenCaloriesHasValue(260);

    await whenProductSaved(user);

    expect(onSubmitMock).toHaveBeenCalledWith(
      expect.objectContaining<Partial<productModel.ProductFormValues>>({
        quantityUnit: QuantityUnit.Grams,
        defaultQuantity: 200,
        calories: 130,
      }),
    );
  });
});

describe('nutrition suggestions', () => {
  test('empty nutrition fields are filled after clicking a suggest button', async () => {
    const user = userEvent.setup();
    const categories = givenCategories('Dairy');
    givenNutritionSuggestion({
      calories: 402,
      protein: 25,
      fats: 33.1,
      carbs: 1.3,
      sugar: null,
      salt: 1.8,
    });

    render(
      givenProductInputDialog()
        .withCategoriesForSelect(categories)
        .withProduct({ name: 'Cheddar cheese' })
        .please(),
    );

    await whenDialogOpened(user);
    await whenNutritionPanelExpanded(user);
    await whenSuggestClicked(user, /估算蛋白質/);

    await thenProteinEventuallyHasValue('25');
    await thenFatsEventuallyHasValue('33.1');
    await thenCaloriesEventuallyHasValue('100');
  });

  test('clicking the calories button overwrites its default value', async () => {
    const user = userEvent.setup();
    const categories = givenCategories('Dairy');
    givenNutritionSuggestion({ calories: 402 });

    render(
      givenProductInputDialog()
        .withCategoriesForSelect(categories)
        .withProduct({ name: 'Cheddar cheese' })
        .please(),
    );

    await whenDialogOpened(user);
    await whenSuggestClicked(user, /估算卡路里/);

    await thenCaloriesEventuallyHasValue('402');
  });

  test('clicking a filled field button overwrites only that field', async () => {
    const user = userEvent.setup();
    const categories = givenCategories('Dairy');
    givenNutritionSuggestion({ protein: 25, carbs: 99 });

    render(
      givenProductInputDialog()
        .withCategoriesForSelect(categories)
        .withProduct({ name: 'Cheddar cheese', protein: 1.2, carbs: 5 })
        .please(),
    );

    await whenDialogOpened(user);
    await whenSuggestClicked(user, /估算蛋白質/);

    await thenProteinEventuallyHasValue('25');
    await thenCarbsEventuallyHasValue('5');
  });

  test('a cleared field is filled after clicking another field suggest button', async () => {
    const user = userEvent.setup();
    const categories = givenCategories('Dairy');
    givenNutritionSuggestion({ calories: 402, protein: 25, fats: 33.1 });

    render(
      givenProductInputDialog()
        .withCategoriesForSelect(categories)
        .withProduct({ name: 'Cheddar cheese' })
        .please(),
    );

    await whenDialogOpened(user);
    await whenNutritionPanelExpanded(user);
    await whenProteinChanged(user, '5');
    await whenProteinCleared(user);
    await whenSuggestClicked(user, /估算卡路里/);

    await thenCaloriesEventuallyHasValue('402');
    await thenProteinEventuallyHasValue('25');
  });

  test('a whitespace-only field is filled after clicking another field suggest button', async () => {
    const user = userEvent.setup();
    const categories = givenCategories('Dairy');
    givenNutritionSuggestion({ calories: 402, protein: 25 });

    render(
      givenProductInputDialog()
        .withCategoriesForSelect(categories)
        .withProduct({ name: 'Cheddar cheese' })
        .please(),
    );

    await whenDialogOpened(user);
    await whenNutritionPanelExpanded(user);
    await whenProteinChanged(user, '   ');
    await whenSuggestClicked(user, /估算卡路里/);

    await thenCaloriesEventuallyHasValue('402');
    await thenProteinEventuallyHasValue('25');
  });

  test('suggest buttons are disabled until the name is valid', async () => {
    const user = userEvent.setup();
    const categories = givenCategories('Dairy');

    render(givenProductInputDialog().withCategoriesForSelect(categories).please());

    await whenDialogOpened(user);
    thenSuggestButtonIsDisabled(/估算卡路里/);

    await whenProductNameChanged(user, 'Cheddar cheese');
    thenSuggestButtonIsEnabled(/估算卡路里/);
  });

  test.each([
    ['empty once trimmed', '    '],
    ['longer than 100 characters', 'a'.repeat(101)],
  ])('suggest buttons are disabled when the name is %s', async (_, name) => {
    const user = userEvent.setup();
    const categories = givenCategories('Dairy');

    render(givenProductInputDialog().withCategoriesForSelect(categories).please());

    await whenDialogOpened(user);
    await whenProductNameChanged(user, name);

    thenSuggestButtonIsDisabled(/估算卡路里/);
  });

  test('the trimmed name is sent for suggestion', async () => {
    const user = userEvent.setup();
    const categories = givenCategories('Dairy');
    const requests = givenNutritionSuggestion({ calories: 402 });

    render(
      givenProductInputDialog()
        .withCategoriesForSelect(categories)
        .withProduct({ name: '  Cheddar cheese  ' })
        .please(),
    );

    await whenDialogOpened(user);
    await whenSuggestClicked(user, /估算卡路里/);
    await thenCaloriesEventuallyHasValue('402');

    expect(requests.names).toStrictEqual<string[]>(['Cheddar cheese']);
  });

  test('inputs and submit are disabled while a suggestion is pending', async () => {
    const user = userEvent.setup();
    const categories = givenCategories('Dairy');
    const pendingSuggestion = givenPendingNutritionSuggestion({ calories: 402 });

    render(
      givenProductInputDialog()
        .withCategoriesForSelect(categories)
        .withProduct({ name: 'Cheddar cheese' })
        .please(),
    );

    await whenDialogOpened(user);
    await whenSuggestClicked(user, /估算卡路里/);

    thenNameInputIsDisabled();
    thenSubmitIsDisabled();

    pendingSuggestion.resolve();
    await thenCaloriesEventuallyHasValue('402');
    await thenSubmitIsEnabled();
  });

  test('API failure shows an error message', async () => {
    const user = userEvent.setup();
    const categories = givenCategories('Dairy');
    givenNutritionSuggestionFails();

    render(
      givenProductInputDialog()
        .withCategoriesForSelect(categories)
        .withProduct({ name: 'Cheddar cheese' })
        .please(),
    );

    await whenDialogOpened(user);
    await whenSuggestClicked(user, /估算卡路里/);

    await thenAlertShown(/model response was invalid/i);
  });

  test('info message shown when nothing can be estimated', async () => {
    const user = userEvent.setup();
    const categories = givenCategories('Dairy');
    givenNutritionSuggestion();

    render(
      givenProductInputDialog()
        .withCategoriesForSelect(categories)
        .withProduct({ name: 'Cheddar cheese' })
        .please(),
    );

    await whenDialogOpened(user);
    await whenSuggestClicked(user, /估算卡路里/);

    await thenAlertShown(/未能估算/);
  });

  test('nutrition panel auto-expands when a calories suggestion also fills macro fields', async () => {
    const user = userEvent.setup();
    const categories = givenCategories('Dairy');
    givenNutritionSuggestion({
      calories: 402,
      protein: 25,
      fats: 33.1,
      carbs: 1.3,
      sugar: null,
      salt: 1.8,
    });

    render(
      givenProductInputDialog()
        .withCategoriesForSelect(categories)
        .withProduct({ name: 'Cheddar cheese' })
        .please(),
    );

    await whenDialogOpened(user);
    await whenSuggestClicked(user, /估算卡路里/);

    await thenNutritionPanelIsExpanded();
  });

  test('closing the dialog while a suggestion is pending is blocked', async () => {
    const user = userEvent.setup();
    const categories = givenCategories('Dairy');
    const pendingSuggestion = givenPendingNutritionSuggestion({ calories: 402 });

    render(
      givenProductInputDialog()
        .withCategoriesForSelect(categories)
        .withProduct({ name: 'Cheddar cheese' })
        .please(),
    );

    await whenDialogOpened(user);
    await whenSuggestClicked(user, /估算卡路里/);

    await whenCloseIconClicked(user);
    await thenProductFormIsVisible();

    pendingSuggestion.resolve();
    await thenCaloriesEventuallyHasValue('402');
  });
});
