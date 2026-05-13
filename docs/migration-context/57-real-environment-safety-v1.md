# 57 Real Environment Safety v1

Esta fase cierra rutas auxiliares antes de operar mas negocios desde
`app.puntelio.com`.

## Cambios

- `/Dev/Outbox` queda disponible por default solo en `Development`.
- Fuera de `Development`, `/Dev/Outbox` requiere:
  - `DigitalCards:Diagnostics:EnableDevOutbox=true`;
  - cookie admin valida.
- Los links visuales hacia Outbox se ocultan cuando la ruta no esta habilitada.
- `/internal/wallet-diagnostics/{cardId}` conserva
  `DigitalCards:Diagnostics:EnableWalletDiagnostics`, pero ahora tambien exige
  policy admin antes de devolver diagnostico.

## Configuracion

Mantener en real:

```json
{
  "DigitalCards": {
    "Diagnostics": {
      "EnableDevOutbox": false,
      "EnableWalletDiagnostics": false
    }
  }
}
```

Activar temporalmente `EnableDevOutbox=true` solo para soporte controlado y
desactivarlo al terminar.

## Validacion

- `/Dev/Outbox` devuelve `404` fuera de Development si el flag esta apagado.
- Con el flag encendido, `/Dev/Outbox` redirige a `/Admin/Login` sin cookie
  admin.
- Un admin autenticado puede abrir `/Dev/Outbox` cuando el flag esta encendido.
- Wallet diagnostics no queda publico aunque el flag de diagnostico este activo.

No hay SQL nuevo.
