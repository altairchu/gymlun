import { productApi } from '@/entities/product';
import { type PhotoChange } from '../model';

interface Result {
  /** Resolves to false when the photo could not be saved */
  savePhoto: (productId: number, change: PhotoChange) => Promise<boolean>;
  isSavingPhoto: boolean;
}

export const useSaveProductPhoto = (): Result => {
  const [setProductPhoto, setResult] = productApi.useSetProductPhotoMutation();
  const [deleteProductPhoto, deleteResult] = productApi.useDeleteProductPhotoMutation();

  const savePhoto = async (productId: number, change: PhotoChange): Promise<boolean> => {
    switch (change.type) {
      case 'unchanged':
        return true;
      case 'set':
        return (await setProductPhoto({ productId, photo: change.photo })).error === undefined;
      case 'removed':
        return (await deleteProductPhoto(productId)).error === undefined;
    }
  };

  return { savePhoto, isSavingPhoto: setResult.isLoading || deleteResult.isLoading };
};
