# Migration Roadmap

## Fase 0: analisis y seguridad

Objetivo: reducir riesgo antes de tocar el flujo productivo.

- Documentar arquitectura actual.
- Inventariar secretos y rotarlos.
- Remover service account JSON, SMTP credentials y connection strings del repo.
- Limpiar o separar SQL dump con datos sensibles.
- Crear `.gitignore` para `bin`, `obj`, secrets y artifacts.
- Decidir target framework ASP.NET Core.
- Definir ambiente local seguro con MySQL no productivo.
- Capturar flujos actuales con screenshots o notas antes de migrar.

Entregable: PR documental + seguridad inicial, sin cambios funcionales.

## Fase 1: levantar proyecto ASP.NET Core base

Objetivo: tener esqueleto moderno en paralelo.

- Crear solucion modular.
- Crear proyectos `Domain`, `Application`, `Infrastructure`, `Web`.
- Agregar health check basico.
- Agregar configuracion tipada y validacion.
- Agregar logging estructurado.
- Agregar proyecto de pruebas unitarias/integracion.
- Agregar README de ejecucion local.
- No conectar a produccion.

Entregable: app ASP.NET Core vacia que compila y corre.

## Fase 2: migrar autenticacion/usuarios/negocios

Objetivo: modelar identidades de forma segura.

- Definir si se usara ASP.NET Core Identity.
- Crear modelo de usuario, cliente y negocio.
- Migrar login de cliente/admin/negocio.
- Reemplazar SHA-256 simple por hashing seguro.
- Agregar autorizacion por politicas.
- Agregar pantallas o endpoints equivalentes.
- Preparar estrategia para migrar hashes legacy en login exitoso.

Entregable: login/registro moderno contra base de prueba.

## Fase 3: migrar tarjetas y sellos

Objetivo: mover el nucleo del negocio sin Wallet todavia.

- Crear entidad `LoyaltyCard`.
- Crear casos de uso: enrolar cliente, consultar tarjetas, agregar sello.
- Mantener regla de una tarjeta por cliente-negocio.
- Modelar reset de sellos e historico acumulado.
- Agregar `StampLedger` si se adopta auditoria por eventos.
- Crear UI de negocio para agregar sello.
- Crear tests unitarios de reglas.

Entregable: flujo de tarjeta/sellos funcional en ASP.NET Core con datos de prueba.

## Fase 4: Google Wallet

Objetivo: migrar Google Wallet con seguridad e idempotencia.

- Crear `IGoogleWalletService`.
- Mover credenciales a secret store.
- Persistir `GoogleClassId` y `GoogleObjectId`.
- Crear clase/objeto de forma idempotente.
- Generar link de guardar tarjeta.
- Patch de sellos con reintentos.
- Agregar logs y estado de sincronizacion.
- Agregar tests de payload sin llamar a Google.

Entregable: Google Wallet emitido y actualizado desde el nuevo sistema.

## Fase 5: Apple Wallet

Objetivo: implementar Apple Wallet completo.

- Disenar `AppleWalletPass` y `AppleWalletDeviceRegistration`.
- Configurar certificados fuera del repo.
- Generar `.pkpass`.
- Crear endpoint de descarga.
- Implementar web service Apple Wallet.
- Registrar/desregistrar dispositivos.
- Listar seriales actualizados.
- Servir pass actualizado.
- Integrar APNs.
- Probar en iPhone real.

Entregable: Apple Wallet instalable y actualizable.

## Fase 6: emails

Objetivo: separar email del code-behind y soportar seleccion de plataforma.

- Crear `IEmailSender`.
- Crear plantillas versionadas.
- Crear link de enrolamiento con token seguro.
- Crear landing que permita elegir Apple/Google.
- Agregar tracking de envio.
- Agregar retry o outbox si se requiere.
- Evitar incluir tokens sensibles en logs.

Entregable: correo moderno con seleccion de Wallet y trazabilidad.

## Fase 7: pruebas end-to-end con Playwright

Objetivo: proteger flujos criticos.

- Instalar/configurar Playwright para el nuevo proyecto.
- Crear fixtures de datos y ambiente aislado.
- Probar registro de cliente.
- Probar login de negocio.
- Probar alta de cliente en negocio.
- Probar link de correo simulado.
- Probar Apple/Google Wallet mediante mocks/stubs.
- Probar agregado de sello y tarjeta actualizada.
- Ejecutar responsive en iPhone/Android viewports.

Entregable: suite E2E inicial en CI.

## Fase 8: despliegue

Objetivo: pasar a produccion con rollout controlado.

- Definir hosting para ASP.NET Core.
- Configurar secrets productivos.
- Configurar migraciones/scripts de base.
- Configurar health checks y monitoreo.
- Publicar staging.
- Ejecutar smoke tests.
- Migrar trafico por ruta o subdominio.
- Mantener rollback.
- Retirar Web Forms cuando los flujos criticos esten cubiertos.

Entregable: despliegue controlado con monitoreo y rollback.

