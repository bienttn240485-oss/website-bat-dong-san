import "preline";

const initializePreline = () => {
  if (window.HSStaticMethods?.autoInit) {
    window.HSStaticMethods.autoInit();
  }
};

document.addEventListener("DOMContentLoaded", initializePreline);
