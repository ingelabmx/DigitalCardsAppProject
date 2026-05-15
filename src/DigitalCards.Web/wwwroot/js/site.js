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

(() => {
  const colorInputs = document.querySelectorAll("[data-branding-color-input]");
  if (colorInputs.length === 0) {
    return;
  }

  const normalizeHex = (value) => {
    const trimmed = (value || "").trim();
    if (/^[0-9a-fA-F]{6}$/.test(trimmed)) {
      return `#${trimmed}`;
    }

    return trimmed;
  };

  const setPreviewValue = (input) => {
    const form = input.closest("form");
    const preview = form?.querySelector("[data-testid$='branding-preview']");
    if (!preview) {
      return;
    }

    const value = normalizeHex(input.value);
    if (!/^#[0-9a-fA-F]{6}$/.test(value)) {
      return;
    }

    const target = input.dataset.brandingColorInput;
    if (target === "primary") {
      preview.style.setProperty("--brand-primary", value);
    } else if (target === "secondary") {
      preview.style.setProperty("--brand-secondary", value);
    } else if (target === "custom") {
      preview.style.setProperty("--brand-custom", value);
    }
  };

  colorInputs.forEach((input) => {
    input.addEventListener("input", () => setPreviewValue(input));
    input.addEventListener("blur", () => {
      input.value = normalizeHex(input.value);
      setPreviewValue(input);
    });
  });
})();

(() => {
  const logoInputs = document.querySelectorAll("[data-branding-logo-upload]");
  if (logoInputs.length === 0) {
    return;
  }

  const objectUrls = [];

  logoInputs.forEach((input) => {
    input.addEventListener("change", () => {
      const file = input.files?.[0];
      if (!file) {
        return;
      }

      const isPng = file.type === "image/png" || file.name.toLowerCase().endsWith(".png");
      if (!isPng) {
        return;
      }

      const form = input.closest("form");
      const preview = form?.querySelector("[data-branding-logo-preview-container]");
      const image = preview?.querySelector("[data-branding-logo-preview-image]");
      if (!preview || !image) {
        return;
      }

      const objectUrl = URL.createObjectURL(file);
      objectUrls.push(objectUrl);
      image.src = objectUrl;
      image.hidden = false;
      preview.hidden = false;

      form.querySelectorAll("[data-branding-logo-placeholder]").forEach((placeholder) => {
        placeholder.hidden = true;
      });
    });
  });

  window.addEventListener("beforeunload", () => {
    objectUrls.forEach((objectUrl) => URL.revokeObjectURL(objectUrl));
  });
})();
