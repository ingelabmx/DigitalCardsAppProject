# 31 Business Branding v1

Esta fase agrega branding basico de negocio sin modificar la tabla legacy
`Business` ni apagar Web Forms. El admin moderno administra marca publica desde
`/Admin/BusinessProfile/{businessId}` y la app usa esos datos en superficies de
cliente y Wallet.

## Cambios

- Nueva tabla `ModernBusinessBranding` en HostGator.
- Nuevo repositorio `IBusinessBrandingRepository` con implementaciones InMemory
  y MySQL.
- `/Admin/BusinessProfile/{businessId}` agrega seccion `Branding Wallet`.
- Correos Wallet usan el nombre publico.
- `/Wallet/Select/{token}`, dashboard cliente y tarjetas cliente muestran el
  nombre publico.
- Google Wallet usa el nombre del programa y el color primario cuando existe
  branding.
- Apple Wallet usa nombre publico/programa, descripcion, color primario y color
  secundario dentro de `pass.json`.
- Apple Wallet Web Service tambien aplica branding al servir passes actualizados.

## Operacion

Antes de editar branding real en HostGator, ejecutar:

```text
docs/migration-context/31-business-branding-v1-hostgator.sql
```

Luego:

1. Entrar a `/Admin/Login`.
2. Abrir `/Admin/Businesses`.
3. Entrar a `Administrar`.
4. Completar `Branding Wallet`.
5. Reenviar link Wallet desde `/Business/Cards`.
6. Confirmar que correo, landing Wallet y dashboard cliente usan el nombre
   publico.
7. Emitir Apple/Google Wallet y confirmar colores/nombre en dispositivo.

Si la tabla todavia no existe, las lecturas de branding caen a datos legacy.
La edicion de branding falla con error claro hasta aplicar el SQL.

## Seguridad

No se guardan certificados, passwords, JWTs, push tokens ni connection strings.
Los colores y textos son datos operativos no sensibles. El logo sigue siendo
ruta o URL manual; upload de archivos queda para otro PR.

## Pruebas

- Application: branding se guarda y se usa en correo, landing Wallet y dashboard
  cliente.
- Web: admin edita branding desde perfil de negocio.
- Playwright: admin crea/edita negocio, configura branding y valida que Wallet
  landing muestre el nombre publico.

## Rollback

- Dejar de editar branding y limpiar registros especificos de
  `ModernBusinessBranding` si hace falta.
- La app cae a `Business.BusinessName` y `Business.BusinessLogo`.
- Web Forms no depende de esta tabla.
