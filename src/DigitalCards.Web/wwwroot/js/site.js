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

(() => {
  const focusFirstValidationError = (form) => {
    const invalidField = form.querySelector(".input-validation-error, .is-invalid, [aria-invalid='true']");
    if (invalidField && typeof invalidField.focus === "function") {
      invalidField.focus();
      return;
    }

    const summary = form.querySelector(".validation-summary-errors, .text-danger");
    if (summary) {
      summary.setAttribute("tabindex", "-1");
      summary.focus();
    }
  };

  const isValidForSubmit = (form) => {
    if (window.jQuery) {
      const validator = window.jQuery(form).data("validator");
      if (validator) {
        return window.jQuery(form).valid();
      }
    }

    return typeof form.checkValidity === "function" ? form.checkValidity() : true;
  };

  const setSubmitting = (form, submitter) => {
    form.classList.add("is-submitting");
    form.setAttribute("aria-busy", "true");

    form.querySelectorAll("button[type='submit'], input[type='submit']").forEach((button) => {
      if (!button.dataset.originalText) {
        button.dataset.originalText = button.tagName === "INPUT" ? button.value : button.textContent.trim();
      }

      if (button === submitter) {
        const nextText = button.dataset.submittingText || "Procesando...";
        if (button.tagName === "INPUT") {
          button.value = nextText;
        } else {
          button.textContent = nextText;
        }
      }

      button.disabled = true;
    });
  };

  document.querySelectorAll("form[method='post']").forEach((form) => {
    form.addEventListener("submit", (event) => {
      if (form.classList.contains("is-submitting")) {
        return;
      }

      if (!isValidForSubmit(form)) {
        focusFirstValidationError(form);
        return;
      }

      setSubmitting(form, event.submitter);
    });
  });
})();
