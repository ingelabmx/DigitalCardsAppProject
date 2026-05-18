(function () {
  const reduceMotion = window.matchMedia("(prefers-reduced-motion: reduce)").matches;
  const revealItems = document.querySelectorAll("[data-landing-reveal]");
  const carousel = document.querySelector("[data-landing-carousel]");
  const forms = document.querySelectorAll("[data-landing-form]");

  if (!reduceMotion && "IntersectionObserver" in window) {
    const observer = new IntersectionObserver((entries) => {
      for (const entry of entries) {
        if (entry.isIntersecting) {
          entry.target.classList.add("is-visible");
          observer.unobserve(entry.target);
        }
      }
    }, { threshold: 0.18 });

    revealItems.forEach((item) => observer.observe(item));
  } else {
    revealItems.forEach((item) => item.classList.add("is-visible"));
  }

  if (carousel) {
    const track = carousel.querySelector(".landing-carousel-track");
    const slides = Array.from(carousel.querySelectorAll("[data-carousel-slide]"));
    const previous = carousel.querySelector("[data-carousel-prev]");
    const next = carousel.querySelector("[data-carousel-next]");
    let index = 0;

    function showSlide(nextIndex) {
      if (!track || slides.length === 0) {
        return;
      }

      index = (nextIndex + slides.length) % slides.length;
      track.scrollTo({
        left: slides[index].offsetLeft - track.offsetLeft,
        behavior: reduceMotion ? "auto" : "smooth"
      });
    }

    previous?.addEventListener("click", () => showSlide(index - 1));
    next?.addEventListener("click", () => showSlide(index + 1));
  }

  forms.forEach((form) => {
    form.addEventListener("submit", () => {
      const button = form.querySelector("button[type='submit']");
      if (button) {
        button.disabled = true;
        button.textContent = button.dataset.loadingText || "Enviando...";
      }
    });
  });
})();
