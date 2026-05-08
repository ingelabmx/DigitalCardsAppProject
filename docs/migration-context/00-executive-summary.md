# Executive Summary

## Que es el proyecto actual

`DigitalCardsApp` es una aplicacion ASP.NET Web Forms sobre .NET Framework 4.8. La aplicacion registra clientes, administra negocios y crea relaciones cliente-negocio-tarjeta de lealtad con sellos digitales. El almacenamiento principal es MySQL, accedido desde conectores estaticos mediante stored procedures.

El flujo de Wallet que existe hoy esta orientado a Google Wallet: al registrar un negocio se crea una clase de Google Wallet; al asociar un cliente con un negocio se crea un objeto de Wallet, se genera un link `Save to Google Wallet` firmado con JWT y se envia por correo. Cuando el negocio agrega un sello, el sistema incrementa contadores en base de datos y hace patch del objeto de Google Wallet.

## Mayor riesgo

El mayor riesgo es seguridad y operacion de credenciales. Se detectaron secretos hardcodeados en archivos de configuracion, codigo de correo, artefactos compilados y un JSON de service account para Google Wallet dentro del repo. Tambien el SQL dump contiene datos de prueba/produccion como correos, tokens y hashes. No se copiaron valores en esta documentacion. Se recomienda rotar credenciales y remover estos secretos del repositorio antes de iniciar cualquier PR de migracion.

El segundo riesgo fuerte es que Apple Wallet no esta implementado de punta a punta. Hay tabla `ApplePass`, un stored procedure incompleto y una plantilla HTML con placeholder de Apple, pero no hay generacion `.pkpass`, certificados, endpoints de registro/push ni flujo de seleccion iPhone/Android.

## Partes reutilizables

- Conocimiento de dominio: usuarios/clientes, negocios, tarjetas, sellos actuales e historicos.
- Esquema MySQL inicial y stored procedures como mapa de comportamiento.
- Flujo Google Wallet como prototipo funcional: crear clase, crear objeto, generar JWT, patch de sellos.
- Assets visuales existentes: logos de negocios, boton de Google Wallet, layout administrativo.
- Reglas visibles de negocio: una tarjeta por cliente-negocio, sello inicial al crear tarjeta, reinicio de sellos al pasar cierto umbral, historial acumulado.

## Partes que conviene reescribir

- Autenticacion y autorizacion, usando ASP.NET Core Identity o un modelo equivalente con hashes seguros.
- Acceso a datos, reemplazando conectores estaticos por servicios inyectables y repositorios/query services.
- Manejo de secretos y configuracion, usando opciones tipadas y secret store.
- Envio de correos, moviendolo a un servicio dedicado con plantillas versionadas.
- Wallets, especialmente Apple Wallet, porque falta la infraestructura de certificados, endpoints y actualizaciones.
- HTML generado manualmente con `StringBuilder`, por vistas fuertemente tipadas o API contracts.

## Arquitectura recomendada

La recomendacion inicial es un monolito modular ASP.NET Core, preferentemente `net10.0` si el hosting lo soporta. La separacion sugerida es:

- `Domain`: entidades y reglas puras.
- `Application`: casos de uso como registrar cliente, enrolar tarjeta, agregar sello, generar wallet link.
- `Infrastructure`: MySQL, Google Wallet, Apple Wallet, correo, storage, clock, secrets.
- `Web`: MVC/Razor Pages para UI administrativa y endpoints HTTP para Wallet.
- `Worker`: opcional para correos, reintentos de sincronizacion Wallet y tareas de mantenimiento.

Esta opcion reduce el riesgo de una migracion grande, mantiene todo desplegable como una sola aplicacion y deja listos limites internos para extraer servicios despues si el volumen lo justifica.

## Primer PR recomendado

Primer PR tecnico: crear un proyecto ASP.NET Core base en paralelo, sin reemplazar Web Forms todavia, con solucion modular, health check, configuracion de secretos, `README` de ejecucion, proyecto de pruebas y contratos iniciales del dominio. Ese PR debe compilar, no conectarse a produccion y no modificar el flujo Web Forms actual.

