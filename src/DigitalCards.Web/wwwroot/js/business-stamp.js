(function () {
  const panel = document.querySelector("[data-business-stamp-scanner]");
  if (!panel) {
    return;
  }

  const form = document.querySelector("[data-testid='stamp-form']");
  const hiddenInput = document.querySelector("[data-testid='stamp-scanned-code']");
  const startButton = panel.querySelector("[data-qr-start]") ?? document.querySelector("[data-qr-start]");
  const stopButton = panel.querySelector("[data-qr-stop]");
  const status = panel.querySelector("[data-qr-status]");
  const video = panel.querySelector("[data-qr-video]");
  let stream = null;
  let scanning = false;

  function setStatus(message) {
    if (status) {
      status.textContent = message;
    }
  }

  function stop() {
    scanning = false;
    if (stream) {
      for (const track of stream.getTracks()) {
        track.stop();
      }
    }
    stream = null;
    if (video) {
      video.srcObject = null;
    }
    panel.classList.remove("active");
    setStatus("Listo");
  }

  async function scanLoop(detector) {
    if (!scanning || !video) {
      return;
    }

    try {
      const codes = await detector.detect(video);
      if (codes.length > 0 && hiddenInput instanceof HTMLInputElement) {
        hiddenInput.value = (codes[0].rawValue || "").trim();
        stop();
        form?.submit();
        return;
      }
    } catch {
      setStatus("Sin lectura");
    }

    window.requestAnimationFrame(() => scanLoop(detector));
  }

  async function start() {
    if (!("BarcodeDetector" in window) || !navigator.mediaDevices?.getUserMedia || !(video instanceof HTMLVideoElement)) {
      setStatus("No disponible");
      startButton?.setAttribute("disabled", "disabled");
      return;
    }

    try {
      const detector = new window.BarcodeDetector({ formats: ["qr_code"] });
      stream = await navigator.mediaDevices.getUserMedia({
        video: { facingMode: { ideal: "environment" } },
        audio: false
      });

      video.srcObject = stream;
      await video.play();
      scanning = true;
      panel.classList.add("active");
      setStatus("Escaneando");
      await scanLoop(detector);
    } catch {
      stop();
      setStatus("No disponible");
    }
  }

  startButton?.addEventListener("click", start);
  stopButton?.addEventListener("click", stop);
  window.addEventListener("pagehide", stop);

  if (!("BarcodeDetector" in window) || !navigator.mediaDevices?.getUserMedia) {
    setStatus("No disponible");
  }
})();
