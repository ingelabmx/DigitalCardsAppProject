# Playwright Test Plan

## Contexto

`npx` esta disponible en el entorno local, por lo que Playwright puede ejecutarse por CLI o como suite `@playwright/test` cuando se cree el proyecto moderno. Para esta fase no se generaron tests, solo el plan.

La recomendacion es empezar con tests del proyecto ASP.NET Core nuevo, no del Web Forms actual, salvo que se necesite capturar comportamiento legacy antes de migrar.

## Principios

- Usar base de datos aislada por test run.
- No llamar APIs reales de Google Wallet, Apple Wallet ni SMTP en E2E.
- Mockear/stubbear servicios externos y verificar payload/estado interno.
- Usar datos deterministas.
- Probar los flujos criticos como usuario final, no solo endpoints.
- Guardar screenshots/traces solo en fallos o debugging.

## Flujos E2E prioritarios

### Registro de cliente

Caso feliz:

1. Abrir pagina de registro.
2. Llenar username, nombre, apellido, email y contrasena.
3. Enviar.
4. Ver confirmacion.
5. Verificar en base de prueba que el cliente existe.

Errores:

- Email invalido.
- Passwords no coinciden.
- Username duplicado.
- Email duplicado.
- Campos requeridos vacios.

### Login de negocio

Caso feliz:

1. Abrir login de negocio.
2. Ingresar credenciales validas.
3. Ver dashboard.
4. Confirmar que se muestra nombre/logo de negocio.

Errores:

- Credenciales invalidas.
- Intentos fallidos.
- Acceso a dashboard sin sesion redirige a no autorizado/login.

### Asociar cliente con negocio

Caso feliz:

1. Login como negocio.
2. Abrir pantalla de alta/asociacion de cliente.
3. Buscar/ingresar username del cliente.
4. Registrar cliente en negocio.
5. Ver confirmacion.
6. Verificar que se creo tarjeta cliente-negocio.
7. Verificar que el servicio Wallet stub recibio solicitud de emision.

Errores:

- Cliente inexistente.
- Cliente ya registrado en ese negocio.
- Servicio Wallet falla y el sistema deja estado recuperable.

### Recepcion o simulacion del link de correo

Caso feliz:

1. Completar asociacion cliente-negocio.
2. Capturar email desde fake SMTP/outbox.
3. Abrir link de enrolamiento.
4. Ver landing de seleccion Wallet.
5. Confirmar que el token de enrolamiento es valido y de un solo alcance.

Errores:

- Token expirado.
- Token inexistente.
- Token ya usado si se decide hacerlo one-time.

### Seleccion de Google Wallet

Caso feliz:

1. Abrir landing desde email.
2. Click en Google Wallet.
3. Ver redireccion o link generado hacia Google Save URL en modo stub.
4. Verificar que se persistio `GoogleWalletPass` con object id y estado.

Errores:

- Servicio Google no disponible.
- Tarjeta ya emitida: el flujo debe ser idempotente.
- Negocio sin clase Google configurada.

### Seleccion de Apple Wallet

Caso feliz:

1. Abrir landing desde email en viewport iPhone.
2. Click en Apple Wallet.
3. Descargar respuesta con content type de pass.
4. Verificar que se creo `AppleWalletPass` con serial number.
5. Verificar que el paquete se genero con firma en entorno de prueba o stub.

Errores:

- Certificado no configurado.
- Token invalido.
- Pass serial inexistente.

### Agregado de sello

Caso feliz:

1. Login como negocio.
2. Abrir pantalla de sellos.
3. Ingresar cliente.
4. Agregar sello.
5. Ver confirmacion.
6. Verificar incremento de `CheckQTY` e `HistoricCheckQTY`.
7. Verificar que Google/Apple sync se encolo o ejecuto segun configuracion.

Errores:

- Cliente no pertenece al negocio.
- Tarjeta no existe.
- Wallet sync falla despues de guardar el sello.

### Visualizacion de tarjeta actualizada

Caso feliz:

1. Login como cliente.
2. Abrir vista de tarjetas.
3. Ver negocio, fecha de creacion y sellos.
4. Agregar sello desde sesion de negocio.
5. Refrescar vista cliente.
6. Ver sellos actualizados.

Errores:

- Cliente sin tarjetas.
- Sesion expirada.

## Pruebas responsive

Viewports sugeridos:

- iPhone SE.
- iPhone 14/15.
- Pixel 7/8.
- Desktop 1366x768.

Casos:

- Registro de cliente en movil.
- Landing de seleccion Wallet en iPhone muestra Apple como opcion natural.
- Landing de seleccion Wallet en Android muestra Google como opcion natural.
- Login de negocio y dashboard no rompen layout en movil.
- Botones Wallet no se superponen y son tocables.

## Primeros tests a crear

Orden recomendado:

1. Registro de cliente.
2. Login de negocio.
3. Asociar cliente con negocio usando Wallet services fake.
4. Capturar correo desde outbox fake y abrir link.
5. Seleccion Google Wallet con stub.
6. Agregar sello y verificar actualizacion de tarjeta.
7. Responsive de landing Wallet en iPhone/Android.

Apple debe entrar despues de tener generador `.pkpass` y endpoints base, porque hoy no existe implementacion funcional que probar.

