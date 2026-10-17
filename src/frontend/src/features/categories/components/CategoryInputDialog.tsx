import { TextField } from '@mui/material';
import {
  type FC,
  useEffect,
  type Dispatch,
  type SetStateAction,
  type FormEventHandler,
} from 'react';
import { type categoryModel } from '@/entities/category';
import { useInput } from '@/shared/hooks';
import { mapToTextInputProps, validateCategoryName } from '@/shared/lib';
import { Button, AppDialog } from '@/shared/ui';
import { type CategoryFormData } from '../types';

interface CreateEditCategoryDialogProps {
  isOpened: boolean;
  setIsOpened: Dispatch<SetStateAction<boolean>>;
  title: string;
  submitText: string;
  onSubmit: (category: CategoryFormData) => void;
  loading: boolean;
  category?: categoryModel.Category;
}

const CategoryInputDialog: FC<CreateEditCategoryDialogProps> = ({
  isOpened: isDialogOpened,
  setIsOpened: setIsDialogOpened,
  title,
  submitText,
  onSubmit,
  loading,
  category,
}) => {
  const {
    inputProps: categoryNameInputProps,
    value: categoryName,
    clearValue: clearCategoryName,
    isInvalid: isCategoryNameInvalid,
    isTouched: isCategoryNameTouched,
  } = useInput({
    initialValue: category?.name ?? '',
    errorHelperText: '分類名稱無效',
    validate: validateCategoryName,
    mapToInputProps: mapToTextInputProps,
  });

  const isSubmitDisabled = isCategoryNameInvalid || !isCategoryNameTouched;

  useEffect(() => {
    if (isDialogOpened) {
      clearCategoryName();
    }
  }, [isDialogOpened, clearCategoryName, category]);

  const handleClose = (): void => {
    setIsDialogOpened(false);
  };

  const handleSubmit: FormEventHandler<HTMLFormElement> = event => {
    event.preventDefault();
    onSubmit({ name: categoryName });
  };

  return (
    <AppDialog
      title={title}
      isOpened={isDialogOpened}
      onClose={handleClose}
      content={
        <form id="category-input-form" onSubmit={handleSubmit}>
          <TextField
            {...categoryNameInputProps}
            autoFocus
            margin="dense"
            fullWidth
            label="分類名稱"
            placeholder="輸入分類名稱"
          />
        </form>
      }
      actionSubmit={
        <Button
          type="submit"
          form="category-input-form"
          variant="text"
          color="primary"
          disabled={isSubmitDisabled}
          loading={loading}
          aria-label={`${category ? '儲存' : '新增'}「${categoryName}」並關閉對話框`}
        >
          {submitText}
        </Button>
      }
      actionCancel={
        <Button
          type="button"
          variant="text"
          color="inherit"
          disabled={loading}
          onClick={handleClose}
        >
          取消
        </Button>
      }
    />
  );
};

export default CategoryInputDialog;
