import "preline";

const initializePreline = () => {
  if (window.HSStaticMethods?.autoInit) {
    window.HSStaticMethods.autoInit();
  }
};

const applyPropertyImageFallback = (image) => {
  if (image.dataset.fallbackApplied === "true") {
    return;
  }

  const fallbackSrc = image.dataset.fallbackSrc || "/images/properties/property-placeholder.svg";
  image.dataset.fallbackApplied = "true";
  image.src = fallbackSrc;
};

const initializeAreaSuggestions = () => {
  document.querySelectorAll("[data-area-options-json]").forEach((script) => {
    const form = script.closest("aside")?.querySelector("form");
    const projectSelect = form?.querySelector("[data-project-select]");
    const areaSelect = form?.querySelector("[data-area-select]");
    if (!(projectSelect instanceof HTMLSelectElement) || !(areaSelect instanceof HTMLSelectElement)) {
      return;
    }

    let options = [];
    try {
      options = JSON.parse(script.textContent || "[]");
    } catch {
      options = [];
    }

    const currentArea = areaSelect.value;
    const renderAreas = () => {
      const selectedProject = projectSelect.value;
      const selectedArea = areaSelect.value || currentArea;
      const filtered = options
        .filter((item) => !selectedProject || item.Project === selectedProject)
        .map((item) => item.Area)
        .filter((area, index, areas) => area && areas.indexOf(area) === index)
        .sort((left, right) => left.localeCompare(right, "vi"));

      areaSelect.replaceChildren(new Option("Tất cả phân khu", ""));
      filtered.forEach((area) => areaSelect.add(new Option(area, area, false, area === selectedArea)));
    };

    projectSelect.addEventListener("change", renderAreas);
    renderAreas();
  });
};

document.addEventListener(
  "error",
  (event) => {
    const target = event.target;
    if (target instanceof HTMLImageElement && target.matches("[data-property-image]")) {
      applyPropertyImageFallback(target);
    }
  },
  true,
);

document.addEventListener("DOMContentLoaded", () => {
  initializePreline();
  initializeAreaSuggestions();
});
