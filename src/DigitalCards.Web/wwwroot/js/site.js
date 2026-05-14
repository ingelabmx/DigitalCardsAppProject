// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

(() => {
  const menuButton = document.querySelector("[data-legacy-menu-button]");
  const backdrop = document.querySelector("[data-legacy-sidebar-backdrop]");

  if (!menuButton || !backdrop) {
    return;
  }

  const setOpen = (isOpen) => {
    document.body.classList.toggle("legacy-sidebar-open", isOpen);
    menuButton.setAttribute("aria-expanded", isOpen ? "true" : "false");
  };

  menuButton.addEventListener("click", () => {
    setOpen(!document.body.classList.contains("legacy-sidebar-open"));
  });

  backdrop.addEventListener("click", () => {
    setOpen(false);
  });

  document.addEventListener("keydown", (event) => {
    if (event.key === "Escape") {
      setOpen(false);
    }
  });
})();
