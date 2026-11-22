import { Alert, Button, LinearProgress } from '@mui/material';
import { type FC, useState } from 'react';
import { type BodyMeasurementResult, useGetBodyCompositionQuery } from '@/entities/gym/gymApi';
import { dateLib } from '@/shared/lib';
import { PageContainer } from '@/shared/ui';
import { bodyFieldGroups, bodyProfileFields, bodyRegions, segmentFields } from './bodyFields';
import { BodyNavigation } from './BodyNavigation';
import { formatValue } from './fields';

interface Metric {
  key: string;
  label: string;
  unit: string;
  group: string;
  reading: (entry: BodyMeasurementResult) => number | null | undefined;
  isCalculated: (entry: BodyMeasurementResult) => boolean;
}

const reportMetrics: Metric[] = bodyFieldGroups.flatMap(([group, fields]) =>
  fields.map(([key, label, unit]) => ({
    key,
    label,
    unit,
    group,
    reading: (entry: BodyMeasurementResult) => entry.metrics[key] ?? entry.derivedMetrics?.[key],
    isCalculated: (entry: BodyMeasurementResult) => entry.metrics[key] == null,
  })),
);

const profileMetrics: Metric[] = bodyProfileFields.map(([key, label, unit]) => ({
  key,
  label,
  unit,
  group: '量度資料',
  reading: entry => entry.profile?.[key],
  isCalculated: () => false,
}));

const segmentMetrics: Metric[] = bodyRegions.flatMap(([region, name]) =>
  segmentFields.map(([field, label]) => ({
    key: `${region}-${field}`,
    label: `${name}${label}`,
    unit: 'kg',
    group: '分部量度',
    reading: (entry: BodyMeasurementResult) => entry.segments?.[region]?.[field],
    isCalculated: () => false,
  })),
);

const metrics = [...reportMetrics, ...profileMetrics, ...segmentMetrics];
const groups = [...new Set(metrics.map(metric => metric.group))];
const dayMs = 24 * 60 * 60 * 1000;

interface Sample {
  id: number;
  time: number;
  reportIndex: number;
  date: string;
  value: number;
  calculated: boolean;
}

interface BodyChartProps {
  metric: Metric;
  samples: Sample[];
}

const BodyChart: FC<BodyChartProps> = ({ metric, samples }) => {
  const first = samples.at(0);
  const latest = samples.at(-1);
  if (first === undefined || latest === undefined) {
    return <p className="gym-card gym-muted">呢段時間未有呢項指標嘅數據。</p>;
  }

  const values = samples.map(sample => sample.value);
  const min = Math.min(...values);
  const max = Math.max(...values);
  const verticalPadding = Math.max((max - min) * 0.08, Math.abs(max) * 0.01, 0.01);
  const low = min - verticalPadding;
  const span = max - low + verticalPadding;
  const coordinates = samples.map(sample => ({
    ...sample,
    x:
      first.time === latest.time
        ? 160
        : 14 + (292 * (sample.time - first.time)) / (latest.time - first.time),
    y: 115 - (99 * (sample.value - low)) / span,
  }));
  const points = coordinates
    .map(({ x, y, reportIndex }, index) => {
      const previous = coordinates.at(index - 1);
      const command = index > 0 && previous?.reportIndex === reportIndex - 1 ? 'L' : 'M';
      return `${command}${x.toFixed(2)},${y.toFixed(2)}`;
    })
    .join(' ');
  const delta = latest.value - first.value;
  const unit = metric.unit === '%' ? '百分點' : metric.unit;
  const measured = samples.filter(sample => !sample.calculated).length;

  return (
    <section className="gym-card gym-analysis-chart">
      <div className="gym-feature-heading">
        <div>
          <h3>{metric.label}</h3>
          <strong>{formatValue(latest.value, metric.unit)}</strong>
        </div>
        {samples.length > 1 ? (
          <p className="gym-muted">
            變化 {delta > 0 ? '+' : ''}
            {formatValue(delta, unit)}
          </p>
        ) : null}
      </div>
      <figure>
        <svg
          viewBox="0 0 320 132"
          role="img"
          aria-label={`${metric.label}趨勢，${samples.length} 次量度`}
        >
          <path
            d="M14 17H306 M14 66H306 M14 115H306"
            fill="none"
            stroke="currentColor"
            strokeOpacity=".16"
          />
          <path
            d={points}
            fill="none"
            stroke="currentColor"
            strokeWidth="2.5"
            strokeLinecap="round"
          />
          {coordinates.map(sample => (
            <circle
              key={sample.id}
              cx={sample.x}
              cy={sample.y}
              r={4}
              className={sample.calculated ? 'gym-derived-point' : undefined}
              stroke="currentColor"
              fill="currentColor"
              strokeWidth="1.5"
            >
              <title>
                {sample.date} · {formatValue(sample.value, metric.unit)}
              </title>
            </circle>
          ))}
        </svg>
        <figcaption>
          {first.date}
          {first.date === latest.date ? '' : ` 至 ${latest.date}`} · {measured} 筆原始 ·{' '}
          {samples.length - measured} 筆計算
        </figcaption>
      </figure>
      <details>
        <summary>檢視量度紀錄</summary>
        <ul className="gym-analysis-values">
          {samples.map(sample => (
            <li key={sample.id}>
              <time dateTime={sample.date}>{sample.date}</time>
              <span>
                {formatValue(sample.value, metric.unit)}
                {sample.calculated ? '（計算）' : ''}
              </span>
            </li>
          ))}
        </ul>
      </details>
    </section>
  );
};

export const BodyAnalysisPage: FC = () => {
  const { data, isLoading, isError, refetch } = useGetBodyCompositionQuery();
  const [metricKey, setMetricKey] = useState('bodyFatPercent');
  const [range, setRange] = useState('all');
  const [today] = useState(() =>
    Date.parse(dateLib.formatToISOStringWithoutTime(dateLib.getCurrentDate())),
  );
  const metric = metrics.find(option => option.key === metricKey) ?? metrics[0];
  const earliest = range === 'all' ? -Infinity : today - (Number(range) - 1) * dayMs;
  const samples = (data ?? [])
    .toSorted((left, right) => left.date.localeCompare(right.date) || left.id - right.id)
    .flatMap((entry, reportIndex) => {
      const value = metric.reading(entry);
      const time = Date.parse(entry.date);
      return value === null || value === undefined || !Number.isFinite(time) || time < earliest
        ? []
        : [
            {
              id: entry.id,
              time,
              date: entry.date,
              value,
              reportIndex,
              calculated: metric.isCalculated(entry),
            },
          ];
    });

  return (
    <PageContainer className="gym-feature-page gym-analysis">
      <BodyNavigation />
      <h2>數據分析</h2>
      <div className="gym-analysis-controls gym-card">
        <label>
          指標
          <select value={metricKey} onChange={event => setMetricKey(event.currentTarget.value)}>
            {groups.map(group => (
              <optgroup key={group} label={group}>
                {metrics
                  .filter(option => option.group === group)
                  .map(option => (
                    <option key={option.key} value={option.key}>
                      {option.label}
                    </option>
                  ))}
              </optgroup>
            ))}
          </select>
        </label>
        <label>
          日期範圍
          <select value={range} onChange={event => setRange(event.currentTarget.value)}>
            <option value="30">近 30 日</option>
            <option value="90">近 90 日</option>
            <option value="all">全部</option>
          </select>
        </label>
      </div>
      {isLoading ? <LinearProgress aria-label="讀取數據分析" /> : null}
      {isError ? (
        <Alert
          severity="error"
          action={
            <Button
              onClick={() => {
                void refetch();
              }}
            >
              重試
            </Button>
          }
        >
          未能讀取數據分析
        </Alert>
      ) : null}
      {data ? <BodyChart metric={metric} samples={samples} /> : null}
    </PageContainer>
  );
};
