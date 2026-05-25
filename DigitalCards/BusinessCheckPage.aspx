<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="BusinessCheckPage.aspx.cs" Inherits="DigitalCardsApp.BusinessCheckPage" %>

<!DOCTYPE html>
<html lang="en">

<head>
    <meta charset="utf-8">
    <meta name="viewport" content="width=device-width, initial-scale=1">
    <title>Sellos</title>
    <link rel="shortcut icon" type="image/png" href="../assets/images/logos/DigitalCards-Icon-removebg.png" />
    <link rel="stylesheet" href="assets/css/styles.min.css" />
</head>

<body>
    <form id="form1" runat="server">
        <!--  Body Wrapper -->
        <div class="page-wrapper" id="main-wrapper" data-layout="vertical" data-navbarbg="skin6" data-sidebartype="full"
            data-sidebar-position="fixed" data-header-position="fixed">
            <!-- Sidebar Start -->
            <aside class="left-sidebar">
                <!-- Sidebar scroll-->
                <div>
                    <div class="brand-logo d-flex align-items-center justify-content-between">
                        <a href="./BusinessDashboardPage.aspx" class="text-nowrap logo-img">
                            <img id="imgBusinessLogo" src="#" alt="Business Logo" width="220" height="100" runat="server" />
                        </a>
                        <div class="close-btn d-xl-none d-block sidebartoggler cursor-pointer" id="sidebarCollapse">
                            <i class="ti ti-x fs-8"></i>
                        </div>
                    </div>
                    <!-- Sidebar navigation-->
                    <nav class="sidebar-nav scroll-sidebar" data-simplebar="">
                        <ul id="sidebarnav">
                            <li class="nav-small-cap">
                                <i class="ti ti-dots nav-small-cap-icon fs-6"></i>
                                <span class="hide-menu">Dueños de negocios</span>
                            </li>
                            <li class="sidebar-item">
                                <a class="sidebar-link" href="BusinessDashboardPage.aspx" aria-expanded="false">
                                    <span>
                                        <iconify-icon icon="solar:home-smile-bold-duotone" class="fs-6"></iconify-icon>
                                    </span>
                                    <span class="hide-menu">Dashboard</span>
                                </a>
                            </li>
                            <li class="sidebar-item">
                                <a class="sidebar-link" href="BusinessInsertionPage.aspx" aria-expanded="false">
                                    <span>
                                        <iconify-icon icon="solar:layers-minimalistic-bold-duotone" class="fs-6"></iconify-icon>
                                    </span>
                                    <span class="hide-menu">Tarjetas</span>
                                </a>
                            </li>
                            <li class="sidebar-item">
                                <a class="sidebar-link" href="BusinessCheckPage.aspx" aria-expanded="false">
                                    <span>
                                        <iconify-icon icon="solar:danger-circle-bold-duotone" class="fs-6"></iconify-icon>
                                    </span>
                                    <span class="hide-menu">Checadas</span>
                                </a>
                            </li>
                            <li class="nav-small-cap">
                                <iconify-icon icon="solar:menu-dots-linear" class="nav-small-cap-icon fs-6" class="fs-6"></iconify-icon>
                                <span class="hide-menu">Autenticación</span>
                            </li>
                            <li class="sidebar-item">
                                <a class="sidebar-link" href="Logout.aspx" aria-expanded="false">
                                    <span>
                                        <iconify-icon icon="solar:login-3-bold-duotone" class="fs-6"></iconify-icon>
                                    </span>
                                    <span class="hide-menu">Cerrar sesión</span>
                                </a>
                            </li>
                        </ul>
                    </nav>
                    <!-- End Sidebar navigation -->
                </div>
                <!-- End Sidebar scroll-->
            </aside>
            <!--  Sidebar End -->
            <!--  Main wrapper -->
            <div class="body-wrapper">
                <!--  Header Start -->
                <header class="app-header">
                    <nav class="navbar navbar-expand-lg navbar-light">
                        <ul class="navbar-nav">
                            <li class="nav-item d-block d-xl-none">
                                <a class="nav-link sidebartoggler nav-icon-hover" id="headerCollapse" href="javascript:void(0)">
                                    <i class="ti ti-menu-2"></i>
                                </a>
                            </li>
                        </ul>
                        <div class="navbar-collapse justify-content-end px-0" id="navbarNav">
                            <h5 class="m-0">Bienvenido,
                                <asp:Label ID="BusinessNameLabel" runat="server" Text="Negocio"></asp:Label>
                            </h5>
                        </div>
                    </nav>
                </header>
                <!--  Header End -->

                <div class="container-fluid">
                    <!-- Alerts -->
                    <div class="alert alert-success" id="successAlert" role="alert" visible="false" runat="server">
                        La checada ha sido exitosa.
                    </div>
                    <div class="alert alert-danger" id="failAlert" role="alert" visible="false" runat="server">
                        Fallo en la checada.
                    </div>
                    <!-- Card Contents -->
                    <div class="card">
                        <div class="card-body">
                            <div class="card">
                                <div class="card-body">
                                    <div class="d-flex justify-content-center gap-2">
                                        <h5 class="card-title">Sellos</h5>
                                    </div>
                                    <div class="mb-3">
                                        <input type="text" class="form-control" placeholder="Escanear o buscar un cliente por su nombre de usuario o correo electrónico..." id="tbCliente" runat="server">
                                    </div>
                                    <div class="d-flex justify-content-center gap-2">
                                        <button type="submit" class="btn btn-primary" id="btChecar" onserverclick="btChecar_ServerClick" runat="server">Marcar checada</button>
                                        <button type="button" class="btn btn-success" id="btEscanear" runat="server">Escanear código QR</button>
                                    </div>
                                    <div id="qr-scanner" style="display: none; width: 100%; max-width: 400px; margin-top: 20px;"></div>
                                    <div id="qr-result" class="alert alert-info" style="display: none; margin-top: 10px;">QR Code: <span id="qr-code"></span></div>
                                </div>
                            </div>
                        </div>
                    </div>
                    <div class="py-6 px-6 text-center">
                        <p class="mb-0 fs-4">
                            Propiedad de Ingelab® 
                        </p>
                    </div>
                </div>
            </div>
        </div>
        <script src="../assets/libs/jquery/dist/jquery.min.js"></script>
        <script src="../assets/libs/bootstrap/dist/js/bootstrap.bundle.min.js"></script>
        <script src="../assets/libs/apexcharts/dist/apexcharts.min.js"></script>
        <script src="../assets/libs/simplebar/dist/simplebar.js"></script>
        <script src="../assets/js/sidebarmenu.js"></script>
        <script src="../assets/js/app.min.js"></script>
        <script src="../assets/js/dashboard.js"></script>
        <script src="https://cdn.jsdelivr.net/npm/iconify-icon@1.0.8/dist/iconify-icon.min.js"></script>
        <script src="https://cdn.jsdelivr.net/npm/html5-qrcode@2.3.8/html5-qrcode.min.js"></script>
        <script>
            // Function to detect if the user is on a mobile device
            function isMobileDevice() {
                return /Android|iPhone|iPad|iPod|BlackBerry|IEMobile|Opera Mini/i.test(navigator.userAgent);
            }

            // Enable or disable the 'Escanear código QR' button based on device type
            document.addEventListener("DOMContentLoaded", function () {
                const scanButton = document.getElementById("btEscanear");

                if (isMobileDevice()) {
                    scanButton.disabled = false; // Enable button for mobile
                } else {
                    scanButton.disabled = true; // Disable button for desktop
                    scanButton.title = "Disponible solo en dispositivos móviles"; // Add tooltip for desktop
                }
            });
        </script>
        <script>
            document.getElementById("btEscanear").addEventListener("click", function () {
                const qrScanner = document.getElementById("qr-scanner");
                const tbCliente = document.getElementById("<%= tbCliente.ClientID %>"); // Access server-side control
                const qrResult = document.getElementById("qr-result");
                const qrCodeSpan = document.getElementById("qr-code");
                const btChecar = document.getElementById("<%= btChecar.ClientID %>"); // Access 'Marcar checada' button

                // Show the scanner area
                qrScanner.style.display = "block";

                // Initialize the QR code scanner
                const html5QrCode = new Html5Qrcode("qr-scanner");

                html5QrCode.start(
                    { facingMode: "environment" }, // Use back camera
                    {
                        fps: 10, // Frames per second for the scanning
                        qrbox: { width: 250, height: 250 }, // Scanning box dimensions
                    },
                    (decodedText) => {
                        // Success callback - QR Code scanned
                        tbCliente.value = decodedText; // Set the QR code content in tbCliente
                        qrCodeSpan.textContent = decodedText; // Display QR code content
                        qrResult.style.display = "block";

                        // Programmatically trigger the 'Marcar checada' button click
                        btChecar.click();

                        // Stop scanning after success
                        html5QrCode.stop().then(() => {
                            qrScanner.style.display = "none";
                        }).catch((err) => console.error("Failed to stop QR scanner", err));
                    },
                    (errorMessage) => {
                        // Error callback (optional)
                        console.warn("QR scanning error:", errorMessage);
                    }
                ).catch((err) => {
                    console.error("Unable to start QR scanner:", err);
                });
            });
        </script>
    </form>
</body>

</html>
