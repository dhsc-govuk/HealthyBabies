import React from 'react';
import {
  BarElement,
  CategoryScale,
  Chart as ChartJS,
  ChartData,
  ChartOptions,
  Legend,
  LinearScale,
  Tooltip,
} from 'chart.js';
import ChartDataLabels from 'chartjs-plugin-datalabels';
import { Bar } from 'react-chartjs-2';
import { StrandForecastDto } from '../queries';
import { ALLOCATION_STRANDS } from './constants';

ChartJS.register(CategoryScale, LinearScale, BarElement, Tooltip, Legend, ChartDataLabels);

interface SpendChartProps {
  data: StrandForecastDto[];
}

const FORECAST_COLOUR = '#1d70b8';
const ALLOCATION_COLOUR = '#b1d7ff';

const SpendChart = ({ data }: SpendChartProps): React.ReactElement => {
  // Keep a consistent strand order regardless of the API response ordering.
  const ordered = ALLOCATION_STRANDS.map((strand) => data.find((d) => d.module === strand.key)).filter(
    (d): d is StrandForecastDto => d !== undefined,
  );

  const chartData: ChartData<'bar'> = {
    labels: ALLOCATION_STRANDS.filter((s) => ordered.some((o) => o.module === s.key)).map((s) => s.shortLabel),
    datasets: [
      {
        label: 'Forecasted spend',
        data: ordered.map((d) => d.forecastedSpend),
        backgroundColor: FORECAST_COLOUR,
      },
      {
        label: 'Allocated spend',
        data: ordered.map((d) => d.allocation),
        backgroundColor: ALLOCATION_COLOUR,
      },
    ],
  };

  const options: ChartOptions<'bar'> = {
    indexAxis: 'y',
    responsive: true,
    maintainAspectRatio: false,
    scales: {
      x: {
        beginAtZero: true,
        ticks: { callback: (value) => Number(value).toLocaleString('en-GB') },
      },
    },
    plugins: {
      legend: { position: 'bottom' },
      tooltip: {
        callbacks: {
          label: (ctx) => `${ctx.dataset.label}: £${Number(ctx.parsed.x).toLocaleString('en-GB')}`,
        },
      },
      datalabels: {
        anchor: 'end',
        align: 'start',
        color: (ctx) => (ctx.datasetIndex === 0 ? '#ffffff' : '#0b0c0c'),
        font: { size: 11 },
        formatter: (value: number) => (value > 0 ? Math.round(value).toLocaleString('en-GB') : ''),
      },
    },
  };

  return (
    <div className="la-chart-container" style={{ height: `${Math.max(ordered.length * 56, 240)}px` }}>
      <Bar data={chartData} options={options} />
    </div>
  );
};

export default SpendChart;
