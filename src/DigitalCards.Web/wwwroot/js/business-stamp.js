(function () {
  const scanner = document.querySelector("[data-stamp-camera]");
  const form = document.querySelector("[data-testid='stamp-form']");
  const hiddenInput = document.querySelector("[data-testid='stamp-scanned-code']");
  const startButton = document.querySelector("[data-testid='stamp-scan-submit']");
  const status = document.querySelector("[data-qr-status]");
  const preview = document.querySelector("[data-stamp-scan-preview]");
  const previewText = document.querySelector("[data-stamp-scan-preview-text]");
  const video = scanner?.querySelector("[data-qr-video]");
  const canvas = scanner?.querySelector("[data-qr-canvas]");
  const context = canvas instanceof HTMLCanvasElement ? canvas.getContext("2d", { willReadFrequently: true }) : null;
  const scanTimeoutMs = 10000;
  let stream = null;
  let scanning = false;
  let detector = null;
  let lastScanAt = 0;
  let scanTimeout = null;

  if (!scanner || !form || !hiddenInput || !startButton || !(video instanceof HTMLVideoElement)) {
    return;
  }

  function setStatus(message) {
    if (status) {
      status.textContent = message;
    }
  }

  function setPreview(state, message) {
    if (preview) {
      preview.dataset.scanState = state;
    }

    if (previewText) {
      previewText.textContent = message;
    }
  }

  function setButton(active) {
    startButton.textContent = active ? "Escaneando..." : "Escanear QR";
    startButton.classList.toggle("active", active);
  }

  function clearScanTimeout() {
    if (scanTimeout) {
      window.clearTimeout(scanTimeout);
      scanTimeout = null;
    }
  }

  function stop(message, state, previewMessage) {
    scanning = false;
    clearScanTimeout();
    if (stream) {
      for (const track of stream.getTracks()) {
        track.stop();
      }
    }

    stream = null;
    video.srcObject = null;
    setButton(false);
    setStatus(message || "Listo");
    setPreview(state || "ready", previewMessage || message || "Listo para escanear");
  }

  function submitCode(value) {
    const normalized = (value || "").trim();
    if (!normalized || !(hiddenInput instanceof HTMLInputElement)) {
      return false;
    }

    hiddenInput.value = normalized;
    stop("QR detectado", "success", "QR detectado, aplicando sello...");
    form.submit();
    return true;
  }

  function handleScanTimeout() {
    if (!scanning) {
      return;
    }

    stop("No se pudo leer el QR", "error");
    setPreview("error", "No se pudo leer el QR. Intenta de nuevo.");
  }

  async function detectWithNative() {
    if (!detector) {
      return false;
    }

    const codes = await detector.detect(video);
    return codes.length > 0 && submitCode(codes[0].rawValue);
  }

  function detectWithJsQr() {
    if (typeof window.jsQR !== "function" || !(canvas instanceof HTMLCanvasElement) || !context) {
      return false;
    }

    if (!video.videoWidth || !video.videoHeight) {
      return false;
    }

    canvas.width = video.videoWidth;
    canvas.height = video.videoHeight;
    context.drawImage(video, 0, 0, canvas.width, canvas.height);
    const image = context.getImageData(0, 0, canvas.width, canvas.height);
    const code = window.jsQR(image.data, image.width, image.height, { inversionAttempts: "dontInvert" });
    return Boolean(code?.data) && submitCode(code.data);
  }

  async function scanLoop() {
    if (!scanning) {
      return;
    }

    const now = Date.now();
    if (now - lastScanAt < 140) {
      window.requestAnimationFrame(scanLoop);
      return;
    }

    lastScanAt = now;
    try {
      if (await detectWithNative()) {
        return;
      }

      if (detectWithJsQr()) {
        return;
      }
    } catch {
      setStatus("No se pudo leer el QR");
    }

    window.requestAnimationFrame(scanLoop);
  }

  async function start() {
    if (scanning) {
      stop();
      return;
    }

    if (!navigator.mediaDevices?.getUserMedia) {
      setStatus("Camara no disponible");
      setPreview("error", "Camara no disponible. Captura el usuario manualmente.");
      return;
    }

    try {
      setStatus("Solicitando permiso");
      setPreview("pending", "Solicitando permiso de camara...");

      if ("BarcodeDetector" in window) {
        detector = new window.BarcodeDetector({ formats: ["qr_code"] });
      }

      stream = await navigator.mediaDevices.getUserMedia({
        video: { facingMode: { ideal: "environment" } },
        audio: false
      });

      video.srcObject = stream;
      await video.play();
      scanning = true;
      setButton(true);
      setStatus("Escaneando");
      setPreview("active", "Buscando QR...");
      scanTimeout = window.setTimeout(handleScanTimeout, scanTimeoutMs);
      await scanLoop();
    } catch {
      stop("Camara no disponible", "error");
      setPreview("error", "Camara no disponible. Captura el usuario manualmente.");
    }
  }

  startButton.addEventListener("click", start);
  window.addEventListener("pagehide", stop);
})();
