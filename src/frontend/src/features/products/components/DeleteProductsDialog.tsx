import { Typography } from '@mui/material';
import {
  useEffect,
  type FC,
  type Dispatch,
  type SetStateAction,
  type FormEventHandler,
} from 'react';
import { productApi, productLib } from '@/entities/product';
import { Button, AppDialog } from '@/shared/ui';

interface DeleteProductsDialogProps {
  isOpened: boolean;
  setIsOpened: Dispatch<SetStateAction<boolean>>;
}

const DeleteProductsDialog: FC<DeleteProductsDialogProps> = ({
  isOpened: isDialogOpened,
  setIsOpened: setIsDialogOpened,
}) => {
  const checkedProductIds = productLib.useCheckedProductIds();
  const products = productLib.useProducts();
  const [deleteProducts, deleteProductRequest] = productApi.useDeleteProductsMutation();

  useEffect(() => {
    if (deleteProductRequest.isSuccess && products.isChanged) {
      setIsDialogOpened(false);
    }
  }, [deleteProductRequest.isSuccess, products.isChanged, setIsDialogOpened]);

  const handleClose = (): void => {
    setIsDialogOpened(false);
  };

  const handleSubmit: FormEventHandler<HTMLFormElement> = event => {
    event.preventDefault();
    void deleteProducts({ ids: checkedProductIds });
  };

  return (
    <AppDialog
      title="刪除食物"
      isOpened={isDialogOpened}
      content={
        <form id="delete-products" onSubmit={handleSubmit}>
          <Typography>確定要刪除已選取的食物嗎？</Typography>
        </form>
      }
      actionSubmit={
        <Button
          type="submit"
          form="delete-products"
          variant="text"
          color="error"
          loading={deleteProductRequest.isLoading || products.isFetching}
          autoFocus
        >
          是
        </Button>
      }
      actionCancel={
        <Button
          type="button"
          variant="text"
          color="inherit"
          onClick={handleClose}
          disabled={deleteProductRequest.isLoading}
        >
          否
        </Button>
      }
      onClose={handleClose}
    />
  );
};

export default DeleteProductsDialog;
