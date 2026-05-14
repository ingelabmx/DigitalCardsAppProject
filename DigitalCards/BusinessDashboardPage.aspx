<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="BusinessDashboardPage.aspx.cs" Inherits="DigitalCardsApp.BusinessDashboardPage" %>

<!DOCTYPE html>
<html lang="en">

<head>
    <meta charset="utf-8">
    <meta name="viewport" content="width=device-width, initial-scale=1">
    <title>Dashboard</title>
    <link rel="shortcut icon" type="image/png" href="../assets/images/logos/DigitalCards-Icon-removebg.png" />
    <link rel="stylesheet" href="../assets/css/styles.min.css" />
</head>

<body>
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
                <div class="row">
                    <div class="col-lg-8">
                        <div class="card">
                            <div class="card-body">
                                <div class="d-flex justify-content-center gap-2">
                                    <h5 class="card-title d-flex align-items-center gap-2 mb-5 pb-3">Nuevos clientes registrados</h5>
                                </div>
                                <canvas id="cardsChart"></canvas>
                                <asp:Literal ID="ltScriptData" runat="server"></asp:Literal>
                            </div>
                        </div>
                    </div>
                    <div class="col-lg-4">
                        <div class="card">
                            <div class="card-body text-center">
                                <img src="assets/images/logos/DigitalCards-Icon-removebg.png" alt="image" class="img-fluid" width="205">
                                <h4 class="mt-7">Ayuda y soporte</h4>
                                <p class="card-subtitle mt-2 mb-3">
                                    En caso de tener problemas con tu cuenta, dudas o sugerencias sobre nuestro servicio 
                                    ¡Contáctanos!
                                </p>
                                <p class="card-subtitle mt-2 mb-3">
                                    Envíanos un mensaje al correo:
                                </p>
                                <p class="card-subtitle mt-2 mb-3">
                                    <b>ingelabmx@gmail.com</b>
                                </p>
                                <p class="card-subtitle mt-2 mb-3">
                                    O llámanos al número:                                     
                                </p>
                                <p class="card-subtitle mt-2 mb-3">
                                    <b>(+52) 664 197 2204</b>
                                </p>
                            </div>
                        </div>
                    </div>
                    <div class="col-lg-12">
                        <div class="card">
                            <div class="card-body">
                                <div class="d-flex justify-content-center gap-2">
                                    <h5 class="card-title">Últimas checadas</h5>
                                </div>
                                <div class="table-responsive">
                                    <table class="table text-nowrap align-middle mb-0">
                                        <thead>
                                            <tr class="border-2 border-bottom border-primary border-0">
                                                <asp:Literal ID="ltTblHeader" runat="server"></asp:Literal>
                                            </tr>
                                        </thead>
                                        <tbody class="table-group-divider">
                                            <tr>
                                                <asp:Literal ID="ltTblContent" runat="server"></asp:Literal>
                                            </tr>
                                        </tbody>
                                    </table>
                                </div>
                            </div>
                        </div>
                    </div>
                    <div class="py-6 px-6 text-center">
                        <p class="mb-0 fs-4">
                            Propiedad de IngeLabs® 
                        </p>
                    </div>
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
    <script src="https://cdn.jsdelivr.net/npm/iconify-icon@1.0.8/dist/iconify-icon.min.js"></script>
    <script src="https://cdn.jsdelivr.net/npm/chart.js"></script>
    <script src="../assets/js/dashboard.js"></script>
    <script>
        // Define an array of month names in Spanish
        const monthNames = [
            "Enero", "Febrero", "Marzo", "Abril", "Mayo", "Junio",
            "Julio", "Agosto", "Septiembre", "Octubre", "Noviembre", "Diciembre"
        ];

        // Process the data for the chart
        const labels = chartData.map(item => monthNames[item.Month - 1]); // Map month numbers to names
        const data = chartData.map(item => item.CardCount); // Card count

        const ctx = document.getElementById('cardsChart');
        ctx.height = 133;

        const myChart = new Chart(ctx, {
            type: 'bar', // Bar chart
            data: {
                labels: labels, // Use month names
                datasets: [{
                    label: 'Tarjetas creadas',
                    data: data,
                    backgroundColor: 'rgba(54, 162, 235, 0.5)',
                    borderColor: 'rgba(54, 162, 235, 1)',
                    borderWidth: 1
                }]
            },
            options: {
                scales: {
                    y: {
                        beginAtZero: true,
                        ticks: {
                            stepSize: 1, // Ensure numbers increase in whole increments
                            callback: function (value) {
                                return Number(value).toFixed(0); // Force whole numbers
                            }
                        }
                    }
                }
            }
        });
    </script>
</body>

</html>
