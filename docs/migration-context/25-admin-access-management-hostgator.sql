-- Admin Access Management v1
-- Ejecutar manualmente en HostGator antes de administrar credenciales admin modernas.
-- No modifica tablas legacy ni crea endpoints publicos.

create table if not exists ModernAdminCredential (
    UserID int(11) not null,
    PasswordHash varchar(512) not null,
    CreatedAt datetime(6) not null,
    UpdatedAt datetime(6) not null,
    primary key (UserID),
    constraint ModernAdminCredential_ibfk_1
        foreign key (UserID) references UserClient (UserID)
) engine=InnoDB default charset=utf8mb4 collate=utf8mb4_unicode_ci;
