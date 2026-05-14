<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="AdminModPage.aspx.cs" Inherits="DigitalCardsApp.AdminModPage" %>

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
                    <!-- Alerts -->
                    <div class="alert alert-success" id="successAlert" role="alert" visible="false" runat="server">
                        La Actualización ha sido exitosa.
                    </div>
                    <div class="alert alert-danger" id="failAlert" role="alert" visible="false" runat="server">
                        Fallo en la actualización.
                    </div>
                    <!-- Card Contents -->
                    <div class="card">
                        <div class="card-body">
                            <!--<h5 class="card-title fw-semibold mb-4">Agregar un negocio</h5>-->
                            <div class="card">
                                <div class="card-body">
                                    <div class="d-flex justify-content-center gap-2">
                                        <h5 class="card-title">Agregar un negocio</h5>
                                    </div>
                                    <div class="mb-3">
                                        <label for="tbNombreNegocio" class="form-label">Nombre del negocio</label>
                                        <input type="Text" class="form-control" id="tbNombreNegocio" aria-describedby="lbNegocioHelp" runat="server">
                                        <div id="lbNegocioHelp" class="form-text">Cambie el nombre del negocio.</div>
                                    </div>
                                    <div class="mb-3">
                                        <label for="tbContraNegocio" class="form-label">Contraseña del negocio</label>
                                        <input type="Password" class="form-control" id="tbContraNegocio" aria-describedby="lbContraNegocioHelp" runat="server">
                                        <div id="lbContraNegocioHelp" class="form-text">Cambie la contraseña para la cuenta del negocio.</div>
                                    </div>
                                    <div class="mb-3">
                                        <label for="tbContraNegocio2" class="form-label">Repetir contraseña del negocio</label>
                                        <input type="Password" class="form-control" id="tbContraNegocio2" aria-describedby="lbContraNegocioHelp2" runat="server">
                                        <div id="lbContraNegocioHelp2" class="form-text">Repita la contraseña para la cuenta del negocio.</div>
                                    </div>
                                    <div class="mb-3">
                                        <label for="tbEmailNegocio" class="form-label">Email del negocio</label>
                                        <input type="text" class="form-control" id="tbEmailNegocio" aria-describedby="lbEmailNegocioHelp" runat="server">
                                        <div id="lbEmailNegocioHelp" class="form-text">Cambie el correo electrónico del negocio.</div>
                                    </div>
                                    <div
                                        id="dropZone"
                                        class="border border-primary rounded p-3 text-center"
                                        ondragover="event.preventDefault();"
                                        ondrop="handleFileDrop(event)">
                                        Arrastra aquí el logo.
   
     <input type="file" accept="image/*" class="form-control d-none" id="tbLogoNegocio2" runat="server" onchange="previewImage(this)">
                                    </div>
                                    <div id="previewImageContainer" class="mt-3">
                                        <!-- Preview will be shown here -->
                                    </div>
                                    <div class="d-flex justify-content-center gap-2">
                                        <button type="submit" class="btn btn-primary" id="btActualizar" onserverclick="btActualizar_ServerClick" runat="server">Actualizar</button>
                                        <button type="button" class="btn btn-danger" data-bs-toggle="modal" data-bs-target="#confirmDeleteModal">
                                            Borrar                  
                                        </button>
                                    </div>
                                </div>
                            </div>
                        </div>
                    </div>
                    <!-- Modals -->
                    <div class="modal fade" id="confirmDeleteModal" tabindex="-1" aria-labelledby="confirmDeleteModalLabel" aria-hidden="true">
                        <div class="modal-dialog">
                            <div class="modal-content">
                                <div class="modal-header">
                                    <h5 class="modal-title" id="confirmDeleteModalLabel">Confirmar eliminación</h5>
                                    <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close"></button>
                                </div>
                                <div class="modal-body">
                                    ¿Está seguro de que desea eliminar este negocio? Esta acción es irreversible.
               
                                </div>
                                <div class="modal-footer">
                                    <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Cancelar</button>
                                    <!-- Server-side deletion on confirmation -->
                                    <button type="submit" class="btn btn-danger" id="btBorrarConfirm" onserverclick="btBorrar_ServerClick" runat="server">Confirmar</button>
                                </div>
                            </div>
                        </div>
                    </div>
                    <!-- Footer -->
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
        <script>
            function handleFileDrop(event) {
                event.preventDefault();
                const fileInput = document.getElementById("tbLogoNegocio2");
                const files = event.dataTransfer.files;
                if (files.length > 0) {
                    fileInput.files = files; // Assign dropped file to input element
                    previewImage(fileInput); // Call preview
                }
            }

            function previewImage(input) {
                const previewContainer = document.getElementById("previewImageContainer");
                previewContainer.innerHTML = ""; // Clear existing previews
                const file = input.files[0];
                if (file) {
                    const reader = new FileReader();
                    reader.onload = function (e) {
                        const img = document.createElement("img");
                        img.src = e.target.result;
                        img.alt = "Preview";
                        img.className = "img-thumbnail";
                        img.style.maxWidth = "150px";
                        previewContainer.appendChild(img);
                    };
                    reader.readAsDataURL(file);
                }
            }
        </script>
    </form>
</body>
</html>
