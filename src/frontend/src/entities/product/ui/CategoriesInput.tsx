import {
  Autocomplete,
  CircularProgress,
  createFilterOptions,
  type FilterOptionsState,
  TextField,
} from '@mui/material';
import { type FC, type Ref } from 'react';
import { validateCategoryName } from '@/shared/lib';
import { type SelectOption } from '@/shared/types';
import { MAX_PRODUCT_CATEGORIES } from '../model';

interface NewCategoryOption {
  newCategoryName: string;
}

type CategoryOption = SelectOption | NewCategoryOption;

const isNewCategory = (option: CategoryOption): option is NewCategoryOption =>
  'newCategoryName' in option;

const isExistingCategory = (option: CategoryOption): option is SelectOption =>
  !isNewCategory(option);

const getOptionLabel = (option: CategoryOption): string =>
  isNewCategory(option) ? `新增分類「${option.newCategoryName}」` : option.name;

const isSameCategory = (first: CategoryOption, second: CategoryOption): boolean =>
  isExistingCategory(first) && isExistingCategory(second) && first.id === second.id;

const filterExistingCategories = createFilterOptions<CategoryOption>();

interface Props {
  value: SelectOption[];
  options: SelectOption[];
  error: boolean;
  helperText: string;
  loading: boolean;
  disabled: boolean;
  creating: boolean;
  ref?: Ref<HTMLInputElement>;
  onChange: (categories: SelectOption[]) => void;
  onCreate: (name: string) => void;
  onBlur: () => void;
}

export const CategoriesInput: FC<Props> = ({
  value,
  options,
  error,
  helperText,
  loading,
  disabled,
  creating,
  ref,
  onChange,
  onCreate,
  onBlur,
}) => {
  const limitReached = value.length >= MAX_PRODUCT_CATEGORIES;

  // MUI warns about selected values missing from the options, which a category created
  // inline is until the categories are refetched
  const optionsWithSelected = [
    ...options,
    ...value.filter(selected => !options.some(option => option.id === selected.id)),
  ];

  const isNameTaken = (name: string): boolean => {
    const normalizedName = name.toLocaleLowerCase();
    return optionsWithSelected.some(
      category => category.name.toLocaleLowerCase() === normalizedName,
    );
  };

  const filterOptions = (
    available: CategoryOption[],
    state: FilterOptionsState<CategoryOption>,
  ): CategoryOption[] => {
    const filtered = filterExistingCategories(available, state);
    const newCategoryName = state.inputValue.trim();

    return !loading && validateCategoryName(newCategoryName) && !isNameTaken(newCategoryName)
      ? [...filtered, { newCategoryName }]
      : filtered;
  };

  const handleChange = (selected: CategoryOption[]): void => {
    const newCategory = selected.find(isNewCategory);

    if (newCategory) {
      onCreate(newCategory.newCategoryName);
    } else {
      onChange(selected.filter(isExistingCategory));
    }
  };

  return (
    <Autocomplete<CategoryOption, true>
      multiple
      autoHighlight
      filterSelectedOptions
      blurOnSelect="touch"
      value={value}
      options={optionsWithSelected}
      disabled={disabled}
      readOnly={creating}
      loading={loading}
      filterOptions={filterOptions}
      getOptionLabel={getOptionLabel}
      getOptionDisabled={() => limitReached}
      isOptionEqualToValue={isSameCategory}
      onChange={(_, selected) => handleChange(selected)}
      onBlur={onBlur}
      slotProps={{ chip: { size: 'small' } }}
      renderInput={params => (
        <TextField
          {...params}
          inputRef={ref}
          label="分類（選填）"
          placeholder={value.length === 0 ? '選擇或輸入新分類' : undefined}
          margin="normal"
          error={error}
          helperText={
            !error && limitReached ? `最多可選 ${MAX_PRODUCT_CATEGORIES} 個分類` : helperText
          }
          slotProps={{
            ...params.slotProps,
            input: {
              ...params.slotProps.input,
              endAdornment:
                loading || creating ? (
                  <CircularProgress
                    color="inherit"
                    size={20}
                    aria-label={creating ? '正在新增分類' : '正在載入分類'}
                  />
                ) : (
                  params.slotProps.input.endAdornment
                ),
            },
          }}
        />
      )}
    />
  );
};
