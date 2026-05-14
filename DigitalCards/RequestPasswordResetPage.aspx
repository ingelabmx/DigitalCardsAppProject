<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="RequestPasswordResetPage.aspx.cs" Inherits="DigitalCardsApp.RequestPasswordResetPage" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <title>Cambio de contraseña</title>
    <link rel="shortcut icon" type="image/png" href="../assets/images/logos/DigitalCards-Icon-removebg.png" />
    <link rel="stylesheet" href="assets/css/styles.min.css" />
</head>

<body>
    <form id="form1" runat="server" method="post">
        <!-- Alerts -->
        <div class="alert alert-success" id="successAlert" role="alert" visible="false" runat="server">
            El correo de reinicio de contraseña ha sido enviado con éxito.
        </div>
        <div class="alert alert-danger" id="failAlert" role="alert" visible="false" runat="server">
            Fallo en el envío del correo.
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
                                        <img src="assets/images/logos/DigitalCards-Logo.jpg" alt="" width="145" height="80" fill="none" />
                                    </a>
                                    <p class="text-center">Tus tarjetas de recompensas</p>
                                    <div class="mb-4">
                                        <label for="tbContrasena" class="form-label">Inserte el correo de su cuenta:</label>
                                        <input type="text" class="form-control" id="tbCorreo" name="tbCorreo" runat="server" />
                                    </div>
                                    <a href="#" class="btn btn-primary w-100 py-8 fs-4 mb-4" id="btEnviar" runat="server" onserverclick="btEnviar_Click">Enviar</a>
                                    <div class="d-flex align-items-center justify-content-center">
                                        <p class="fs-4 mb-0 fw-bold">¿Ya posees una cuenta?</p>
                                        <a class="text-primary fw-bold ms-2" href="Login.aspx">Ingresar</a>
                                    </div>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        </div>
        <script src="../assets/libs/jquery/dist/jquery.min.js"></script>
        <script src="../assets/libs/bootstrap/dist/js/bootstrap.bundle.min.js"></script>
        <script src="https://cdn.jsdelivr.net/npm/iconify-icon@1.0.8/dist/iconify-icon.min.js"></script>
    </form>
</body>
</html>
