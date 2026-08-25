import React from 'react';
import { BarElement, CategoryScale, Chart as ChartJS, ChartData, ChartOptions, Legend, LinearScale, Tooltip } from 'chart.js';
import ChartDataLabels from 'chartjs-plugin-datalabels';
import { Bar } from 'react-chartjs-2';
import { StrandVarianceDto } from './types';
import { ALLOCATION_STRANDS } from '../../../../components/Global/DeliveryPlanAnalytics/constants';

ChartJS.register(CategoryScale, LinearScale, BarElement, Tooltip, Legend, ChartDataLabels);

interface UnderOverspendChartProps {
  data: StrandVarianceDto[];
}

// Single colour regardless of sign, matching the approved design and the existing SpendChart
// convention — polarity is already conveyed unambiguously by bar direction (left/right of the zero
// baseline) and the signed percentage label, so colour-coding by sign would be redundant rather than
// load-bearing for accessibility.
const BAR_COLOUR = '#1d70b8';

const formatSigned = (value: number): string => `${value > 0 ? '+' : ''}${value.toFixed(2)}`;

const UnderOverspendChart = ({ data }: UnderOverspendChartProps): React.ReactElement => {
  // Keep a consistent strand order regardless of the API response ordering.
  const ordered = ALLOCATION_STRANDS.map((strand) => data.find((d) => d.module === strand.key)).filter((d): d is StrandVarianceDto => d !== undefined);

  // ±20% matches the approved design for the common case, but auto-extends (rounded up to a clean
  // multiple of 5) so an unusually large variance is never clipped/pinned at the scale edge with no
  // visual indication its true magnitude is larger.
  const maxAbsVariance = ordered.reduce((max, d) => Math.max(max, Math.abs(d.variancePercentage)), 0);
  const axisBound = Math.max(20, Math.ceil(maxAbsVariance / 5) * 5);

  const chartData: ChartData<'bar'> = {
    labels: ordered.map((d) => ALLOCATION_STRANDS.find((s) => s.key === d.module)?.shortLabel ?? d.displayName),
    datasets: [
      {
        label: 'Under/overspend',
        data: ordered.map((d) => d.variancePercentage),
        backgroundColor: BAR_COLOUR,
        borderRadius: 4,
      },
    ],
  };

  const options: ChartOptions<'bar'> = {
    indexAxis: 'y',
    responsive: true,
    maintainAspectRatio: false,
    scales: {
      x: {
        min: -axisBound,
        max: axisBound,
        ticks: {
          callback: (value) => `${Number(value) > 0 ? '+' : ''}${value}%`,
        },
        grid: {
          // Emphasise the zero baseline so the diverging direction reads clearly at a glance.
          color: (ctx) => (ctx.tick.value === 0 ? '#0b0c0c' : '#b1b4b6'),
          lineWidth: (ctx) => (ctx.tick.value === 0 ? 2 : 1),
        },
      },
    },
    plugins: {
      legend: { display: false },
      tooltip: {
        callbacks: {
          label: (ctx) => `${formatSigned(Number(ctx.parsed.x))}%`,
        },
      },
      datalabels: {
        // Anchored to the bar's tip and pulled back inside it (rather than placed outside the
        // tip) so the label never extends past the bar's own footprint — with values that run
        // close to the axis edge (e.g. -95%), an outside-the-tip label would overflow into the
        // category labels on the y-axis.
        anchor: 'end',
        align: 'start',
        color: '#ffffff',
        font: { size: 11, weight: 'bold' },
        formatter: (value: number) => formatSigned(value),
      },
    },
  };

  return (
    <div className="la-chart-container" style={{ height: `${Math.max(ordered.length * 56, 240)}px` }}>
      <Bar data={chartData} options={options} aria-label="Under- and overspend by strand, percent" />
    </div>
  );
};

export default UnderOverspendChart;
