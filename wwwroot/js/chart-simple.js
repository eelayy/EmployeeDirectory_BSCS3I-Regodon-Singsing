// Shared simple Chart.js defaults for a clean, minimal look
if (typeof Chart !== "undefined") {
  Chart.defaults.font.family =
    "Inter, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif";
  Chart.defaults.font.size = 12;
  Chart.defaults.color = "#495057";
  Chart.defaults.maintainAspectRatio = false;

  // Hide legends by default for a cleaner appearance
  if (Chart.defaults.plugins && Chart.defaults.plugins.legend) {
    Chart.defaults.plugins.legend.display = false;
  }

  // Tighter tooltips
  if (Chart.defaults.plugins && Chart.defaults.plugins.tooltip) {
    Chart.defaults.plugins.tooltip.padding = 8;
    Chart.defaults.plugins.tooltip.cornerRadius = 4;
  }

  // Line/area smoothing and smaller points
  if (Chart.defaults.elements) {
    Chart.defaults.elements.line.tension = 0.25;
    Chart.defaults.elements.point.radius = 3;
    Chart.defaults.elements.point.hoverRadius = 4;
  }

  // Use subtle grid lines
  Chart.defaults.scales = Chart.defaults.scales || {};
  Object.keys(Chart.defaults.scales).forEach(function (scale) {
    var s = Chart.defaults.scales[scale];
    if (s.grid) {
      s.grid.color = "rgba(0,0,0,0.04)";
    }
    if (s.ticks) {
      s.ticks.color = "#6c757d";
    }
  });
}

// Helper: make chart canvas fill its container height
document.addEventListener("DOMContentLoaded", function () {
  document
    .querySelectorAll(".chart-card canvas, .chart-container canvas")
    .forEach(function (c) {
      c.style.maxWidth = "100%";
      c.style.height = c.style.height || "220px";
    });
});
