import { render, screen } from '@testing-library/react';
import { RootProvider } from '@/app/RootProvider';
import { configureStore } from '@/app/store';
import { type SelectOption } from '@/shared/types';
import { type ProductFormValues, QuantityUnit } from '../model';
import { type CreateCategoryFn, type OnSubmitProductFn, ProductForm } from './ProductForm';

const category: SelectOption = { id: 1, name: 'Cereals' };

const defaultValues: ProductFormValues = {
  name: 'Oat granola',
  defaultQuantity: 100,
  quantityUnit: QuantityUnit.Grams,
  categories: [category],
  calories: 412,
  protein: null,
  fats: null,
  carbs: null,
  sugar: null,
  salt: null,
  isFavorite: false,
};

const renderForm = (autoFocus: boolean): void => {
  render(
    <RootProvider store={configureStore()}>
      <ProductForm
        formId="product-form"
        autoFocus={autoFocus}
        defaultValues={defaultValues}
        categories={[category]}
        categoriesLoading={false}
        categoryCreating={false}
        onSubmit={vi.fn<OnSubmitProductFn>()}
        onCreateCategory={vi.fn<CreateCategoryFn>()}
      />
    </RootProvider>,
  );
};

test('should focus the name field when autoFocus enabled', () => {
  renderForm(true);

  expect(screen.getByRole('textbox', { name: /名稱/ })).toHaveFocus();
});

test('should not focus the name field when autoFocus disabled', () => {
  renderForm(false);

  expect(screen.getByRole('textbox', { name: /名稱/ })).not.toHaveFocus();
});
