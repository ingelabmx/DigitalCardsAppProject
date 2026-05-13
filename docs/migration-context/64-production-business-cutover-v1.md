# 64 Production Business Cutover v1

## Objetivo

Convertir el piloto moderno en un flujo operativo repetible para mover un
negocio especifico a `ModernPrimary` sin apagar Web Forms globalmente.

Este PR no cambia tablas ni rutas criticas. Formaliza el procedimiento para que
cada negocio avance por estados controlados, con smoke real y rollback claro.

## Estados Operativos

- `LegacyOnly`: el negocio sigue operando principalmente en Web Forms.
- `PilotModern`: el negocio puede probar la app moderna con guardrails.
- `ModernPrimary`: la app moderna es el flujo principal para negocio y cliente.
- `LegacyRetired`: Web Forms debe quedar bloqueado manualmente para ese negocio.
- `Inactive`: el negocio no debe iniciar sesion en moderno.

## Precondiciones Por Negocio

Antes de mover un negocio a `ModernPrimary`:

1. El negocio esta habilitado desde `/Admin/Businesses`.
2. El branding esta completo:
   - nombre publico;
   - logo Wallet;
   - colores;
   - nombre/descripción del programa.
3. `https://app.puntelio.com/health` responde 200.
4. `https://app.puntelio.com/health/ready` responde 200.
5. El admin puede abrir `/Admin/Support` y `/Admin/Cutover`.
6. El negocio puede entrar a `/Business/Dashboard`.
7. El cliente controlado puede registrarse/asociarse por flujo moderno.
8. SMTP envia correo con links `https://app.puntelio.com`.
9. Google Wallet guarda o actualiza la tarjeta.
10. Apple Wallet instala el `.pkpass` y registra el dispositivo.
11. Un sello moderno actualiza Apple/Google.
12. `StampLedger` registra `ModernBusiness`.
13. Si Web Forms sigue aplicando sellos, `LegacyWalletSync` registra
    `LegacySync` y actualiza Wallets.

## Ejecucion De Cutover

1. Entrar como admin a `/Admin/Cutover`.
2. Revisar el negocio objetivo:
   - estado actual;
   - branding;
   - tarjetas recientes;
   - Wallets emitidas;
   - errores recientes;
   - estado de `LegacyWalletSync`.
3. Mantener el negocio en `PilotModern` mientras se ejecuta el smoke.
4. Ejecutar el smoke real:
   - login negocio;
   - registrar/asociar cliente;
   - recibir correo;
   - Apple Wallet en iPhone;
   - Google Wallet;
   - agregar sello moderno;
   - revisar `/Admin/Support`.
5. Si todo pasa, cambiar estado a `ModernPrimary`.
6. Operar el negocio desde moderno durante una ventana controlada.
7. Mantener Web Forms como fallback hasta completar soporte y monitoreo.

## Rollback Por Negocio

Si hay fallo:

1. Entrar a `/Admin/Cutover`.
2. Cambiar el negocio a `PilotModern` o `LegacyOnly`.
3. Si el fallo viene de sync legacy, apagar temporalmente:

```json
{
  "DigitalCards": {
    "LegacyWalletSync": {
      "Enabled": false
    }
  }
}
```

4. Operar nuevamente desde Web Forms.
5. Registrar el incidente en soporte interno usando los datos seguros de
   `/Admin/Support`.
6. No borrar tarjetas, tokens, passes ni registros Wallet durante rollback.

## Retiro Gradual De Web Forms

Un negocio solo debe pasar a `LegacyRetired` cuando:

- el negocio ya opera sellos desde `/Business/Cards`;
- el registro/asociacion cliente funciona desde moderno;
- correo, Apple Wallet, Google Wallet y updates estan validados;
- soporte admin puede diagnosticar tarjetas del negocio;
- existe una instruccion manual para bloquear el acceso Web Forms de ese negocio.

Mientras no exista bloqueo automatico en Web Forms, `LegacyRetired` es una señal
operativa visible en la app moderna, no una garantia tecnica global.

## Smoke Real Minimo

```powershell
Invoke-WebRequest https://app.puntelio.com/health -UseBasicParsing
Invoke-WebRequest https://app.puntelio.com/health/ready -UseBasicParsing
```

Despues validar manualmente:

- `/Admin/Login`;
- `/Admin/Cutover`;
- `/Business/Login`;
- `/Business/Dashboard`;
- `/Business/Cards`;
- correo real;
- Apple Wallet iPhone;
- Google Wallet;
- sello moderno;
- `/Admin/Support`;
- evento `ModernBusiness` en `StampLedger`.

## SQL

No requiere SQL nuevo.
