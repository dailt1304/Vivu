import React, { useMemo } from "react";
import {
  Chart as ChartJS,
  CategoryScale,
  LinearScale,
  BarElement,
  Title,
  Tooltip,
  Legend,
} from "chart.js";
import { Bar } from "react-chartjs-2";

ChartJS.register(
  CategoryScale,
  LinearScale,
  BarElement,
  Title,
  Tooltip,
  Legend,
);

const UserGrowthChart = ({ data }) => {
  const chartData = useMemo(
    () => ({
      labels: data?.labels || ["T1", "T2", "T3", "T4", "T5", "T6"],
      datasets: [
        {
          label: "Người dùng mới",
          data: data?.values || [0, 0, 0, 0, 0, 0],
          backgroundColor: "#3b82f6",
          borderRadius: 4,
        },
      ],
    }),
    [data],
  );

  const options = useMemo(
    () => ({
      responsive: true,
      maintainAspectRatio: false,
      plugins: {
        legend: {
          display: false,
        },
      },
      scales: {
        y: {
          beginAtZero: true,
          grid: {
            color: "rgba(0, 0, 0, 0.05)",
          },
        },
        x: {
          grid: {
            display: false,
          },
        },
      },
    }),
    [],
  );

  return <Bar data={chartData} options={options} />;
};

export default UserGrowthChart;
