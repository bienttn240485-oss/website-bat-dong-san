import Chart from "chart.js/auto";

const palette = {
  success: { border: "#15803d", background: "rgba(21, 128, 61, 0.14)" },
  info: { border: "#2563eb", background: "rgba(37, 99, 235, 0.14)" },
  active: { border: "#0f766e", background: "rgba(15, 118, 110, 0.16)" },
  warning: { border: "#d97706", background: "rgba(217, 119, 6, 0.16)" },
  danger: { border: "#dc2626", background: "rgba(220, 38, 38, 0.14)" }
};

const formatValue = (value) => {
  if (Number(value) >= 100000) {
    return `${Number(value).toLocaleString("vi-VN")} ₫`;
  }

  return Number(value).toLocaleString("vi-VN");
};

const buildDataset = (dataset) => {
  const colors = palette[dataset.tone] || palette.info;
  return {
    type: dataset.type || "bar",
    label: dataset.label,
    data: dataset.data,
    borderColor: colors.border,
    backgroundColor: colors.background,
    borderWidth: 2,
    borderRadius: dataset.type === "bar" ? 4 : 0,
    tension: 0.28,
    fill: dataset.type === "line"
  };
};

const toMonthlyPayload = (payload) => {
  const monthTotals = new Map();
  payload.labels.forEach((label, index) => {
    const parts = String(label).split("/");
    const month = parts.length === 3 ? Number(parts[1]) : Number.NaN;
    if (!Number.isFinite(month)) {
      return;
    }

    const monthLabel = `Tháng ${month}`;
    monthTotals.set(monthLabel, (monthTotals.get(monthLabel) || 0) + Number(payload.datasets[0]?.data[index] || 0));
  });

  return {
    labels: Array.from(monthTotals.keys()),
    datasets: payload.datasets.map((dataset) => ({
      ...dataset,
      type: "bar",
      data: Array.from(monthTotals.values())
    }))
  };
};

const renderChart = async (canvas) => {
  const url = canvas.dataset.chartUrl;
  const errorElement = canvas.closest("section")?.querySelector("[data-chart-error]");

  try {
    const response = await fetch(url, { headers: { Accept: "application/json" } });
    if (!response.ok) {
      throw new Error("Không tải được dữ liệu biểu đồ.");
    }

    const rawPayload = await response.json();
    const payload = canvas.dataset.chartMode === "quarter-monthly" ? toMonthlyPayload(rawPayload) : rawPayload;
    new Chart(canvas, {
      data: {
        labels: payload.labels,
        datasets: payload.datasets.map(buildDataset)
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        interaction: {
          intersect: false,
          mode: "index"
        },
        plugins: {
          legend: {
            labels: {
              boxWidth: 10,
              color: "#334155",
              font: { family: "Inter, system-ui, sans-serif" }
            }
          },
          tooltip: {
            callbacks: {
              label: (context) => `${context.dataset.label}: ${formatValue(context.parsed.y)}`
            }
          }
        },
        scales: {
          x: {
            ticks: { color: "#64748b", maxRotation: 0, autoSkip: true },
            grid: { color: "#e2e8f0" }
          },
          y: {
            beginAtZero: true,
            ticks: { color: "#64748b", callback: formatValue },
            grid: { color: "#e2e8f0" }
          }
        }
      }
    });
  } catch {
    errorElement?.classList.remove("hidden");
  }
};

document.querySelectorAll("[data-report-chart]").forEach((canvas) => {
  renderChart(canvas);
});
