import AddIcon from '@mui/icons-material/Add';
import { IconButton, Tooltip } from '@mui/material';
import { type FC, type ReactElement, useMemo, useState } from 'react';
import { useAppDispatch } from '@/app/store';
import { categoryLib } from '@/entities/category';
import { productApi, productModel } from '@/entities/product';
import { mapToCreateProductRequest, useSaveProductPhoto } from '../lib';
import { type PhotoChange, PHOTO_UNCHANGED } from '../model';
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
  const [photo, setPhoto] = useState<PhotoChange>(PHOTO_UNCHANGED);
  const { categories, categoriesLoading } = categoryLib.useCategoriesForSelect();
  const [createProduct] = productApi.useCreateProductMutation();
  const { savePhoto } = useSaveProductPhoto();

  const productFormValues = useMemo<productModel.ProductFormValues>(
    () => ({ ...productModel.EMPTY_FORM_VALUES, name: initialName }),
    [initialName],
  );

  const handleDialogOpen = (): void => {
    setPhoto(PHOTO_UNCHANGED);
    setIsDialogOpened(true);
  };

  const handleDialogSubmit = async (formData: productModel.ProductFormValues): Promise<void> => {
    setIsSaving(true);

    try {
      const { data } = await createProduct(mapToCreateProductRequest(formData));

      if (data === undefined) {
        return;
      }

      // Keeping the dialog open after a failed photo would add the product twice on the next try
      await savePhoto(data.id, photo);

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
        savedPhotoUrl={null}
        photo={photo}
        photoSaveFailed={false}
        categories={categories}
        categoriesLoading={categoriesLoading}
        onSubmit={handleDialogSubmit}
        onPhotoChange={setPhoto}
        onClose={handleDialogClose}
      />
    </>
  );
};
