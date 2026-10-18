import { type ReactElement, type FC, useState, useEffect, useMemo } from 'react';
import { categoryLib } from '@/entities/category';
import { type Product, productApi, productLib, type productModel } from '@/entities/product';
import { mapToEditProductRequest, useSaveProductPhoto } from '../lib';
import { type PhotoChange, PHOTO_UNCHANGED } from '../model';
import { ProductInputDialog } from './ProductInputDialog';

interface Props {
  product: Product;
  renderTrigger: (openDialog: () => void) => ReactElement;
}

export const EditProduct: FC<Props> = ({ product, renderTrigger }) => {
  const [isEditDialogOpened, setIsEditDialogOpened] = useState(false);
  const [editProduct, editProductRequest] = productApi.useEditProductMutation();
  const { savePhoto, isSavingPhoto } = useSaveProductPhoto();
  const [photo, setPhoto] = useState<PhotoChange>(PHOTO_UNCHANGED);
  const [photoSaveFailed, setPhotoSaveFailed] = useState(false);
  const products = productLib.useProducts();
  const productFormData = useMemo(() => productLib.mapToProductFormData(product), [product]);
  const { categories, categoriesLoading } = categoryLib.useCategoriesForSelect();

  useEffect(() => {
    if (editProductRequest.isSuccess && products.isChanged) {
      setIsEditDialogOpened(false);
    }
  }, [editProductRequest.isSuccess, products.isChanged]);

  const handleEditClick = (): void => {
    setPhoto(PHOTO_UNCHANGED);
    setPhotoSaveFailed(false);
    setIsEditDialogOpened(true);
  };

  const handleEditDialogSubmit = async (
    formData: productModel.ProductFormValues,
  ): Promise<void> => {
    const photoSaved = await savePhoto(product.id, photo);
    setPhotoSaveFailed(!photoSaved);

    if (photoSaved) {
      await editProduct(mapToEditProductRequest(product.id, formData));
    }
  };

  const handleEditDialogClose = (): void => {
    setIsEditDialogOpened(false);
  };

  return (
    <>
      {renderTrigger(handleEditClick)}
      <ProductInputDialog
        opened={isEditDialogOpened}
        title="編輯食物"
        submitText="儲存"
        isLoading={editProductRequest.isLoading || products.isFetching || isSavingPhoto}
        productFormValues={productFormData}
        savedPhotoUrl={product.photoUrl}
        photo={photo}
        photoSaveFailed={photoSaveFailed}
        categories={categories}
        categoriesLoading={categoriesLoading}
        onSubmit={handleEditDialogSubmit}
        onPhotoChange={setPhoto}
        onClose={handleEditDialogClose}
      />
    </>
  );
};
