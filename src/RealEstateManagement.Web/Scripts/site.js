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

document.addEventListener("DOMContentLoaded", initializePreline);
