<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="ClientPage.aspx.cs" Inherits="DigitalCardsApp.ClientPage2" %>

<!DOCTYPE html>
<html>
<head runat="server">
    <meta charset="utf-8">
    <meta name="viewport" content="width=device-width, initial-scale=1">
    <title>Digital Cards</title>
    <link rel="shortcut icon" type="image/png" href="assets/images/logos/DigitalCards-Icon-removebg.png" />
    <link href="assets/libs/simplebar/dist/simplebar.min.css" rel="stylesheet" />
    <link rel="stylesheet" href="assets/css/styles.min.css" />
</head>
<body>
    <form id="form1" runat="server">
        <div class="page-wrapper" id="main-wrapper" data-layout="vertical" data-navbarbg="skin6"
            data-sidebartype="full" data-sidebar-position="fixed" data-header-position="fixed">

            <!-- Sidebar Start -->
            <aside class="left-sidebar">
                <div>
                    <div class="brand-logo d-flex align-items-center justify-content-between">
                        <a href="./ClientPage.aspx" class="text-nowrap logo-img">
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
                                <span class="hide-menu">Cliente</span>
                            </li>
                            <li class="sidebar-item">
                                <a class="sidebar-link" href="ClientPage.aspx" aria-expanded="false">
                                    <span>
                                        <iconify-icon icon="solar:bookmark-square-minimalistic-bold-duotone" class="fs-6"></iconify-icon>
                                    </span>
                                    <span class="hide-menu">Mis Tarjetas</span>
                                </a>
                            </li>
                            <li class="nav-small-cap">
                                <iconify-icon icon="solar:menu-dots-linear" class="nav-small-cap-icon fs-6"></iconify-icon>
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
                </div>
            </aside>
            <!-- Sidebar End -->

            <!-- Main Wrapper -->
            <div class="body-wrapper">
                <!-- Header Start -->
                <header class="app-header">
                    <nav class="navbar navbar-expand-lg navbar-light">
                        <ul class="navbar-nav">
                            <li class="nav-item d-block d-xl-none">
                                <a class="nav-link sidebartoggler nav-icon-hover" id="headerCollapse" href="javascript:void(0)">
                                    <i class="ti ti-menu-2"></i>
                                </a>
                            </li>
                        </ul>
                        <div class="navbar-collapse justify-content-between px-0" id="navbarNav">
                            <h5 class="m-0">Bienvenido,
                                <asp:Label ID="UserNameLabel" runat="server" Text="Usuario"></asp:Label>
                            </h5>
                        </div>
                    </nav>
                </header>
                <!-- Header End -->

                <div style="height: 50px;"></div>

                <div class="container-fluid">
                    <div class="row justify-content-center">
                        <!-- QR Code Section -->
                        <section class="col-md-6">
                            <article class="card">
                                <header class="card-header">
                                    <h5 class="card-title fw-semibold mb-4">Código QR</h5>
                                </header>
                                <div class="card-body text-center">
                                    <img src="<%= ViewState["QrCodeImage"] %>"
                                         class="card-img-top mb-3"
                                         alt="Generated QR Code"
                                         style="width: 200px; height: 200px; object-fit: contain;">
                                    <h6>¡Escaneame!</h6>
                                    <p class="card-text">
                                        Permite que tus tiendas favoritas escaneen este QR para generar visitas y así ganar diferentes premios.
                                    </p>
                                </div>
                            </article>
                        </section>

                        <!-- Card List Section -->
                        <section class="col-md-6">
                            <article class="card">
                                <header class="card-header">
                                    <h5 class="card-title fw-semibold mb-4">Lista de Tarjetas</h5>
                                </header>
                                <div class="card-body">
                                    <div class="list-group">
                                        <asp:Repeater ID="CardListRepeater" runat="server">
                                            <ItemTemplate>
                                                <a href="#" class="list-group-item list-group-item-action">
                                                    <div class="d-flex w-100 justify-content-between">
                                                        <h5 class="mb-1"><%# Eval("Title") %></h5>
                                                    </div>
                                                    <p class="mb-1"><%# Eval("Description") %></p>
                                                </a>
                                            </ItemTemplate>
                                        </asp:Repeater>
                                    </div>
                                </div>
                            </article>
                        </section>
                    </div>
                </div>

                <!-- Footer -->
                <footer class="mt-4">
                    <table width="100%">
                        <tr>
                            <td align="center">
                                <p class="mb-0 fs-4">Propiedad de Ingelab®</p>
                            </td>
                        </tr>
                    </table>
                </footer>
            </div>
        </div>
    </form>
</body>
</html>

