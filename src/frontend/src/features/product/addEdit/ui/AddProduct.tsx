import AddIcon from '@mui/icons-material/Add';
import { IconButton, Tooltip } from '@mui/material';
import { type FC, type ReactElement, useMemo, useState } from 'react';
import { useAppDispatch } from '@/app/store';
import { categoryLib } from '@/entities/category';
import { productApi, productModel } from '@/entities/product';
import { mapToCreateProductRequest } from '../lib';
import { ProductInputDialog } from './ProductInputDialog';

interface Props {
  initialName?: string;
  renderTrigger?: (openDialog: () => void) => ReactElement;
  onCreated?: (productId: number) => void;
}

const renderDefaultTrigger = (openDialog: () => void): ReactElement => (
  <Tooltip title="加入新食物">
    <IconButton size="large" aria-label="加入新食物" onClick={openDialog}>
      <AddIcon />
    </IconButton>
  </Tooltip>
);

export const AddProduct: FC<Props> = ({
  initialName = '',
  renderTrigger = renderDefaultTrigger,
  onCreated,
}) => {
  const dispatch = useAppDispatch();
  const [isDialogOpened, setIsDialogOpened] = useState(false);
  const [isSaving, setIsSaving] = useState(false);
  const { categories, categoriesLoading } = categoryLib.useCategoriesForSelect();
  const [createProduct] = productApi.useCreateProductMutation();

  const productFormValues = useMemo<productModel.ProductFormValues>(
    () => ({ ...productModel.EMPTY_FORM_VALUES, name: initialName }),
    [initialName],
  );

  const handleDialogOpen = (): void => {
    setIsDialogOpened(true);
  };

  const handleDialogSubmit = async (formData: productModel.ProductFormValues): Promise<void> => {
    setIsSaving(true);

    try {
      const { data } = await createProduct(mapToCreateProductRequest(formData));

      if (data === undefined) {
        return;
      }

      // Keeps the dialog open until the queries refetched by the product tag invalidation
      // have settled, so the caller already sees the new product
      await Promise.all(dispatch(productApi.util.getRunningQueriesThunk()));
      setIsDialogOpened(false);
      onCreated?.(data.id);
    } finally {
      setIsSaving(false);
    }
  };

  const handleDialogClose = (): void => {
    setIsDialogOpened(false);
  };

  return (
    <>
      {renderTrigger(handleDialogOpen)}
      <ProductInputDialog
        opened={isDialogOpened}
        title="新食物"
        submitText="加入"
        isLoading={isSaving}
        productFormValues={productFormValues}
        categories={categories}
        categoriesLoading={categoriesLoading}
        onSubmit={handleDialogSubmit}
        onClose={handleDialogClose}
      />
    </>
  );
};
