# 19 Business Operations v1

## Objetivo

Esta fase empieza a reemplazar la operacion diaria de negocio de Web Forms sin apagar Web Forms todavia. El flujo moderno deja de depender de capturar un usuario libre para agregar sellos y pasa a operar desde una tarjeta cliente-negocio encontrada y validada.

## Alcance Del PR

- Nueva pagina protegida `/Business/Cards`.
- Busqueda de tarjetas por `UserName`, correo, nombre o apellido del cliente.
- Detalle de tarjeta con:
  - cliente;
  - negocio;
  - sellos actuales;
  - sellos historicos;
  - estado Google Wallet;
  - estado Apple Wallet tracking;
  - dispositivos Apple registrados;
  - ultimo sello/update.
- Reenvio de correo Wallet desde la tarjeta seleccionada.
- Agregado de sello desde la tarjeta seleccionada.
- Validacion de propiedad: el negocio autenticado no puede ver ni modificar tarjetas de otro negocio.
- `Pilot` sigue bloqueando negocios y clientes fuera del allowlist.
- No se agregan tablas nuevas.
- No se retira Web Forms.

## Flujo Operativo

1. El negocio inicia sesion en `/Business/Login`.
2. Entra a `/Business/Dashboard`.
3. Abre `Tarjetas y sellos`.
4. Busca cliente por username o email.
5. Selecciona la tarjeta.
6. Puede reenviar el link Wallet.
7. Puede agregar sello desde el detalle.
8. El sello actualiza `ClientCard`, Google Wallet y Apple Wallet/APNs usando los servicios existentes.

## Seguridad

- `BusinessId` sale de la cookie de negocio, no de query string ni hidden input.
- Los handlers de `/Business/Cards` validan que `cardId` pertenezca al negocio autenticado.
- Las acciones modernas quedan bloqueadas si `DigitalCards:Pilot:Enabled=true` y el negocio/cliente no estan allowlisted.
- No se muestran passwords, tokens Wallet, JWTs, push tokens, certificados ni connection strings.

## Compatibilidad

`/Business/Stamp` sigue existiendo como ruta legacy moderna interna, pero el dashboard principal ahora dirige la operacion de sellos a `/Business/Cards`. Web Forms sigue siendo fallback operativo.

## Configuracion

No requiere nuevas claves. Usa la configuracion existente:

```json
{
  "DigitalCards": {
    "PublicBaseUrl": "https://app.puntelio.com",
    "PersistenceProvider": "MySql",
    "Pilot": {
      "Enabled": true
    }
  }
}
```

## Pruebas

Automatizadas:

```powershell
dotnet test DigitalCardsApp.Modern.sln
$env:RUN_PLAYWRIGHT='1'
dotnet test tests\DigitalCards.E2E.Tests\DigitalCards.E2E.Tests.csproj
Remove-Item Env:\RUN_PLAYWRIGHT -ErrorAction SilentlyContinue
```

Cobertura nueva:

- busqueda respeta el negocio autenticado;
- un negocio no puede ver tarjeta de otro negocio;
- un negocio no puede agregar sello a tarjeta de otro negocio;
- reenvio usa `DigitalCards:PublicBaseUrl`;
- Playwright cubre login, busqueda, detalle, reenvio, Wallet landing y sello desde detalle.

## Smoke Real Recomendado

Con `app.puntelio.com` activo:

1. Login de negocio allowlisted.
2. Registrar/asociar cliente controlado.
3. Abrir `/Business/Cards`.
4. Buscar el cliente.
5. Reenviar correo Wallet.
6. Abrir link del correo.
7. Instalar Apple Wallet en iPhone.
8. Guardar Google Wallet.
9. Agregar sello desde `/Business/Cards`.
10. Confirmar update en Apple y Google.

Si falla el flujo moderno, apagar `Pilot` y operar desde Web Forms mientras se revisan logs.
