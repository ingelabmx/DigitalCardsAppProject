# 103 UI Design Final Polish v1

Esta fase cierra una pasada visual transversal sin cambiar reglas de negocio,
persistencia ni integraciones Wallet. El objetivo es que Puntelio se sienta mas
consistente como producto final mientras conserva el shell tipo Web Forms:
sidebar, header superior, cards, tablas y footer de IngeLabs.

## Cambios

- Refuerza tokens visuales compartidos en `site.css`: sombras, estados hover,
  formularios, botones, metricas, cards y paneles.
- Mejora zonas de peligro para borrados permanentes de negocio, cliente y
  tarjeta con borde rojo, fondo suave y controles agrupados.
- Ajusta responsive para que botones, confirmaciones y formularios se apilen
  correctamente en mobile.
- Mantiene el lenguaje unificado de tarjeta/Wallet ya aplicado en paginas
  autenticadas.
- Agrega prueba smoke para evitar que se pierdan los tokens visuales, las zonas
  de peligro y el contenedor QR del cliente.

## Sin SQL Nuevo

No hay cambios de esquema ni pasos manuales en HostGator.

## Validacion

- `git diff --check`
- `dotnet test DigitalCardsApp.Modern.sln`
- `RUN_PLAYWRIGHT=1 dotnet test tests\DigitalCards.E2E.Tests\DigitalCards.E2E.Tests.csproj`

## Notas

- Este PR no cambia endpoints Wallet ni autenticacion.
- Los borrados siguen usando las confirmaciones y handlers existentes.
- La paridad visual busca coherencia funcional con Web Forms, no copia
  pixel-perfect del frontend legacy.
