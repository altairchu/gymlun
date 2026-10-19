import { Alert, Stack } from '@mui/material';
import { type FC, useState } from 'react';
import { categoryApi } from '@/entities/category';
import { ProductForm, type productModel } from '@/entities/product';
import { type SelectOption } from '@/shared/types';
import { Button, Dialog } from '@/shared/ui';
import { type PhotoChange } from '../../model';
import { ProductPhotoInput } from '../ProductPhotoInput';

interface ProductInputDialogProps {
  opened: boolean;
  title: string;
  submitText: string;
  isLoading: boolean;
  categories: SelectOption[];
  categoriesLoading: boolean;
  productFormValues: productModel.ProductFormValues;
  savedPhotoUrl: string | null;
  photo: PhotoChange;
  photoSaveFailed: boolean;
  onSubmit: (product: productModel.ProductFormValues) => Promise<void>;
  onPhotoChange: (photo: PhotoChange) => void;
  onClose: () => void;
}

export const ProductInputDialog: FC<ProductInputDialogProps> = ({
  opened,
  title,
  submitText,
  isLoading,
  categories,
  categoriesLoading,
  productFormValues,
  savedPhotoUrl,
  photo,
  photoSaveFailed,
  onSubmit,
  onPhotoChange,
  onClose,
}) => {
  const [isNutritionSuggesting, setIsNutritionSuggesting] = useState(false);
  const [createCategory, createCategoryRequest] = categoryApi.useCreateCategoryMutation();

  const handleClose = (): void => {
    if (!isNutritionSuggesting) {
      onClose();
    }
  };

  const handleCreateCategory = async (name: string): Promise<SelectOption> => {
    const id = await createCategory({ name }).unwrap();
    return { id, name };
  };

  return (
    <Dialog
      pinToTop
      renderMode="fullScreenOnMobile"
      title={title}
      opened={opened}
      onClose={handleClose}
      content={
        <Stack spacing={2}>
          <ProductPhotoInput
            savedPhotoUrl={savedPhotoUrl}
            change={photo}
            disabled={isLoading}
            onChange={onPhotoChange}
          />
          {photoSaveFailed && <Alert severity="error">相片儲存唔到，請再試一次</Alert>}
          <ProductForm
            formId="product-input-form"
            autoFocus
            defaultValues={productFormValues}
            categories={categories}
            categoriesLoading={categoriesLoading}
            categoryCreating={createCategoryRequest.isLoading}
            onSubmit={onSubmit}
            onCreateCategory={handleCreateCategory}
            onNutritionSuggestingChange={setIsNutritionSuggesting}
          />
        </Stack>
      }
      renderSubmit={submitProps => (
        <Button
          {...submitProps}
          type="submit"
          form="product-input-form"
          disabled={isNutritionSuggesting || createCategoryRequest.isLoading}
          loading={isLoading}
        >
          {submitText}
        </Button>
      )}
      renderCancel={cancelProps => (
        <Button
          {...cancelProps}
          type="button"
          disabled={isLoading || isNutritionSuggesting}
          onClick={handleClose}
        >
          取消
        </Button>
      )}
    />
  );
};
