-- Client Pilot Management v1
-- Ejecutar en HostGator / MySQL antes de usar /Admin/Clients con datos reales.
-- No modifica tablas legacy. UserClient sigue siendo la fuente de verdad.

create table if not exists ModernPilotClient (
    UserID int(11) not null,
    IsEnabled tinyint(1) not null default 0,
    Notes varchar(500) null,
    CreatedAt datetime(6) not null,
    UpdatedAt datetime(6) not null,
    UpdatedByAdminUserID int(11) not null,
    primary key (UserID),
    key IX_ModernPilotClient_IsEnabled (IsEnabled),
    key IX_ModernPilotClient_UpdatedAt (UpdatedAt),
    key IX_ModernPilotClient_UpdatedByAdminUserID (UpdatedByAdminUserID)
) engine=InnoDB default charset=utf8mb4 collate=utf8mb4_unicode_ci;
