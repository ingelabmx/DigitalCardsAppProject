# Migration Target Architecture

## Opciones evaluadas

### ASP.NET Core MVC/Razor Pages

Ventajas:

- Encaja con el producto actual, que es page/form oriented.
- Facilita migrar dashboards, login, administracion de negocios y flujos de registro.
- Permite reutilizar parte del modelo mental Web Forms sin arrastrar `System.Web`.
- Puede convivir con endpoints API para Wallet.

Riesgos:

- Si se copia el code-behind tal cual, se trasladan los problemas de acoplamiento.
- Las vistas deben ser fuertemente tipadas y no generar HTML manual desde `StringBuilder`.

### ASP.NET Core Web API + frontend separado

Ventajas:

- Mejor separacion para frontend moderno.
- Bueno si se planea app movil o portal con React/Next.js.
- API limpia para integraciones.

Riesgos:

- Mayor costo inicial.
- Duplica decisiones de auth, despliegue, CORS, build frontend y hosting.
- No parece necesario para la primera fase de migracion.

### Monolito modular ASP.NET Core

Ventajas:

- Mejor balance para migrar sin romper el flujo actual.
- Un solo deploy inicial.
- Permite separar dominio, casos de uso e infraestructura.
- Deja listos limites para extraer servicios mas tarde.
- Compatible con MVC/Razor Pages y endpoints API en el mismo host.

Riesgos:

- Requiere disciplina de dependencias para que no se vuelva otro monolito acoplado.
- Hay que evitar que `Infrastructure` contamine `Domain`.

### Servicios separados

Ventajas:

- Aisla Wallet, correo y procesos async.
- Escala componentes de forma independiente.

Riesgos:

- Excesivo para la primera migracion.
- Aumenta complejidad operativa.
- Requiere colas, observabilidad, deploys multiples y contratos mas maduros.

## Recomendacion

Crear un monolito modular ASP.NET Core con MVC/Razor Pages para UI y endpoints API para Wallet. Target recomendado: `net10.0` si el hosting lo soporta. Si el hosting actual obliga a otra version, usar `net8.0` LTS como puente, pero no mezclar APIs nuevas si el target no las soporta.

## Estructura sugerida de solucion

```text
DigitalCardsApp.Modern.sln
src/
  DigitalCards.Domain/
  DigitalCards.Application/
  DigitalCards.Infrastructure/
  DigitalCards.Web/
  DigitalCards.Worker/        optional
tests/
  DigitalCards.Domain.Tests/
  DigitalCards.Application.Tests/
  DigitalCards.Infrastructure.Tests/
  DigitalCards.Web.Tests/
  DigitalCards.E2E.Tests/     Playwright
```

## Responsabilidades por capa

### Domain

- Entidades: `Client`, `Business`, `LoyaltyCard`, `Stamp`, `WalletPass`.
- Value objects: email, ids, stamp count, serial numbers.
- Reglas: una tarjeta por cliente-negocio, conteo de sellos, reset de ciclo, estados de Wallet.
- Sin referencias a ASP.NET Core, MySQL, Google, Apple o MailKit.

### Application

- Casos de uso:
  - registrar cliente;
  - autenticar usuario/negocio;
  - crear negocio;
  - enrolar cliente en negocio;
  - emitir tarjeta Wallet;
  - agregar sello;
  - enviar correo de enrolamiento;
  - consultar tarjetas del cliente.
- Interfaces:
  - `IClientRepository`;
  - `IBusinessRepository`;
  - `ILoyaltyCardRepository`;
  - `IGoogleWalletService`;
  - `IAppleWalletService`;
  - `IEmailSender`;
  - `IClock`;
  - `IIdGenerator`.

### Infrastructure

- MySQL con EF Core o Dapper.
- Adaptador Google Wallet.
- Adaptador Apple Wallet.
- MailKit o proveedor transaccional de correo.
- Storage de logos/assets.
- Secret store y options tipadas.
- Implementacion de reintentos/logging.

### Web/API

- Razor Pages o MVC para:
  - login;
  - registro;
  - dashboard de negocio;
  - admin de negocios;
  - vista de cliente.
- Controllers/Minimal APIs para:
  - endpoints Apple Wallet;
  - endpoints de seleccion Wallet desde correo;
  - health checks;
  - APIs internas de soporte.
- Auth/authorization con politicas.
- Antiforgery en forms.

### Worker

Opcional al inicio. Agregar cuando existan procesos async reales:

- reintentos de sync Wallet;
- envio de correos en background;
- mantenimiento de tokens expirados;
- notificaciones push Apple.

## Datos y migracion de base

Estrategia recomendada:

1. Mantener MySQL al inicio para reducir riesgo.
2. Crear modelo nuevo compatible con tablas existentes.
3. Introducir tablas nuevas de Wallet sin romper `ClientCard`.
4. Agregar migraciones o scripts versionados.
5. Crear repositorios que puedan leer datos legacy y escribir datos modernos en paralelo si hace falta.

Tablas nuevas recomendadas:

- `LoyaltyCards` o evolucion controlada de `ClientCard`.
- `WalletPasses`.
- `GoogleWalletPasses`.
- `AppleWalletPasses`.
- `AppleWalletDeviceRegistrations`.
- `StampLedger`.
- `EmailOutbox`.
- `BusinessAssets`.

## Seguridad destino

- Secret Manager para desarrollo.
- Secret store productivo, no archivos versionados.
- Rotacion inmediata de credenciales detectadas.
- ASP.NET Core Identity o autenticacion propia con password hashing moderno.
- Cookies seguras, HTTPS, HSTS y antiforgery.
- Politicas de autorizacion para admin, negocio y cliente.
- Data Protection keys persistidas si hay mas de una instancia.
- Logs sin secretos ni tokens.

## Wallets destino

Google:

- Servicio idempotente para crear clase/objeto y patch.
- Persistencia de ids y estado.
- Reintentos fuera del request cuando sea posible.

Apple:

- Generador `.pkpass`.
- Endpoints web service Apple.
- APNs.
- Registro de dispositivos y seriales actualizados.

## Observabilidad

- Health checks para base de datos, correo y dependencias Wallet.
- Logs estructurados por `CorrelationId`.
- Metricas para:
  - registros;
  - tarjetas emitidas;
  - errores Google;
  - errores Apple;
  - correos enviados/fallidos;
  - sellos agregados.

