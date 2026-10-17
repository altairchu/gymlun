import { useTheme } from '@mui/material';
import { LineChart } from '@mui/x-charts/LineChart';
import { type FC } from 'react';
import { type WeightLogItem, weightLogsApi, type GetWeightLogsRequest } from '@/entities/weightLog';
import { dateLib } from '@/shared/lib';

const mapToDatasetElementType = ({
  date,
  value,
}: WeightLogItem): Record<string, Date | number> => ({
  date: new Date(date),
  value,
});

const DAY_MS = 24 * 60 * 60 * 1000;

// A time axis with a single date has an empty domain, which renders NaN paths and an empty chart
const getSinglePointDomain = (
  dataset: Record<string, Date | number>[],
): { min?: Date; max?: Date } => {
  const [onlyPoint] = dataset;

  if (dataset.length !== 1 || !(onlyPoint.date instanceof Date)) {
    return {};
  }

  return {
    min: new Date(onlyPoint.date.getTime() - DAY_MS),
    max: new Date(onlyPoint.date.getTime() + DAY_MS),
  };
};

interface Props {
  weightLogsRequest: GetWeightLogsRequest;
}

export const WeightChart: FC<Props> = ({ weightLogsRequest }) => {
  const theme = useTheme();

  const { dataset } = weightLogsApi.useWeightLogsQuery(weightLogsRequest, {
    selectFromResult: ({ data }) => ({
      dataset: data?.weightLogs?.map(mapToDatasetElementType).reverse() ?? [],
    }),
  });

  return (
    <LineChart
      colors={[theme.palette.primary.main]}
      xAxis={[
        {
          dataKey: 'date',
          scaleType: 'time',
          tickMinStep: DAY_MS,
          ...getSinglePointDomain(dataset),
          valueFormatter: (value: Date, context) =>
            context.location === 'tick'
              ? dateLib.formatToDayAndMonthString(value)
              : dateLib.formatToUserFriendlyString(value),
        },
      ]}
      yAxis={[{ width: 56, valueFormatter: (value: number) => value.toFixed(1) }]}
      series={[
        {
          dataKey: 'value',
          label: '體重 (kg)',
          showMark: true,
        },
      ]}
      hideLegend
      localeText={{ loading: '載入數據中…', noData: '未有數據' }}
      dataset={dataset}
      height={300}
    />
  );
};
