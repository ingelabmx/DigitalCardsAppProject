-- Branding Custom Field Color v1
-- Ejecutar manualmente en HostGator despues del merge de PR 113.
-- No modifica datos legacy. Agrega un tercer color visible para textos/campos personalizados.

alter table ModernBusinessBranding
    add column CustomFieldColor varchar(7) not null default '#FFFFFF'
    after SecondaryColor;
