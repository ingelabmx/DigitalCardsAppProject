-- Admin Pilot Management v1
-- Ejecutar manualmente en HostGator antes de administrar pilotos desde /Admin.
-- No modifica tablas legacy ni cambia negocios existentes.

create table if not exists ModernPilotBusiness (
    BusinessID int(11) not null,
    IsEnabled tinyint(1) not null default 0,
    Notes varchar(500) null,
    CreatedAt datetime(6) not null,
    UpdatedAt datetime(6) not null,
    UpdatedByAdminUserID int(11) not null,
    primary key (BusinessID),
    key IX_ModernPilotBusiness_IsEnabled (IsEnabled),
    key IX_ModernPilotBusiness_UpdatedAt (UpdatedAt)
) engine=InnoDB default charset=utf8mb4 collate=utf8mb4_unicode_ci;
