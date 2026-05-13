-- Business Self-Service v1
-- Ejecutar manualmente en HostGator antes de permitir /Business/Branding real.
-- Permite que el negocio actualice branding sin inventar un admin como actor.
-- Web Forms no usa esta tabla.

alter table ModernBusinessBranding
    modify UpdatedByAdminUserID int(11) null;
