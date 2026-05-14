# 70 Business Onboarding Email v1

## Objetivo

Permitir que un admin invite a un negocio a configurar su acceso moderno sin comunicar passwords planos. El flujo usa el mecanismo existente de reset de contrasena de negocio, por lo que no agrega tablas ni tokens nuevos.

## Alcance

- `/Admin/CreateBusiness` puede crear el negocio y enviar invitacion en el mismo submit.
- `/Admin/BusinessProfile/{businessId}` agrega una accion para reenviar invitacion al correo actual del negocio.
- El correo enviado apunta a `/Business/ResetPassword/{token}` usando `DigitalCards:PublicBaseUrl`.
- El token se guarda solo como hash en `ModernPasswordResetToken`.
- Fake email sigue funcionando en desarrollo, CI y Playwright.

## Seguridad

- No se muestra el password inicial despues del submit.
- No se manda password plano por correo.
- No se renderiza el link de reset en la pantalla admin.
- No se registran passwords, hashes ni tokens en logs.
- Solo admins autenticados pueden crear negocios o reenviar invitaciones.

## Operacion

Para HostGator no hay SQL nuevo en este PR. Antes de usar invitaciones reales deben estar aplicados los SQL previos:

- `16-business-password-hardening-hostgator.sql`
- `22-admin-pilot-management-hostgator.sql`
- `33-password-reset-flows-v1-hostgator.sql`

Smoke real recomendado:

1. Entrar a `/Admin/Login`.
2. Crear negocio desde `/Admin/CreateBusiness` con `Enviar invitacion por correo`.
3. Confirmar recepcion del correo.
4. Abrir link de reset y configurar password.
5. Iniciar sesion en `/Business/Login`.
6. Si ya existe negocio, reenviar invitacion desde `/Admin/BusinessProfile/{businessId}`.

## Rollback

Si SMTP falla, el admin puede seguir usando reset manual desde `/Admin/BusinessProfile/{businessId}` o comunicar credenciales temporalmente por fuera. El flujo de login de negocio no cambia.
