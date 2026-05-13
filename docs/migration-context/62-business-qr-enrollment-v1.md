# 62 Business QR Enrollment v1

Esta fase permite que el negocio genere un link publico y QR desde su dashboard
moderno.

## Cambios

- `/Business/Dashboard` incluye panel de registro publico.
- El negocio puede generar link y QR para `/Enroll/{businessToken}`.
- El QR se renderiza server-side como SVG con `QRCoder`.
- Generar un nuevo QR revoca tokens activos anteriores del negocio.
- El token plano se muestra solo en la respuesta actual y no se guarda en base
  de datos.

## Operacion

1. El admin debe haber habilitado el negocio para moderno.
2. El negocio entra a `/Business/Dashboard`.
3. Presiona `Generar link y QR`.
4. Muestra o imprime el QR para clientes.
5. El cliente abre el link, se registra y recibe correo Wallet.

No hay SQL nuevo si ya se aplico `61-public-business-enrollment-v1-hostgator.sql`.
