# 27 Client Dashboard v1

Esta fase crea el primer dashboard moderno de cliente sin apagar Web Forms. El
cliente entra con credenciales legacy de `UserClient.RoleID=2` y una cookie
separada `DigitalCards.Client`.

## Cambios

- Nuevo login cliente en `/Client/Login`.
- Nueva cookie/policy:
  - scheme `DigitalCards.Client`;
  - policy `ClientOnly`;
  - claims `ClientId`, `ClientEmail`, `ClientUserName`, `ClientName`,
    `Role=Client`.
- Nuevo `/Client/Dashboard`.
- `/Client/Cards` queda protegido por cookie y deja de aceptar `UserName` como
  fuente de verdad.
- Las tarjetas del cliente se cargan por `ClientId` autenticado.
- Los links Wallet del dashboard usan token opaco nuevo, no `CardID` directo.
- El registro moderno de cliente ahora pide password y guarda hash legacy
  compatible con `UserClient.UserPassword varchar(25)`.

No se introduce ASP.NET Core Identity ni tabla nueva de password moderno para
clientes en este PR.

## Flujo Real Corregido

- Admin habilita el negocio piloto desde `/Admin/Businesses`.
- Negocio inicia sesion.
- Negocio asocia/habilita al cliente usando `/Business/Enroll`.
- Negocio opera la tarjeta desde `/Business/Cards`.
- Cliente entra a `/Client/Login` y consulta sus tarjetas desde
  `/Client/Dashboard`.

`/Admin/Clients` queda como control operativo/piloto temporal. El flujo normal
de negocio no requiere que admin habilite cada cliente.

## Seguridad

- Cliente solo puede ver tarjetas asociadas a su propio `ClientId`.
- Cookies de admin o negocio no autorizan `/Client/Dashboard` ni
  `/Client/Cards`.
- Login cliente no acepta `Business` ni admins porque el repositorio filtra
  `UserClient.RoleID=2`.
- No se muestran passwords, hashes, tokens internos ni connection strings.

## Pruebas

- Application: login cliente valido, invalido y credenciales de negocio
  rechazadas.
- Web: paginas cliente protegidas, cookie cliente, logout y tarjetas propias.
- Playwright: registro cliente con password, negocio asocia cliente, Wallet fake,
  sello y dashboard cliente autenticado.
