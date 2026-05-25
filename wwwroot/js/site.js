window.showAdminToast = function (title, message, type) {
  const toastContainer = document.getElementById("toastContainer");
  if (!toastContainer) {
    return;
  }

  const bgClass =
    type === "success"
      ? "text-bg-success"
      : type === "warning"
        ? "text-bg-warning"
        : "text-bg-danger";
  const element = document.createElement("div");
  element.className = `toast align-items-center ${bgClass} border-0`;
  element.role = "alert";
  element.ariaLive = "assertive";
  element.ariaAtomic = "true";
  element.innerHTML = `
		<div class="d-flex">
			<div class="toast-body"><strong>${title}</strong><br/>${message}</div>
			<button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast" aria-label="Close"></button>
		</div>`;

  toastContainer.appendChild(element);
  const toast = new bootstrap.Toast(element, { delay: 2500 });
  toast.show();
  element.addEventListener("hidden.bs.toast", () => element.remove());
};

(function registerAdminGlobalSearch() {
  const input = document.getElementById("adminGlobalSearchInput");
  if (!input) {
    return;
  }

  let timeoutId;
  input.addEventListener("input", () => {
    clearTimeout(timeoutId);
    timeoutId = setTimeout(async () => {
      const term = input.value.trim();
      if (!term) {
        return;
      }

      try {
        const response = await fetch(
          `/Admin/SearchEmployees?term=${encodeURIComponent(term)}`,
        );
        const result = await response.json();
        const count = Array.isArray(result.data) ? result.data.length : 0;
        const badge = document.getElementById("adminNotificationCount");
        if (badge) {
          badge.textContent = String(count);
        }
      } catch {
        window.showAdminToast(
          "Search",
          "Unable to run global employee search.",
          "error",
        );
      }
    }, 250);
  });
})();
