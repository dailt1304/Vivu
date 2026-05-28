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

const LocationCategoryChart = ({ data }) => {
  const chartData = useMemo(() => {
    const labels = Object.keys(data || {});
    const values = Object.values(data || {});

    return {
      labels: labels.length > 0 ? labels : ["No Data"],
      datasets: [
        {
          label: "Số lượng địa điểm",
          data: values.length > 0 ? values : [0],
          backgroundColor: [
            "rgba(16, 185, 129, 0.9)",   // Emerald 500
            "rgba(52, 211, 153, 0.9)",   // Emerald 400
            "rgba(110, 231, 183, 0.9)",  // Emerald 300
            "rgba(5, 150, 105, 0.9)",    // Emerald 600
            "rgba(4, 120, 87, 0.9)",     // Emerald 700
            "rgba(167, 243, 208, 0.9)",  // Emerald 200
            "rgba(209, 250, 229, 0.9)",  // Emerald 100
            "rgba(6, 78, 59, 0.9)",      // Emerald 900
          ],
          hoverBackgroundColor: "rgba(16, 185, 129, 1)",
          borderRadius: 6,
        },
      ],
    };
  }, [data]);

  const options = useMemo(
    () => ({
      responsive: true,
      maintainAspectRatio: false,
      plugins: {
        legend: {
          display: false,
        },
        tooltip: {
          backgroundColor: "rgba(255, 255, 255, 0.9)",
          titleColor: "#111827",
          bodyColor: "#4b5563",
          borderColor: "#e5e7eb",
          borderWidth: 1,
          padding: 12,
          boxPadding: 4,
        },
      },
      scales: {
        y: {
          beginAtZero: true,
          ticks: {
            stepSize: 1,
          },
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

export default LocationCategoryChart;
