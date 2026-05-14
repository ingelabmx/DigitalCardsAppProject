<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="AdminDisplayPage.aspx.cs" Inherits="DigitalCardsApp.AdminDisplayPage" %>

<!DOCTYPE html>

<html>
<head runat="server">
    <meta charset="utf-8">
    <meta name="viewport" content="width=device-width, initial-scale=1">
    <title>Negocios</title>
    <link rel="shortcut icon" type="image/png" href="assets/images/logos/DigitalCards-Icon-removebg.png" />
    <link href="assets/libs/simplebar/dist/simplebar.min.css" rel="stylesheet" />
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
                        <a href="./AdminInsertionPage.aspx" class="text-nowrap logo-img">
                            <img src="assets/images/logos/DigitalCards-Logo.jpg" alt="" width="220" height="100" />
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
                                <span class="hide-menu">Administradores</span>
                            </li>
                            <li class="sidebar-item">
                                <a class="sidebar-link" href="AdminInsertionPage.aspx" aria-expanded="false">
                                    <span>
                                        <iconify-icon icon="solar:file-text-bold-duotone" class="fs-6"></iconify-icon>
                                    </span>
                                    <span class="hide-menu">Agregar un negocio</span>
                                </a>
                            </li>
                            <li class="sidebar-item">
                                <a class="sidebar-link" href="AdminDisplayPage.aspx" aria-expanded="false">
                                    <span>
                                        <iconify-icon icon="solar:text-field-focus-bold-duotone" class="fs-6"></iconify-icon>
                                    </span>
                                    <span class="hide-menu">Negocios</span>
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
                            <ul class="navbar-nav flex-row ms-auto align-items-center justify-content-end">
                                <!-- Could be used for a logo or a Profile picture -->
                            </ul>
                        </div>
                    </nav>
                </header>
                <!--  Header End -->
                <div class="container-fluid">
                    <div class="card">
                        <div class="card-body">
                            <div class="card">
                                <div class="card-body">
                                    <div class="d-flex justify-content-center gap-2">
                                        <h5 class="card-title">Tabla de negocios</h5>
                                    </div>
                                    <div class="table-responsive">
                                        <table class="table table-striped table-bordered table-hover dataTables-example">
                                            <thead>
                                                <tr>
                                                    <asp:Literal ID="ltTblHeader" runat="server"></asp:Literal>
                                                </tr>
                                            </thead>
                                            <tbody>
                                                <asp:Literal ID="ltTblContent" runat="server"></asp:Literal>
                                            </tbody>
                                        </table>
                                    </div>
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
        <script src="assets/libs/jquery/dist/jquery.min.js"></script>
        <script src="assets/libs/bootstrap/dist/js/bootstrap.bundle.min.js"></script>
        <script src="assets/libs/simplebar/dist/simplebar.js"></script>
        <script src="assets/js/sidebarmenu.js"></script>
        <script src="assets/js/app.min.js"></script>
        <script src="https://cdn.jsdelivr.net/npm/iconify-icon@1.0.8/dist/iconify-icon.min.js"></script>

        <!-- Table References -->
        <link href="DataTables/datatables.min.css" rel="stylesheet">
        <script src="Datatables/datatables.min.js"></script>
        <!-- Table Script -->
        <script>
            $(document).ready(function () {
                $('.dataTables-example').DataTable({
                    pageLength: 25,
                    responsive: true,
                    dom: '<"html5buttons"B>lTfgitp',
                    buttons: [
                        { extend: 'copy' },
                        { extend: 'csv' },
                        { extend: 'excel', title: 'ExampleFile' },
                        { extend: 'pdf', title: 'ExampleFile' },

                        {
                            extend: 'print',
                            customize: function (win) {
                                $(win.document.body).addClass('white-bg');
                                $(win.document.body).css('font-size', '10px');

                                $(win.document.body).find('table')
                                    .addClass('compact')
                                    .css('font-size', 'inherit');
                            }
                        }
                    ]

                });

            });

        </script>
    </form>
</body>
</html>
