# 89 Web Forms Parity Final Check v1

## Objetivo

Cerrar una fotografia operativa de paridad entre Web Forms y la app moderna. Este documento no retira Web Forms; define que flujos ya pueden operar en moderno, cuales quedan en piloto controlado y cuales necesitan una decision final antes de apagar rutas legacy por negocio.

## Leyenda

- **Moderno listo:** existe en ASP.NET Core, tiene pruebas y puede usarse en operacion controlada.
- **Piloto moderno:** funciona, pero debe activarse por negocio y observarse antes de ampliar.
- **Parcial:** existe base tecnica, pero falta UI, monitoreo o decision operativa.
- **Legacy fallback:** Web Forms sigue siendo el camino de respaldo.

## Admin

| Flujo | Estado moderno | Ruta moderna | Nota de cutover |
| --- | --- | --- | --- |
| Login admin | Moderno listo | `/Admin/Login` | Usa `UserClient.RoleID=1` y cookie separada. |
| Crear/resetear admins | Moderno listo | `/Admin/AdminUsers`, `/Admin/CreateAdmin` | Sin bootstrap publico. |
| Crear negocio | Moderno listo | `/Admin/CreateBusiness` | Inserta en `Business` legacy y credencial moderna. |
| Editar negocio/reset password | Moderno listo | `/Admin/BusinessProfile/{businessId}` | Mantiene compatibilidad con Web Forms. |
| Activar/desactivar negocio | Moderno listo | `/Admin/Businesses`, `/Admin/Cutover` | Estados modernos por negocio. |
| Branding/logo negocio | Moderno listo | `/Admin/BusinessProfile/{businessId}` | Logo publico y branding Wallet. |
| Soporte diagnostico | Moderno listo | `/Admin/Support` | Export seguro sin secretos. |
| Auditoria operativa | Moderno listo | `/Admin/Audit` | Eventos modernos sensibles. |
| Reportes admin | Piloto moderno | `/Admin/Reports` | Reporte minimo, no reemplaza todos los reportes historicos. |

## Negocio

| Flujo | Estado moderno | Ruta moderna | Nota de cutover |
| --- | --- | --- | --- |
| Login/logout negocio | Moderno listo | `/Business/Login` | Cookie separada y pilot gate por negocio. |
| Dashboard operativo | Piloto moderno | `/Business/Dashboard` | Resumen y acciones rapidas. |
| Buscar tarjeta/cliente | Piloto moderno | `/Business/Cards` | Respeta `BusinessId` autenticado. |
| Asociar cliente | Piloto moderno | `/Business/Enroll` | El negocio habilitado asocia clientes; ya no depende de allowlist de cliente. |
| Reenviar link Wallet | Moderno listo | `/Business/Cards` | Usa token opaco. |
| Agregar sello | Piloto moderno | `/Business/Cards`, `/Business/CheckIn`, `/Business/Stamp` | Actualiza Google/Apple y registra `StampLedger`. |
| Modo mostrador | Piloto moderno | `/Business/CheckIn` | QR/username/email para busqueda rapida. |
| Reportes negocio | Piloto moderno | `/Business/Reports` | Minimo viable por negocio autenticado. |
| Branding self-service | Piloto moderno | `/Business/Branding` | Admin conserva permisos criticos. |
| Operacion legacy | Legacy fallback | Web Forms | Sigue disponible hasta marcar `LegacyRetired`. |

## Cliente

| Flujo | Estado moderno | Ruta moderna | Nota de cutover |
| --- | --- | --- | --- |
| Registro cliente | Piloto moderno | `/Register`, `/Enroll/{businessToken}` | Incluye consentimiento y token publico por negocio. |
| Login/logout cliente | Moderno listo | `/Client/Login` | Cookie separada. |
| Dashboard cliente | Moderno listo | `/Client/Dashboard` | QR real, resumen y acceso Wallet. |
| Mis tarjetas | Moderno listo | `/Client/Cards` | Solo tarjetas propias. |
| Perfil cliente | Moderno listo | `/Client/Profile` | Edita datos basicos; username queda fijo. |
| Cambio/reset password | Moderno listo | `/Client/ChangePassword`, reset por correo | Usa credencial moderna y compat legacy. |
| Historial detallado de actividad | Parcial | `/Client/Cards` | Muestra estado actual; historial fino depende de reportes/auditoria. |

## Wallet Y Correo

| Flujo | Estado moderno | Ruta moderna | Nota de cutover |
| --- | --- | --- | --- |
| Landing Wallet | Moderno listo | `/Wallet/Select/{token}` | Publica por token opaco, con branding. |
| Apple Wallet install | Moderno listo | `/Wallet/Apple/{token}` | `.pkpass`, Web Service y APNs reales por config. |
| Google Wallet save | Moderno listo | `/Wallet/Google/{token}` | Issue/patch real por config. |
| Email Wallet | Moderno listo | SMTP real o fake por config | Link opaco, no JWT directo. |
| Updates por sello moderno | Moderno listo | Servicios Application | Google patch + Apple APNs. |
| Updates por sello legacy | Piloto moderno | `LegacyWalletSync` | Requiere worker activo y observabilidad. |
| Reintento soporte Wallet | Moderno listo | `/Admin/Support` | Registra intento seguro. |

## Operacion

| Flujo | Estado moderno | Ruta moderna | Nota de cutover |
| --- | --- | --- | --- |
| Health/readiness | Moderno listo | `/health`, `/health/ready` | No imprime secretos. |
| Cloudflare/app.puntelio.com | Moderno listo | Runbook ops | Dominio canonico para Wallets. |
| Servicio Windows/tunnel | Piloto moderno | `ops/windows` | Scripts listos; hosting final sigue por decidir. |
| Cutover por negocio | Piloto moderno | `/Admin/Cutover` | Estados y evidencia de smoke. |
| Web Forms guards | Parcial | Proyecto `DigitalCards` | Lee estado moderno para advertir/bloquear segun cutover. |
| Rollback | Piloto moderno | Config/admin | Volver a Web Forms por negocio sigue documentado. |

## Criterio Para Mover Un Negocio A `ModernPrimary`

1. Negocio habilitado y con branding revisado.
2. Cliente controlado registrado/asociado desde moderno.
3. Correo real recibido con link de `app.puntelio.com`.
4. Apple Wallet instalado en iPhone.
5. Google Wallet guardado.
6. Sello agregado desde moderno y Wallets actualizadas.
7. `StampLedger`, `/Admin/Support` y `/Admin/Cutover` muestran estado correcto.
8. Rollback documentado: volver a Web Forms o bajar estado del negocio.

## Pendientes Antes De Retirar Web Forms Globalmente

- Decidir hosting final y monitoreo externo.
- Ejecutar cutover por negocio, no global.
- Validar Web Forms guard en negocio real marcado `ModernPrimary`.
- Completar evidencia de smoke por negocio.
- Confirmar que soporte puede diagnosticar correo, Wallets, sellos y sync legacy sin SQL manual.

## Validacion

Este PR es documental y no requiere SQL.

```powershell
git diff --check
dotnet test DigitalCardsApp.Modern.sln
$env:RUN_PLAYWRIGHT='1'
dotnet test tests\DigitalCards.E2E.Tests\DigitalCards.E2E.Tests.csproj
Remove-Item Env:\RUN_PLAYWRIGHT -ErrorAction SilentlyContinue
```
