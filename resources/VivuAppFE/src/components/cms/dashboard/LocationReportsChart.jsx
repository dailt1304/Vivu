import React, { useMemo } from "react";
import { Chart as ChartJS, ArcElement, Tooltip, Legend } from "chart.js";
import { Doughnut } from "react-chartjs-2";

ChartJS.register(ArcElement, Tooltip, Legend);

const LocationReportsChart = ({ data }) => {
  const chartData = useMemo(() => {
    const labels = Object.keys(data || {}).map((type) =>
      type === "WRONG_INFO"
        ? "Thông tin sai"
        : type === "NEW_LOCATION"
          ? "Địa điểm mới"
          : type,
    );
    const values = Object.values(data || {});

    return {
      labels: labels.length > 0 ? labels : ["No Data"],
      datasets: [
        {
          label: "Số lượng báo cáo",
          data: values.length > 0 ? values : [0],
          backgroundColor: [
            "rgba(245, 158, 11, 0.9)", // Amber 500
            "rgba(16, 185, 129, 0.9)", // Emerald 500
            "rgba(244, 63, 94, 0.9)",  // Rose 500
          ],
          hoverBackgroundColor: [
            "rgba(245, 158, 11, 1)", 
            "rgba(16, 185, 129, 1)", 
            "rgba(244, 63, 94, 1)",  
          ],
          borderColor: ["#ffffff"],
          borderWidth: 2,
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
          position: "bottom",
          labels: {
            padding: 20,
            usePointStyle: true,
          },
        },
      },
      cutout: "70%",
    }),
    [],
  );

  return <Doughnut data={chartData} options={options} />;
};

export default LocationReportsChart;
