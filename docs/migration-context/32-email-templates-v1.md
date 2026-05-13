# 32 Email Templates v1

Esta fase centraliza las plantillas de correo en Infrastructure sin cambiar
providers ni activar nuevos flujos de negocio. SMTP sigue siendo real solo por
configuracion local, y Fake sigue siendo default para CI y Playwright.

## Cambios

- Nuevo contrato `IEmailTemplateRenderer`.
- Nueva implementacion `EmailTemplateRenderer`.
- Nuevo modelo `RenderedEmailTemplate` con subject, texto plano y HTML.
- Plantillas disponibles:
  - Wallet enrollment;
  - bienvenida;
  - reset de contrasena;
  - alerta interna.
- `SmtpEmailSender` ya no contiene HTML inline del correo Wallet; delega el
  render al template renderer.
- El correo Wallet conserva branding:
  - nombre publico;
  - logo URL/ruta publica cuando es URL `http/https`;
  - color primario seguro;
  - nombre del programa.

## Alcance

En este PR, solo el correo Wallet se envia desde flujos existentes. Las
plantillas de bienvenida, reset de contrasena y alertas internas quedan listas
para los PRs posteriores que implementen esos flujos.

No se agregan tablas nuevas. No se cambia Web Forms.

## Seguridad

- El HTML escapa nombres, textos y resumenes.
- Links y logos en HTML solo se usan como `http/https` absolutos.
- Colores se aceptan solo como `#RRGGBB`; si son invalidos se usa fallback.
- No se registran passwords, hashes, JWTs, push tokens, certificados ni
  connection strings.

## Operacion

El smoke SMTP existente sigue siendo valido:

```powershell
$env:RUN_SMTP_SMOKE='1'
dotnet test tests\DigitalCards.Application.Tests\DigitalCards.Application.Tests.csproj --filter SmtpSmoke
Remove-Item Env:\RUN_SMTP_SMOKE -ErrorAction SilentlyContinue
```

El smoke real de Wallet debe confirmar que el correo recibido conserva:

1. link `https://app.puntelio.com/Wallet/Select/...`;
2. nombre publico del negocio;
3. branding del programa si existe;
4. boton de Wallet funcional.

## Pruebas

- Application: renderer genera Wallet, bienvenida, reset y alerta interna.
- Application: renderer escapa HTML y rechaza URLs/colores inseguros en HTML.
- DI: Infrastructure registra `IEmailTemplateRenderer`.
- Regression: suite completa y Playwright con fakes.
