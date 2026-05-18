<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Login.aspx.cs" Inherits="DigitalCardsApp.Login" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <title>Iniciar sesión</title>
    <link rel="shortcut icon" type="image/png" href="assets/images/logos/DigitalCards-Icon-removebg.png" />
    <link rel="stylesheet" href="assets/css/styles.min.css" />
</head>
<body>
    <form id="form1" runat="server">
        <!-- Alerts -->
        <div class="alert alert-success" id="successAlert" role="alert" visible="false" runat="server">
            Inicio de sesión exitoso.
        </div>
        <div class="alert alert-danger" id="failAlert" role="alert" visible="false" runat="server">
            Error en el inicio de sesión.
        </div>
        <!--  Body Wrapper -->
        <div class="page-wrapper" id="main-wrapper" data-layout="vertical" data-navbarbg="skin6" data-sidebartype="full"
            data-sidebar-position="fixed" data-header-position="fixed">
            <div
                class="position-relative overflow-hidden radial-gradient min-vh-100 d-flex align-items-center justify-content-center">
                <div class="d-flex align-items-center justify-content-center w-100">
                    <div class="row justify-content-center w-100">
                        <div class="col-md-8 col-lg-6 col-xxl-3">
                            <div class="card mb-0">
                                <div class="card-body">
                                    <a href="./Login.aspx" class="text-nowrap logo-img text-center d-block py-3 w-100">
                                        <img src="assets/images/logos/DigitalCards-Logo.jpg" alt="" width="145" height="80" fill="none"/>
                                    </a>
                                    <p class="text-center">Tus tarjetas de recompensas</p>
                                    <div class="mb-3">
                                        <label for="tbUsuario" class="form-label">Usuario o Correo electrónico</label>
                                        <input type="text" class="form-control" id="tbUsuario" aria-describedby="UsuarioHelp" runat="server" />
                                    </div>
                                    <div class="mb-4">
                                        <label for="exampleInputPassword1" class="form-label">Contraseña</label>
                                        <input type="password" class="form-control" id="tbContrasena" runat="server" />
                                    </div>
                                    <div class="d-flex align-items-center justify-content-between mb-4">
                                        <a class="text-primary fw-bold" href="RequestPasswordResetPage.aspx">¿Olvidaste tu contraseña?</a>
                                    </div>
                                    <button href="#" class="btn btn-primary w-100 py-8 fs-4 mb-4" runat="server" onserverclick="Login_Click">Iniciar sesión</button>
                                    <div class="d-flex align-items-center justify-content-center">
                                        <p class="fs-4 mb-0 fw-bold">¿Nuevo en DigitalCards?
                                        <a class="text-primary fw-bold ms-2" href="Registry.aspx">  Crea una cuenta</a> </p>
                                    </div>
                                    <div></div>
                                    <div class="d-flex align-items-center justify-content-center">
                                        <p class="fs-4 mb-0 fw-bold">¿Tienes un negocio?
                                        <a class="text-primary fw-bold ms-2" href="BusinessLogin.aspx">  Ingresa con tu cuenta</a></p>
                                    </div>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        </div>
                            <div class="py-6 px-6 text-center">
                        <p class="mb-0 fs-4">
                            Propiedad de Ingelab® 
                        </p>
                    </div>
        <script src="assets/libs/jquery/dist/jquery.min.js"></script>
        <script src="assets/libs/bootstrap/dist/js/bootstrap.bundle.min.js"></script>
        <script src="https://cdn.jsdelivr.net/npm/iconify-icon@1.0.8/dist/iconify-icon.min.js"></script>
    </form>
</body>
</html>
