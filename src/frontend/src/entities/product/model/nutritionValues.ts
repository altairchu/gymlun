import AdjustOutlinedIcon from '@mui/icons-material/AdjustOutlined';
import CookieOutlinedIcon from '@mui/icons-material/CookieOutlined';
import FitnessCenterOutlinedIcon from '@mui/icons-material/FitnessCenterOutlined';
import GrainOutlinedIcon from '@mui/icons-material/GrainOutlined';
import WaterDropOutlinedIcon from '@mui/icons-material/WaterDropOutlined';
import WhatshotOutlinedIcon from '@mui/icons-material/WhatshotOutlined';
import { type SvgIconProps } from '@mui/material';
import { blue, pink, grey, amber, purple, deepOrange } from '@mui/material/colors';
import { type ComponentType } from 'react';

export type NutritionValueType = 'calories' | 'protein' | 'fats' | 'carbs' | 'sugar' | 'salt';

type RequiredNutritionValueType = Extract<NutritionValueType, 'calories'>;
type OptionalNutritionValueType = Exclude<NutritionValueType, 'calories'>;

export type OptionalNutritionValues = Record<OptionalNutritionValueType, number | null>;
export type NutritionValues = Record<RequiredNutritionValueType, number> & OptionalNutritionValues;

type Unit = 'kcal' | 'g';

interface NutritionValueConfig {
  readonly label: string;
  readonly IconComponent: ComponentType<SvgIconProps>;
  readonly color: string;
  readonly unit: Unit;
}

type NutritionValuesConfig = Readonly<Record<NutritionValueType, NutritionValueConfig>>;

export const nutritionValuesConfig: NutritionValuesConfig = {
  calories: {
    label: '卡路里',
    IconComponent: WhatshotOutlinedIcon,
    color: deepOrange[500],
    unit: 'kcal',
  },
  protein: {
    label: '蛋白質',
    IconComponent: FitnessCenterOutlinedIcon,
    color: blue[500],
    unit: 'g',
  },
  fats: {
    label: '脂肪',
    IconComponent: WaterDropOutlinedIcon,
    color: amber.A700,
    unit: 'g',
  },
  carbs: {
    label: '碳水化合物',
    IconComponent: GrainOutlinedIcon,
    color: purple[500],
    unit: 'g',
  },
  sugar: {
    label: '糖',
    IconComponent: CookieOutlinedIcon,
    color: pink[500],
    unit: 'g',
  },
  salt: {
    label: '鹽',
    IconComponent: AdjustOutlinedIcon,
    color: grey[500],
    unit: 'g',
  },
};

export const hasMissingNutritionValues = (nutritionValues: OptionalNutritionValues): boolean =>
  Object.values(nutritionValues).every(value => value === null);
